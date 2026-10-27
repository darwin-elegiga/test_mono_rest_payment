using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Tests.TestModels;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class FaxArchiveMessageProcessorTests
    {
        private readonly MoqFileSystem _fileSystem = new MoqFileSystem();
        private readonly Mock<IStorageManager> _storageManager = new Mock<IStorageManager>();

        private readonly IFaxArchiveMessageProcessor SUT;

        public FaxArchiveMessageProcessorTests()
        {
            _fileSystem.FileMoq
                .Setup(_ => _.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            _fileSystem.DirectoryMoq
                .Setup(_ => _.CreateDirectory(It.IsAny<string>()))
                .Returns(new Mock<IDirectoryInfo>().Object);

            _fileSystem.DirectoryMoq
                .Setup(_ => _.CreateDirectory(It.IsAny<string>()));

            _fileSystem.PathMoq
                .Setup(_ => _.GetFileName(It.IsAny<string>()))
                .Returns(Guid.NewGuid().ToString());

            _fileSystem.PathMoq
                .Setup(_ => _.ChangeExtension(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid().ToString());

            _fileSystem.PathMoq
                .Setup(_ => _.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid().ToString());

            _storageManager
                .Setup(_ => _.CreateArchive(It.IsAny<IArchiveFile>(), It.IsAny<List<IGeneratedFile>>(), false, It.IsAny<DateTime>()))
                .ReturnsAsync(-1L);

            SUT = new FaxArchiveMessageProcessor(
                _fileSystem,
                _storageManager.Object,
                new NullLogger<FaxArchiveMessageProcessor>());
        }

        [Fact]
        public async Task ShouldThrowExceptionOnNullMessage()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => SUT.ProcessMessage(null));
        }

        [Fact]
        public async Task ShouldDoNothingOnEmptyTransactionIdList()
        {
            await SUT.ProcessMessage(new FaxRetryArchiveMessage
            {
                TransactionIds = Array.Empty<long>()
            });

            _fileSystem.FileMoq.Verify(_ => _.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _fileSystem.DirectoryMoq.Verify(_ => _.CreateDirectory(It.IsAny<string>()), Times.Never);
            _fileSystem.DirectoryMoq.Verify(_ => _.Delete(It.IsAny<string>()), Times.Never);
            _fileSystem.PathMoq.Verify(_ => _.GetFileName(It.IsAny<string>()), Times.Never);
            _fileSystem.PathMoq.Verify(_ => _.ChangeExtension(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _fileSystem.PathMoq.Verify(_ => _.Combine(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            _storageManager.Verify(_ =>
                _.CreateArchive(
                    It.IsAny<IArchiveFile>(),
                    It.IsAny<List<IGeneratedFile>>(),
                    It.IsAny<bool>(),
                    It.IsAny<DateTime>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(GetTransactionIds))]
        public async Task ShouldUseOneDirectoryForAllMessages(List<long> transactionIds)
        {
            await SUT.ProcessMessage(new FaxRetryArchiveMessage
            {
                TransactionIds = transactionIds
            });

            _fileSystem.DirectoryMoq.Verify(_ => _.CreateDirectory(It.IsAny<string>()), Times.Once);
            _fileSystem.DirectoryMoq.Verify(_ => _.Delete(It.IsAny<string>(), true), Times.Once);
        }

        [Theory]
        [MemberData(nameof(GetTransactionIds))]
        public async Task ShouldCreateDetailTypeOfText(List<long> transactionIds)
        {
            await SUT.ProcessMessage(new FaxRetryArchiveMessage
            {
                TransactionIds = transactionIds
            });

            _storageManager.Verify(_ =>
                _.CreateArchive(
                    It.IsAny<IArchiveFile>(),
                    It.Is<List<IGeneratedFile>>(l => l.All(f => f.FileDetailType == FileDetailType.Text)),
                    It.IsAny<bool>(),
                    It.IsAny<DateTime>()),
                Times.Exactly(transactionIds.Count));
        }

        [Theory]
        [MemberData(nameof(GetTransactionIds))]
        public async Task ShouldProcessEachTransactionIdSeparately(List<long> transactionIds)
        {
            var count = transactionIds.Count();

            await SUT.ProcessMessage(new FaxRetryArchiveMessage
            {
                TransactionIds = transactionIds
            });

            _fileSystem.FileMoq.Verify(_ => _.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(count));
            _fileSystem.PathMoq.Verify(_ => _.GetFileName(It.IsAny<string>()), Times.Exactly(count));
            _fileSystem.PathMoq.Verify(_ => _.ChangeExtension(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(count));
            _fileSystem.PathMoq.Verify(_ => _.Combine(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(count));

            _storageManager.Verify(_ =>
                _.CreateArchive(
                    It.IsAny<IArchiveFile>(),
                    It.IsAny<List<IGeneratedFile>>(),
                    false,
                    It.IsAny<DateTime>()),
                Times.Exactly(count));
        }

        public static IEnumerable<object[]> GetTransactionIds
        {
            get
            {
                var list = new List<object[]>
                {
                    new[] { new List<long>{ 1L } },
                    new[] { new List<long>{ 1L, 2L, 3L, 4L, 5L } },
                };

                return list;
            }
        }
    }
}
