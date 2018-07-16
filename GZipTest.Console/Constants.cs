using System;

namespace GZipTest.ConsoleApp
{
    public class Constants
    {
        // In feature version of framework it will be Timeout.InfiniteTimeStamp
        public static readonly TimeSpan InfiniteTimeout = TimeSpan.FromMilliseconds(-1);

        // Magic number from Stream.CopyTo method
        public static int BufferSize = 81920;
    }
}