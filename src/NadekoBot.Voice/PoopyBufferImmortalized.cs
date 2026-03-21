#nullable enable
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Voice
{
    public sealed class PoopyBufferImmortalized : ISongBuffer
    {
        private readonly byte[] _buffer;
        private readonly byte[] _outputArray;
        private CancellationToken _cancellationToken;
        private bool _isStopped;

        private volatile int _readPosition;
        private volatile int _writePosition;

        public int ReadPosition
        {
            get => _readPosition;
            private set => _readPosition = value;
        }

        public int WritePosition
        {
            get => _writePosition;
            private set => _writePosition = value;
        }

        public int ContentLength => WritePosition >= ReadPosition
            ? WritePosition - ReadPosition
            : (_buffer.Length - ReadPosition) + WritePosition;

        public int FreeSpace => _buffer.Length - ContentLength;

        public bool Stopped => _cancellationToken.IsCancellationRequested || _isStopped;

        public PoopyBufferImmortalized(int frameSize)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(1_000_000);
            _outputArray = new byte[frameSize];

            ReadPosition = 0;
            WritePosition = 0;
        }

        public void Stop()
            => _isStopped = true;

        // this method needs a rewrite
        public Task<bool> BufferAsync(ITrackDataSource source, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            var bufferingCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task.Run(async () =>
            {
                var output = ArrayPool<byte>.Shared.Rent(38400);
                try
                {
                    int read;
                    while (!Stopped && (read = source.Read(output)) > 0)
                    {
                        while (!Stopped && FreeSpace <= read)
                        {
                            bufferingCompleted.TrySetResult(true);
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }

                        if (Stopped)
                            break;

                        Write(output, read);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(output);
                    bufferingCompleted.TrySetResult(true);
                }
            }, cancellationToken);

            return bufferingCompleted.Task;
        }

        private void Write(byte[] input, int writeCount)
        {
            if (WritePosition + writeCount < _buffer.Length)
            {
                Buffer.BlockCopy(input, 0, _buffer, WritePosition, writeCount);
                WritePosition += writeCount;
                return;
            }

            var wroteNormally = _buffer.Length - WritePosition;
            Buffer.BlockCopy(input, 0, _buffer, WritePosition, wroteNormally);
            var wroteFromStart = writeCount - wroteNormally;
            Buffer.BlockCopy(input, wroteNormally, _buffer, 0, wroteFromStart);
            WritePosition = wroteFromStart;
        }

        public Span<byte> Read(int count, out int length)
        {
            var rp = ReadPosition;
            var wp = WritePosition;
            var cl = wp >= rp
                ? wp - rp
                : (_buffer.Length - rp) + wp;

            if (cl == 0)
            {
                length = 0;
                return Span<byte>.Empty;
            }

            var toRead = Math.Min(cl, count);
            var toEnd = _buffer.Length - rp;

            Span<byte> toReturn = _outputArray;
            if (toRead <= toEnd)
            {
                ((Span<byte>)_buffer).Slice(rp, toRead).CopyTo(toReturn);
                ReadPosition = rp + toRead;
            }
            else
            {
                var bufferSpan = (Span<byte>)_buffer;
                bufferSpan.Slice(rp, toEnd).CopyTo(toReturn);
                var fromStart = toRead - toEnd;
                bufferSpan.Slice(0, fromStart).CopyTo(toReturn.Slice(toEnd));
                ReadPosition = fromStart;
            }

            length = toRead;
            return toReturn;
        }

        public void Dispose()
            => ArrayPool<byte>.Shared.Return(_buffer);

        public void Reset()
        {
            ReadPosition = 0;
            WritePosition = 0;
        }
    }
}