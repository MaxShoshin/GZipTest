namespace GZipTest.ConsoleApp
{
    internal sealed class PipelineInfo
    {
        public PipelineInfo(long uncompressedSize, int blockSize)
        {
            BlockSize = blockSize;
            UncompressedSize = uncompressedSize;

            BlockCount = GetBlockCount(uncompressedSize, blockSize);
        }

        public int BlockSize { get; }

        public long UncompressedSize { get; }

        public int BlockCount { get; }

        private int GetBlockCount(long fileLength, int blockSize)
        {
            var blockCount = (int)(fileLength / blockSize);
            if (fileLength % blockSize != 0)
            {
                blockCount++;
            }

            return blockCount;
        }

    }
}