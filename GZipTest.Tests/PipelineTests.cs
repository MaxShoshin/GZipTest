using System;
using System.IO;
using FluentAssertions;
using GZipTest.ConsoleApp;
using GZipTest.ConsoleApp.Infrastructure;
using GZipTest.Tests.Mocks;
using Xunit;

namespace GZipTest.Tests
{
    public sealed class PipelineTests
    {
        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void Should_Fail_If_Exception_Occured(bool errorOnRead, bool errorOnTransform, bool errorOnWrite)
        {
            var pipeline = new TestPipeline(errorOnRead, errorOnTransform, errorOnWrite);
            Action process = () => pipeline.Process();

            process.Should().Throw<PipelineException>().WithInnerException<ExpectedException>();
        }

        private sealed class TestPipeline : Pipeline
        {
            private readonly bool _readFail;
            private readonly bool _transformFail;
            private readonly bool _writeFail;

            public TestPipeline(bool readFail, bool transformFail, bool writeFail)
                : base(new MemoryStream(new byte[5]), new MemoryStream(), Settings.Default)
            {
                _readFail = readFail;
                _transformFail = transformFail;
                _writeFail = writeFail;
            }

            protected override Block ReadBlock(int index, PipelineInfo pipelineInfo)
            {
                if (_readFail)
                {
                    throw new ExpectedException();
                }

                return new Block(index, new MemoryStream());
            }

            protected override void TransformStream(Stream sourceStream, Stream destinationStream)
            {
                if (_transformFail)
                {
                    throw new ExpectedException();
                }

                sourceStream.SmartCopyTo(destinationStream);
            }

            protected override void BeforeBlockWrite(Block block, PipelineInfo pipelineInfo)
            {
                if (_writeFail)
                {
                    throw new ExpectedException();
                }
            }

            protected override PipelineInfo Initialize()
            {
                return new PipelineInfo(100, 10);
            }

            protected override void PrepareForWrite(PipelineInfo pipelineInfo)
            {
            }
        }

    }
}