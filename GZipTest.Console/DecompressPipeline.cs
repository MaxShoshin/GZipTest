using System;
using System.IO;
using System.IO.Compression;
using GZipTest.ConsoleApp.Infrastructure;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp
{
    internal class DecompressPipeline : Pipeline
    {
        [NotNull] private readonly byte[] _buffer = new byte[sizeof(long)];

        public DecompressPipeline([NotNull] Stream sourceStream, [NotNull] Stream destinationStream, [NotNull] Settings settings)
            : base(sourceStream, destinationStream, settings)
        {
        }

        public DecompressPipeline([NotNull] Stream sourceStream, [NotNull] Stream destinationStream, [NotNull] Settings settings, [NotNull] IBlockingQueue<Block> transformQueue, [NotNull] IBlockingQueue<Block> writeQueue)
            : base(sourceStream, destinationStream, settings, transformQueue, writeQueue)
        {
        }

        [NotNull]
        protected override PipelineInfo Initialize()
        {
            var fileLength = ReadInt64();

            if (fileLength < 0)
            {
                throw new PipelineException("Source file corrupted.");
            }

            var blockSize = ReadInt32();

            if (blockSize <= 0)
            {
                throw new PipelineException("Source file corrupted.");
            }

            return new PipelineInfo(fileLength, blockSize);
        }

        [NotNull]
        protected override Block ReadBlock(int index, PipelineInfo pipelineInfo)
        {
            var position = ReadInt32();
            if (position < 0 || position >= pipelineInfo.BlockCount)
            {
                throw new PipelineException("Source file corrupted.");
            }

            var compressedLength = ReadInt32();

            if (compressedLength <= 0)
            {
                throw new PipelineException("Source file corrupted.");
            }

            var compressedStream = MemoryStreamManager.GetStream("Compressed");

            SourceStream.CopyBlockTo(compressedStream, compressedLength);
            compressedStream.Position = 0;

            return new Block(position, compressedStream);
        }

        protected override void TransformStream([NotNull] Stream from, [NotNull] Stream to)
        {
            using (var decompressStream = CreateDecompressStream(from))
            {
                decompressStream.SmartCopyTo(to);
            }
        }

        protected override void PrepareForWrite([NotNull] PipelineInfo pipelineInfo)
        {
            DestinationStream.SetLength(pipelineInfo.UncompressedSize);
        }

        protected override void BeforeBlockWrite([NotNull] Block block, [NotNull] PipelineInfo pipelineInfo)
        {
            var offset = (long)block.Position * pipelineInfo.BlockSize;
            DestinationStream.Seek(offset, SeekOrigin.Begin);
        }

        [NotNull]
        protected virtual Stream CreateDecompressStream([NotNull] Stream source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
        }

        private long ReadInt64()
        {
            SourceStream.ReadExactly(_buffer, sizeof(long));

            return IndependentBitConverter.ReadInt64(_buffer);
        }

        private int ReadInt32()
        {
            SourceStream.ReadExactly(_buffer, sizeof(int));

            return IndependentBitConverter.ReadInt32(_buffer);
        }
    }
}