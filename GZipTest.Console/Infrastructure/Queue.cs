using System;
using System.Threading;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp.Infrastructure
{
    // Monitor-free queue
    internal sealed class Queue<T> : IQueue<T>
    {
        [NotNull] private Element _tail;
        [NotNull] private Element _head;

        public Queue()
        {
            _head = _tail = new Element();
        }

        public void Enqueue([NotNull] T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var newTail = new Element(value);

            Element previousTail;
            Element tail;
            do
            {
                tail = _tail;
                previousTail = Interlocked.CompareExchange(ref _tail, newTail.Next, tail);
            } while (previousTail != tail);

            // ReSharper disable once PossibleNullReferenceException
            Interlocked.Exchange(ref previousTail.Next, newTail);
        }

        [ContractAnnotation("false <= value:null")]
        public bool TryDequeue(out T value)
        {
            value = default;

            Element head;
            Element previousHead;

            do
            {
                head = _head;
                previousHead = null;

                if (head.IsFake)
                {
                    // Is final node?
                    if (head.Next == head)
                    {
                         return false;
                    }

                    // Step to next non fake node
                    Interlocked.CompareExchange(ref _head, head.Next, head);

                    continue;
                }

                previousHead = Interlocked.CompareExchange(ref _head, head.Next, head);

            } while (previousHead != head);

            // ReSharper disable once PossibleNullReferenceException
            value = previousHead.Value;

            return true;
        }

        private sealed class Element
        {
            [NotNull] public Element Next;

            public readonly T Value;
            public readonly bool IsFake;

            public Element(T value)
            {
                Value = value;

                // TODO: Don't create fake element for every enqueue, it also need skip such elements during dequeue
                Next = new Element();
            }

            public Element()
            {
                IsFake = true;
                Next = this;
            }
        }
    }
}