using Bogus;
using System.IO.Abstractions;
using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;
using System;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

using Xunit;

namespace VPay.Distribution.Tests
{
    public class FaxFileProcessorTests
    {
        private readonly Mock<IFileSystem> _fileSystem = new Mock<IFileSystem>();
        private readonly Mock<IFile> _file = new Mock<IFile>();
        private readonly Mock<IPath> _path = new Mock<IPath>();
        private readonly Mock<IDirectory> _directory = new Mock<IDirectory>();

        private const string STSLines = @"00COM       SE        origin                                        FS49180069  000000000000000000000000.020180515015642PsedVP0322182_hung02_68_SWKPF01001.txt                                                                                                                                                                                                                           Bogner Load File                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
50001800808748HMEMAX0800000000000236971421            1000      RELEASE                       CLEFT     0000100358000010491                201904092000057644569       claimDateSOUTH SHORE ORTHOPEDICS LLC   000000000000540000fa4bb1fa-0b4c-4cca-90ac-c35a385651e6              M201804090000                                                                                                                                                                                                                                                                IC                                                SWK       SWKPF01001          |RR50668035             FSSI      N                                                                                                                                                                                                                                                                                                                                                                               ";

        private FaxFileProcessor _ops;

        private static readonly Faker faker = new Faker();

        public FaxFileProcessorTests()
        {
            _fileSystem.Setup(_ => _.File).Returns(_file.Object);
            _fileSystem.Setup(_ => _.Directory).Returns(_directory.Object);
            _fileSystem.Setup(_ => _.Path).Returns(_path.Object);

            _path
                .Setup(_ => _.DirectorySeparatorChar)
                .Returns('/');

            _path
                .Setup(_ => _.GetDirectoryName(It.IsAny<string>()))
                .Returns((string s) => s.Substring(0, s.LastIndexOf('/')));

            _path
                .Setup(_ => _.Combine(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string, string>((a, b, c) => string.Join('/', a, b, c));

            _ops = new FaxFileProcessor(
                Mock.Of<IConfiguration>(),
                _fileSystem.Object,
                Mock.Of<IDocSysHttpClient>(),
                Mock.Of<IDistributionRepository>(),
                new ApiSettings(),
                new NullLogger<FaxFileProcessor>());
        }

        [Theory]
        [InlineData("TPA", "/SRVFS/bogner/transfer/ORIGIN/TPA/YYYY/MM/DD/name_of_sts_file.sts")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void ShouldConstructBasePath(string tpa, string stsFilePath)
        {
            if (string.IsNullOrEmpty(stsFilePath))
            {
                Assert.Throws<ArgumentException>(() => _ops.ConstructBasePath(new DocDistMsg()
                {
                    StatusFilePath = stsFilePath,
                    TPA = tpa
                }));
            }
            else
            {
                var expectedPath = _path.Object.GetDirectoryName(stsFilePath);

                var actualPath = _ops.ConstructBasePath(new DocDistMsg()
                {
                    StatusFilePath = stsFilePath,
                    TPA = tpa
                });


                actualPath.Should().Be(expectedPath);
            }
        }
    }
}
