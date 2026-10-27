using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using FluentAssertions;
using Moq;
using VPay.DocSys.Parsing.Common.Interfaces;
using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class CheckFileParserTests
    {
        private readonly ICheckFileParser SUT;

        public CheckFileParserTests()
        {
            var fileSystem = new Mock<IFileSystem>();
            var logger = TestLogger.Create<CheckFileParser>();

            SUT = new CheckFileParser(fileSystem.Object, logger);

            fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(true);
        }

        [Fact]
        public void ShouldBeEmptyForARedCardTpa()
        {
            var actual = SUT.ParseCheckFileAsync("invalidPath", false, true);

            actual.Should()
                .NotBeNull()
                .And.BeEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldThrowExceptionForACustomSendBackTpaWithInvalidCheckXmlFile(bool isRedCardTpa)
        {
            // catching exception instead of crafting a test with an xml file to serialize
            Assert.Throws<FileNotFoundException>(() => SUT.ParseCheckFileAsync("invalidPath", true, isRedCardTpa));
        }
    }
}
