using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class DocSysConfigServiceTests
    {
        /// <summary>
        /// must be lower-case-able
        /// </summary>
        private const string _testDataPath = "config_file_test_data/";
        private const string _testEobMergeSettingsPath = _testDataPath + "eob_merge/settings.ini";
        private const string _testEobMergeConfigPath = _testDataPath + "eob_merge/eob_merge.ini";
        private const string _testEobMergeInvalidPath = _testDataPath + "eob_merge/eob_merge_invalid.ini";
        private readonly Mock<ILogger<DocSysConfigService>> _mockLogger;
        private readonly IServiceCollection _testServiceCollection;
        private void VerifyLog(LogLevel expectedLevel, Func<string,bool> isExpectedMessage, Func<Exception,bool> isExpectedException)
        {
            _mockLogger.Verify(
                m => m.Log(
                    It.Is<LogLevel>(l => l.Equals(expectedLevel)),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => isExpectedMessage(v.ToString())),
                    It.Is<Exception>(e => isExpectedException(e)),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()));
        }

        public DocSysConfigServiceTests()
        {
            Assert.True(File.Exists(_testDataPath + "aux1/aux1.ini"));
            Assert.True(File.Exists(_testDataPath + "con1/con1.ini"));
            Assert.True(File.Exists(_testDataPath + "test/test.ini"));
            _mockLogger = new Mock<ILogger<DocSysConfigService>>();
            _testServiceCollection = new ServiceCollection()
                .AddTransient<IConfiguration>(s => new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "StatusFile:PythonHome", _testDataPath },
                        { "StatusFile:EobMergeConfig", _testEobMergeConfigPath },
                        { "StatusFile:EobMergeSettings", _testEobMergeSettingsPath }
                    })
                    .Build())
                .AddTransient(s => _mockLogger.Object)
                .AddTransient<DocSysConfigService, DocSysConfigService>()
                .AddTransient<IDocSysConfigService, DocSysConfigService>();
        }

        [Theory]
        [InlineData("test",
            _testDataPath + "test/test.ini", 3, _testDataPath + "test/config.ini", 3, _testDataPath + "test/test.otb")]
        [InlineData("con",
            _testDataPath + "con1/con1.ini", 3, _testDataPath + "con1/config.ini", 3, _testDataPath + "con1/con1.otb")]
        public void ShouldReadConfig_NoErrors(string tpa, string expectedTpaIni, int expectedEnvCount, string expectedCfg, int expectedAppCount, string expectedOtb)
        {
            // arrange
            var svc = _testServiceCollection
                .BuildServiceProvider();
            var sut = svc
                .GetRequiredService<IDocSysConfigService>();

            // act
            var output = sut.ReadConfig(tpa);

            // assert
            Assert.Equal(expectedEnvCount, output.EnvironmentSettings.Count);
            VerifyLog(LogLevel.Information,
                m => m.Equals($"Started parsing config file [{expectedTpaIni}]"),
                e => true);
            Assert.Equal(tpa.ToUpper(), output.EnvironmentSettings["tpa_code"]);
            Assert.Equal(expectedCfg, output.EnvironmentSettings["tpa_config_file"]);
            Assert.Equal(expectedAppCount, output.AppSettings.Count);
            Assert.Equal(expectedOtb, output.AppSettings["otb_file"]);
        }

        [Theory]
        [InlineData("con",
            "An unexpected exception occurred", _testDataPath + "con/config.ini")]
        public void ShouldReadConfig_IniParserExceptionOnInvalidEobMergeConfig(string tpa, string expectedMsg, string expectedCfg)
        {
            // arrange
            var svc = _testServiceCollection
                .AddTransient<IConfiguration>(s => new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "StatusFile:PythonHome", _testDataPath },
                        { "StatusFile:EobMergeConfig", _testEobMergeInvalidPath },
                        { "StatusFile:EobMergeSettings", _testEobMergeSettingsPath }
                    })
                    .Build())
                .BuildServiceProvider();
            var sut = svc
                .GetRequiredService<IDocSysConfigService>();

            // act
            DocSysConfiguration act() => sut.ReadConfig(tpa);

            // assert
            Assert.Throws<IniParser.Exceptions.ParsingException>(act);
            VerifyLog(LogLevel.Error,
                m => m.Equals(expectedMsg),
                e => e.Message.Contains(expectedCfg));
        }

        [Theory]
        [InlineData("aux",
            "An unexpected exception occurred", _testDataPath + "aux/config.ini")]
        public void ShouldReadConfig_IniParserExceptionOnInvalidTpaIni(string tpa, string expectedMsg, string expectedCfg)
        {
            // arrange
            var svc = _testServiceCollection
                .BuildServiceProvider();
            var sut = svc
                .GetRequiredService<IDocSysConfigService>();

            // act
            DocSysConfiguration act() => sut.ReadConfig(tpa);

            // assert
            Assert.Throws<IniParser.Exceptions.ParsingException>(act);
            VerifyLog(LogLevel.Error,
                m => m.Equals(expectedMsg),
                e => e.Message.Contains(expectedCfg));
        }
    }
}
