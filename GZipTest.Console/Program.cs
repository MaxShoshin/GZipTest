using System;
using System.IO;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp
{
    internal static class Program
    {

        public static int Main(string[] args)
        {
            if (!Arguments.TryParseArguments(args, out var arguments))
            {
                PrintUsage();
                return -1;
            }

            try
            {
                using (var source = new FileStream(arguments.SourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                using (var destination = new FileStream(arguments.DestinationFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, Constants.BufferSize))
                {
                    var settings = Settings.Default;

                    using (var pipeline = arguments.IsCompress
                        ? (Pipeline)new CompressPipeline(source, destination, settings)
                        : new DecompressPipeline(source, destination, settings))
                    {
                        pipeline.Process();
                    }
                }
            }
            catch (IOException ex)
            {
                PrintError(ex);

                return -2;
            }
            catch (PipelineException ex)
            {
                PrintError(ex.InnerException ?? ex);

                return -3;
            }
            catch (Exception ex)
            {
                PrintGeneralError(ex);

                return -4;
            }

            return 0;
        }

        private static void PrintGeneralError([NotNull] Exception exception)
        {
            Console.WriteLine();
            Console.WriteLine(exception);
            Console.WriteLine();
            Console.WriteLine("Unexpected error:");
            Console.WriteLine(exception.Message);
            Console.WriteLine();
        }

        private static void PrintError([NotNull] IOException ioException)
        {
            Console.WriteLine();
            Console.WriteLine("IO Error:");
            Console.WriteLine(ioException.Message);
            Console.WriteLine();
        }

        private static void PrintError([NotNull] Exception pipelineException)
        {
            Console.WriteLine();
            Console.WriteLine("IO Error occured:");
            Console.WriteLine(pipelineException.Message);
            Console.WriteLine();
        }


        private static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Wrong arguments.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("GZipTest.exe compress|decompress sourceFileName resultFileName");
            Console.WriteLine();
        }


        private sealed class Arguments
        {
            public readonly bool IsCompress;
            public readonly string SourceFileName;
            public readonly string DestinationFileName;

            private Arguments(bool isCompress, [NotNull] string sourceFileName, [NotNull] string destinationFileName)
            {
                if (sourceFileName == null) throw new ArgumentNullException(nameof(sourceFileName));
                if (destinationFileName == null) throw new ArgumentNullException(nameof(destinationFileName));

                IsCompress = isCompress;
                SourceFileName = sourceFileName;
                DestinationFileName = destinationFileName;
            }

            [ContractAnnotation("true<=arguments:notnull; args:null=>false; false <= arguments:null")]
            public static bool TryParseArguments([CanBeNull] string[] args, out Arguments arguments)
            {
                arguments = null;

                if (args == null || args.Length != 3)
                {
                    return false;
                }

                var isCompress = string.Equals(args[0], "compress", StringComparison.InvariantCultureIgnoreCase);
                var isDecompress = string.Equals(args[0], "decompress", StringComparison.InvariantCultureIgnoreCase);

                if (!isCompress && !isDecompress)
                {
                    return false;
                }

                string sourceFileName;
                string destinationFileName;
                try
                {
                    sourceFileName = Path.GetFullPath(args[1]);
                    destinationFileName = Path.GetFullPath(args[2]);
                }
                catch(ArgumentException)
                {
                    return false;
                }

                arguments = new Arguments(
                    isCompress,
                    sourceFileName,
                    destinationFileName);

                return true;
            }

        }
    }
}