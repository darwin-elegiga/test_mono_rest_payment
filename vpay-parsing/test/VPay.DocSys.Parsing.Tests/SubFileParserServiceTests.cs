using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Tests.Helpers;
using VPay.DocSys.Parsing.Tests.TestModels;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class SubFileParserServiceTests
    {

        [Fact]
        public void ParseData_WithValidFileData_ShouldReturnDictionaryByTXID()
        {
            var logger = TestLogger.Create<SubFileParserService>();
            var sut = new SubFileParserService(logger);
            IDictionary<string, SubstituteDataInfo> actual;


            var fileString = SubstituteTestModels.ValidTestFile;

            using (var steam = GenerateStreamFromString(fileString))
            using (var reader = new StreamReader(steam, Encoding.UTF8, true))
            {
                actual = sut.ParseData(reader);
            }

            var expected = SubstituteTestModels.ExpectedTestFileData();

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ParseData_WithInvalidHeader_ShouldThrowInvalidFileFormatException()
        {
            var logger = TestLogger.Create<SubFileParserService>();
            var sut = new SubFileParserService(logger);
            Action act;

            var fileString = SubstituteTestModels.InvalidHeaderTestFile;

            using (var steam = GenerateStreamFromString(fileString))
            using (var reader = new StreamReader(steam, Encoding.UTF8, true))
            {
                act = () => sut.ParseData(reader);

                act.Should().Throw<InvalidFileFormatException>()
                    .WithMessage("First Record in sub file is not \"00|\"");

            }
        }

        [Fact]
        public void ParseData_WithInvalidRecordType_ShouldThrowInvalidFileFormatException()
        {
            var logger = TestLogger.Create<SubFileParserService>();
            var sut = new SubFileParserService(logger);
            Action act;

            var fileString = SubstituteTestModels.InvalidRecordTypeTestFile;

            using (var steam = GenerateStreamFromString(fileString))
            using (var reader = new StreamReader(steam, Encoding.UTF8, true))
            {
                act = () => sut.ParseData(reader);

                act.Should().Throw<InvalidFileFormatException>()
                    .WithMessage("Record 2 has no \"23|\" intro");

            }
        }

        [Fact]
        public void ParseData_WithInvalidRecordLength_ShouldThrowInvalidFileFormatException()
        {
            var logger = TestLogger.Create<SubFileParserService>();
            var sut = new SubFileParserService(logger);
            Action act;

            var fileString = SubstituteTestModels.InvalidRecordLengthTestFile;

            using (var steam = GenerateStreamFromString(fileString))
            using (var reader = new StreamReader(steam, Encoding.UTF8, true))
            {
                act = () => sut.ParseData(reader);

                act.Should().Throw<InvalidFileFormatException>()
                    .WithMessage("Record 2 has bad field count (23 sb 22)");

            }
        }

        [Fact]
        public void ParseData_WithDuplicateTXID_ShouldThrowInvalidFileFormatException()
        {
            var logger = TestLogger.Create<SubFileParserService>();
            var sut = new SubFileParserService(logger);
            Action act;

            var fileString = SubstituteTestModels.DuplicateTXIDTestFile;

            using (var steam = GenerateStreamFromString(fileString))
            using (var reader = new StreamReader(steam, Encoding.UTF8, true))
            {
                act = () => sut.ParseData(reader);

                act.Should().Throw<InvalidFileFormatException>()
                    .WithMessage("*** DUPLICATE (txid: 40539131)");

            }
        }


        public static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

    }
}
