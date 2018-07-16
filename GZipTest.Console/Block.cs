using System;
using System.IO;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp
{
    internal sealed class Block
    {
        public readonly int Position;

        [NotNull] public readonly MemoryStream Data;

        public Block(int position, [NotNull] MemoryStream data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            Position = position;
            Data = data;
        }
    }
}