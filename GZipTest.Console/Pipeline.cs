using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GZipTest.ConsoleApp.Infrastructure;
using JetBrains.Annotations;
using Microsoft.IO;

namespace GZipTest.ConsoleApp
{
    internal abstract class Pipeline : IDisposable
    {
        [NotNull] protected static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        [NotNull] private readonly IBlockingQueue<Block> _writeQueue;
        [NotNull] private readonly IBlockingQueue<Block> _transformQueue;
        [CanBeNull] private Exception _error;

        protected Pipeline([NotNull] Stream sourceStream, [NotNull] Stream destinationStream, [NotNull] Settings settings)
            : this(sourceStream, destinationStream, settings,
                new BlockingQueue<Block>(settings.TransformWorkerBounds),
                new BlockingQueue<Block>(settings.WriteWorkerBounds))
        {
        }

        protected Pipeline(
            [NotNull] Stream sourceStream,
            [NotNull] Stream destinationStream,
            [NotNull] Settings settings,
            [NotNull] IBlockingQueue<Block> transformQueue,
            [NotNull] IBlockingQueue<Block> writeQueue)
        {
            if (sourceStream == null) throw new ArgumentNullException(nameof(sourceStream));
            if (destinationStream == null) throw new ArgumentNullException(nameof(destinationStream));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (transformQueue == null) throw new ArgumentNullException(nameof(transformQueue));
            if (writeQueue == null) throw new ArgumentNullException(nameof(writeQueue));

            _transformQueue = transformQueue;
            _writeQueue = writeQueue;

            Settings = settings;

            SourceStream = sourceStream;
            DestinationStream = destinationStream;
        }

        [NotNull]
        protected Settings Settings { get; }

        [NotNull]
        protected Stream SourceStream { get; }

        [NotNull]
        protected Stream DestinationStream { get; }

        [NotNull]
        protected abstract PipelineInfo Initialize();

        [NotNull]
        protected abstract Block ReadBlock(int index, [NotNull] PipelineInfo pipelineInfo);

        protected abstract void TransformStream([NotNull] Stream sourceStream, [NotNull] Stream destinationStream);

        protected abstract void PrepareForWrite([NotNull] PipelineInfo pipelineInfo);

        protected abstract void BeforeBlockWrite([NotNull] Block block, [NotNull] PipelineInfo pipelineInfo);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                (_writeQueue as IDisposable)?.Dispose();
                (_transformQueue as IDisposable)?.Dispose();
            }
        }

        public void Process()
        {
            var pipelineInfo = Initialize();

            var readThread = new Thread(ReadWorker);
            readThread.Start(pipelineInfo);

            var threads = new List<Thread>(Settings.WorkThreadCount);

            for (int i = 0; i < Settings.WorkThreadCount; i++)
            {
                var compressThread = new Thread(TransformWorker);
                threads.Add(compressThread);

                compressThread.Start();
            }

            var writeThread = new Thread(WriteWorker);
            writeThread.Start(pipelineInfo);

            readThread.Join();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            _writeQueue.CompleteAdding();

            writeThread.Join();

            if (_error != null)
            {
                throw new PipelineException("Exception during processing", _error);
            }
        }

        private void ReadWorker(object state)
        {
            try
            {
                var pipelineInfo = (PipelineInfo)state;

                for (var index = 0; index < pipelineInfo.BlockCount; index++)
                {
                    _transformQueue.Enqueue(ReadBlock(index, pipelineInfo));
                }

                _transformQueue.CompleteAdding();
            }
            catch (Exception ex)
            {
                ProcessError(ex);
            }
        }

        private void TransformWorker()
        {
            try
            {
                foreach (var sourceBlock in _transformQueue.GetConsumingEnumerable())
                {
                    var transformedData = MemoryStreamManager.GetStream("Transformed");

                    TransformStream(sourceBlock.Data, transformedData);

                    transformedData.Position = 0;

                    sourceBlock.Data.Dispose();

                    _writeQueue.Enqueue(new Block(sourceBlock.Position, transformedData));
                }
            }
            catch (Exception ex)
            {
                ProcessError(ex);
            }
        }

        private void WriteWorker(object state)
        {
            try
            {
                var pipelineInfo = (PipelineInfo)state;

                PrepareForWrite(pipelineInfo);

                foreach (var block in _writeQueue.GetConsumingEnumerable())
                {
                    BeforeBlockWrite(block, pipelineInfo);

                    block.Data.SmartCopyTo(DestinationStream);

                    block.Data.Dispose();
                }
            }
            catch (Exception ex)
            {
                ProcessError(ex);
            }
        }

        private void ProcessError(Exception exception)
        {
            Interlocked.CompareExchange(ref _error, exception, null);

            _transformQueue.CompleteAdding();
            _writeQueue.CompleteAdding();
        }
    }
}