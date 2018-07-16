using System;
using System.IO;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp.Infrastructure
{
    internal static class StreamExtensions
    {
        private static readonly BufferPool BufferPool = new BufferPool();

        public static void CopyBlockTo([NotNull] this Stream source, [NotNull] Stream destination, int blockSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (blockSize <= 0) throw new ArgumentOutOfRangeException(nameof(blockSize));

            using (var buffer = BufferPool.GetBuffer(Constants.BufferSize))
            {
                int bytesOutstanding = blockSize;

                while (bytesOutstanding > 0)
                {
                    var bytesToRead = Math.Min(buffer.Length, bytesOutstanding);
                    var count = source.Read(buffer.Data, offset: 0, count: bytesToRead);

                    if (count == 0)
                    {
                        return;
                    }

                    destination.Write(buffer.Data, offset: 0, count: count);

                    bytesOutstanding -= count;
                }
            }
        }

        // The same as Stream.CopyTo, but uses pool of buffers
        public static void SmartCopyTo([NotNull] this Stream source, [NotNull] Stream destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using (var buffer = BufferPool.GetBuffer(Constants.BufferSize))
            {
                int count;
                while ((count = source.Read(buffer.Data, offset: 0, count: buffer.Length)) != 0)
                {
                    destination.Write(buffer.Data, offset: 0, count: count);
                }
            }
        }

        public static void ReadExactly([NotNull] this Stream source, [NotNull] byte[] buffer, int byteCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (byteCount <= 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
            if (buffer.Length < byteCount) throw new ArgumentException("Buffer is too small to read specified count of bytes", nameof(buffer));

            var totalRead = 0;
            var bytesOutstanding = byteCount;
            while (bytesOutstanding > 0)
            {
                var count = source.Read(buffer, totalRead, bytesOutstanding);
                if (count == 0)
                {
                    throw new PipelineException("Source file corrupted.");
                }

                bytesOutstanding -= count;
                totalRead += count;
            }
        }
    }
}