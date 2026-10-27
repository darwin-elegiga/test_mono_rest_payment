using System;
using VPay.DocSys.Storage;
using VPay.DocSys.Generation.Common.Helpers;
using Xunit;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.DocSys.Generation.Common.Exceptions;
using VPay.DocSys.Common;
using MySql.Data.MySqlClient;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace VPay.DocSys.Generation.Api.Tests
{
    public class CleanDocsServiceTests
    {
        #region constants, constructors, privates

        private const string TEST_STATUS_FILE_PATH = "one/two/R20120817_0000003.sts";

        private const long TEST_ARCHIVE_FILE_ID = 12345;
        private const long TEST_ARCHIVE_FILE_TXID = 876543;
        private const string TEST_ARCHIVE_FILE_NAME = "TESTME.ZIP";
        private const string TEST_ARCHIVE_FILE_PATH = "one/two/three/";
        private const string TEST_ARCHIVE_FILE_GUID = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA";
        private const long TEST_ARCHIVE_DETAIL_ID = 123456;

        private static IArchiveDetail TEST_ARCHIVE_DETAIL = new ArchiveDetail
        {
            ArchiveFileId = TEST_ARCHIVE_FILE_ID,
            ArchiveDetailId = TEST_ARCHIVE_DETAIL_ID,
            TransactionId = TEST_ARCHIVE_FILE_TXID,
        };

        private static IArchiveFile TEST_ARCHIVE_FILE = new ArchiveFile
        {
            ArchiveFileId = TEST_ARCHIVE_FILE_ID,
            ArchiveDetails = new List<IArchiveDetail> { TEST_ARCHIVE_DETAIL },
            TransactionId = TEST_ARCHIVE_FILE_TXID,
            FileName = TEST_ARCHIVE_FILE_NAME,
            PathName = TEST_ARCHIVE_FILE_PATH,
            DocumentUniqueId = new Guid(TEST_ARCHIVE_FILE_GUID),
        };

        private const long TEST_REISSUE_ARCHIVE_FILE_ID = 23456;
        private const long TEST_REISSUE_ARCHIVE_FILE_TXID = 1003314691;
        private const string TEST_REISSUE_ARCHIVE_FILE_NAME = "NEWTESTME.ZIP";
        private const string TEST_REISSUE_ARCHIVE_FILE_PATH = "one/two/four";
        private static string TEST_REISSUE_ARCHIVE_FILE_FULL_PATH = System.IO.Path.Combine(TEST_REISSUE_ARCHIVE_FILE_PATH, TEST_REISSUE_ARCHIVE_FILE_NAME);
        private const string TEST_REISSUE_ARCHIVE_FILE_GUID = "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF";
        private const long TEST_REISSUE_ARCHIVE_DETAIL_ID = 234567;

        private static IArchiveDetail TEST_REISSUE_ARCHIVE_DETAIL = new ArchiveDetail
        {
            ArchiveFileId = TEST_REISSUE_ARCHIVE_FILE_ID,
            ArchiveDetailId = TEST_REISSUE_ARCHIVE_DETAIL_ID
        };

        private static IArchiveFile TEST_REISSUE_ARCHIVE_FILE = new ArchiveFile
        {
            ArchiveFileId = TEST_REISSUE_ARCHIVE_FILE_ID,
            ArchiveDetails = new List<IArchiveDetail> { TEST_REISSUE_ARCHIVE_DETAIL },
        };

        private static DeletionTarget TEST_DELETION_TARGET = new DeletionTarget
        {
            archive_file_id = TEST_REISSUE_ARCHIVE_FILE_ID,
            detailCount = 1,
            archive_name = TEST_REISSUE_ARCHIVE_FILE_NAME,
            archive_path = TEST_REISSUE_ARCHIVE_FILE_PATH,
            guid = TEST_REISSUE_ARCHIVE_FILE_GUID,
            TransactionId = TEST_REISSUE_ARCHIVE_FILE_TXID,
        };

        private static CleanDocsRequestResult TEST_CLEAN_DOCS_REQUEST_RESULT = new CleanDocsRequestResult
        {
            PathToDelete = TEST_REISSUE_ARCHIVE_FILE_FULL_PATH,
            ArchiveFileId = TEST_REISSUE_ARCHIVE_FILE_ID,
            RequestId = 123,
            StatusFilePath = TEST_STATUS_FILE_PATH,
        };

        private static Dictionary<string, int> TEST_DELETE_RESULT = new Dictionary<string, int>
        {
            {"archives", 1},
            {"details", 1},
        };

        public CleanDocsServiceTests(ITestOutputHelper output)
        {
            _file = new Mock<IFile>(MockBehavior.Strict);
            _path = new Mock<IPath>(MockBehavior.Strict);
            _directory = new Mock<IDirectory>(MockBehavior.Strict);

            _fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _fileSystem.Setup(s => s.File).Returns(_file.Object);
            _fileSystem.Setup(s => s.Path).Returns(_path.Object);
            _fileSystem.Setup(s => s.Directory).Returns(_directory.Object);

            _storageManager = new Mock<IStorageManager>(MockBehavior.Strict);
            _cleandocOps = new Mock<ICleanDocsOperations>(MockBehavior.Strict);

            _services = new ServiceCollection()
                .AddLogging(l => l.AddProvider(new Utilities.XUnitLoggerProvider(output)))
                .AddTransient<IFileSystem>(s => _fileSystem.Object)
                .AddTransient<IStorageManager>(s => _storageManager.Object)
                .AddTransient<ICleanDocsOperations>(s => _cleandocOps.Object)
                .AddTransient<CleanDocsService>()
                .BuildServiceProvider();
        }

        protected readonly Mock<IFile> _file;
        protected readonly Mock<IPath> _path;
        protected readonly Mock<IDirectory> _directory;
        protected readonly Mock<IFileSystem> _fileSystem;
        protected readonly Mock<IStorageManager> _storageManager;
        protected readonly Mock<ICleanDocsOperations> _cleandocOps;
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _services;

        private void MockFileSystemDirectory()
        {
            _directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns<string>(s => System.IO.Directory.Exists(s));
        }

        private void MockFileSystemPath()
        {
            _path.Setup(p => p.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns<string>(s => System.IO.Path.GetFileNameWithoutExtension(s));
            _path.Setup(p => p.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(s => System.IO.Path.GetDirectoryName(s));
            _path.Setup(p => p.GetFileName(It.IsAny<string>()))
                .Returns<string>(s => System.IO.Path.GetFileName(s));
            _path.Setup(p => p.Combine(It.IsAny<string[]>()))
                .Returns<string[]>(s => System.IO.Path.Combine(s));
            _path.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((s1, s2) => System.IO.Path.Combine(s1, s2));
            _path.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string, string>((s1, s2, s3) => System.IO.Path.Combine(s1, s2, s3));
            _path.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string, string, string>((s1, s2, s3, s4) => System.IO.Path.Combine(s1, s2, s3, s4));
            _path.Setup(p => p.DirectorySeparatorChar)
                .Returns(System.IO.Path.DirectorySeparatorChar);
        }

        private void MockFileSystemFileReadAllLines(string statusFilePath)
        {
            _file
                .Setup(f => f.ReadAllLines(
                    It.Is<string>(s => s.Equals(statusFilePath, StringComparison.OrdinalIgnoreCase))))
                .Returns(System.IO.File.ReadAllLines(
                    System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "TestFiles",
                        "test.sts")));
        }

        private void MockFileSystemFileExistsDeleteExists(string archiveFilePath)
        {
            _file
                .Setup(f => f.Exists(
                    It.Is<string>(s => s.Equals(archiveFilePath, StringComparison.OrdinalIgnoreCase))))
                .Returns(true);
            _file
                .Setup(f => f.Delete(
                    It.Is<string>(s => s.Equals(archiveFilePath, StringComparison.OrdinalIgnoreCase))))
                .Callback<string>(arg =>
                {
                    _file
                        .Setup(f => f.Exists(
                            It.Is<string>(s => s.Equals(archiveFilePath, StringComparison.OrdinalIgnoreCase))))
                        .Returns(false);
                });
        }

        private void MockCleanReissueReferencesFromOriginalArchives(
            List<IArchiveDetail> onGetArchiveDetailsForReissuedToFiles,
            bool onDeleteArchiveDetailFromArchiveFile,
            int onDeleteReissueDataByReissue)
        {
            _storageManager
                .Setup(o => o.GetArchiveDetailsForReissuedToFiles(
                    It.IsAny<IList<long>>(), It.IsAny<bool>()))
                .ReturnsAsync(onGetArchiveDetailsForReissuedToFiles);
            _storageManager
                .Setup(o => o.DeleteArchiveDetailFromArchiveFile(
                    It.IsAny<IArchiveDetail>()))
                .ReturnsAsync(onDeleteArchiveDetailFromArchiveFile);
            _storageManager
                .Setup(o => o.DeleteReissueDataByReissue(
                    It.IsAny<long>()))
                .ReturnsAsync(onDeleteReissueDataByReissue);

            MockFileSystemPath();
        }

        private void MockCleanDocs(
            List<DeletionTarget> onGetArchivesToDelete,
            Dictionary<string, int> onDeleteDatabaseRecords,
            List<IArchiveDetail> onGetArchiveDetailsForReissuedToFiles,
            bool onDeleteArchiveDetailFromArchiveFile,
            int onDeleteReissueDataByReissue,
            string statusFilePath,
            string fullArchiveFilePath)
        {
            _storageManager
                .Setup(o => o.GetArchivesToDelete(
                    It.IsAny<CleanParams>(), It.IsAny<bool>()))
                .ReturnsAsync(onGetArchivesToDelete);
            _cleandocOps
                .Setup(o => o.DeleteDatabaseRecords(
                    It.IsAny<List<long>>()))
                .ReturnsAsync(onDeleteDatabaseRecords);

            MockCleanReissueReferencesFromOriginalArchives(
                onGetArchiveDetailsForReissuedToFiles,
                onDeleteArchiveDetailFromArchiveFile,
                onDeleteReissueDataByReissue);

            MockFileSystemPath();

            MockFileSystemFileReadAllLines(statusFilePath);

            MockFileSystemFileExistsDeleteExists(fullArchiveFilePath);
        }

        private void MockCleanSingleTransaction(
            List<DeletionTarget> onGetArchivesToDelete,
            Dictionary<string, int> onDeleteDatabaseRecords,
            List<IArchiveDetail> onGetArchiveDetailsForReissuedToFiles,
            bool onDeleteArchiveDetailFromArchiveFile,
            int onDeleteReissueDataByReissue,
            string statusFilePath,
            string fullArchiveFilePath)
        {
            _storageManager
                .Setup(o => o.GetArchivesToDelete(
                    It.IsAny<CleanParams>(), It.IsAny<bool>()))
                .ReturnsAsync(onGetArchivesToDelete);
            _cleandocOps
                .Setup(o => o.DeleteDatabaseRecords(
                    It.IsAny<List<long>>()))
                .ReturnsAsync(onDeleteDatabaseRecords);

            MockCleanReissueReferencesFromOriginalArchives(
                onGetArchiveDetailsForReissuedToFiles,
                onDeleteArchiveDetailFromArchiveFile,
                onDeleteReissueDataByReissue);

            MockFileSystemPath();

            MockFileSystemFileReadAllLines(statusFilePath);

            MockFileSystemFileExistsDeleteExists(fullArchiveFilePath);
        }

        private void MockCleanDocsRequestInsert(
            List<DeletionTarget> onGetArchivesToDelete,
            List<CleanDocsRequestResult> onCleanDocsRequestInsert,
            string statusFilePath,
            string fullArchiveFilePath)
        {
            _storageManager
                .Setup(o => o.GetArchivesToDelete(
                    It.IsAny<CleanParams>(), It.IsAny<bool>()))
                .ReturnsAsync(onGetArchivesToDelete);
            _cleandocOps
                .Setup(o => o.CleanDocsRequestInsert(
                    It.IsAny<string>(), It.IsAny<IList<long>>()))
                .ReturnsAsync(onCleanDocsRequestInsert);

            MockFileSystemPath();

            MockFileSystemFileReadAllLines(statusFilePath);

            MockFileSystemFileExistsDeleteExists(fullArchiveFilePath);
        }

        private void MockCleanDocsRequestDelete(
            Dictionary<long, IArchiveFile> onGetArchiveForArchiveFileId,
            StateManagementCleanDocsResult onCleanPaymentStatusFile,
            Dictionary<string, int> onCleanDocsRequestDelete,
            List<IArchiveDetail> onGetArchiveDetailsForReissuedToFiles,
            bool onDeleteArchiveDetailFromArchiveFile,
            int onDeleteReissueDataByReissue,
            string statusFilePath,
            string fullArchiveFilePath)
        {
            _storageManager
                .Setup(o => o.GetArchiveForArchiveFileId(
                    It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync((IArchiveFile)null);
            foreach (var pair in onGetArchiveForArchiveFileId)
            {
                _storageManager
                    .Setup(o => o.GetArchiveForArchiveFileId(
                        It.Is<long>(i => i.Equals(pair.Key)), It.IsAny<bool>()))
                    .ReturnsAsync(pair.Value);
            }
            _storageManager
                .Setup(o => o.CleanPaymentStatusFile(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(onCleanPaymentStatusFile);
            _cleandocOps
                .Setup(o => o.CleanDocsRequestDelete(
                    It.IsAny<int>()))
                .ReturnsAsync(onCleanDocsRequestDelete);

            MockCleanReissueReferencesFromOriginalArchives(
                onGetArchiveDetailsForReissuedToFiles,
                onDeleteArchiveDetailFromArchiveFile,
                onDeleteReissueDataByReissue);

            MockFileSystemPath();

            MockFileSystemDirectory();

            MockFileSystemFileReadAllLines(statusFilePath);

            MockFileSystemFileExistsDeleteExists(fullArchiveFilePath);

            MockFileSystemFileExistsDeleteExists(statusFilePath.GetAuxiliaryFilePathFromStatusFilePath());
        }

        #endregion constants, constructors, privates

        [Theory]
        [InlineData("MasterArchiveSearch", "MASTER-REISSUE.%.R20120817_0000003' drop database document_system '")]
        [InlineData("Guids", null, "ffffffff-ffff-ffff-ffff-ffffffffffff' drop database document_system '")]
        [InlineData("Guids", null, "")]
        [InlineData("Guids", null, "MASTER-REISSUE.%.R20120817_0000003")]
        public void ShouldFailCleanParamsValidation(string expectedFailArgument, string search, params string[] guids)
        {
            Assert.Throws<ArgumentException>(
                expectedFailArgument,
                () => new CleanParams { MasterArchiveSearch = search, Guids = guids.ToList() });
        }

        [Theory]
        [InlineData("MASTER-REISSUE.%.R20120817_0000003", "ffffffff-ffff-ffff-ffff-ffffffffffff", "74151eec-f277-4745-ae20-a97eaa07c301")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData(null, "ffffffff-ffff-ffff-ffff-ffffffffffff")]
        public void ShouldPassCleanParamsValidation(string search, params string[] guids)
        {
            var cpVerified = new CleanParams { MasterArchiveSearch = search, Guids = guids.ToList() };

            Assert.Equal(search, cpVerified.MasterArchiveSearch);
            for (int i = 0; i < guids.Length; i++)
            {
                Assert.Equal(guids[i], cpVerified.Guids[i]);
            }
        }

        [Theory]
        [InlineData("PFisabadfile.sts", true, 1,
            "statusFilePath")]
        [InlineData("REALLYSQLINJECTION'; delete * from document_system_archive_file; select 1 where ''='", true, 1,
            "MasterArchiveSearch")]
        public async Task ShouldFailValidation(string statusFilePath, bool confirmDeletes, int recordCount,
            string expectedArgument)
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            MockFileSystemPath();
            MockFileSystemFileReadAllLines(statusFilePath);

            // act
            var act = new Func<Task>(async () => await svc.CleanDocs(statusFilePath, confirmDeletes, recordCount));

            // assert
            await Assert.ThrowsAsync<ArgumentException>(expectedArgument, act);
        }

        [Fact]
        public async Task ShouldPassCleanDocs()
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            MockCleanDocs(
                new List<DeletionTarget> { TEST_DELETION_TARGET },
                TEST_DELETE_RESULT,
                new List<IArchiveDetail> { TEST_REISSUE_ARCHIVE_DETAIL },
                true,
                1,
                TEST_STATUS_FILE_PATH,
                TEST_REISSUE_ARCHIVE_FILE_FULL_PATH);

            // act
            var act = await svc.CleanDocs(TEST_STATUS_FILE_PATH, true, 1);

            // assert
            Assert.Equal(1, act.DeletedFileCount);
            Assert.Equal(1, act.DeletedArchiveCount);
            Assert.Equal(1, act.DeletedDetailCount);
            Assert.Equal(1, act.DeletedReissueTransactionCount);
        }

        [Fact]
        public async Task ShouldPassCleanSingleTransaction()
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            MockCleanSingleTransaction(
                new List<DeletionTarget> { TEST_DELETION_TARGET },
                TEST_DELETE_RESULT,
                new List<IArchiveDetail> { TEST_REISSUE_ARCHIVE_DETAIL },
                true,
                1,
                TEST_STATUS_FILE_PATH,
                TEST_REISSUE_ARCHIVE_FILE_FULL_PATH);

            // act
            var act = await svc.CleanSingleTransaction(TEST_STATUS_FILE_PATH, TEST_REISSUE_ARCHIVE_FILE_TXID);

            // assert
            Assert.Equal(1, act.DeletedFileCount);
            Assert.Equal(1, act.DeletedArchiveCount);
            Assert.Equal(1, act.DeletedDetailCount);
            Assert.Equal(1, act.DeletedReissueTransactionCount);
        }

        [Fact]
        public async Task ShouldPassCleanDocsRequestInsert()
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            MockCleanDocsRequestInsert(
                new List<DeletionTarget> { TEST_DELETION_TARGET },
                new List<CleanDocsRequestResult> { TEST_CLEAN_DOCS_REQUEST_RESULT },
                TEST_STATUS_FILE_PATH,
                TEST_REISSUE_ARCHIVE_FILE_FULL_PATH);

            // act
            var act = await svc.CleanDocsRequestInsert(TEST_STATUS_FILE_PATH, "1003314691,109876");

            // assert
            Assert.Equal(TEST_REISSUE_ARCHIVE_FILE_FULL_PATH, act[0].PathToDelete);
            Assert.Equal(TEST_REISSUE_ARCHIVE_FILE_ID, act[0].ArchiveFileId);
            Assert.Equal(TEST_STATUS_FILE_PATH, act[0].StatusFilePath);
        }

        [Fact]
        public async Task ShouldPassCleanDocsRequestGet()
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            _cleandocOps
                .Setup(o => o.CleanDocsRequestGet(
                    It.Is<int>(i => i.Equals(TEST_CLEAN_DOCS_REQUEST_RESULT.RequestId))))
                .ReturnsAsync(new List<CleanDocsRequestResult> { TEST_CLEAN_DOCS_REQUEST_RESULT });

            // act
            var act = await svc.CleanDocsRequestGet(TEST_CLEAN_DOCS_REQUEST_RESULT.RequestId);

            // assert
            Assert.Equal(TEST_REISSUE_ARCHIVE_FILE_FULL_PATH, act[0].PathToDelete);
            Assert.Equal(TEST_REISSUE_ARCHIVE_FILE_ID, act[0].ArchiveFileId);
            Assert.Equal(TEST_STATUS_FILE_PATH, act[0].StatusFilePath);
        }

        [Fact]
        public async Task ShouldPassCleanDocsRequestDelete()
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            MockCleanDocsRequestDelete(
                new Dictionary<long, IArchiveFile>
                {
                                    { TEST_ARCHIVE_FILE_ID, TEST_ARCHIVE_FILE},
                                    { TEST_REISSUE_ARCHIVE_FILE_ID, TEST_REISSUE_ARCHIVE_FILE}
                },
                new StateManagementCleanDocsResult { Success = true },
                TEST_DELETE_RESULT,
                new List<IArchiveDetail> { TEST_REISSUE_ARCHIVE_DETAIL },
                true,
                1,
                TEST_STATUS_FILE_PATH,
                TEST_REISSUE_ARCHIVE_FILE_FULL_PATH);

            // act
            var act = await svc.CleanDocsRequestDelete(
                new List<CleanDocsRequestResult> { TEST_CLEAN_DOCS_REQUEST_RESULT },
                true,
                true,
                true);

            // assert
            Assert.Equal(1, act.DeletedFileCount);
            Assert.Equal(1, act.DeletedArchiveCount);
            Assert.Equal(1, act.DeletedDetailCount);
            Assert.Equal(1, act.DeletedReissueTransactionCount);
        }

        [Theory]
        [InlineData(TEST_STATUS_FILE_PATH, 1)]
        public async Task ShouldPassCleanDocsDeleteWhenNoArchive(string statusFilePath, int expectedDeletions)
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            _storageManager
                .Setup(x => x.CleanPaymentStatusFile(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new StateManagementCleanDocsResult
                {
                    Success = true,
                    DocumentTransactionRowsDeleted = expectedDeletions,
                    DistributionStatusRowsDeleted = expectedDeletions,
                    DocumentDispositionRowsDeleted = expectedDeletions,
                });
            _storageManager
                .Setup(x => x.GetArchiveDetailsForReissuedToFiles(
                    It.IsAny<IList<long>>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<IArchiveDetail>());
            // don't need to mock DeleteArchiveDetailFromArchiveFile or DeleteReissueDataByReissue since they should not run
            _cleandocOps
                .Setup(x => x.CleanDocsRequestDelete(
                    It.IsAny<int>()))
                .ReturnsAsync(TEST_DELETE_RESULT);
            _path
                .Setup(x => x.DirectorySeparatorChar)
                .Returns('/');
            _path
                .Setup(x => x.GetFileName(
                    It.IsAny<string>()))
                .Returns(System.IO.Path.GetFileName(statusFilePath));
            _path
                .Setup(x => x.GetDirectoryName(
                    It.IsAny<string>()))
                .Returns(System.IO.Path.GetDirectoryName(statusFilePath));
            _file
                .Setup(x => x.Exists(
                    It.IsAny<string>()))
                .Returns(false);

            // act
            var act = await svc.CleanDocsRequestDelete(new List<CleanDocsRequestResult>
            {
                new CleanDocsRequestResult { RequestId = 1, StatusFilePath = statusFilePath}
            },
            true,
            false,
            false);

            // assert
            Assert.NotNull(act);
            Assert.Equal(expectedDeletions, act.DeletedDocumentTransactionRows);
            Assert.Equal(expectedDeletions, act.DeletedDocumentDispositonRows);
        }

        [Theory]
        [InlineData(TEST_STATUS_FILE_PATH, TEST_ARCHIVE_FILE_ID, 1)]
        public async Task ShouldPassCleanDocsDeleteWhenMissingArchive(string statusFilePath, long archiveFileId, int expectedDeletions)
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            _storageManager
                .Setup(x => x.CleanPaymentStatusFile(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new StateManagementCleanDocsResult
                {
                    Success = true,
                    DocumentTransactionRowsDeleted = expectedDeletions,
                    DistributionStatusRowsDeleted = expectedDeletions,
                    DocumentDispositionRowsDeleted = expectedDeletions,
                });
            _storageManager
                .Setup(x => x.GetArchiveDetailsForReissuedToFiles(
                    It.IsAny<IList<long>>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<IArchiveDetail>());
            _storageManager
                .Setup(x => x.GetArchiveForArchiveFileId(
                    It.Is<long>(i => i.Equals(archiveFileId)), It.IsAny<bool>()))
                .ThrowsAsync(new ArchiveFileNotFoundException("", archiveFileId));
            // don't need to mock DeleteArchiveDetailFromArchiveFile or DeleteReissueDataByReissue since they should not run
            _cleandocOps
                .Setup(x => x.CleanDocsRequestDelete(
                    It.IsAny<int>()))
                .ReturnsAsync(TEST_DELETE_RESULT);
            _path
                .Setup(x => x.DirectorySeparatorChar)
                .Returns('/');
            _path
                .Setup(x => x.GetFileName(
                    It.IsAny<string>()))
                .Returns(System.IO.Path.GetFileName(statusFilePath));
            _path
                .Setup(x => x.GetDirectoryName(
                    It.IsAny<string>()))
                .Returns(System.IO.Path.GetDirectoryName(statusFilePath));
            _file
                .Setup(x => x.Exists(
                    It.IsAny<string>()))
                .Returns(false);

            // act
            var act = await svc.CleanDocsRequestDelete(new List<CleanDocsRequestResult>
            {
                new CleanDocsRequestResult { RequestId = 1, ArchiveFileId = archiveFileId, StatusFilePath = statusFilePath}
            },
            true,
            false,
            false);

            // assert
            Assert.NotNull(act);
            Assert.Equal(expectedDeletions, act.DeletedDocumentTransactionRows);
            Assert.Equal(expectedDeletions, act.DeletedDocumentDispositonRows);
        }

        [Theory]
        [InlineData(TEST_REISSUE_ARCHIVE_FILE_TXID)]
        public async Task ShouldPassCleanReissueReferencesFromOriginalArchives(params long[] reissueTransactionIds)
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            MockCleanReissueReferencesFromOriginalArchives(
                new List<IArchiveDetail> { TEST_REISSUE_ARCHIVE_DETAIL },
                true,
                1);

            // act
            var act = await svc.CleanReissueReferencesFromOriginalArchives(
                reissueTransactionIds, true);

            // assert
            Assert.Equal(1, act.DeletedFileCount);
            Assert.Equal(1, act.DeletedDetailCount);
            Assert.Equal(1, act.DeletedReissueTransactionCount);
        }

        [Theory]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff", 12345)]
        [InlineData("00000000-0000-0000-0000-000000000000", 12345)]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff", 0)]
        public async Task ShouldFailExpungeArchive(string transactionGuid, long archiveId)
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            var guid = new Guid(transactionGuid);
            _storageManager
                .Setup(x => x.ExpungeArchive(
                    It.IsAny<Guid>(), It.IsAny<long>()))
                .Returns(() => throw new Exception());

            // act
            var act = await svc.ExpungeArchive(guid, archiveId);

            // assert
            Assert.True(!act);
        }

        [Theory]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff", 12345)]
        public async Task ShouldPassExpungeArchive(string transactionGuid, long archiveId)
        {
            // arrange
            var svc = _services.GetRequiredService<CleanDocsService>();
            var guid = new Guid(transactionGuid);
            _storageManager
                .Setup(x => x.ExpungeArchive(
                    It.IsAny<Guid>(), It.IsAny<long>()))
                .ReturnsAsync(true);

            // act
            var act = await svc.ExpungeArchive(guid, archiveId);

            // assert
            Assert.True(act);
        }
    }
}
