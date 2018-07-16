using System;
using System.Collections.Generic;
using System.IO;
using GZipTest.ConsoleApp;
using GZipTest.ConsoleApp.Infrastructure;
using GZipTest.Tests.Mocks;
using GZipTest.Tests.Utils;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace GZipTest.Tests
{
    public sealed class Benchmarks
    {
        private const int IterationCount = 25;
        private const int BlockSize = 1024 * 1024;

        private readonly ITestOutputHelper _output;

        public Benchmarks([NotNull] ITestOutputHelper output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));

            _output = output;
        }

        [Fact]
        public void UnderlyingCollectionCompressBenchmarks()
        {
            using (var benchmark = new Benchmark(new Settings(BlockSize, Environment.ProcessorCount, 10)))
            {
                benchmark.Prepare(stream => Rnd.FillStream(stream, 123 * BlockSize));

                _output.WriteLine("BlockingQueue, lock free Queue:");
                Measurement.Display(
                    _output,
                    benchmark.Perform(
                        IterationCount,
                        (source, destination, settings) => new CompressPipeline(source, destination, settings)));

                _output.WriteLine("ConcurrentCollection:");
                Measurement.Display(
                    _output,
                    benchmark.Perform(
                        IterationCount,
                        (source, destination, settings) => new CompressPipeline(
                            source, destination, settings,
                            new ConcurrentBlockingCollectionQueue<Block>(settings.TransformWorkerBounds),
                            new ConcurrentBlockingCollectionQueue<Block>(settings.WriteWorkerBounds))));

                _output.WriteLine("SimpleLockQueue:");
                Measurement.Display(
                    _output,
                    benchmark.Perform(
                        IterationCount,
                        (source, destination, settings) => new CompressPipeline(
                            source, destination, settings,
                            new BlockingQueue<Block>(settings.TransformWorkerBounds, new LockingQueue<Block>()),
                            new BlockingQueue<Block>(settings.WriteWorkerBounds, new LockingQueue<Block>()))));
            }
        }

        private sealed class Benchmark : IDisposable
        {
            [NotNull] private readonly string _sourceFileName;
            [NotNull] private readonly string _destinationFileName;
            [NotNull] private readonly Settings _settings;
            private FileStream _source;
            private FileStream _destination;

            public Benchmark([NotNull] Settings settings)
            {
                if (settings == null) throw new ArgumentNullException(nameof(settings));

                _settings = settings;

                _sourceFileName = Path.GetTempFileName();
                _destinationFileName = Path.GetTempFileName();
            }

            public void Prepare([NotNull] Action<Stream> initializeSource)
            {
                if (initializeSource == null) throw new ArgumentNullException(nameof(initializeSource));

                _source = new FileStream(_sourceFileName, FileMode.Truncate, FileAccess.ReadWrite);
                _destination = new FileStream(_destinationFileName, FileMode.Truncate, FileAccess.ReadWrite);

                initializeSource(_source);
            }

            [NotNull]
            public IReadOnlyList<Measurement> Perform(int count, [NotNull] Func<Stream, Stream, Settings, Pipeline> pipelineFactory)
            {
                var blockCount = new PipelineInfo(_source.Length, _settings.BlockSize).BlockCount;

                var measurements = new List<Measurement>(count);

                for (int i = 0; i < count; i++)
                {
                    _source.Seek(0, SeekOrigin.Begin);
                    _destination.SetLength(0);

                    var pipeline = pipelineFactory(_source, _destination, _settings);
                    measurements.Add(Measurement.Measure("", () => pipeline.Process(), blockCount));
                }

                return measurements;
            }

            public void Dispose()
            {
                _source.Dispose();
                _destination.Dispose();

                File.Delete(_sourceFileName);
                File.Delete(_destinationFileName);
            }
        }
    }
}