using System;
using System.Collections.Generic;
using Serilog;

namespace NadekoBot.Voice
{
    /// <summary>
    /// Manages the DAVE E2EE protocol state machine for a voice connection.
    /// </summary>
    public sealed class DaveSessionManager : IDisposable
    {
        private const int INIT_TRANSITION_ID = 0;
        private const uint MLS_NEW_GROUP_EXPECTED_EPOCH = 1;

        private readonly ulong _channelId;
        private readonly string _selfUserId;
        private readonly DaveSession _session;
        private readonly HashSet<string> _recognizedUserIds = new();
        private readonly Dictionary<int, int> _protocolTransitions = new();
        private int _latestPreparedVersion;
        private int? _lastTransitionId;
        private bool _reinitializing;
        private bool _disposed;

        private static readonly LibDave.LogSinkCallback _nativeLogCallback = OnNativeLog;
        private static bool _logCallbackSet;

        public DaveSessionManager(ulong channelId, ulong userId)
        {
            _channelId = channelId;
            _selfUserId = userId.ToString();
            _session = new DaveSession();

            if (!_logCallbackSet)
            {
                LibDave.SetLogSinkCallback(_nativeLogCallback);
                _logCallbackSet = true;
            }
        }

        public bool HasKeyRatchet => _session.HasKeyRatchet();

        public bool IsReinitializing => _reinitializing;

        public int? LastTransitionId => _lastTransitionId;

        public void AssignSsrcToCodec(uint ssrc)
            => _session.AssignSsrcToCodec(ssrc);

        public int Encrypt(uint ssrc, byte[] frame, int frameLength, byte[] output, int outputCapacity)
            => _session.Encrypt(ssrc, frame, frameLength, output, outputCapacity);

        public int GetMaxCiphertextSize(int frameSize)
            => _session.GetMaxCiphertextSize(frameSize);

        /// <summary>
        /// Called when SessionDescription (op 4) is received with dave_protocol_version.
        /// </summary>
        public bool OnSessionDescription(int protocolVersion)
        {
            return HandleProtocolInit(protocolVersion);
        }

        /// <summary>
        /// Called when DAVE_PREPARE_TRANSITION (op 21) is received.
        /// Returns true if executed immediately (transitionId==0), meaning caller should NOT send TRANSITION_READY.
        /// </summary>
        public bool OnPrepareTransition(int transitionId, int protocolVersion)
        {
            _protocolTransitions[transitionId] = protocolVersion;

            if (transitionId == INIT_TRANSITION_ID)
            {
                HandleExecuteTransition(transitionId);
                return true;
            }

            return false;
        }

        public void OnExecuteTransition(int transitionId)
        {
            HandleExecuteTransition(transitionId);
        }

        /// <summary>
        /// Called when PrepareEpoch (op 24) is received.
        /// </summary>
        public bool OnPrepareEpoch(uint epoch, int protocolVersion)
        {
            HandlePrepareEpoch(epoch, protocolVersion);
            return epoch == MLS_NEW_GROUP_EXPECTED_EPOCH;
        }

        public void OnExternalSender(byte[] externalSenderPackage)
        {
            _session.SetExternalSender(externalSenderPackage);
        }

        public byte[]? OnProposals(byte[] proposals)
        {
            var result = _session.ProcessProposals(proposals, GetRecognizedUserIds());
            Log.Information("DAVE proposals processed, commitWelcome={HasCommit}", result != null && result.Length > 0);
            return result;
        }

        public CommitProcessResult OnCommitTransition(int transitionId, byte[] commit)
        {
            var result = _session.ProcessCommit(commit);
            if (result == CommitProcessResult.Success)
            {
                if (transitionId != INIT_TRANSITION_ID)
                {
                    var version = _session.GetProtocolVersion();
                    PrepareRatchets(transitionId, version);
                }
                else
                {
                    _reinitializing = false;
                }

                _lastTransitionId = transitionId;
            }

            return result;
        }

        /// <returns>True if welcome was processed successfully</returns>
        public bool OnWelcome(int transitionId, byte[] welcome)
        {
            var success = _session.ProcessWelcome(welcome, GetRecognizedUserIds());
            if (success)
            {
                if (transitionId != INIT_TRANSITION_ID)
                {
                    var version = _session.GetProtocolVersion();
                    PrepareRatchets(transitionId, version);
                }
                else
                {
                    _reinitializing = false;
                }

                _lastTransitionId = transitionId;
                return true;
            }

            return false;
        }

        public byte[]? GetKeyPackage()
        {
            var kp = _session.GetKeyPackage();
            Log.Information("DAVE key package generated, length={Length}", kp?.Length ?? 0);
            return kp;
        }

        public void AddUser(string userId)
        {
            _recognizedUserIds.Add(userId);
        }

        public void RemoveUser(string userId)
        {
            _recognizedUserIds.Remove(userId);
        }

        /// <summary>
        /// Returns true if a key package should be sent.
        /// </summary>
        public bool HandleProtocolInit(int protocolVersion, bool isRecovery = false)
        {
            if (isRecovery)
                _reinitializing = true;

            if (protocolVersion > 0)
            {
                Log.Information("DAVE protocol init v{Version}, creating new MLS group (recovery={Recovery})",
                    protocolVersion, isRecovery);
                HandlePrepareEpoch(MLS_NEW_GROUP_EXPECTED_EPOCH, protocolVersion);
                return true;
            }
            else
            {
                Log.Information("DAVE protocol init v0, passthrough mode");
                PrepareRatchets(INIT_TRANSITION_ID, protocolVersion);
                HandleExecuteTransition(INIT_TRANSITION_ID);
                return false;
            }
        }

        private void HandlePrepareEpoch(uint epoch, int protocolVersion)
        {
            if (epoch == MLS_NEW_GROUP_EXPECTED_EPOCH)
            {
                Log.Information("DAVE session init: epoch {Epoch} v{Version} channel {Channel}", epoch, protocolVersion, _channelId);
                _session.Init((ushort)protocolVersion, _channelId, _selfUserId);
            }
        }

        private void HandleExecuteTransition(int transitionId)
        {
            if (!_protocolTransitions.TryGetValue(transitionId, out var protocolVersion))
            {
                Log.Warning("DAVE execute transition {TransitionId} not found in pending transitions", transitionId);
                return;
            }

            _protocolTransitions.Remove(transitionId);
            Log.Information("DAVE executing transition {TransitionId} v{Version}", transitionId, protocolVersion);

            if (protocolVersion == 0)
            {
                _session.Reset();
            }

            _session.UpdateSelfKeyRatchet(_selfUserId);
            _reinitializing = false;
            _lastTransitionId = transitionId;
            Log.Information("DAVE self key ratchet updated, hasRatchet={HasRatchet}", _session.HasKeyRatchet());
        }

        private void PrepareRatchets(int transitionId, int protocolVersion)
        {
            if (transitionId == INIT_TRANSITION_ID)
            {
                _session.UpdateSelfKeyRatchet(_selfUserId);
            }
            else
            {
                _protocolTransitions[transitionId] = protocolVersion;
            }

            _latestPreparedVersion = protocolVersion;
        }

        private string[] GetRecognizedUserIds()
        {
            var list = new List<string>(_recognizedUserIds);
            if (!list.Contains(_selfUserId))
                list.Add(_selfUserId);
            return list.ToArray();
        }

        private static void OnNativeLog(int severity, string file, int line, string message)
        {
            if (severity >= 2)
                Log.Warning("[libdave] {Message}", message);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _session.Dispose();
        }
    }
}
