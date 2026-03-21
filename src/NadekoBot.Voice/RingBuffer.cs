using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Voice
{
    /// <summary>
    /// Lock-free SPSC (single-producer, single-consumer) ring buffer.
    /// Uses monotonic 64-bit head/tail with power-of-2 masking and
    /// Volatile.Read/Write for correct memory ordering on all architectures.
    /// </summary>
    public sealed class RingBuffer : ISongBuffer
    {
        private const int MIN_CAPACITY = 1 << 20; // 1MB

        private readonly byte[] _buffer;
        private readonly int _capacity;
        private readonly int _capacityMask;
        private readonly byte[] _outputArray;

        private long _head; // consumer advances, producer reads
        private long _tail; // producer advances, consumer reads

        private CancellationToken _cancellationToken;
        private volatile bool _isStopped;

        public bool Stopped => _cancellationToken.IsCancellationRequested || _isStopped;

        internal long ContentLength
            => Volatile.Read(ref _tail) - Volatile.Read(ref _head);

        internal long FreeSpace
            => _capacity - ContentLength;

        public RingBuffer(int frameSize)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(MIN_CAPACITY);
            _capacity = HighestPowerOf2(_buffer.Length);
            _capacityMask = _capacity - 1;
            _outputArray = new byte[frameSize];
        }

        private static int HighestPowerOf2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            return (v + 1) >> 1;
        }

        public void Stop()
            => _isStopped = true;

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

        /// <summary>
        /// Writes <paramref name="count"/> bytes from <paramref name="input"/> into the buffer.
        /// Called by the producer thread only.
        /// </summary>
        public void Write(byte[] input, int count)
        {
            var tail = Volatile.Read(ref _tail);
            var bufIdx = (int)(tail & _capacityMask);
            var toEnd = _capacity - bufIdx;

            if (count <= toEnd)
            {
                Buffer.BlockCopy(input, 0, _buffer, bufIdx, count);
            }
            else
            {
                Buffer.BlockCopy(input, 0, _buffer, bufIdx, toEnd);
                Buffer.BlockCopy(input, toEnd, _buffer, 0, count - toEnd);
            }

            Volatile.Write(ref _tail, tail + count);
        }

        /// <summary>
        /// Reads up to <paramref name="count"/> bytes from the buffer.
        /// Called by the consumer thread only.
        /// Returns empty span when no data is available.
        /// </summary>
        public Span<byte> Read(int count, out int length)
        {
            var available = ContentLength;
            if (available == 0)
            {
                length = 0;
                return Span<byte>.Empty;
            }

            var toRead = (int)Math.Min(available, count);
            var head = Volatile.Read(ref _head);
            var bufIdx = (int)(head & _capacityMask);
            var toEnd = _capacity - bufIdx;

            Span<byte> toReturn = _outputArray;
            if (toRead <= toEnd)
            {
                ((Span<byte>)_buffer).Slice(bufIdx, toRead).CopyTo(toReturn);
            }
            else
            {
                var bufSpan = (Span<byte>)_buffer;
                bufSpan.Slice(bufIdx, toEnd).CopyTo(toReturn);
                bufSpan.Slice(0, toRead - toEnd).CopyTo(toReturn.Slice(toEnd));
            }

            Volatile.Write(ref _head, head + toRead);

            length = toRead;
            return toReturn;
        }

        public void Reset()
        {
            _head = 0;
            _tail = 0;
            _isStopped = false;
        }

        public void Dispose()
            => ArrayPool<byte>.Shared.Return(_buffer);
    }
}
