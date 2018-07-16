using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp.Infrastructure
{
    // Inspired by BlockingCollection
    internal sealed class BlockingQueue<T> : IBlockingQueue<T>, IDisposable
    {
        private const int Completed = 1;

        [NotNull] private readonly IQueue<T> _queue;

        [NotNull] private readonly Semaphore _enqueueSemaphore;
        [NotNull] private readonly Semaphore _dequeueSemaphore;

        [NotNull] private readonly ManualResetEvent _complete = new ManualResetEvent(false);

        [NotNull] private readonly WaitHandle[] _enqueueWaitHandles;
        [NotNull] private readonly WaitHandle[] _dequeWaitHandles;
        private int _completeFlag;

        public BlockingQueue(int maxItems, [CanBeNull] IQueue<T> queue = null)
        {
            if (maxItems <= 0) throw new ArgumentOutOfRangeException(nameof(maxItems));

            _enqueueSemaphore = new Semaphore(maxItems, maxItems);
            _dequeueSemaphore = new Semaphore(0, maxItems);
            _queue = queue ?? new Queue<T>();

            _enqueueWaitHandles = new WaitHandle[] {_enqueueSemaphore, _complete};
            _dequeWaitHandles = new WaitHandle[] {_dequeueSemaphore, _complete};
        }

        private bool IsCompleted => _completeFlag == Completed;

        public bool TryEnqueue(T value, TimeSpan timeout)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException("Queue already been closed for Enqueue.");
            }

            var waitHandleIndex = WaitHandle.WaitAny(_enqueueWaitHandles, timeout);

            if (waitHandleIndex == WaitHandle.WaitTimeout)
            {
                return false;
            }

            // Is wake up by CompleteAdding?
            if (waitHandleIndex != 0)
            {
                throw new InvalidOperationException("CompleteAdding during Enqueue is not supported.");
            }

            _queue.Enqueue(value);

            _dequeueSemaphore.Release();

            return true;
        }

        public bool TryDequeue(out T value, TimeSpan timeout)
        {
            value = default;

            if (IsCompleted)
            {
                return false;
            }

            var takenIndex = WaitHandle.WaitAny(_dequeWaitHandles, timeout);
            var dequeueSemaphoreTaken = takenIndex == 0;

            if (IsCompleted || !dequeueSemaphoreTaken)
            {
                return false;
            }

            if (!_queue.TryDequeue(out value))
            {
                return false;
            }

            _enqueueSemaphore.Release();

            return true;
        }

        public IEnumerable<T> GetConsumingEnumerable()
        {
            while(!IsCompleted)
            {
                if (TryDequeue(out var value, Constants.InfiniteTimeout))
                {
                    yield return value;
                }
            }

            // Process outstanding messages even queue is completed
            while(_queue.TryDequeue(out var value))
            {
                yield return value;
            }
        }

        public void CompleteAdding()
        {
            Interlocked.Exchange(ref _completeFlag, Completed);

            _complete.Set();
        }

        public void Dispose()
        {
            _dequeueSemaphore.Close();
            _enqueueSemaphore.Close();
            _complete.Close();
        }
    }
}