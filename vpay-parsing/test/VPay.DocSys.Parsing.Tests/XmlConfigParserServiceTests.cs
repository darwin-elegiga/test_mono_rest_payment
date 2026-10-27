using Bogus.DataSets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class XmlConfigParserServiceTests
    {
        private readonly Mock<IDocSysConfigService> _docSysConfigService = new Mock<IDocSysConfigService>();
        private readonly Mock<IFileSystem> _fileSystem = new Mock<IFileSystem>();
        private readonly NullLogger<XmlConfigParserService> _logger = new NullLogger<XmlConfigParserService>();

        private readonly IXmlConfigParserService SUT;

        private DocSysConfiguration _docSysConfiguration;
        private Mock<IFile> _file = new Mock<IFile>();

        public XmlConfigParserServiceTests()
        {
            _docSysConfiguration = new DocSysConfiguration
            {
                AppSettings = new Dictionary<string, string>
                {
                    { "required_vpayge_config_list", "" }
                }
            };

            _docSysConfigService
                .Setup(_ => _.ReadConfig(It.IsAny<string>()))
                .Returns(_docSysConfiguration);

            _file.Invocations.Clear();
            _fileSystem.Invocations.Clear();

            var xml = "<?xml version='1.0' encoding='UTF-8'?><Configuration><common></common><vcard></vcard><check></check><eft></eft><npf></npf></Configuration>".ToCharArray().Select(Convert.ToByte).ToArray();
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { "config.xml", new MockFileData(xml) }
            });

            _file
                .Setup(_ => _.Exists(It.IsAny<string>()))
                .Returns(true);

            _file
                .Setup(_ => _.OpenRead(It.IsAny<string>()))
                .Returns(() => fileSystem.File.OpenRead("config.xml"));

            _fileSystem.Setup(_ => _.File).Returns(_file.Object);

            SUT = new XmlConfigParserService(
                _docSysConfigService.Object,
                _fileSystem.Object,
                _logger);
        }

        [Fact]
        public void ShouldThrowExceptionForNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new XmlConfigParserService(null, _fileSystem.Object, _logger));
            Assert.Throws<ArgumentNullException>(() => new XmlConfigParserService(_docSysConfigService.Object, null, _logger));
            Assert.Throws<ArgumentNullException>(() => new XmlConfigParserService(_docSysConfigService.Object, _fileSystem.Object, null));
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null, null)]
        [InlineData(typeof(ArgumentException), "", null)]
        [InlineData(typeof(ArgumentNullException), "TEST", null)]
        [InlineData(typeof(ArgumentException), "TEST", "")]
        public void ShouldThrowExceptionForInvalidParameters(Type ex, string tpa, string be)
        {
            Assert.Throws(ex, () => SUT.ReadXmlConfiguration(tpa, be));
        }

        [Theory]
        [MemberData(nameof(BadAppSettings))]
        public void ThrowExceptionOnBadDocSysAppSettings(Type ex, DocSysConfiguration docSysConfiguration)
        {
            _docSysConfigService
                .Setup(_ => _.ReadConfig(It.IsAny<string>()))
                .Returns(docSysConfiguration);

            Assert.Throws(ex, () => SUT.ReadXmlConfiguration("test", "test"));
        }

        [Fact]
        public void NoConfigurationEntriesForRequiredListOrBeCodeReturnEmptyConfiguration()
        {
            var actual = SUT.ReadXmlConfiguration("tpa", "be");

            Assert.Empty(actual.Common);
            Assert.Empty(actual.Check);
            Assert.Empty(actual.CLCCK);
            Assert.Empty(actual.CLCCP);
            Assert.Empty(actual.CLCFT);
            Assert.Empty(actual.CLVCH);
            Assert.Empty(actual.EFT);
            Assert.Empty(actual.NPF);
            Assert.Empty(actual.VCard);
        }

        [Fact]
        public void NoConfigurationEntriesForRequiredListOrBeCodeMeansNoFileAccess()
        {
            var actual = SUT.ReadXmlConfiguration("tpa", "be");

            _file.Verify(_ => _.Exists(It.IsAny<string>()), Times.Never);
            _file.Verify(_ => _.OpenRead(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(BeDictionary))]
        public void OnlyOpenFilesForValidBeCodes(Dictionary<string, string> beDictionary)
        {
            _docSysConfiguration.AppSettings = _docSysConfiguration.AppSettings
                .Union(beDictionary)
                .ToDictionary(k => k.Key, v => v.Value);

            var actual = SUT.ReadXmlConfiguration("tpa", "be1234");

            _file.Verify(_ => _.Exists(It.IsAny<string>()), Times.Exactly(beDictionary.Count));

            foreach (var v in beDictionary.Values)
            {
                _file.Verify(_ => _.OpenRead(v), Times.AtLeastOnce);
            }
        }

        [Fact]
        public void CheckForLowerCaseBeCodeValues()
        {
            _docSysConfiguration.AppSettings.Add("vpayge_config_be123", "fakepath123");

            var actual = SUT.ReadXmlConfiguration("tpa", "BE1234");

            _file.Verify(_ => _.Exists(It.IsAny<string>()), Times.Exactly(1));
            _file.Verify(_ => _.OpenRead(It.IsAny<string>()), Times.AtLeast(1));
        }


        public static IEnumerable<object[]> BadAppSettings
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { typeof(NullReferenceException), null },
                    new object[] { typeof(NullReferenceException), new DocSysConfiguration { AppSettings= null } },
                    new object[] { typeof(KeyNotFoundException), new DocSysConfiguration { AppSettings= new Dictionary<string, string> { { "k", "v" } } } },
                };
            }
        }

        public static IEnumerable<object[]> BeDictionary
        {
            get
            {
                var dictionary = new Dictionary<string, string>
                {
                    { "vpayge_config_be", "fakepath" }
                };
                yield return new object[] { dictionary };

                dictionary.Add("vpayge_config_be1", "fakepath1");
                yield return new object[] { dictionary };

                dictionary.Add("vpayge_config_be123", "fakepath123");
                yield return new object[] { dictionary };
            }
        }
    }
}
