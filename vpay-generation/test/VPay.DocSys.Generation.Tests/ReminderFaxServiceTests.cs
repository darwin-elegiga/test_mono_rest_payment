using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class ReminderFaxServiceTests
    {
        protected readonly Mock<IPlatformConfigurationApiClient> _platformConfigurationApiClient = new Mock<IPlatformConfigurationApiClient>();
        protected readonly Mock<IPayorDetailResolver> _payorDetailResolver = new Mock<IPayorDetailResolver>();
        protected readonly Mock<IParsingApiClient> _parsingApiClient = new Mock<IParsingApiClient>();
        protected readonly Mock<IStorageManager> _storageManager = new Mock<IStorageManager>();
        protected readonly Mock<IPdfTools> _pdfTools = new Mock<IPdfTools>();

        protected readonly Mock<IReminderFaxMessageQueuer> _reminderFaxMessageQueuer = new Mock<IReminderFaxMessageQueuer>();

        protected readonly Mock<IReminderFaxErrorReportBuilder> _reminderFaxErrorReportBuilder = new Mock<IReminderFaxErrorReportBuilder>();
        protected readonly Mock<IGreenArrowApiClient> _greenArrowApiClient = new Mock<IGreenArrowApiClient>();

        protected readonly IReminderFaxService SUT;

        protected ReminderFaxRequest _request;
        protected PayorDetail _payorDetail;

        protected Mock<IArchiveDetailStream> _archiveDetailStream = new Mock<IArchiveDetailStream>();

        public ReminderFaxServiceTests()
        {
            _request = new ReminderFaxRequest
            {
                BillingEntity = "TEST BE",
                ClientType = ClientType.MEDICAL,
                CorrespondenceId = 0L,
                UseFaxNumber = "TEST FAX NUM",
                OriginCode = "TEST ORIGIN",
                LastFaxDate = DateTime.Now,
                PayeeName = "TEST PAYEE NAME",
                AvailableBalance = 1.23M,
                ReminderType = ReminderFaxType.REMIND_BEG,
                TPACode = "TEST TPA",
                SeTransactionId = 0L
            };

            var reminderFaxEmailSettings = new ReminderFaxEmailSettings
            {
                FromEmail = "FROM@EMAIL.COM",
                FromName = "TEST FROM",
                ToEmail = "TO@EMAIL.COM",
                ToName = "TEST TO"
            };

            _platformConfigurationApiClient
                .Setup(_ => _.GetOtbDetail(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new OtbDetailResponse
                {
                    BillingEntityCode = "TEST BE CODE",
                    BillingEntityFullName = "TEST BE FULL NAME",
                    ClientCode = "TEST CLIENT CODE",
                    ClientFullName = "TEST CLIENT FULL NAME",
                    ClientPhone = "TEST CLIENT PHONE",
                    OriginCode = "TEST ORIGIN CODE",
                    OriginName = "TEST ORIGIN NAME",
                    VPayPhone = "TEST VPAY PHONE"
                })
                .Verifiable();

            _parsingApiClient
                .Setup(_ => _.GetXmlConfiguration(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ClientVPaygeSettingsResponse
                {
                    Common = new List<FieldConfiguration>(0),
                    VCard = new List<FieldConfiguration>(0),
                })
                .Verifiable();

            _payorDetail = new PayorDetail
            {
                IsEobDependent = true,
                PayorName = "TEST PAYOR NAME",
                PayorPhone = "TEST PAYOR PHONE",
                VPayPhone = "TEST VPAY PHONE"
            };

            _payorDetailResolver
                .Setup(_ => _.ResolvePayorDetails(It.IsAny<ClientType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<OtbDetailResponse>(), It.IsAny<FieldDictionary>()))
                .ReturnsAsync(_payorDetail)
                .Verifiable();

            _payorDetailResolver
                .Setup(_ => _.CreateFieldDictionary(It.IsAny<OtbDetailResponse>(), It.IsAny<ClientVPaygeSettingsResponse>()))
                .Returns(new FieldDictionary())
                .Verifiable();

            _archiveDetailStream.Setup(_ => _.Open()).Returns(() => new MemoryStream());
            _archiveDetailStream.Setup(_ => _.FileName).Returns("TEST FILE NAME.PDF");
            _archiveDetailStream.Setup(_ => _.PageCount).Returns(1);

            _storageManager
                .Setup(_ => _.GetDistributionPacketStream(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(_archiveDetailStream.Object)
                .Verifiable();

            _pdfTools
                .Setup(_ => _.GetNumberOfPagesAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(1)
                .Verifiable();

            _reminderFaxMessageQueuer
                .Setup(_ => _.QueueReminderFaxMessage(It.IsAny<ReminderFaxMessage>()))
                .Verifiable();

            _reminderFaxErrorReportBuilder
                .Setup(_ => _.BuildReport(
                    It.IsAny<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>(),
                    It.IsAny<int>()))
                .Returns("TEST EMAIL REPORT")
                .Verifiable();

            _greenArrowApiClient
                .Setup(_ => _.SendEmail(It.IsAny<IEmailRequest>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            SUT = new ReminderFaxService(
                _platformConfigurationApiClient.Object,
                _parsingApiClient.Object,
                _payorDetailResolver.Object,
                _storageManager.Object,
                _pdfTools.Object,
                _reminderFaxMessageQueuer.Object,
                _reminderFaxErrorReportBuilder.Object,
                _greenArrowApiClient.Object,
                reminderFaxEmailSettings,
                new NullLogger<ReminderFaxService>());
        }

        public class WhenUsingBadData : ReminderFaxServiceTests
        {
            [Fact]
            public void ThrowExceptionOnInvalidParamsInCtor()
            {
                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    null,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    _pdfTools.Object,
                    _reminderFaxMessageQueuer.Object,
                    _reminderFaxErrorReportBuilder.Object,
                    _greenArrowApiClient.Object,
                    new ReminderFaxEmailSettings(),
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    null,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    _pdfTools.Object,
                    _reminderFaxMessageQueuer.Object,
                    _reminderFaxErrorReportBuilder.Object,
                    _greenArrowApiClient.Object,
                    new ReminderFaxEmailSettings(),
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    null,
                    _pdfTools.Object,
                    _reminderFaxMessageQueuer.Object,
                    _reminderFaxErrorReportBuilder.Object,
                    _greenArrowApiClient.Object,
                    new ReminderFaxEmailSettings(),
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    null,
                    _reminderFaxMessageQueuer.Object,
                    _reminderFaxErrorReportBuilder.Object,
                    _greenArrowApiClient.Object,
                    new ReminderFaxEmailSettings(),
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    _pdfTools.Object,
                    null,
                    _reminderFaxErrorReportBuilder.Object,
                    _greenArrowApiClient.Object,
                    new ReminderFaxEmailSettings(),
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    _pdfTools.Object,
                    _reminderFaxMessageQueuer.Object,
                    null,
                    _greenArrowApiClient.Object,
                    new ReminderFaxEmailSettings(),
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    _pdfTools.Object,
                    _reminderFaxMessageQueuer.Object,
                    _reminderFaxErrorReportBuilder.Object,
                    null,
                    new ReminderFaxEmailSettings(),
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    _pdfTools.Object,
                    _reminderFaxMessageQueuer.Object,
                    _reminderFaxErrorReportBuilder.Object,
                    _greenArrowApiClient.Object,
                    null,
                    new NullLogger<ReminderFaxService>()));

                Assert.Throws<ArgumentNullException>(() => new ReminderFaxService(
                    _platformConfigurationApiClient.Object,
                    _parsingApiClient.Object,
                    _payorDetailResolver.Object,
                    _storageManager.Object,
                    _pdfTools.Object,
                    _reminderFaxMessageQueuer.Object,
                    _reminderFaxErrorReportBuilder.Object,
                    _greenArrowApiClient.Object,
                    new ReminderFaxEmailSettings(),
                    null));
            }

            [Fact]
            public async Task ThrowExceptionOnNullReminderFaxRequest()
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => SUT.ProcessReminderFaxRequest(null)).ConfigureAwait(false);
            }

            [Fact]
            public async Task ThrowExceptionOnNullReminderFaxeQueueMessage()
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => SUT.ProcessReminderFaxes(null)).ConfigureAwait(false);
            }
        }

        public class WhenExpectingErrorReport : ReminderFaxServiceTests
        {
            private static readonly Func<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>, ReminderFaxErrorType, bool> ValidateErrorDictionary = new Func<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>, ReminderFaxErrorType, bool>((dictionary, type) => dictionary
            .ToList()
            .Any(_ => _.Key == type
            ? _.Value.Count == 1
            : _.Value.Count == 0));

            public WhenExpectingErrorReport()
            {
                _payorDetailResolver
                   .Setup(_ => _.CreateFieldDictionary(It.IsAny<OtbDetailResponse>(), It.IsAny<ClientVPaygeSettingsResponse>()))
                   .Returns(new FieldDictionary())
                   .Verifiable();
            }

            [Fact]
            public async Task ShouldSendEmailOnMissingOtbInformation()
            {
                _platformConfigurationApiClient
                    .Setup(_ => _.GetOtbDetail(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(null as OtbDetailResponse)
                    .Verifiable();

                await SUT.ProcessReminderFaxRequest(_request).ConfigureAwait(false);

                _platformConfigurationApiClient.Verify();
                _reminderFaxErrorReportBuilder
                    .Verify(_ => _.BuildReport(
                        It.Is<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>
                        (__ => ValidateErrorDictionary(__, ReminderFaxErrorType.MissingOtbInformation)),
                        It.IsAny<int>()),
                        Times.Once);
                _greenArrowApiClient.Verify();

                _platformConfigurationApiClient.VerifyNoOtherCalls();
                _payorDetailResolver.VerifyNoOtherCalls();
                _storageManager.VerifyNoOtherCalls();
                _pdfTools.VerifyNoOtherCalls();
                _reminderFaxMessageQueuer.VerifyNoOtherCalls();
                _reminderFaxErrorReportBuilder.VerifyNoOtherCalls();
                _greenArrowApiClient.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task ShouldSendEmailOnNotResolvingPayorName()
            {
                _payorDetail.PayorName = null;

                _payorDetailResolver
                    .Setup(_ => _.ResolvePayorDetails(It.IsAny<ClientType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<OtbDetailResponse>(), It.IsAny<FieldDictionary>()))
                    .ReturnsAsync(_payorDetail)
                    .Verifiable();

                await SUT.ProcessReminderFaxRequest(_request).ConfigureAwait(false);

                _platformConfigurationApiClient.Verify();
                _payorDetailResolver.Verify();
                _reminderFaxErrorReportBuilder
                    .Verify(_ => _.BuildReport(
                        It.Is<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>
                        (__ => ValidateErrorDictionary(__, ReminderFaxErrorType.CannotResolvePayorName)),
                        It.IsAny<int>()),
                        Times.Once);
                _greenArrowApiClient.Verify();

                _platformConfigurationApiClient.VerifyNoOtherCalls();
                _payorDetailResolver.VerifyNoOtherCalls();
                _storageManager.VerifyNoOtherCalls();
                _pdfTools.VerifyNoOtherCalls();
                _reminderFaxMessageQueuer.VerifyNoOtherCalls();
                _reminderFaxErrorReportBuilder.VerifyNoOtherCalls();
                _greenArrowApiClient.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task ShouldSendEmailOnMissingDistributionPacket()
            {
                _storageManager
                    .Setup(_ => _.GetDistributionPacketStream(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync(null as IArchiveDetailStream)
                    .Verifiable();

                await SUT.ProcessReminderFaxRequest(_request).ConfigureAwait(false);

                _platformConfigurationApiClient.Verify();
                _payorDetailResolver.Verify();
                _storageManager.Verify();
                _reminderFaxErrorReportBuilder
                    .Verify(_ => _.BuildReport(
                        It.Is<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>
                        (__ => ValidateErrorDictionary(__, ReminderFaxErrorType.MissingDistributionPacket)),
                        It.IsAny<int>()),
                        Times.Once);
                _greenArrowApiClient.Verify();

                _platformConfigurationApiClient.VerifyNoOtherCalls();
                _payorDetailResolver.VerifyNoOtherCalls();
                _storageManager.VerifyNoOtherCalls();
                _pdfTools.VerifyNoOtherCalls();
                _reminderFaxMessageQueuer.VerifyNoOtherCalls();
                _reminderFaxErrorReportBuilder.VerifyNoOtherCalls();
                _greenArrowApiClient.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task ShouldSendEmailOnExceptionFromPayorDetailResolver()
            {
                _payorDetailResolver
                    .Setup(_ => _.ResolvePayorDetails(It.IsAny<ClientType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<OtbDetailResponse>(), It.IsAny<FieldDictionary>()))
                    .ThrowsAsync(new Exception())
                    .Verifiable();

                await SUT.ProcessReminderFaxRequest(_request).ConfigureAwait(false);

                _platformConfigurationApiClient.Verify();
                _payorDetailResolver.Verify();
                _reminderFaxErrorReportBuilder
                    .Verify(_ => _.BuildReport(
                        It.Is<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>
                        (__ => ValidateErrorDictionary(__, ReminderFaxErrorType.GenericError)),
                        It.IsAny<int>()),
                        Times.Once);
                _greenArrowApiClient.Verify();

                _platformConfigurationApiClient.VerifyNoOtherCalls();
                _payorDetailResolver.VerifyNoOtherCalls();
                _storageManager.VerifyNoOtherCalls();
                _pdfTools.VerifyNoOtherCalls();
                _reminderFaxMessageQueuer.VerifyNoOtherCalls();
                _reminderFaxErrorReportBuilder.VerifyNoOtherCalls();
                _greenArrowApiClient.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task ShouldSendEmailOnGenericError()
            {
                _payorDetailResolver
                    .Setup(_ => _.ResolvePayorDetails(It.IsAny<ClientType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<OtbDetailResponse>(), It.IsAny<FieldDictionary>()))
                    .ReturnsAsync(null as PayorDetail)
                    .Verifiable();

                await SUT.ProcessReminderFaxRequest(_request).ConfigureAwait(false);

                _platformConfigurationApiClient.Verify();
                _payorDetailResolver.Verify();
                _reminderFaxErrorReportBuilder
                    .Verify(_ => _.BuildReport(
                        It.Is<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>
                        (__ => ValidateErrorDictionary(__, ReminderFaxErrorType.GenericError)),
                        It.IsAny<int>()),
                        Times.Once);
                _greenArrowApiClient.Verify();

                _platformConfigurationApiClient.VerifyNoOtherCalls();
                _payorDetailResolver.VerifyNoOtherCalls();
                _storageManager.VerifyNoOtherCalls();
                _pdfTools.VerifyNoOtherCalls();
                _reminderFaxMessageQueuer.VerifyNoOtherCalls();
                _reminderFaxErrorReportBuilder.VerifyNoOtherCalls();
                _greenArrowApiClient.VerifyNoOtherCalls();
            }
        }

        public class WhenProcessingReminderFaxes : ReminderFaxServiceTests
        {
            [Theory]
            [MemberData(nameof(EmptyReminderFaxes))]
            public async Task ShouldDoNothingIfNoReminderFaxes(ReminderFax[] reminderFaxes)
            {
                _reminderFaxErrorReportBuilder
                    .Setup(_ => _.BuildReport(
                        It.IsAny<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>(),
                        It.IsAny<int>()))
                    .Returns(string.Empty)
                    .Verifiable();

                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = reminderFaxes
                }).ConfigureAwait(false);

                _platformConfigurationApiClient.VerifyNoOtherCalls();
                _payorDetailResolver.VerifyNoOtherCalls();
                _pdfTools.VerifyNoOtherCalls();
                _storageManager.VerifyNoOtherCalls();
                _reminderFaxMessageQueuer.VerifyNoOtherCalls();
                _reminderFaxErrorReportBuilder.VerifyNoOtherCalls();
                _greenArrowApiClient.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(ValidReminderFaxQueueMessages))]
            public async Task ShouldAlwaysDoTheFollowingForEachReminderFax(ReminderFaxQueueMessage reminderFaxQueueMessage)
            {
                var count = Times.Exactly(reminderFaxQueueMessage.ReminderFaxes.Length);

                await SUT.ProcessReminderFaxes(reminderFaxQueueMessage).ConfigureAwait(false);

                _payorDetailResolver.Verify(
                    _ => _.ResolvePayorDetails(It.IsAny<ClientType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<OtbDetailResponse>(), It.IsAny<FieldDictionary>()),
                    count);
                _storageManager.Verify(_ => _.GetDistributionPacketStream(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>()), count);
                _reminderFaxMessageQueuer.Verify(_ => _.QueueReminderFaxMessage(It.IsAny<ReminderFaxMessage>()), count);
            }

            [Theory]
            [InlineData(ClientType.MEDICAL)]
            [InlineData(ClientType.WARRANTY)]
            public async Task ShouldNotCreateErrorReportWhenSuccessful(ClientType clientType)
            {
                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = new ReminderFax[] { new ReminderFax { ClientType = clientType } }
                }).ConfigureAwait(false);

                _reminderFaxErrorReportBuilder
                    .Verify(_ => _.BuildReport(
                        It.Is<IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>>>
                        (__ =>
                        __[ReminderFaxErrorType.CannotResolvePayorName].Count == 0 &&
                        __[ReminderFaxErrorType.MissingOtbInformation].Count == 0 &&
                        __[ReminderFaxErrorType.MissingDistributionPacket].Count == 0),
                        It.IsAny<int>()),
                        Times.Once);
            }

            [Theory]
            [MemberData(nameof(PayorDetails))]
            public async Task ShouldUseSamePayorDetailIfNotEobDependent(PayorDetail[] payorDetails, int resolveCount)
            {
                var faxes = new List<ReminderFax>();
                for (int i = 0; i < payorDetails.Length; i++) { faxes.Add(new ReminderFax()); }

                _payorDetailResolver
                    .Setup(_ => _.ResolvePayorDetails(
                        It.IsAny<ClientType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<long>(),
                        It.IsAny<OtbDetailResponse>(),
                        It.IsAny<FieldDictionary>()))
                    .ReturnsAsync(new Queue<PayorDetail>(payorDetails).Dequeue);

                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = faxes.ToArray()
                }).ConfigureAwait(false);

                _payorDetailResolver.Verify(_ => _.ResolvePayorDetails(
                    It.IsAny<ClientType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<OtbDetailResponse>(), It.IsAny<FieldDictionary>()),
                    Times.Exactly(resolveCount));
            }

            [Theory]
            [MemberData(nameof(ReminderFaxesForOtb))]
            public async Task ShouldGetOtbDetailForEachDistinctBillingEntityTpaCombination(ReminderFax[] reminderFaxes, int distinctOtb)
            {
                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = reminderFaxes
                }).ConfigureAwait(false);

                _platformConfigurationApiClient.Verify(
                    _ => _.GetOtbDetail(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Exactly(distinctOtb));
            }

            [Theory]
            [MemberData(nameof(ReminderFaxesForOtb))]
            public async Task ShouldGetXmlConfigurationForEachDistinctBillingEntityTpaCombinationWhenMedicalClients(ReminderFax[] reminderFaxes, int distinctOtb)
            {
                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = reminderFaxes
                }).ConfigureAwait(false);

                _parsingApiClient.Verify(
                    _ => _.GetXmlConfiguration(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Exactly(distinctOtb));
            }

            [Theory]
            [MemberData(nameof(ReminderFaxesForOtb))]
            public async Task ShouldGetCreateFieldDictionaryForEachDistinctBillingEntityTpaCombinationWhenMedicalClients(ReminderFax[] reminderFaxes, int distinctOtb)
            {
                foreach (var r in reminderFaxes)
                {
                    r.ClientType = ClientType.MEDICAL;
                }

                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = reminderFaxes
                }).ConfigureAwait(false);

                _payorDetailResolver.Verify(_ =>
                    _.CreateFieldDictionary(It.IsAny<OtbDetailResponse>(), It.IsAny<ClientVPaygeSettingsResponse>()),
                    Times.Exactly(distinctOtb));

                _payorDetailResolver.Verify(_ =>
                    _.ResolvePayorDetails(
                        It.IsAny<ClientType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<long>(),
                        It.IsAny<OtbDetailResponse>(),
                        It.Is<FieldDictionary>(__ => __ != null)),
                    Times.Exactly(reminderFaxes.Length));
            }

            [Theory]
            [MemberData(nameof(ReminderFaxes))]
            public async Task ShouldNotGetXmlConfigurationForWarrantyClients(ReminderFax[] reminderFaxes)
            {
                foreach (var r in reminderFaxes)
                {
                    r.ClientType = ClientType.WARRANTY;
                }

                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = reminderFaxes
                }).ConfigureAwait(false);

                _parsingApiClient.Verify(
                    _ => _.GetXmlConfiguration(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Never);
            }

            [Theory]
            [MemberData(nameof(ReminderFaxes))]
            public async Task ShouldNotCreateFieldDictionaryForWarrantyClients(ReminderFax[] reminderFaxes)
            {
                foreach (var r in reminderFaxes)
                {
                    r.ClientType = ClientType.WARRANTY;
                }

                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    EndDate = DateTime.Now,
                    StartDate = DateTime.Now,
                    ReminderFaxes = reminderFaxes
                }).ConfigureAwait(false);

                _payorDetailResolver.Verify(_ =>
                    _.CreateFieldDictionary(It.IsAny<OtbDetailResponse>(), It.IsAny<ClientVPaygeSettingsResponse>()),
                    Times.Never);

                _payorDetailResolver.Verify(_ =>
                    _.ResolvePayorDetails(
                        It.IsAny<ClientType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<long>(),
                        It.IsAny<OtbDetailResponse>(),
                        null),
                    Times.Exactly(reminderFaxes.Length));
            }

            [Theory]
            [InlineData(0)]
            [InlineData(-1)]
            [InlineData(1)]
            public async Task ShouldOnlyGetPageCountIfLessThanOne(int pageCount)
            {
                var times = pageCount > 0 ? Times.Never() : Times.Once();

                _archiveDetailStream.Setup(_ => _.PageCount).Returns(pageCount);

                await SUT.ProcessReminderFaxes(new ReminderFaxQueueMessage
                {
                    ReminderFaxes = new ReminderFax[] { new ReminderFax() }
                }).ConfigureAwait(false);

                _pdfTools.Verify(_ => _.GetNumberOfPagesAsync(It.IsAny<byte[]>()), times);
            }

            public static IEnumerable<object[]> ValidReminderFaxQueueMessages
            {
                get
                {
                    var list = new List<ReminderFax>();

                    // default: 1, 2, 3, 4 faxes
                    for (int i = 1; i < 4; i++)
                    {
                        list.Add(new ReminderFax());

                        yield return new object[] { new ReminderFaxQueueMessage { ReminderFaxes = list.ToArray() } };
                    }

                    // 1 medical
                    yield return new object[] {
                        new ReminderFaxQueueMessage {
                            ReminderFaxes = new[]
                            {
                                new ReminderFax
                                {
                                    ClientType= ClientType.MEDICAL,
                                    TPACode= "TPA1",
                                    BillingEntity= "BE1"
                                }
                            }
                        }
                    };

                    // 1 warranty
                    yield return new object[] {
                        new ReminderFaxQueueMessage {
                            ReminderFaxes = new[]
                            {
                                new ReminderFax
                                {
                                    ClientType= ClientType.WARRANTY,
                                    TPACode= "WTPA1",
                                    BillingEntity= "WBE1"
                                }
                            }
                        }
                    };

                    // 2 medical, same tpa/be
                    yield return new object[] {
                        new ReminderFaxQueueMessage {
                            ReminderFaxes = new[]
                            {
                                new ReminderFax
                                {
                                    ClientType= ClientType.MEDICAL,
                                    TPACode= "TPA1",
                                    BillingEntity= "BE1"
                                },
                                new ReminderFax
                                {
                                    ClientType= ClientType.MEDICAL,
                                    TPACode= "TPA1",
                                    BillingEntity= "BE1"
                                }
                            }
                        }
                    };

                    // 2 medical, different be, same tpa
                    yield return new object[] {
                        new ReminderFaxQueueMessage {
                            ReminderFaxes = new[]
                            {
                                new ReminderFax
                                {
                                    ClientType= ClientType.MEDICAL,
                                    TPACode= "TPA1",
                                    BillingEntity= "BE1"
                                },
                                new ReminderFax
                                {
                                    ClientType= ClientType.MEDICAL,
                                    TPACode= "TPA1",
                                    BillingEntity= "BE2"
                                }
                            }
                        }
                    };

                    // 2 medical, different tpa/be
                    yield return new object[] {
                        new ReminderFaxQueueMessage {
                            ReminderFaxes = new[]
                            {
                                new ReminderFax
                                {
                                    ClientType= ClientType.MEDICAL,
                                    TPACode= "TPA1",
                                    BillingEntity= "BE1"
                                },
                                new ReminderFax
                                {
                                    ClientType= ClientType.MEDICAL,
                                    TPACode= "TPA2",
                                    BillingEntity= "BE2"
                                }
                            }
                        }
                    };
                }
            }

            public static IEnumerable<object[]> EmptyReminderFaxes
            {
                get
                {
                    yield return new object[] { null };
                    yield return new object[] { Array.Empty<ReminderFax>() };
                }
            }

            public static IEnumerable<object[]> PayorDetails
            {
                get
                {
                    yield return new object[] {
                        new PayorDetail[] {
                            new PayorDetail{ PayorName = " " },
                            new PayorDetail{ PayorName = " " },
                        }, 1 };

                    yield return new object[] {
                        new PayorDetail[] {
                            new PayorDetail{ IsEobDependent = true, PayorName = " " },
                            new PayorDetail{ IsEobDependent = true, PayorName = " " },
                        }, 2 };
                }
            }

            public static IEnumerable<object[]> ReminderFaxes
            {
                get
                {
                    yield return new object[] {
                        new ReminderFax[] {
                            new ReminderFax { },
                            new ReminderFax { },
                            new ReminderFax { },
                        }
                    };

                    yield return new object[] {
                        new ReminderFax[] {
                            new ReminderFax { BillingEntity = "be1" },
                            new ReminderFax { },
                            new ReminderFax { },
                        }
                    };

                    yield return new object[] {
                        new ReminderFax[] {
                            new ReminderFax { BillingEntity = "be1" },
                            new ReminderFax { BillingEntity = "be1", TPACode = "tpa1" },
                            new ReminderFax { TPACode = "tpa2"},
                        }
                    };
                }
            }

            public static IEnumerable<object[]> ReminderFaxesForOtb
            {
                get
                {
                    foreach (var rm in ReminderFaxes)
                    {
                        yield return new object[] {
                            rm[0],
                            (rm[0] as ReminderFax[])
                            .GroupBy(_ => { (string TPACode, string BillingEntity) k = (_.TPACode, _.BillingEntity); return k; })
                            .Count()
                        };
                    }
                }
            }
        }
    }
}
