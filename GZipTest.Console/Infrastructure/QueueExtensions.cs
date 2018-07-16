using System;
using System.Threading;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp.Infrastructure
{
    internal static class QueueExtensions
    {
        [NotNull]
        public static T Dequeue<T>([NotNull] this IQueue<T> queue)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var spinWait = new SpinWait();

            T value;
            while (!queue.TryDequeue(out value))
            {
                spinWait.SpinOnce();
            }

            return value;
        }

        public static void Enqueue<T>([NotNull] this IBlockingQueue<T> queue, [NotNull]T value)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            queue.TryEnqueue(value, Constants.InfiniteTimeout);
        }
    }
}