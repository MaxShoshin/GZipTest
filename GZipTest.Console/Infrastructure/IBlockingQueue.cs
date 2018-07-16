using System;
using System.Collections.Generic;

namespace GZipTest.ConsoleApp.Infrastructure
{
    internal interface IBlockingQueue<T>
    {
        bool TryEnqueue(T value, TimeSpan timeout);
        IEnumerable<T> GetConsumingEnumerable();
        void CompleteAdding();
    }
}