using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using VPay.Data.Db2.Abstractions;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class BundledFaxServiceTests
    {
        protected readonly Mock<IFileSystem> _fileSystem = new Mock<IFileSystem>();
        protected readonly Mock<IStorageManager> _storageManager = new Mock<IStorageManager>();
        protected readonly Mock<IFaxManager> _faxManager = new Mock<IFaxManager>();
        protected readonly Mock<IPdfTools> _pdfTools = new Mock<IPdfTools>();
        protected readonly Mock<ILoggerFactory> _loggerFactory = new Mock<ILoggerFactory>();
        protected readonly Mock<IFopApiClient> _fopApiClient = new Mock<IFopApiClient>();
        protected readonly DocSysSettings _docSysSettings = new DocSysSettings();
        protected readonly Mock<IFax> _faxRepository = new Mock<IFax>();
        protected readonly Mock<ITiffMessageQueuer> _tiffMessageQueuer = new Mock<ITiffMessageQueuer>();
        protected readonly Mock<IBundledFaxRepository> _bundledFaxRepository = new Mock<IBundledFaxRepository>();

        protected readonly Mock<IFile> _file = new Mock<IFile>();
        protected readonly Mock<IPath> _path = new Mock<IPath>();
        protected readonly Mock<IDirectory> _directory = new Mock<IDirectory>();

        protected IBundledFaxService SUT => new BundledFaxService(
            _fileSystem.Object,
            _storageManager.Object,
            _faxManager.Object,
            _pdfTools.Object,
            _loggerFactory.Object,
            _fopApiClient.Object,
            _docSysSettings,
            _faxRepository.Object,
            _tiffMessageQueuer.Object,
            _bundledFaxRepository.Object
            );

        protected BundledFaxParams input = new BundledFaxParams();
        protected BundledFaxCorrespondenceIdResult bundledFaxCorrespondenceIdResult;

        protected List<long> transactionList;

        protected IArchiveFile archiveFile;

        protected BundledFaxServiceTests()
        {
            _fopApiClient
                .Setup(_ => _.PostFile(It.IsAny<string>()))
                .ReturnsAsync(new MemoryStream());


            _fileSystem.Setup(_ => _.File).Returns(_file.Object);
            _fileSystem.Setup(_ => _.Directory).Returns(_directory.Object);
            _fileSystem.Setup(_ => _.Path).Returns(_path.Object);

            transactionList = new List<long> { 1, 2, 3 };

            archiveFile = new ArchiveFile
            {
                ArchiveFileId = -1000,
                ArchiveDetails = new List<IArchiveDetail>
                {
                    new ArchiveDetail
                    {
                        ArchiveDetailId= -1001,
                        FileDetailType=FileDetailType.BundledFax
                    }
                }
            };

            bundledFaxCorrespondenceIdResult = new BundledFaxCorrespondenceIdResult()
            {
                PaymentTypeCode = "CLVCP",
                OriginCode = "SE",
                ClientCode = "VPAY",
                BillingEntityCode = "VPAY010001",
                TransactionId = 1,
                CorrespondenceId = 2
            };
        }
    }

    public class WhenTestingCreateCoverPage : BundledFaxServiceTests
    {
        [Fact]
        public async ValueTask ShouldCreateCoverPage()
        {
            var input = new BundledFaxParams();

            const string tempFileName = "tempfolder/file.tmp";
            _path.Setup(_ => _.GetTempFileName()).Returns(tempFileName);

            var coverPagePath = await SUT.CreateCoverPage(input);

            _file.Verify(_ => _.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
            _path.Verify(_ => _.GetTempFileName(), Times.Once);

            coverPagePath.Should().Be(tempFileName);
        }
    }

    public class WhenTestingFormulateBundledFaxMessage : BundledFaxServiceTests
    {
        [Fact]
        public void ShouldBeCorrectReserveName()
        {
            var message = Act();

            message.ReserveName.Should().Be(FaxReserveName.BFAX);
        }

        [Fact]
        public void ShouldBePriority15()
        {
            var message = Act();

            message.Priority.Should().Be(15);
        }

        [Theory]
        [MemberData(nameof(ArchiveFileTestData))]
        public void ShouldHaveCorrectArchiveDetailId(ArchiveFile file)
        {
            var expected = file.ArchiveDetails
                .First(_ => _.FileDetailType == FileDetailType.BundledFax)
                .ArchiveDetailId;

            archiveFile = file;

            var message = Act();

            message.ArchiveDetailId.Should().Be(expected);
        }

        [Fact]
        public void ShouldThrowExceptionForNoBundledFaxDetail()
        {
            archiveFile = new ArchiveFile();

            Assert.Throws<NullReferenceException>(() => Act());
        }

        public static IEnumerable<object[]> ArchiveFileTestData
        {
            get
            {
                yield return new object[]
                {
                    new ArchiveFile
                    {
                        ArchiveFileId = -45,
                        ArchiveDetails = new List<IArchiveDetail>
                        {
                            new ArchiveDetail
                            {
                                ArchiveDetailId= -34534,
                                FileDetailType=FileDetailType.BundledFax
                            }
                        }
                    }
                };

                yield return new object[]
                {
                    new ArchiveFile
                    {
                        ArchiveFileId = -28309238,
                        ArchiveDetails = new List<IArchiveDetail>
                        {
                            new ArchiveDetail
                            {
                                ArchiveDetailId = 930293,
                                FileDetailType = FileDetailType.Hidden
                            },
                            new ArchiveDetail
                            {
                                ArchiveDetailId= 23849023,
                                FileDetailType=FileDetailType.BundledFax
                            }
                        }
                    }
                };
            }
        }

        private TiffQueueMessage Act()
        {
            return SUT.FormulateBundledFaxMessage(bundledFaxCorrespondenceIdResult, input, transactionList, archiveFile);
        }
    }
}
