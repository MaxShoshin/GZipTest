using System;
using System.IO;
using System.IO.Compression;
using GZipTest.ConsoleApp.Infrastructure;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp
{
    internal class CompressPipeline : Pipeline
    {
        private readonly byte[] _buffer = new byte[sizeof(long)];

        public CompressPipeline(
            [NotNull] Stream sourceStream,
            [NotNull] Stream destinationStream,
            [NotNull] Settings settings)
            : base(sourceStream, destinationStream, settings)
        {
        }

        public CompressPipeline([NotNull] Stream sourceStream, [NotNull] Stream destinationStream, [NotNull] Settings settings, [NotNull] IBlockingQueue<Block> transformQueue, [NotNull] IBlockingQueue<Block> writeQueue)
            : base(sourceStream, destinationStream, settings, transformQueue, writeQueue)
        {
        }

        [NotNull]
        protected override PipelineInfo Initialize()
        {
            var fileLength = SourceStream.Length;

            return new PipelineInfo(fileLength, Settings.BlockSize);
        }

        [NotNull]
        protected override Block ReadBlock(int index, [NotNull] PipelineInfo pipelineInfo)
        {
            var uncompressedStream = MemoryStreamManager.GetStream("Uncompressed");

            SourceStream.CopyBlockTo(uncompressedStream, pipelineInfo.BlockSize);
            uncompressedStream.Position = 0;

            return new Block(index, uncompressedStream);
        }

        protected override void TransformStream([NotNull] Stream from, [NotNull] Stream to)
        {
            using (var compressStream = CreateCompressStream(to))
            {
                from.SmartCopyTo(compressStream);
            }
        }

        protected override void PrepareForWrite(PipelineInfo pipelineInfo)
        {
            // Write file header
            Write(pipelineInfo.UncompressedSize);
            Write(pipelineInfo.BlockSize);
        }

        protected override void BeforeBlockWrite(Block block, PipelineInfo pipelineInfo)
        {
            // Write block header
            Write(block.Position);
            Write((int)block.Data.Length);
        }

        [NotNull]
        protected virtual Stream CreateCompressStream([NotNull] Stream destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            return new GZipStream(destination, CompressionMode.Compress, true);
        }

        private void Write(int value)
        {
            IndependentBitConverter.WriteBytes(value, _buffer);

            DestinationStream.Write(_buffer, 0, sizeof(int));
        }

        private void Write(long value)
        {
            IndependentBitConverter.WriteBytes(value, _buffer);

            DestinationStream.Write(_buffer, 0, sizeof(long));
        }
    }

}