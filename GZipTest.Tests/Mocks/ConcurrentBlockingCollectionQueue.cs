using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GZipTest.ConsoleApp.Infrastructure;

namespace GZipTest.Tests.Mocks
{
    public sealed class ConcurrentBlockingCollectionQueue<T> : IBlockingQueue<T>
    {
        private readonly BlockingCollection<T> _collection;

        public ConcurrentBlockingCollectionQueue(int bounds)
        {
            _collection = new BlockingCollection<T>(bounds);
        }

        public bool TryEnqueue(T value, TimeSpan timeout)
        {
            return _collection.TryAdd(value, timeout);
        }

        public bool TryDequeue(out T value, TimeSpan timeout)
        {
            return _collection.TryTake(out value, timeout);
        }

        public IEnumerable<T> GetConsumingEnumerable()
        {
            return _collection.GetConsumingEnumerable();
        }

        public void CompleteAdding()
        {
            _collection.CompleteAdding();
        }
    }
}