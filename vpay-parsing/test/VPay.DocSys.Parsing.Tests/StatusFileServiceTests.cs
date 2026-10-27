using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    /// <summary>
    /// Tests for <see cref="StatusFileService"/> class.
    /// </summary>
    [Collection(nameof(StatusFileService))] // Required to run tests sequentially due to shared file system test files.
    public class StatusFileServiceTests : BaseStatusFileServiceTests
    {
        private readonly StatusFile _babStatusFile;
        private readonly StatusFile _stpStatusFile;

        public StatusFileServiceTests()
        {
            var babStatusFileName = Path.Combine(Path.GetTempPath(), "BABTest.sts");
            SetupInitialMocks(babStatusFileName);
            SetupStatusFileTracker("PaymentStatusFileId", 100);

            TestFiles.Add(babStatusFileName);
            File.WriteAllLines(babStatusFileName, StatusFileData);
            _babStatusFile = new StatusFile(StatusFileData);

            var stpStatusFileName = Path.Combine(Path.GetTempPath(), "STPTest.sts");
            SetupInitialMocks(stpStatusFileName);
            SetupStatusFileTracker("PaymentStatusFileId", 100);

            TestFiles.Add(stpStatusFileName);
            File.WriteAllLines(stpStatusFileName, StpStatusFileData);
            _stpStatusFile = new StatusFile(StpStatusFileData);

            // The sample STP status file has four payments:
            // 770376643 (STP, B2B)
            // 770376673 (STP, B2B)
            // 770376701 (STP, C2B)
            // 770376781 (Non-STP)
        }

        public class CreateStpPaymentRecords : StatusFileServiceTests
        {
            [Fact]
            public async Task WhenStatusFileHasNoStpGroups_SkipsPaymentCreation()
            {
                var service = CreateStatusFileService();
                var existingPayments = new List<StpPayment>();

                _statusFileTrackerMock.Setup(tracker => tracker.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments);

                var result = await service.CreateStpPaymentRecords(_babStatusFile, "O", "C", "B");

                Assert.NotNull(result);
                Assert.Empty(result);
            }

            [Fact]
            public async Task WhenStatusFileHasStpGroupsWithNewPayments_InsertsPayments()
            {
                var service = CreateStatusFileService();
                var existingPayments = new List<StpPayment>();

                // Test STP status file has these 3 STP payments: 770376643 (B2B), 770376673 (B2B), and 770376701 (C2B)
                // If only B2B payments are being inserted as new payments in database, this count should be 2. 
                // If both B2B and C2B payments are inserted as new payments in database, this count should be 3.
                var newPaymentCount = 2; 

                _statusFileTrackerMock.Setup(tracker => tracker.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments);

                var result = await service.CreateStpPaymentRecords(_stpStatusFile, "O", "C", "B");

                Assert.NotNull(result);
                Assert.NotEmpty(result);
                Assert.Equal(newPaymentCount, result.Count());
            }

            [Fact]
            public async Task WhenStatusFileHasStpGroupsWithExistingPayments_InsertsOnlyNewPayments()
            {
                var service = CreateStatusFileService();
                var existingPayments = new List<StpPayment>
                {
                    new() { TransactionId = 770376643 }
                };

                // Test STP status file has these 3 STP payments: 770376643 (B2B), 770376673 (B2B), and 770376701 (C2B)
                // If only B2B payments are being inserted as new payments in database, this count should be 1.
                // If both B2B and C2B payments are being inserted as new payments in database, this count should be 2.
                var newPaymentCount = 1;

                _statusFileTrackerMock.Setup(tracker => tracker.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments);

                var result = await service.CreateStpPaymentRecords(_stpStatusFile, "O", "C", "B");

                Assert.NotNull(result);
                Assert.NotEmpty(result);
                Assert.Equal(newPaymentCount, result.Count());
            }
        }

        public class GetPrsConfiguration : StatusFileServiceTests
        {
            [Fact]
            public async Task WhenPrsIsEnabled_ResultShowsPrsIsEnabledAsTrue()
            {
                var service = CreateStatusFileService();
                var dto = new OptumPrsConfigurationDto { IsPrsEnabled = true };
                _platformConfigurationApiMock.Setup(api => api.GetOptumPrsConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

                var result = await service.GetPrsConfiguration("C", "O", "B", cancellationToken:CancellationToken.None);
                Assert.NotNull(result);
                Assert.True(result.IsPrsEnabled);
            }

            [Fact]
            public async Task WhenPrsIsDisabled_ResultShowsPrsIsEnabledAsFalse()
            {
                var service = CreateStatusFileService();
                var dto = new OptumPrsConfigurationDto { IsPrsEnabled = false };
                _platformConfigurationApiMock.Setup(api => api.GetOptumPrsConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

                var result = await service.GetPrsConfiguration("C", "O", "B", cancellationToken: CancellationToken.None);
                Assert.NotNull(result);
                Assert.False(result.IsPrsEnabled);
            }
        }

        public class GetEdi835Type : StatusFileServiceTests
        {
            [Fact]
            public async Task WhenPrsPayment_ReturnsEdiTypeAsPrs()
            {
                var service = CreateStatusFileService();
                var dto = new OptumPrsConfigurationDto { IsPrsEnabled = true };
                _platformConfigurationApiMock.Setup(api => api.GetOptumPrsConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

                var result = await service.GetEdi835Type("O", "C", "B", cancellationToken: CancellationToken.None);
                Assert.Equal(Edi835Types.PRS, result);
            }

            [Fact]
            public async Task WhenSdsPayment_ReturnsEdiTypeAsSds()
            {
                var service = CreateStatusFileService();
                var dto = new OptumPrsConfigurationDto { IsPrsEnabled = false };
                _platformConfigurationApiMock.Setup(api => api.GetOptumPrsConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

                var result = await service.GetEdi835Type("O", "C", "B", cancellationToken: CancellationToken.None);
                Assert.Equal(Edi835Types.SDS, result);
            }
        }
    }
}
