using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;
using VPay.Queuing;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public abstract class TiffQueueMessageProcessorTestBase
    {
        protected readonly Mock<IFileSystem> _fileSystem = new Mock<IFileSystem>();
        protected readonly Mock<ITiffOperations> _tiffOperations = new Mock<ITiffOperations>();
        protected readonly Mock<IStorageManager> _storageManager = new Mock<IStorageManager>();
        protected readonly Mock<IQueueService> _queueService = new Mock<IQueueService>();

        protected readonly ITiffQueueMessageProcessor SUT;

        protected readonly Mock<IFile> _file = new Mock<IFile>();
        protected readonly Mock<IFileInfoFactory> _fileInfoFactory = new Mock<IFileInfoFactory>();
        protected readonly Mock<IFileStreamFactory> _fileStreamFactory = new Mock<IFileStreamFactory>();
        protected readonly Mock<IDirectory> _directory = new Mock<IDirectory>();
        protected readonly Mock<IPath> _path = new Mock<IPath>();
        protected readonly Mock<IArchiveDetailStream> _archiveDetailStream = new Mock<IArchiveDetailStream>();

        protected readonly QueueConfigurations _queueConfigurations;
        protected readonly FaxBase _faxBase;
        protected readonly FaxJobSettings _faxJobSettings;
        protected readonly ArchiveFile _archiveFile;

        protected readonly long _newArchiveFileId;
        protected readonly Random _random = new Random();

        protected TiffQueueMessageProcessorTestBase()
        {
            _newArchiveFileId = _random.Next();

            _queueConfigurations = new QueueConfigurations
            {
                FaxJobDataQueue = new QueueConfigurationSettings()
                {
                    MaxPriority = 100
                }
            };

            _faxBase = new FaxBase
            {
                Path = "testPath"
            };

            _faxJobSettings = new FaxJobSettings
            {
                PaddingInSeconds = 1,
                Scale = 1,
                MaxHoursCap = 1
            };

            _archiveFile = new ArchiveFile
            {
                ArchiveFileId = _newArchiveFileId,
                ArchiveDetails = new[]
                {
                    new ArchiveDetail
                    {
                        Data = Array.Empty<byte>(),
                        PageCount = 1,
                        FileName = $"{Guid.NewGuid()}.pdf"
                    },
                }
            };

            _archiveDetailStream.Setup(_ => _.Open()).Returns(new MemoryStream(Array.Empty<byte>()));
            _archiveDetailStream.Setup(_ => _.ArchiveFileId).Returns(_newArchiveFileId);
            _archiveDetailStream.Setup(_ => _.FileName).Returns($"{Guid.NewGuid()}.pdf");
            _archiveDetailStream.Setup(_ => _.PageCount).Returns(_random.Next() + 1);

            _tiffOperations
                .Setup(_ => _.CreateTiffFromPdf(It.IsAny<string>()))
                .ReturnsAsync((string a) => $"{a}.tiff");

            _storageManager
                .Setup(_ => _.GetArchiveDetailStreams(It.IsAny<long>(), It.IsAny<MatchingCriteria>()))
                .ReturnsAsync((long a, MatchingCriteria b) => new ArchiveFile
                {
                    ArchiveFileId = _newArchiveFileId,
                    ArchiveDetails = new[]
                    {
                        new ArchiveDetail
                        {
                            FileName = $"{Guid.NewGuid()}{b.Criteria.First()}",
                            Data = Array.Empty<byte>(),
                            PageCount = 100
                        },
                    }
                });

            _storageManager
                .Setup(_ => _.GetArchiveForTransactionId(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync((long a, string b) => new ArchiveFile
                {
                    ArchiveFileId = _newArchiveFileId,
                    ArchiveDetails = new[]
                    {
                        new ArchiveDetail
                        {
                            TransactionId = a,
                            Data = Array.Empty<byte>(),
                            PageCount = 1,
                            FileName = $"{Guid.NewGuid()}{b}"
                        },
                    }
                });

            _storageManager
                .Setup(_ => _.GetDistributionPacketStream(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(_archiveDetailStream.Object);

            _storageManager
                .Setup(_ => _.GetArchiveDetailStream(It.IsAny<long>()))
                .ReturnsAsync(_archiveDetailStream.Object);

            _storageManager
                .Setup(_ => _.AppendArchive(It.IsAny<long>(), It.IsAny<List<IGeneratedFile>>(), It.IsAny<bool>(), null))
                .ReturnsAsync(new Mock<IArchiveFile>().Object);

            _storageManager
                .Setup(_ => _.CreateArchive(It.IsAny<IArchiveFile>(), It.IsAny<List<IGeneratedFile>>(),
                    It.IsAny<bool>(), null))
                .ReturnsAsync(_newArchiveFileId);

            _queueService.Setup(_ => _.PublishQueueMessage(
                    It.IsAny<QueueConfigurationSettings>(),
                    It.IsAny<string>(),
                    It.IsAny<byte>()));

            SUT = new TiffQueueMessageProcessor(
                _queueConfigurations,
                _fileSystem.Object,
                _tiffOperations.Object,
                _storageManager.Object,
                _queueService.Object,
                _faxJobSettings,
                _faxBase,
                new NullLogger<TiffQueueMessageProcessor>());

            _fileSystem.Setup(_ => _.FileInfo).Returns(_fileInfoFactory.Object);
            _fileSystem.Setup(_ => _.File).Returns(_file.Object);
            _fileSystem.Setup(_ => _.FileStream).Returns(_fileStreamFactory.Object);
            _fileSystem.Setup(_ => _.Directory).Returns(_directory.Object);
            _fileSystem.Setup(_ => _.Path).Returns(_path.Object);

            var fileInfo = new Mock<IFileInfo>();
            fileInfo
                .Setup(_ => _.Length)
                .Returns(0);

            _fileInfoFactory
                .Setup(_ => _.New(It.IsAny<string>()))
                .Returns(fileInfo.Object);

            _file.Setup(_ => _.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()));
            _file.Setup(_ => _.Copy(It.IsAny<string>(), It.IsAny<string>()));

            // With this fix:
            //_file.Setup(_ => _.Create(It.IsAny<string>()))
            //    .Returns((string path) =>
            //    {
            //        // Use FileSystemStream from System.IO.Abstractions.TestingHelpers
            //        var accessor = new MockFileSystem();
            //        return new MockFileStream(accessor, path, FileMode.Create, FileAccess.ReadWrite, FileOptions.None);
            //    });
            // Fix for ArgumentNullException in test setup for _file.Create
            // Fix for CS0029 and CS1662 in the _file.Setup(_ => _.Create(It.IsAny<string>())) lambda
            _file.Setup(_ => _.Create(It.IsAny<string>()))
                .Returns((string path) =>
                {
                    // Use a shared MockFileSystem instance to ensure the stream is valid and not disposed
                    // This prevents returning a null or invalid stream
                    if (_fileSystem.Object is MockFileSystem mfs)
                    {
                        return new MockFileStream(mfs, path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, FileOptions.None);
                    }
                    // Fallback: create a new MockFileSystem and return a MockFileStream (must return FileSystemStream)
                    var fallbackMfs = new MockFileSystem();
                    return new MockFileStream(fallbackMfs, path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, FileOptions.None);
                });
            _directory.Setup(_ => _.CreateDirectory(It.IsAny<string>()));
           
            _directory.Setup(_ => _.Delete(It.IsAny<string>(), It.IsAny<bool>()));
            _directory
                .Setup(_ => _.Exists(It.IsAny<string>()))
                .Returns(true);

            _path
                .Setup(_ => _.Combine(It.IsAny<string[]>()))
                .Returns(Guid.NewGuid().ToString());
            _path
                .Setup(_ => _.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string a, string b) => Guid.NewGuid() + b + ".pdf");
            _path
                .Setup(_ => _.GetFileName(It.IsAny<string>()))
                .Returns(Guid.NewGuid().ToString());
            _path
                .Setup(_ => _.GetDirectoryName(It.IsAny<string>()))
                .Returns(Guid.NewGuid().ToString());
            _path
                .Setup(_ => _.ChangeExtension(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid().ToString());
        }

        protected TiffQueueMessage GetData(FaxReserveName value, bool hasArchiveDetailId = true)
        {
            var tqm = new TiffQueueMessage
            {
                BoardUploadTimeStamp = DateTime.Now,
                BillingEntity = "TESTBE",
                CorrespondenceId = -10000,
                FaxNumber = "TESTFAXN",
                Origin = "TESTORIGIN",
                PayType = "TESTPAYTYPE",
                PrintVendor = "TESTPRINTVENDOR",
                Tpa = "TESTTPA",
                TransactionIds = new List<long> { -1000 },
                Priority = 50,

                ReserveName = value,
                ArchiveFileId = null,
                PdfFileName = null,
                ArchiveDetailId = null,
            };

            switch (tqm.ReserveName)
            {
                case FaxReserveName.WARRANTY:
                case FaxReserveName.RFAX:
                case FaxReserveName.VPFEE:
                    tqm.ArchiveFileId = _newArchiveFileId;
                    tqm.PdfFileName = $"TEST_{tqm.ReserveName}_PDFFILENAME";
                    break;

                case FaxReserveName.CSS:
                    if (hasArchiveDetailId) { tqm.ArchiveDetailId = -100; }
                    tqm.PdfFileName = "TEST_CSS_PDFFILENAME";
                    break;

                case FaxReserveName.BFAX:
                    tqm.ArchiveFileId = _newArchiveFileId;
                    if (hasArchiveDetailId) { tqm.ArchiveDetailId = -100; }
                    break;

                case FaxReserveName.MEDICAL:
                    break;

                default:
                    throw new ArgumentException($"Need to add test values to handle {tqm.ReserveName}", nameof(tqm.ReserveName));
            }

            return tqm;
        }

        public static IEnumerable<object[]> AllFaxReserveValues
        {
            get
            {
                foreach (FaxReserveName frn in Enum.GetValues(typeof(FaxReserveName)))
                {
                    yield return new object[] { frn };
                }
            }
        }
    }

    public class ShouldPublishFaxJob : TiffQueueMessageProcessorTestBase
    {
        [Theory]
        [MemberData(nameof(AllFaxReserveValues))]
        public async Task WithCorrectFaxReserveName(FaxReserveName frn)
        {
            var tqm = GetData(frn);

            await SUT.ProcessMessageString(JsonConvert.SerializeObject(tqm));

            _queueService.Verify(_ => _.PublishQueueMessage(
                    It.IsAny<QueueConfigurationSettings>(),
                    It.IsAny<string>(),
                    It.IsAny<byte>()),
                Times.Once);
        }
    }

    public class ShouldThrowException : TiffQueueMessageProcessorTestBase
    {
        [Theory]
        [MemberData(nameof(BadCtorValues))]
        public void WhenCtorValuesAreInvalid(Type exception, QueueConfigurations queueConfigurations, IFileSystem fileSystem, ITiffOperations tiffOperations, IStorageManager storageManager, IQueueService queueService, FaxJobSettings faxJobSettings, FaxBase faxBase, ILogger<TiffQueueMessageProcessor> logger)
        {
            Assert.Throws(exception, () => new TiffQueueMessageProcessor(
                 queueConfigurations,
                 fileSystem,
                 tiffOperations,
                 storageManager,
                 queueService,
                 faxJobSettings,
                 faxBase,
                 logger));
        }

        [Theory]
        [MemberData(nameof(BadParameters))]
        public async Task WhenValuesAreNotPresent(FaxReserveName frn, int? fileId, int? detailId, string name, List<long> txIds, Type ex)
        {
            var tqm = new TiffQueueMessage
            {
                ReserveName = frn,
                ArchiveFileId = fileId,
                ArchiveDetailId = detailId,
                PdfFileName = name,
                TransactionIds = txIds,
                Priority = 1
            };

            await Assert.ThrowsAsync(ex, () => SUT.ProcessMessageString(JsonConvert.SerializeObject(tqm)));
        }

        public static IEnumerable<object[]> BadParameters
        {
            get
            {
                var list = new List<long> { 1L };

                foreach (FaxReserveName frn in Enum.GetValues(typeof(FaxReserveName)))
                {
                    yield return new object[] { frn, 1000, 1000, "TESTFILENAME", null, typeof(ArgumentNullException) };
                    yield return new object[] { frn, 1000, 1000, "TESTFILENAME", new List<long>(), typeof(ArgumentException) };

                    switch (frn)
                    {
                        case FaxReserveName.WARRANTY:
                        case FaxReserveName.RFAX:
                        case FaxReserveName.VPFEE:
                            yield return new object[] { frn, 1000, null, null, list, typeof(ArgumentNullException) };
                            yield return new object[] { frn, 1000, null, "", list, typeof(ArgumentException) };
                            yield return new object[] { frn, 1000, null, "   ", list, typeof(ArgumentException) };
                            yield return new object[] { frn, null, null, "TESTFILENAME", list, typeof(ArgumentNullException) };
                            break;

                        case FaxReserveName.CSS:
                            yield return new object[] { frn, 1000, null, null, list, typeof(ArgumentException) };
                            yield return new object[] { frn, 1000, null, "", list, typeof(ArgumentException) };
                            yield return new object[] { frn, 1000, null, "  ", list, typeof(ArgumentException) };
                            break;

                        case FaxReserveName.BFAX:
                            yield return new object[] { frn, null, null, "TESTFILENAME", list, typeof(ArgumentNullException) };
                            yield return new object[] { frn, 1000, null, "TESTFILENAME", list, typeof(ArgumentNullException) };
                            yield return new object[] { frn, null, 1000, "TESTFILENAME", list, typeof(ArgumentNullException) };
                            break;

                        case FaxReserveName.MEDICAL:
                            break;

                        default:
                            throw new ArgumentException($"Need to add test values to handle {frn}", nameof(frn));
                    }
                }
            }
        }

        public static IEnumerable<object[]> BadCtorValues
        {
            get
            {
                var _queueConfigurations = new QueueConfigurations { FaxJobDataQueue = new QueueConfigurationSettings() };
                var _fileSystem = new Mock<IFileSystem>();
                var _tiffOperations = new Mock<ITiffOperations>();
                var _storageManager = new Mock<IStorageManager>();
                var _queueService = new Mock<IQueueService>();
                var _faxBase = new FaxBase { Path = "TESTPATH" };
                var _faxJobSettings = new FaxJobSettings();
                var _logger = new NullLogger<TiffQueueMessageProcessor>();

                yield return new object[] { typeof(NullReferenceException),
                        null,
                        _fileSystem.Object, _tiffOperations.Object, _storageManager.Object, _queueService.Object, _faxJobSettings, _faxBase, _logger };

                yield return new object[] { typeof(ArgumentNullException),
                        new QueueConfigurations { FaxJobDataQueue = null },
                        _fileSystem.Object, _tiffOperations.Object, _storageManager.Object, _queueService.Object, _faxJobSettings, _faxBase, _logger };

                yield return new object[] { typeof(ArgumentNullException), _queueConfigurations,
                        null,
                        _tiffOperations.Object, _storageManager.Object, _queueService.Object, _faxJobSettings, _faxBase, _logger };


                yield return new object[] { typeof(ArgumentNullException), _queueConfigurations, _fileSystem.Object,
                        null,
                        _storageManager.Object, _queueService.Object, _faxJobSettings, _faxBase, _logger };

                yield return new object[] { typeof(ArgumentNullException), _queueConfigurations, _fileSystem.Object, _tiffOperations.Object,
                        null,
                        _queueService.Object, _faxJobSettings, _faxBase, _logger };

                yield return new object[] { typeof(ArgumentNullException), _queueConfigurations, _fileSystem.Object, _tiffOperations.Object, _storageManager.Object,
                        null,
                        _faxJobSettings, _faxBase, _logger };

                yield return new object[] { typeof(ArgumentNullException), _queueConfigurations, _fileSystem.Object, _tiffOperations.Object, _storageManager.Object, _queueService.Object,
                        null,
                        _faxBase, _logger };

                yield return new object[] { typeof(NullReferenceException), _queueConfigurations, _fileSystem.Object, _tiffOperations.Object, _storageManager.Object, _queueService.Object, _faxJobSettings,
                        null,
                        _logger };

                yield return new object[] { typeof(ArgumentNullException), _queueConfigurations, _fileSystem.Object, _tiffOperations.Object, _storageManager.Object, _queueService.Object, _faxJobSettings,
                        new FaxBase(),
                        _logger };

                yield return new object[] { typeof(ArgumentNullException), _queueConfigurations, _fileSystem.Object, _tiffOperations.Object, _storageManager.Object, _queueService.Object, _faxJobSettings, _faxBase,
                        null };
            }
        }
    }

    public class ShouldUseCorrectGeneratedFileValues : TiffQueueMessageProcessorTestBase
    {
        [Theory]
        [MemberData(nameof(OnlyAppendArchiveFaxReserveValues))]
        public async Task WhenUpdatingArchive(FaxReserveName faxReserveName)
        {
            var tqm = GetData(faxReserveName);

            await SUT.ProcessMessageString(JsonConvert.SerializeObject(tqm));

            var gfc = new Func<List<IGeneratedFile>, bool>(gfs =>
            {
                var gf = gfs.First();

                return gfs.Count == 1 &&
                       gf.FileName.EndsWith(".tiff") &&
                       gf.FileDetailType == FileDetailType.Hidden &&
                       gf.PageCount == 0 &&
                       gf.Description == "Fax Tiff";
            });

            var fileId = tqm.ArchiveFileId.Value;

            _storageManager.Verify(_ => _.CreateArchive(
                    It.IsAny<IArchiveFile>(),
                    It.IsAny<List<IGeneratedFile>>(),
                    It.IsAny<bool>(),
                    null),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(AllFaxReserveValues))]
        public async Task WhenAppendingFile(FaxReserveName value)
        {
            var tqm = GetData(value);

            await SUT.ProcessMessageString(JsonConvert.SerializeObject(tqm));

            var agfc = new Func<List<IGeneratedFile>, bool>(gfs =>
            {
                var gf = gfs.First();

                return gfs.Count == 1 &&
                       gf.FileName.EndsWith(".tiff") &&
                       gf.FileDetailType == FileDetailType.Hidden &&
                       gf.PageCount == 0 &&
                       gf.Description == "Fax Tiff";
            });

            _storageManager.Verify(_ => _.AppendArchive(
                _newArchiveFileId,
                It.Is<List<IGeneratedFile>>(__ => agfc(__)),
                true,
                null),
                Times.Once);
        }

        [Fact]
        public async Task WhenAppendingFileForBundledFax()
        {
            var tqm = GetData(FaxReserveName.BFAX);

            await SUT.ProcessMessageString(JsonConvert.SerializeObject(tqm));

            _storageManager.Verify(_ => _.AppendArchive(
                _newArchiveFileId,
                It.Is<List<IGeneratedFile>>(__ => __.All(___ => ___.TransactionId == new string('0', 20))),
                true,
                null),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(CreateNewArchiveFaxReserveValues))]
        public async Task WhenCreatingArchive(FaxReserveName value)
        {
            var tqm = GetData(value);

            await SUT.ProcessMessageString(JsonConvert.SerializeObject(tqm));

            var ngfc = new Func<List<IGeneratedFile>, bool>(gfs =>
            {
                var gf = gfs.First();

                return gfs.Count == 1 &&
                       gf.FileName.EndsWith(".pdf") &&
                       gf.FileDetailType == FileDetailType.FaxDocument &&
                       gf.PageCount == _archiveDetailStream.Object.PageCount &&
                       gf.Description == "Fax";
            });

            _storageManager.Verify(_ => _.CreateArchive(
                It.IsAny<IArchiveFile>(),
                It.Is<List<IGeneratedFile>>(__ => ngfc(__)),
                false,
                null),
                Times.Once());
        }

        public static IEnumerable<object[]> CreateNewArchiveFaxReserveValues
        {
            get
            {
                foreach (FaxReserveName frn in TiffQueueMessageProcessor.CreateNewArchiveTypes)
                {
                    yield return new object[] { frn };
                }
            }
        }

        public static IEnumerable<object[]> OnlyAppendArchiveFaxReserveValues
        {
            get
            {
                var list = AllFaxReserveValues
                    .Select(_ => _.First())
                    .Cast<FaxReserveName>()
                    .Except(TiffQueueMessageProcessor.CreateNewArchiveTypes);

                foreach (FaxReserveName frn in list)
                {
                    yield return new object[] { frn };
                }
            }
        }
    }

    public class ShouldUseCorrectStorageManagerCall : TiffQueueMessageProcessorTestBase
    {
        [Theory]
        [MemberData(nameof(StorageManagerChecks))]
        public async Task ForEachFaxReserveName(
            FaxReserveName faxReserveName,
            Times getDistributionPacketStream,
            Times getArchiveDetailStream,
            Times getArchiveForTransactionId,
            Times getArchiveDetailStreams,
            bool hasArchiveDetailId = true)
        {
            var tqm = GetData(faxReserveName, hasArchiveDetailId);

            await SUT.ProcessMessageString(JsonConvert.SerializeObject(tqm));

            _storageManager.Verify(_ => _.GetDistributionPacketStream(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>()),
                getDistributionPacketStream);
            _storageManager.Verify(_ => _.GetArchiveDetailStream(It.IsAny<long>()),
                getArchiveDetailStream);
            _storageManager.Verify(_ => _.GetArchiveForTransactionId(It.IsAny<long>(), It.IsAny<string>()),
                getArchiveForTransactionId);
            _storageManager.Verify(_ => _.GetArchiveDetailStreams(It.IsAny<long>(), It.IsAny<MatchingCriteria>()),
                getArchiveDetailStreams);
        }

        public static IEnumerable<object[]> StorageManagerChecks
        {
            get
            {
                foreach (FaxReserveName frn in Enum.GetValues(typeof(FaxReserveName)))
                {
                    switch (frn)
                    {
                        case FaxReserveName.CSS:
                            yield return new object[] { frn, Times.Never(), Times.Once(), Times.Never(), Times.Never(), };
                            yield return new object[] { frn, Times.Never(), Times.Once(), Times.Once(), Times.Never(), false, };
                            break;

                        case FaxReserveName.MEDICAL:
                            yield return new object[] { frn, Times.Once(), Times.Never(), Times.Never(), Times.Never(), };
                            break;

                        case FaxReserveName.RFAX:
                        case FaxReserveName.WARRANTY:
                        case FaxReserveName.VPFEE:
                            yield return new object[] { frn, Times.Never(), Times.Never(), Times.Never(), Times.Once(), };
                            break;

                        case FaxReserveName.BFAX:
                            yield return new object[] { frn, Times.Never(), Times.Once(), Times.Never(), Times.Never(), };
                            break;

                        default:
                            throw new ArgumentException($"Need to handle {frn}");
                    }
                }
            }
        }
    }
}
