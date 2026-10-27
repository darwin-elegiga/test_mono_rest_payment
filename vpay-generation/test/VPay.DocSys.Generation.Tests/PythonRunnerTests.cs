using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Models;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class PythonRunnerTests
    {
        private readonly IPythonRunner SUT;
        private readonly Mock<IProcessRunner> _processRunner;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly CustomSendBackOperations _customSendBackOperations = new CustomSendBackOperations();

        private string[] args;

        private readonly Faker _faker = new Faker();

        public PythonRunnerTests(ITestOutputHelper outputHelper)
        {
            _fileSystem = new Mock<IFileSystem>();
            _processRunner = new Mock<IProcessRunner>();

            _processRunner.Setup(x => x.RunProcessAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
                .Callback((string a, string b, string[] c) => args = c)
                .ReturnsAsync((string a, string b, string[] c) => new ProcessResult
                {
                    Output = a,
                    ExitCode = 0
                });

            // custom sendbacks
            _processRunner.Setup(x => x.RunProcessAsync(It.IsAny<string>(), false, It.IsAny<string>(), It.IsAny<string[]>()))
                .Callback((string a, bool _, string b, string[] c) => args = c)
                .ReturnsAsync((string a, bool _, string b, string[] c) => new ProcessResult
                {
                    Output = $"{_faker.Random.String2(10)}.pdf {_faker.Random.String2(10)}.pdf",
                    ExitCode = 0
                });

            // vpayge
            _processRunner.Setup(x => x.RunProcessAsync(It.IsAny<string>(), true, It.IsAny<string>(), It.IsAny<string[]>()))
                .Callback((string a, bool _, string b, string[] c) => args = c)
                .ReturnsAsync((string a, bool _, string b, string[] c) => new ProcessResult
                {
                    Output = c[2],
                    ExitCode = 0
                });

            _customSendBackOperations.CheckMaker = "checkmaker";

            var pythonSettings = new PythonSettings
            {
                DefaultPythonExecutable = "python3",
                PythonExecutableMap = new Dictionary<string, string>
                {
                    { "3.8.18", "/home/bogner/.pyenv/versions/3.8.18/bin/python" },
                    { "3", "python3" },
                    { "2", "python2" }
                }
            };


            SUT = new PythonRunner(
                _customSendBackOperations,
                _fileSystem.Object,
                _processRunner.Object,
                TestLogger.Create<PythonRunner>(outputHelper),
                pythonSettings);
        }

        [Theory]
        [InlineData("")]
        [InlineData((string)null)]
        [InlineData("tpaConfigFile")]
        public async Task ShouldUseCorrectArgumentsWhenCreatingVPayge(string tpaConfigFile)
        {
            var scriptName = "scriptName";
            var method = "method";
            var outputFile = "outputFile";
            var coverPageData = new CoverPageData();
            var pythonVersion = "3";

            var result = await SUT.CreateVPayge(scriptName, method, outputFile, coverPageData, tpaConfigFile, pythonVersion);

            _processRunner.Verify(x => x.RunProcessAsync("python3", true, string.Empty, args), Times.Once);

            result.Should().Match(outputFile);

            args.Should()
                .HaveCount(string.IsNullOrEmpty(tpaConfigFile) ? 8 : 10)
                .And.ContainInOrder("/home/bogner/vpayge_maker/vpayge_maker_runner/__main__.py", method, outputFile, "-l", "/SRVFS/bogner/logs/docsys.log", "-f", scriptName);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        public async Task ShouldUseCorrectArgumentsWhenCreatingEob(bool hasAuxFiles, bool usePy3, bool isCheck)
        {
            var scriptName = "scriptName";
            var outputFile = "outputFile";
            var eobXmlFile = "eobXmlFile";
            var auxFiles = hasAuxFiles ? "auxFiles" : null;
            var payType = "payType";
            var pythonVersion = usePy3 ? "3" : "2";
            var overlayId = "overlayId";
            var overlayDate = new DateTime(2023, 12, 12);

            var result = await SUT.CreateEob(scriptName, outputFile, eobXmlFile, auxFiles, payType, pythonVersion, overlayId, overlayDate, isCheck);

            _processRunner.Verify(x => x.RunProcessAsync(usePy3 ? "python3" : "python2", string.Empty, args), Times.Once);

            result.Should().Match(outputFile);

            args.Should()
                .HaveCount(hasAuxFiles ? 13 : 11)
                .And.ContainInOrder("/home/bogner/vpayge_maker/eob_maker_runner/__main__.py", scriptName, outputFile, eobXmlFile, isCheck ? "T" : "F", overlayId, $"{overlayDate:yyyy-MM-dd}", "-l", "/SRVFS/bogner/logs/docsys.log", "-p", payType);

            if (hasAuxFiles)
            {
                args.Should()
                    .ContainInOrder("-a", auxFiles);
            }
        }

        [Fact]
        public async Task ShouldUseCorrectArgumentsWhenOverlayingEob()
        {
            var scriptName = "scriptName";
            var outputFile = "outputFile";
            var overlayId = "1727821443";
            var overlayDate = DateTime.Today;

            var result = await SUT.OverlayEob(scriptName, outputFile, true, overlayId, overlayDate);

            _processRunner.Verify(x => x.RunProcessAsync("python3", string.Empty, args), Times.Once);

            result.Should().Match(outputFile);

            args.Should()
                .ContainInOrder(scriptName, outputFile, "T", overlayId, $"{overlayDate:yyyy-MM-dd}", "-o", outputFile);
        }

        [Fact]
        public async Task ShouldUseCorrectArgumentsWhenCreatingCustomSendBacks()
        {
            var xmlPath = _faker.Random.String2(10);

            var results = await SUT.CreateCustomSendBacks(xmlPath);

            _processRunner.Verify(x => x.RunProcessAsync("python3", false, string.Empty, args), Times.Once);

            results.Should().HaveCount(2);

            args.Should()
                .ContainInOrder(_customSendBackOperations.CheckMaker, xmlPath);
        }

    }
}
