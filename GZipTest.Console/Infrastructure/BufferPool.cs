using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp.Infrastructure
{
    internal sealed class BufferPool
    {
        // TODO: May be monitor free implementation?
        private readonly object _bufferLock = new object();
        private readonly LinkedList<byte[]> _freeItems = new LinkedList<byte[]>();

        public Buffer GetBuffer(int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            lock(_bufferLock)
            {
                var node = _freeItems.First;

                while (node != null)
                {
                    if (node.Value.Length >= size)
                    {
                        _freeItems.Remove(node);
                        return new Buffer(node.Value, this);
                    }
                    node = node.Next;
                }
            }

            return new Buffer(new byte[size], this);
        }


        private void Release([NotNull] byte[] data)
        {
            lock (_bufferLock)
            {
                _freeItems.AddLast(new LinkedListNode<byte[]>(data));
            }
        }

        public struct Buffer : IDisposable
        {
            private readonly byte[] _data;
            private readonly BufferPool _pool;
            private int _disposed;

            public Buffer([NotNull] byte[] data, [NotNull] BufferPool pool)
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                if (pool == null) throw new ArgumentNullException(nameof(pool));

                _data = data;
                _pool = pool;
                _disposed = 0;
            }

            public byte[] Data => _data;

            public int Length => _data.Length;

            public void Dispose()
            {
                if (Interlocked.Increment(ref _disposed) > 1)
                {
                    return;
                }

                _pool.Release(_data);
            }
        }
    }
}