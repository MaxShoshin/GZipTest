using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace GZipTest.ConsoleApp
{
    [Serializable]
    public sealed class PipelineException : Exception
    {
        public PipelineException()
        {
        }

        public PipelineException(string message) : base(message)
        {
        }

        public PipelineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private PipelineException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}