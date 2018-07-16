using System;
using System.IO;
using GZipTest.ConsoleApp;
using JetBrains.Annotations;

namespace GZipTest.Tests.Utils
{
    internal sealed class Compare
    {
        public static bool StreamContent([NotNull] Stream stream1, [NotNull] Stream stream2)
        {
            if (stream1 == null) throw new ArgumentNullException(nameof(stream1));
            if (stream2 == null) throw new ArgumentNullException(nameof(stream2));

            if (stream1.Length != stream2.Length)
            {
                return false;
            }

            var buffer1 = new byte[Constants.BufferSize];
            var buffer2 = new byte[Constants.BufferSize];

            int read1;
            do
            {
                read1 = stream1.Read(buffer1, 0, Constants.BufferSize);
                var read2 = stream2.Read(buffer2, 0, Constants.BufferSize);

                // TODO: Probably following can be in normal situation, but I never see it
                if (read1 != read2)
                {
                    return false;
                }

                for (int i = 0; i < read1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                    {
                        return false;
                    }
                }

            } while (read1 == Constants.BufferSize);

            return true;
        }
    }
}