using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using Xunit;

namespace VPay.Distribution.Tests
{
    public class MailFileProcessorTest
    {
        private readonly Mock<IConfiguration> _configuration = new Mock<IConfiguration>();
        private readonly Mock<IFileSystem> _fileSystem = new Mock<IFileSystem>();
        private readonly Mock<IDocSysHttpClient> _docSysClient = new Mock<IDocSysHttpClient>();
        private readonly Mock<IDistributionRepository> _distributionRepository = new Mock<IDistributionRepository>();

        private readonly ApiSettings _apiSettings;
        private readonly Phaze04Settings _phaze04Settings;
        private readonly Mock<ILogger<MailFileProcessor>> _logger = new Mock<ILogger<MailFileProcessor>>();


        private readonly IMailFileProcessor SUT;

        private readonly Mock<IFile> _file = new Mock<IFile>();
        private readonly Mock<IPath> _path = new Mock<IPath>();
        private readonly Mock<IDirectory> _directory = new Mock<IDirectory>();

        private readonly Faker _faker = new Faker();

        public MailFileProcessorTest()
        {
            _fileSystem.Setup(_ => _.File).Returns(_file.Object);
            _fileSystem.Setup(_ => _.Path).Returns(_path.Object);
            _fileSystem.Setup(_ => _.Directory).Returns(_directory.Object);

            _apiSettings = new ApiSettings();
            _phaze04Settings = new Phaze04Settings();

            _logger.Invocations.Clear();

            _logger.Setup(_ => _.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));

            SUT = new MailFileProcessor(
                _configuration.Object,
                _fileSystem.Object,
                _docSysClient.Object,
                _distributionRepository.Object,
                _apiSettings,
                _phaze04Settings,
                _logger.Object);
        }

        [Fact]
        public async Task ShouldLogErrorWhenNoControlFileIsCreated()
        {
            string directory = _faker.System.DirectoryPath();
            string fileName = _faker.System.FileName();
            string filePath= directory;

            _distributionRepository
                .Setup(_ => _.TryUpdateDistributionStatus(It.IsAny<long>(), DistributionMethod.Mail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _distributionRepository
                .Setup(_ => _.UpdateFileStatus(It.IsAny<long>(), FileStatusCode.MailDistBegin, It.IsAny<CancellationToken>()));

            _docSysClient
                .Setup(_ => _.GetDocSysConfig(It.IsAny<string>()))
                .ReturnsAsync(new DocSysConfiguration());

            _path.Setup(_ => _.GetDirectoryName(It.IsAny<string>())).Returns(directory);

            _path.Setup(_ => _.GetFileName(It.IsAny<string>())).Returns(fileName);

            _path.Setup(_ => _.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns(filePath);

            Func<Task> act = async () => await SUT.Process(MockDocDistMsg(filePath), false);
            await act.Should().ThrowAsync<Exception>();

            _logger.Verify(_ => _.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        private static DocDistMsg MockDocDistMsg(string filePath)
        {
            return new DocDistMsg
            {
                TPA = "VPAY",
                BillingEntity = "VPAY001001",
                Origin = "SEMED",
                StatusFilePath = filePath,
            };
        }
    }
}
