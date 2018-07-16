using System;
using System.Collections.Generic;
using GZipTest.ConsoleApp.Infrastructure;
using JetBrains.Annotations;

namespace GZipTest.Tests.Mocks
{
    internal sealed class LockingQueue<T> : IQueue<T>
    {
        [NotNull] private readonly LinkedList<T> _linkedList = new LinkedList<T>();
        [NotNull] private readonly object _syncRoot = new object();

        public void Enqueue([NotNull] T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (_syncRoot)
            {
                _linkedList.AddLast(value);
            }
        }

        [ContractAnnotation("false <= value:null")]
        public bool TryDequeue(out T value)
        {
            lock (_syncRoot)
            {
                if (_linkedList.Count == 0)
                {
                    value = default;

                    return false;
                }

                var node = _linkedList.First;
                _linkedList.Remove(node);

                value = node.Value;
                return true;
            }
        }
    }
}