using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using NadekoBot.Voice;

namespace NadekoBot.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SongBufferBenchmarks
{
    private const int FRAME_SIZE = 3840;
    private const int WRITE_CHUNK = 38400;

    private byte[] _writeData = null!;

    private PoopyBufferImmortalized _poopy = null!;
    private RingBuffer _ring = null!;

    [GlobalSetup]
    public void Setup()
    {
        _writeData = new byte[WRITE_CHUNK];
        new Random(42).NextBytes(_writeData);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _poopy?.Dispose();
        _ring?.Dispose();
        _poopy = new PoopyBufferImmortalized(FRAME_SIZE);
        _ring = new RingBuffer(FRAME_SIZE);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _poopy?.Dispose();
        _ring?.Dispose();
        _poopy = null!;
        _ring = null!;
    }

    // --- Sequential Write ---

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SequentialWrite")]
    public void Poopy_Write_1000()
    {
        for (var i = 0; i < 1000; i++)
        {
            _poopy.Read(WRITE_CHUNK, out _);
            _poopy.Write(_writeData, WRITE_CHUNK);
        }
    }

    [Benchmark]
    [BenchmarkCategory("SequentialWrite")]
    public void Ring_Write_1000()
    {
        for (var i = 0; i < 1000; i++)
        {
            _ring.Read(WRITE_CHUNK, out _);
            _ring.Write(_writeData, WRITE_CHUNK);
        }
    }

    // --- Sequential Read ---

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SequentialRead")]
    public int Poopy_Read_1000()
    {
        for (var i = 0; i < 25; i++)
            _poopy.Write(_writeData, WRITE_CHUNK);

        var totalRead = 0;
        for (var i = 0; i < 1000; i++)
        {
            _poopy.Read(FRAME_SIZE, out var len);
            totalRead += len;
            if (len == 0)
            {
                for (var j = 0; j < 25; j++)
                    _poopy.Write(_writeData, WRITE_CHUNK);
            }
        }

        return totalRead;
    }

    [Benchmark]
    [BenchmarkCategory("SequentialRead")]
    public int Ring_Read_1000()
    {
        for (var i = 0; i < 25; i++)
            _ring.Write(_writeData, WRITE_CHUNK);

        var totalRead = 0;
        for (var i = 0; i < 1000; i++)
        {
            _ring.Read(FRAME_SIZE, out var len);
            totalRead += len;
            if (len == 0)
            {
                for (var j = 0; j < 25; j++)
                    _ring.Write(_writeData, WRITE_CHUNK);
            }
        }

        return totalRead;
    }

    // --- Mixed Read/Write (simulates real usage: write chunk, read frames) ---

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("MixedReadWrite")]
    public int Poopy_Mixed_1000()
    {
        var totalRead = 0;
        for (var i = 0; i < 1000; i++)
        {
            _poopy.Write(_writeData, WRITE_CHUNK);
            for (var j = 0; j < 10; j++)
            {
                _poopy.Read(FRAME_SIZE, out var len);
                totalRead += len;
            }
        }

        return totalRead;
    }

    [Benchmark]
    [BenchmarkCategory("MixedReadWrite")]
    public int Ring_Mixed_1000()
    {
        var totalRead = 0;
        for (var i = 0; i < 1000; i++)
        {
            _ring.Write(_writeData, WRITE_CHUNK);
            for (var j = 0; j < 10; j++)
            {
                _ring.Read(FRAME_SIZE, out var len);
                totalRead += len;
            }
        }

        return totalRead;
    }

    // --- Wrap-around stress (forces many buffer wraps through ~10MB) ---

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WrapAround")]
    public int Poopy_WrapAround()
    {
        var totalRead = 0;
        for (var i = 0; i < 260; i++)
        {
            _poopy.Write(_writeData, WRITE_CHUNK);
            for (var j = 0; j < 10; j++)
            {
                _poopy.Read(FRAME_SIZE, out var len);
                totalRead += len;
            }
        }

        return totalRead;
    }

    [Benchmark]
    [BenchmarkCategory("WrapAround")]
    public int Ring_WrapAround()
    {
        var totalRead = 0;
        for (var i = 0; i < 260; i++)
        {
            _ring.Write(_writeData, WRITE_CHUNK);
            for (var j = 0; j < 10; j++)
            {
                _ring.Read(FRAME_SIZE, out var len);
                totalRead += len;
            }
        }

        return totalRead;
    }

    // --- Concurrent SPSC (the real-world two-thread pattern) ---

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ConcurrentSPSC")]
    public int Poopy_Concurrent()
    {
        const int totalWrites = 500;
        var writesDone = 0;

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < totalWrites; i++)
            {
                SpinWait.SpinUntil(() => _poopy.FreeSpace > WRITE_CHUNK);
                _poopy.Write(_writeData, WRITE_CHUNK);
                Interlocked.Increment(ref writesDone);
            }
        });

        var reader = Task.Run(() =>
        {
            var read = 0;
            while (Volatile.Read(ref writesDone) < totalWrites || _poopy.ContentLength > 0)
            {
                _poopy.Read(FRAME_SIZE, out var len);
                read += len;
                if (len == 0)
                    Thread.SpinWait(100);
            }

            return read;
        });

        writer.Wait();
        return reader.Result;
    }

    [Benchmark]
    [BenchmarkCategory("ConcurrentSPSC")]
    public int Ring_Concurrent()
    {
        const int totalWrites = 500;
        var writesDone = 0;

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < totalWrites; i++)
            {
                SpinWait.SpinUntil(() => _ring.FreeSpace > WRITE_CHUNK);
                _ring.Write(_writeData, WRITE_CHUNK);
                Interlocked.Increment(ref writesDone);
            }
        });

        var reader = Task.Run(() =>
        {
            var read = 0;
            while (Volatile.Read(ref writesDone) < totalWrites || _ring.ContentLength > 0)
            {
                _ring.Read(FRAME_SIZE, out var len);
                read += len;
                if (len == 0)
                    Thread.SpinWait(100);
            }

            return read;
        });

        writer.Wait();
        return reader.Result;
    }
}
