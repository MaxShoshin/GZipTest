using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GZipTest.ConsoleApp.Infrastructure;
using Xunit;

namespace GZipTest.Tests
{
    public sealed class BlockingQueueTests
    {
        private const int Value = 345;
        private readonly BlockingQueue<int> _queue = new BlockingQueue<int>(2);
        private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(1);

        [Fact]
        public void Should_Add_Complete()
        {
            _queue.CompleteAdding();

            _queue.TryDequeue(out _, Timeout.InfiniteTimeSpan).Should().BeFalse();
        }

        [Fact]
        public void Should_Enumerate_All_Enqueued_Items()
        {
            _queue.TryEnqueue(Value, _timeout);
            _queue.TryEnqueue(Value + 1, _timeout);

            using (var enumerator = _queue.GetConsumingEnumerable().GetEnumerator())
            {
                enumerator.MoveNext();
                enumerator.Current.Should().Be(Value);

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be(Value + 1);
            }
        }

        [Fact]
        public void Should_Enumerate_Outstanding_Items_On_Completed_Queue()
        {
            _queue.TryEnqueue(Value, _timeout);
            _queue.TryEnqueue(Value + 1, _timeout);

            _queue.CompleteAdding();

            using (var enumerator = _queue.GetConsumingEnumerable().GetEnumerator())
            {
                enumerator.MoveNext();
                enumerator.Current.Should().Be(Value);

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be(Value + 1);

                enumerator.MoveNext().Should().BeFalse();
            }
        }

        [Fact]
        public void Should_Not_Enqueue_When_Overload()
        {
            _queue.TryEnqueue(Value, _timeout).Should().BeTrue();
            _queue.TryEnqueue(Value, _timeout).Should().BeTrue();

            _queue.TryEnqueue(Value, _timeout).Should().BeFalse();
        }

        [Fact]
        public void Should_Not_Be_Overloaded_After_Dequeue()
        {
            _queue.TryEnqueue(Value, _timeout);
            _queue.TryEnqueue(Value, _timeout);
            _queue.TryEnqueue(Value, _timeout).Should().BeFalse();

            _queue.TryDequeue(out _, _timeout).Should().BeTrue();
            _queue.TryEnqueue(Value, _timeout).Should().BeTrue();
        }

        [Fact]
        public async Task Should_Enqueue_Value_If_Overloaded()
        {
            const int OverloadedValue = 234;

            _queue.TryEnqueue(Value, _timeout);
            _queue.TryEnqueue(Value, _timeout);

            await EnqueueOverload(OverloadedValue);

            _queue.TryDequeue(out _, _timeout);
            _queue.TryDequeue(out _, _timeout);
            _queue.TryDequeue(out var value, _timeout).Should().BeTrue();
            value.Should().Be(OverloadedValue);
        }

        [Fact]
        public async Task Should_Wait_For_Enqueue_During_Try_Dequeue()
        {
            var taskStarted =  new TaskCompletionSource<bool>();

            var dequeueTask = Task.Run(() =>
            {
                taskStarted.SetResult(true);

                if (!_queue.TryDequeue(out var value, TimeSpan.FromSeconds(2)))
                {
                    throw new Exception();
                }

                return value;
            });

            await taskStarted.Task.ConfigureAwait(false);

            await Task.WhenAny(dequeueTask, Task.Delay(TimeSpan.FromMilliseconds(100))).ConfigureAwait(false);

            dequeueTask.IsCompleted.Should().BeFalse();

            _queue.TryEnqueue(Value, _timeout);

            var dequeuedValue = await dequeueTask.ConfigureAwait(false);
            dequeuedValue.Should().Be(Value);
        }

        [Fact]
        public async Task Should_Raise_Error_During_Enqueue_And_Complete()
        {
            _queue.TryEnqueue(Value, _timeout);
            _queue.TryEnqueue(Value, _timeout);

            var task = await EnqueueOverload(Value);

            _queue.CompleteAdding();

            await Task.WhenAny(task, Task.Delay(100)).ConfigureAwait(false);

            task.IsCompleted.Should().BeTrue();
            task.IsFaulted.Should().BeTrue();
        }

        private async Task<Task> EnqueueOverload(int value)
        {
            var taskStarted =  new TaskCompletionSource<bool>();
            var enqueueOverloadedTask = Task.Run(() =>
            {
                taskStarted.SetResult(true);
                return _queue.TryEnqueue(value, TimeSpan.FromSeconds(2));
            });

            await taskStarted.Task.ConfigureAwait(false);

            await Task.WhenAny(enqueueOverloadedTask, Task.Delay(_timeout));

            enqueueOverloadedTask.IsCompleted.Should().BeFalse();

            return enqueueOverloadedTask;
        }
    }
}