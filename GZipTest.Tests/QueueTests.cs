using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GZipTest.ConsoleApp.Infrastructure;
using Xunit;

using IntQueue = GZipTest.ConsoleApp.Infrastructure.Queue<int>;

namespace GZipTest.Tests
{
    public sealed class QueueTests
    {
        private const int Value = 345;
        private readonly IntQueue _queue = new IntQueue();

        [Fact]
        public void ShouldEnqueueAndDequeue()
        {
            _queue.Enqueue(Value);

            _queue.Dequeue().Should().Be(Value);
        }

        [Fact]
        public void Should_Dequeue_False_On_Empty()
        {
            _queue.TryDequeue(out _).Should().BeFalse();

            _queue.Enqueue(Value);
            _queue.TryDequeue(out _).Should().BeTrue();
            _queue.TryDequeue(out _).Should().BeFalse();
        }

        [Fact]
        public void Should_Dequeue_Enqueue_Dequeue()
        {
            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(Value + i);

                _queue.Dequeue().Should().Be(Value + i);

                _queue.TryDequeue(out _).Should().BeFalse();
            }
        }

        public sealed class MultiThreading
        {
            private static readonly TimeSpan TestDuration = TimeSpan.FromSeconds(3);
            private readonly IntQueue _queue = new IntQueue();
            private CancellationToken _cancellationToken;
            private readonly ConcurrentQueue<int> _read = new ConcurrentQueue<int>();
            private readonly ConcurrentQueue<int> _written = new ConcurrentQueue<int>();
            private int _id;

            private readonly List<Thread> _threads = new List<Thread>();

            [Theory]
            [InlineData(2,3)]
            [InlineData(1,3)]
            [InlineData(3,1)]
            public void ReadersAndWriters(int readerCount, int writerCount)
            {
                for (int i = 0; i < readerCount; i++)
                {
                    _threads.Add(new Thread(Reader));
                }

                for (int i = 0; i < writerCount; i++)
                {
                    _threads.Add(new Thread(Writer));
                }

                var testTask = Task.Run(() => Start());
                var delay = Task.Delay(TestDuration + TestDuration);

                Task.WaitAny(testTask, delay);

                if (delay.IsCompleted && !testTask.IsCompleted)
                {
                    throw new TimeoutException();
                }

                Validate();
            }

            private void Reader()
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (_queue.TryDequeue(out var value))
                    {
                        _read.Enqueue(value);
                    }
                }
            }

            private void Writer()
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var value = Interlocked.Increment(ref _id);
                    _queue.Enqueue(value);
                    _written.Enqueue(value);
                }
            }

            private void Start()
            {
                _cancellationToken = new CancellationTokenSource(TestDuration).Token;

                foreach (var thread in _threads)
                {
                    thread.Start();
                }

                foreach (var thread in _threads)
                {
                    thread.Join();
                }
            }

            private void Validate()
            {
                var read = new HashSet<int>(_read);
                var outstanding = new HashSet<int>();

                while (_queue.TryDequeue(out var value))
                {
                    outstanding.Add(value);
                }

                while (_written.TryDequeue(out var value))
                {
                    if (!read.Remove(value))
                    {
                        outstanding.Remove(value).Should().BeTrue("Item {0} not present neither in readed nor outstanding elements", value);
                    }
                }

                read.Should().BeEmpty();
                outstanding.Should().BeEmpty();
            }
        }
    }
}