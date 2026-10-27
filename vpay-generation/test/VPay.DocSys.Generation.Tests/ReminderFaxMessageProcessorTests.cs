using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class ReminderFaxMessageProcessorTests
    {
        protected readonly Mock<ResourceManager> _resourceManager = new Mock<ResourceManager>();
        protected readonly Mock<IStorageManager> _storageManager = new Mock<IStorageManager>();
        protected readonly Mock<IReminderFaxPdfCreator> _reminderFaxPdfCreator = new Mock<IReminderFaxPdfCreator>();
        protected readonly Mock<IPdfTools> _pdfTools = new Mock<IPdfTools>();
        protected readonly Mock<ITiffMessageQueuer> _tiffMessageQueuer = new Mock<ITiffMessageQueuer>();

        protected readonly IReminderFaxMessageProcessor SUT;

        public ReminderFaxMessageProcessorTests()
        {
            _resourceManager
                .Setup(_ => _.GetString(It.IsAny<string>()))
                .Returns("TEST VPAY FAX NUMBER")
                .Verifiable();

            _reminderFaxPdfCreator
                .Setup(_ => _.CreatePdfStream(It.IsAny<CoverSheetParameters>()))
                .ReturnsAsync(new MemoryStream())
                .Verifiable();

            _pdfTools
                .Setup(_ => _.GetNumberOfPagesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(1)
                .Verifiable();

            _storageManager
                .Setup(_ => _.CreateArchive(It.IsAny<IArchiveFile>(), It.IsAny<List<IGeneratedFile>>(), false, null))
                .ReturnsAsync(-1L)
                .Verifiable();

            _tiffMessageQueuer
                .Setup(_ => _.QueueTiffMessage(It.IsAny<TiffQueueMessage>()))
                .Verifiable();

            SUT = new ReminderFaxMessageProcessor(
                _resourceManager.Object,
                _storageManager.Object,
                _reminderFaxPdfCreator.Object,
                _pdfTools.Object,
                _tiffMessageQueuer.Object,
                new NullLogger<ReminderFaxMessageProcessor>());
        }

        [Fact]
        public async Task ThrowExceptionOnNullReminderFaxRequest()
        {
            await Assert.ThrowsAsync<NullReferenceException>(() => SUT.ProcessMessage(null));
        }

        [Theory]
        [MemberData(nameof(InvalidCtorParameters))]
        public void ThrowExceptionOnInvalidParamsInCtor(
            Type exception,
            ResourceManager resourceManager,
            IStorageManager storageManager,
            IReminderFaxPdfCreator reminderFaxPdfCreator,
            IPdfTools pdfTools,
            ITiffMessageQueuer tiffMessageQueuer,
            ILogger<ReminderFaxMessageProcessor> logger)
        {
            Assert.Throws(exception, () => new ReminderFaxMessageProcessor(
                resourceManager,
                storageManager,
                reminderFaxPdfCreator,
                pdfTools,
                tiffMessageQueuer,
                logger));
        }

        [Fact]
        public async Task ShouldProcessReminderFaxMessage()
        {
            await SUT.ProcessMessage(new ReminderFaxMessage
            {
                DistributionPacketBytes = Array.Empty<byte>(),
                FileName = "TEST FILE NAME",
                PageCount = 1,
                PayorDetail = new PayorDetail
                {
                    IsEobDependent = false,
                    PayorName = "TEST PAYOR NAME",
                    PayorPhone = "TEST PHONE",
                    VPayPhone = "TEST VPAY PHONE"
                },
                ReminderFax = new ReminderFax
                {
                    ClientType = ClientType.MEDICAL
                }
            });

            _reminderFaxPdfCreator.Verify(_ => _.CreatePdfStream(It.IsAny<CoverSheetParameters>()), Times.Once);
            _pdfTools.Verify(_ => _.MergeAsync(It.IsAny<byte[][]>()), Times.Once);
            _storageManager.Verify(_ => _.CreateArchive(
                It.IsAny<IArchiveFile>(),
                It.Is<List<IGeneratedFile>>(__ => __.Count == 1 && __.First().FileDetailType == FileDetailType.Pdf),
                false,
                null),
                Times.Once);
            _tiffMessageQueuer.Verify(_ => _.QueueTiffMessage(
                It.Is<TiffQueueMessage>(__ => __.Priority == 100)),
                Times.Once);
        }

        public static IEnumerable<object[]> InvalidCtorParameters
        {
            get
            {
                var resourceManager = new Mock<ResourceManager>();
                var storageManager = new Mock<IStorageManager>();
                var reminderFaxPdfCreator = new Mock<IReminderFaxPdfCreator>();
                var pdfTools = new Mock<IPdfTools>();
                var tiffMessageQueuer = new Mock<ITiffMessageQueuer>();
                var logger = new NullLogger<ReminderFaxMessageProcessor>();

                yield return new object[]
                {
                    typeof(ArgumentNullException),
                    null,
                    storageManager.Object,
                    reminderFaxPdfCreator.Object,
                    pdfTools.Object,
                    tiffMessageQueuer.Object,
                    logger,
                };

                yield return new object[]
                {
                    typeof(ArgumentNullException),
                    resourceManager.Object,
                    null,
                    reminderFaxPdfCreator.Object,
                    pdfTools.Object,
                    tiffMessageQueuer.Object,
                    logger,
                };

                yield return new object[]
                {
                    typeof(ArgumentNullException),
                    resourceManager.Object,
                    storageManager.Object,
                    null,
                    pdfTools.Object,
                    tiffMessageQueuer.Object,
                    logger,
                };

                yield return new object[]
                {
                    typeof(ArgumentNullException),
                    resourceManager.Object,
                    storageManager.Object,
                    reminderFaxPdfCreator.Object,
                    null,
                    tiffMessageQueuer.Object,
                    logger,
                };

                yield return new object[]
                {
                    typeof(ArgumentNullException),
                    resourceManager.Object,
                    storageManager.Object,
                    reminderFaxPdfCreator.Object,
                    pdfTools.Object,
                    null,
                    logger,
                };

                yield return new object[]
                {
                    typeof(ArgumentNullException),
                    resourceManager.Object,
                    storageManager.Object,
                    reminderFaxPdfCreator.Object,
                    pdfTools.Object,
                    tiffMessageQueuer.Object,
                    null,
                };
            }
        }
    }
#pragma warning restore CA1034 // Nested types should not be visible
}
