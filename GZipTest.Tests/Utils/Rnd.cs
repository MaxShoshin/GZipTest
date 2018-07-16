using System;
using System.IO;
using GZipTest.ConsoleApp;
using GZipTest.ConsoleApp.Infrastructure;
using JetBrains.Annotations;

namespace GZipTest.Tests.Utils
{
    public static class Rnd
    {
        private static readonly Random Generator = new Random();
        private static readonly byte[] Buffer = new byte[Constants.BufferSize];

        public static void FillStream([NotNull] Stream stream, long size)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var outstandingBytes = size;
            while (outstandingBytes > 0)
            {
                Generator.NextBytes(Buffer);

                var writeByteCount = (int)Math.Min(outstandingBytes, Buffer.Length);
                stream.Write(Buffer, 0, writeByteCount);

                outstandingBytes -= writeByteCount;
            }

            stream.Seek(0, SeekOrigin.Begin);
        }
    }
}