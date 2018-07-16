using System;
using System.IO;
using FluentAssertions;
using GZipTest.ConsoleApp;
using GZipTest.Tests.Utils;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace GZipTest.Tests
{
    public sealed class IntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private const int BlockSize = 1024 * 23;
        private const int BigBlock = 1024 * 1024;

        private readonly string _sourceFileName;
        private readonly string _compressedFileName;
        private readonly string _decompressedFileName;

        public IntegrationTests([NotNull] ITestOutputHelper output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));

            _output = output;

            _sourceFileName = Path.GetTempFileName();
            _compressedFileName = Path.GetTempFileName();
            _decompressedFileName = Path.GetTempFileName();
        }

        [Fact]
        public void Should_Compress_And_Decompress_Simple_File()
        {
            using (var source = new FileStream(_sourceFileName, FileMode.Truncate, FileAccess.ReadWrite))
            using (var compressed = new FileStream(_compressedFileName, FileMode.Truncate, FileAccess.ReadWrite))
            using (var decompressed = new FileStream(_decompressedFileName, FileMode.Truncate, FileAccess.ReadWrite))
            {
                Rnd.FillStream(source, BlockSize);

                CompressDecompressCompare(source, compressed, decompressed, BlockSize);
            }
        }

        [Theory]
        [InlineData(BlockSize + BlockSize / 7, BlockSize)]             // More than block size
        [InlineData(BlockSize / 2, BlockSize)]                         // Less than block size
        [InlineData(BlockSize * 2, BlockSize)]                         // Size MOD block size == 0
        [InlineData(0, BlockSize)]                                     // Empty file
        [InlineData(10 * 1024 * BlockSize + BlockSize / 7, BlockSize)] // Really big file
        [InlineData(300 * BigBlock, BigBlock)]                         // Big file with big blocks
        public void Should_Compress_And_Decompress(long fileSize, int blockSize)
        {
            using (var source = new FileStream(_sourceFileName, FileMode.Truncate, FileAccess.ReadWrite))
            using (var compressed = new FileStream(_compressedFileName, FileMode.Truncate, FileAccess.ReadWrite))
            using (var decompressed = new FileStream(_decompressedFileName, FileMode.Truncate, FileAccess.ReadWrite))
            {
                Rnd.FillStream(source, fileSize);

                CompressDecompressCompare(source, compressed, decompressed, blockSize);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(5)]
        public void CompressAndDecompressManyTime(int count)
        {
            var size = 123 * BigBlock + BigBlock / 7;

            using (var source = new MemoryStream(size))
            {
                Rnd.FillStream(source, size);

                for (int i = 0; i < count; i++)
                {
                    using (var compressed = new MemoryStream(size))
                    using (var decompressed = new MemoryStream(size))
                    {
                        CompressDecompressCompare(source, compressed, decompressed, BigBlock);
                    }
                }
            }
        }

        public void Dispose()
        {
            File.Delete(_sourceFileName);
            File.Delete(_compressedFileName);
            File.Delete(_decompressedFileName);
        }

        private void CompressDecompressCompare([NotNull] Stream source, [NotNull] Stream compressed, [NotNull] Stream decompressed,int blockSize)
        {
            var settings = new Settings(blockSize, Environment.ProcessorCount, 10);
            var blockCount = new PipelineInfo(source.Length, blockSize).BlockCount;

            source.Seek(0, SeekOrigin.Begin);

            var compress = new CompressPipeline(source, compressed, settings);
            Measurement.Measure("Compressing", () => compress.Process(), blockCount).Display(_output);

            compressed.Seek(0, SeekOrigin.Begin);

            var decompress = new DecompressPipeline(compressed, decompressed, settings);
            Measurement.Measure("Decompressing", () => decompress.Process(), blockCount).Display(_output);

            decompressed.Seek(0, SeekOrigin.Begin);
            source.Seek(0, SeekOrigin.Begin);

            Compare.StreamContent(decompressed, source).Should().BeTrue();
        }


    }
}