using System;

namespace GZipTest.ConsoleApp
{
    internal sealed class Settings
    {
        public static readonly Settings Default = new Settings(
            blockSize: 1024 * 1024,
            workThreadCount: Environment.ProcessorCount,
            queueBoundsPerThread: 10);

        public readonly int BlockSize;

        public readonly int WorkThreadCount;

        public readonly int QueueBoundsPerThread;

        public Settings(int blockSize, int workThreadCount, int queueBoundsPerThread)
        {
            if (blockSize <= 0) throw new ArgumentOutOfRangeException(nameof(blockSize));
            if (workThreadCount <= 0) throw new ArgumentOutOfRangeException(nameof(workThreadCount));
            if (queueBoundsPerThread <= 0) throw new ArgumentOutOfRangeException(nameof(queueBoundsPerThread));

            BlockSize = blockSize;
            WorkThreadCount = workThreadCount;
            QueueBoundsPerThread = queueBoundsPerThread;
        }

        public int TransformWorkerBounds => QueueBoundsPerThread * WorkThreadCount;
        public int WriteWorkerBounds => QueueBoundsPerThread;
    }
}