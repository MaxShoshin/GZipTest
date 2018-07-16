using System;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp.Infrastructure
{
    // I know about BitConverter, but it:
    // 1. creates new array every time and
    // 2. its result may be differ on different platform (little/big endian)
    internal class IndependentBitConverter
    {
        public static void WriteBytes(int value, [NotNull] byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < 4) throw new ArgumentException("Buffer has insufficient length", nameof(buffer));

            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
        }

        public static void WriteBytes(long value, [NotNull] byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < 8) throw new ArgumentException("Buffer has insufficient length", nameof(buffer));

            var intValue = (int)value;
            buffer[0] = (byte)intValue;
            buffer[1] = (byte)(intValue >> 8);
            buffer[2] = (byte)(intValue >> 16);
            buffer[3] = (byte)(intValue >> 24);

            intValue = (int)(value >> 32);
            buffer[4] = (byte)intValue;
            buffer[5] = (byte)(intValue >> 8);
            buffer[6] = (byte)(intValue >> 16);
            buffer[7] = (byte)(intValue >> 24);
        }

        public static long ReadInt64([NotNull] byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < 8) throw new ArgumentException("Buffer has insufficient length", nameof(buffer));

            var value = (long) (buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24) |
                        (long) (buffer[4] | buffer[5] << 8 | buffer[6] << 16 | buffer[7] << 24) << 32;

            return value;
        }

        public static int ReadInt32([NotNull] byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < 4) throw new ArgumentException("Buffer has insufficient length", nameof(buffer));

            return buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24;
        }
    }
}