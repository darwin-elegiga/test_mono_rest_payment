using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Refit;
using VPay.Distribution.Common;
using VPay.Distribution.Common.Interfaces;
using VPay.Distribution.Common.Models;
using VPay.Distribution.Http.Abstractions;
using VPay.DocSys.Data;
using Xunit;

namespace VPay.Distribution.Tests
{
    /// <summary>
    /// Tests for <see cref="StpPaymentService"/>.
    /// </summary>
    public class StpPaymentServiceTests
    {
        private readonly IStpPaymentRepository _repository;
        private readonly IPmbMessagePublisher _pmbPublisher;
        private readonly IPaymentRoutingApi _paymentRoutingApi;
        private readonly IRemittanceService _remittanceService;
        private readonly IStorageService _storageService;
        private readonly ILogger<StpPaymentService> _logger;
        private readonly StpPaymentService _service;

        private const string NetworkName = "MY-NETWORK";
        private const string B2BEnrollType = "B2B";
        private const string C2BEnrollType = "C2B";
        private const string SdsEdi835Type = "SDS";
        private const string PrsEdi835Type = "PRS";

        public StpPaymentServiceTests()
        {
            _repository = Mock.Of<IStpPaymentRepository>(MockBehavior.Strict);
            _pmbPublisher = Mock.Of<IPmbMessagePublisher>(MockBehavior.Strict);
            _paymentRoutingApi = Mock.Of<IPaymentRoutingApi>(MockBehavior.Strict);
            _remittanceService = Mock.Of<IRemittanceService>(MockBehavior.Strict);
            _storageService = Mock.Of<IStorageService>(MockBehavior.Strict);
            _logger = new NullLogger<StpPaymentService>();
            _service = new StpPaymentService(
                _repository,
                _pmbPublisher,
                _paymentRoutingApi,
                _remittanceService,
                _storageService,
                _logger
            );
        }

        #region Constructor

        public class Constructor : StpPaymentServiceTests
        {
            [Fact]
            public void Should_Initialize_Service()
            {
                // Arrange
                var service = new StpPaymentService(
                    _repository,
                    _pmbPublisher,
                    _paymentRoutingApi,
                    _remittanceService,
                    _storageService,
                    _logger
                );

                Assert.NotNull(service);
            }

            [Fact]
            public void WhenRepositoryIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StpPaymentService(
                        null!,
                        _pmbPublisher,
                        _paymentRoutingApi,
                        _remittanceService,
                        _storageService,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("repository");
            }

            [Fact]
            public void WhenPmbPublisherIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StpPaymentService(
                        _repository,
                        null!,
                        _paymentRoutingApi,
                        _remittanceService,
                        _storageService,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("pmbPublisher");
            }

            [Fact]
            public void WhenPaymentRoutingApiIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StpPaymentService(
                        _repository,
                        _pmbPublisher,
                        null!,
                        _remittanceService,
                        _storageService,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("paymentRoutingApi");
            }

            [Fact]
            public void WhenRemittanceServiceIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StpPaymentService(
                        _repository,
                        _pmbPublisher,
                        _paymentRoutingApi,
                        null!,
                        _storageService,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("remittanceService");
            }

            [Fact]
            public void WhenStorageServiceIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StpPaymentService(
                        _repository,
                        _pmbPublisher,
                        _paymentRoutingApi,
                        _remittanceService,
                        null!,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("storageService");
            }

            [Fact]
            public void WhenLoggerIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StpPaymentService(
                        _repository,
                        _pmbPublisher,
                        _paymentRoutingApi,
                        _remittanceService,
                        _storageService,
                        null!);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("logger");
            }
        }

        #endregion

        #region Stp Payments

        public class GetStpPayment : StpPaymentServiceTests
        {
            [Fact]
            public async Task GetStpPayment_ReturnsPayment()
            {
                var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type);

                Mock.Get(_repository)
                    .Setup(r => r.GetStpPayment(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payment);

                var result = await _service.GetStpPayment(1);

                result.Should().Be(payment);
            }

            [Fact]
            public async Task GetStpPayment_ThrowsOnException()
            {
                Mock.Get(_repository)
                    .Setup(r => r.GetStpPayment(1, It.IsAny<CancellationToken>()))
                    .Throws(new InvalidOperationException("fail"));

                Func<Task> act = async () => await _service.GetStpPayment(1);

                await act.Should().ThrowAsync<InvalidOperationException>();
            }
        }

        public class GetStpPayments : StpPaymentServiceTests
        {
            [Fact]
            public async Task GetStpPayments_ReturnsPayments()
            {
                var payments = MockStpPayments();

                Mock.Get(_repository)
                    .Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payments);

                var result = await _service.GetStpPayments([1L]);

                result.Should().BeEquivalentTo(payments);
            }

            [Fact]
            public async Task GetStpPayments_ThrowsOnException()
            {
                Mock.Get(_repository)
                    .Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                Func<Task> act = async () => await _service.GetStpPayments([1L]);

                await act.Should().ThrowAsync<Exception>();
            }
        }

        public class SearchStpPayments : StpPaymentServiceTests
        {
            [Fact]
            public async Task SearchStpPayments_ReturnsPayments()
            {
                var payments = MockStpPayments();

                Mock.Get(_repository)
                    .Setup(r => r.SearchStpPayments(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                    It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payments);

                var result = await _service.SearchStpPayments();

                result.Should().BeEquivalentTo(payments);
            }

            [Fact]
            public async Task SearchStpPayments_ThrowsOnException()
            {
                Mock.Get(_repository)
                    .Setup(r => r.SearchStpPayments(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                    It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                Func<Task> act = async () => await _service.SearchStpPayments();

                await act.Should().ThrowAsync<Exception>();
            }
        }

        #endregion

        #region Stp Payment Status

        public class MarkRemitValid : StpPaymentServiceTests
        {
            [Fact]
            public async Task MarkRemitValid_ReturnsPayment()
            {
                var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type);

                Mock.Get(_repository)
                    .Setup(r => r.MarkRemitValid(1, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payment);

                var result = await _service.MarkRemitValid(1);

                result.Should().Be(payment);
            }

            [Fact]
            public async Task MarkRemitValid_ThrowsOnException()
            {
                Mock.Get(_repository)
                    .Setup(r => r.MarkRemitValid(1, null, It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                Func<Task> act = async () => await _service.MarkRemitValid(1);

                await act.Should().ThrowAsync<Exception>();
            }
        }

        public class MarkEdi835Avail : StpPaymentServiceTests
        {
            [Fact]
            public async Task MarkEdi835Avail_ReturnsPayment()
            {
                var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type);

                Mock.Get(_repository).Setup(r => r.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payment);

                var result = await _service.MarkEdi835Avail(1);

                result.Should().Be(payment);
            }

            [Fact]
            public async Task MarkEdi835Avail_ThrowsOnException()
            {
                Mock.Get(_repository)
                    .Setup(r => r.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                Func<Task> act = async () => await _service.MarkEdi835Avail(1);

                await act.Should().ThrowAsync<Exception>();
            }
        }

        public class MarkPaymentSent : StpPaymentServiceTests
        {
            [Fact]
            public async Task MarkPaymentSent_ReturnsPayment()
            {
                var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type);

                Mock.Get(_repository)
                    .Setup(r => r.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payment);

                var result = await _service.MarkPaymentSent(1);

                result.Should().Be(payment);
            }

            [Fact]
            public async Task MarkPaymentSent_ThrowsOnException()
            {
                Mock.Get(_repository)
                    .Setup(r => r.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                Func<Task> act = async () => await _service.MarkPaymentSent(1);

                await act.Should().ThrowAsync<Exception>();
            }
        }

        public class MarkPaymentNotSent : StpPaymentServiceTests
        {
            [Fact]
            public async Task MarkPaymentNotSent_ReturnsPayment()
            {
                var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type);

                Mock.Get(_repository)
                    .Setup(r => r.MarkPaymentNotSent(1, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(payment);

                var result = await _service.MarkPaymentNotSent(1);

                result.Should().Be(payment);
            }

            [Fact]
            public async Task MarkPaymentNotSent_ThrowsOnException()
            {
                Mock.Get(_repository)
                    .Setup(r => r.MarkPaymentNotSent(1, null, It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                Func<Task> act = async () => await _service.MarkPaymentNotSent(1);

                await act.Should().ThrowAsync<Exception>();
            }
        }

        #endregion

        #region PMB Messages

        public class CreatePmbMessage : StpPaymentServiceTests
        {
            [Fact]
            public void CreatePmbMessage_ReturnsMessage()
            {
                var result = _service.CreatePmbMessage(123);

                result.Should().NotBeNull();
                result.Data.Should().Contain("123");
                result.FromSystem.Should().StartWith("FAXNET");
                result.ToSystem.Should().StartWith("FAXISERIES");
                result.Family.Should().StartWith("FAXDIST");
            }
        }

        public class PublishPmbMessage : StpPaymentServiceTests
        {
            [Fact]
            public async Task PublishPmbMessage_CallsPublisher()
            {
                Mock.Get(_pmbPublisher)
                    .Setup(p => p.SendAsync(It.IsAny<PmbMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                await _service.PublishPmbMessage(123);

                Mock.Get(_pmbPublisher).Verify();
            }
        }

        public class SendPmbMessage : StpPaymentServiceTests
        {
            [Fact]
            public async Task SendPmbMessage_CreatesAndPublishesMessage()
            {
                var stpPayment = MockStpPayment(123, NetworkName, B2BEnrollType, PrsEdi835Type);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(stpPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayment);

                Mock.Get(_pmbPublisher)
                    .Setup(p => p.SendAsync(It.IsAny<PmbMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                await _service.SendPmbMessage(stpPayment);

                Mock.Get(_pmbPublisher).Verify();
            }
        }

        #endregion

        #region Pending Stp Payments

        public class StartPendingToSend : StpPaymentServiceTests
        {
            [Fact]
            public async Task StartPendingTransactions_ProcessesSdsAndPrsAndSendsPmbMessages()
            {
                // Arrange
                var sdsPayment = new StpPayment { TransactionId = 1, EnrollType = "B2B", Edi835Type = "SDS" };
                var prsPayment = new StpPayment { TransactionId = 2, EnrollType = "B2B", Edi835Type = "PRS" };

                var sdsPayments = new List<StpPayment> { sdsPayment };
                var prsPayments = new List<StpPayment> { prsPayment };
                var sendPayments = new List<StpPayment> { sdsPayment, prsPayment };

                var sds835Contents = "EDI";
                var prs835Exists = new Outbound835ExistsDto { TransactionId = prsPayment.TransactionId, Exists = true };

                var json = "remit-json";
                var jsonValid = true;

                // SDS
                Mock.Get(_repository).SetupSequence(r => r.SearchStpPayments(
                    It.IsAny<string>(), "B2B", "SDS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayments);

                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sds835Contents);

                Mock.Get(_repository).Setup(m => m.MarkEdi835Avail(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPaymentSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_storageService).Setup(m => m.GetRemitJson(sdsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService).Setup(m => m.PostRemitDocument(sdsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // PRS
                Mock.Get(_repository).SetupSequence(r => r.SearchStpPayments(
                    It.IsAny<string>(), "B2B", "PRS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayments);

                Mock.Get(_paymentRoutingApi).Setup(m => m.CanDownloadOutbound835Document(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prs835Exists);

                Mock.Get(_repository).Setup(m => m.MarkEdi835Avail(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPaymentSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_storageService).Setup(m => m.GetRemitJson(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService).Setup(m => m.PostRemitDocument(prsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // Act
                await _service.StartPendingToSend();

                // Assert
                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(1, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(2, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(2, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(2, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(2, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

                Mock.Get(_pmbPublisher).Verify(p => p.SendAsync(It.IsAny<PmbMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            }

            [Fact]
            public async Task StartPendingTransactions_DoesNotProcessPrsIfPrs835NotExists()
            {
                // Arrange
                var sdsPayment = new StpPayment { TransactionId = 1, EnrollType = "B2B", Edi835Type = "SDS" };
                var prsPayment = new StpPayment { TransactionId = 2, EnrollType = "B2B", Edi835Type = "PRS" };

                var sdsPayments = new List<StpPayment> { sdsPayment };
                var prsPayments = new List<StpPayment> { prsPayment };
                var sendPayments = new List<StpPayment> { sdsPayment, prsPayment };

                var sds835Contents = "EDI";
                var prs835Exists = new Outbound835ExistsDto { TransactionId = prsPayment.TransactionId, Exists = false };

                var json = "remit-json";
                var jsonValid = true;

                // SDS
                Mock.Get(_repository)
                    .SetupSequence(r => r.SearchStpPayments(It.IsAny<string>(), "B2B", "SDS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayments);

                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sds835Contents);

                Mock.Get(_repository)
                    .Setup(m => m.MarkEdi835Avail(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository)
                    .Setup(m => m.MarkPaymentSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_storageService)
                    .Setup(m => m.GetRemitJson(sdsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService)
                    .Setup(m => m.PostRemitDocument(sdsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // PRS
                Mock.Get(_repository)
                    .SetupSequence(r => r.SearchStpPayments(It.IsAny<string>(), "B2B", "PRS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayments);

                Mock.Get(_paymentRoutingApi)
                    .Setup(api => api.CanDownloadOutbound835Document(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prs835Exists);

                Mock.Get(_repository)
                    .Setup(m => m.MarkEdi835Avail(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository)
                    .Setup(m => m.MarkPaymentSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_storageService)
                    .Setup(m => m.GetRemitJson(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService)
                    .Setup(m => m.PostRemitDocument(prsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // Act
                await _service.StartPendingToSend();

                // Assert
                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(1, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(2, null, It.IsAny<CancellationToken>()), Times.Never);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(2, null, It.IsAny<CancellationToken>()), Times.Never);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(2, It.IsAny<CancellationToken>()), Times.Never);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(2, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

                Mock.Get(_pmbPublisher).Verify(p => p.SendAsync(It.IsAny<PmbMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            }

            [Fact]
            public async Task StartPendingTransactions_DoesNotProcessSdsIfSds835NotExists()
            {
                // Arrange
                var sdsPayment = new StpPayment { TransactionId = 1, EnrollType = "B2B", Edi835Type = "SDS" };
                var prsPayment = new StpPayment { TransactionId = 2, EnrollType = "B2B", Edi835Type = "PRS" };

                var sdsPayments = new List<StpPayment> { sdsPayment };
                var prsPayments = new List<StpPayment> { prsPayment };
                var sendPayments = new List<StpPayment> { sdsPayment, prsPayment };

                var sds835Contents = "";
                var prs835Exists = new Outbound835ExistsDto { TransactionId = prsPayment.TransactionId, Exists = true };

                var json = "remit-json";
                var jsonValid = true;

                // SDS
                Mock.Get(_repository).SetupSequence(r => r.SearchStpPayments(
                    It.IsAny<string>(), "B2B", "SDS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayments);

                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sds835Contents);

                Mock.Get(_repository).Setup(m => m.MarkEdi835Avail(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPaymentSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_storageService).Setup(m => m.GetRemitJson(sdsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService).Setup(m => m.PostRemitDocument(sdsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // PRS
                Mock.Get(_repository).SetupSequence(r => r.SearchStpPayments(
                    It.IsAny<string>(), "B2B", "PRS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayments);

                Mock.Get(_paymentRoutingApi).Setup(api => api.CanDownloadOutbound835Document(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prs835Exists);

                Mock.Get(_repository).Setup(m => m.MarkEdi835Avail(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPaymentSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_storageService).Setup(m => m.GetRemitJson(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService).Setup(m => m.PostRemitDocument(prsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // Act
                await _service.StartPendingToSend();

                // Assert
                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()), Times.Never);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()), Times.Never);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(1, It.IsAny<CancellationToken>()), Times.Never);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(2, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(2, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(2, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(2, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

                Mock.Get(_pmbPublisher).Verify(p => p.SendAsync(It.IsAny<PmbMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            }

            [Fact]
            public async Task StartPendingTransactions_ProcessPmbIfSds835sOrPrs835sExist()
            {
                // Arrange
                var sdsPayment = new StpPayment { TransactionId = 1, EnrollType = "B2B", Edi835Type = "SDS" };
                var prsPayment = new StpPayment { TransactionId = 2, EnrollType = "B2B", Edi835Type = "PRS" };

                var sdsPayments = new List<StpPayment> { sdsPayment };
                var prsPayments = new List<StpPayment> { prsPayment };
                var sendPayments = new List<StpPayment>();

                var sds835Contents = "EDI";
                var prs835Exists = new Outbound835ExistsDto { TransactionId = prsPayment.TransactionId, Exists = true };

                var json = "remit-json";
                var jsonValid = true;

                // SDS
                Mock.Get(_repository).SetupSequence(r => r.SearchStpPayments(
                    It.IsAny<string>(), "B2B", "SDS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayments);

                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sds835Contents);

                Mock.Get(_repository).Setup(m => m.MarkEdi835Avail(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPaymentSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(sdsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sdsPayment);

                Mock.Get(_storageService).Setup(m => m.GetRemitJson(sdsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService).Setup(m => m.PostRemitDocument(sdsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // PRS
                Mock.Get(_repository).SetupSequence(r => r.SearchStpPayments(
                    It.IsAny<string>(), "B2B", "PRS", null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayments);

                Mock.Get(_paymentRoutingApi).Setup(m => m.CanDownloadOutbound835Document(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prs835Exists);

                Mock.Get(_repository).Setup(m => m.MarkEdi835Avail(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPaymentSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_repository).Setup(m => m.MarkPmbMessageSent(prsPayment.TransactionId, null, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prsPayment);

                Mock.Get(_storageService).Setup(m => m.GetRemitJson(prsPayment.TransactionId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(json);

                Mock.Get(_remittanceService).Setup(m => m.PostRemitDocument(prsPayment.TransactionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonValid);

                // Act
                await _service.StartPendingToSend();

                // Assert
                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(1, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

                Mock.Get(_repository).Verify(r => r.MarkEdi835Avail(2, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_repository).Verify(r => r.MarkPaymentSent(2, null, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_storageService).Verify(r => r.GetRemitJson(2, It.IsAny<CancellationToken>()), Times.Once);
                Mock.Get(_remittanceService).Verify(r => r.PostRemitDocument(2, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

                Mock.Get(_pmbPublisher).Verify(p => p.SendAsync(It.IsAny<PmbMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
        }

        #endregion

        #region 835 Documents

        public class CanDownloadSds835 : StpPaymentServiceTests
        {
            [Fact]
            public async Task CanDownloadSds835Document_ReturnsTrueOnSuccess()
            {
                var sds835Contents = "EDI";
                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sds835Contents);

                var result = await _service.CanDownloadSds835(1, CancellationToken.None);

                result.Exists.Should().BeTrue();
            }

            [Fact]
            public async Task CanDownloadSds835Document_ReturnsFalseOnRefitNotFound()
            {
                var expectedStatusCode = System.Net.HttpStatusCode.NotFound;
                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .Throws(await ApiException.Create(
                    new HttpRequestMessage(),
                    HttpMethod.Get,
                    new HttpResponseMessage(expectedStatusCode),
                    new RefitSettings()));

                var result = await _service.CanDownloadSds835(1, CancellationToken.None);

                result.Exists.Should().BeFalse();
            }

            [Fact]
            public async Task CanDownloadSds835Document_ReturnsFalseOnException()
            {
                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                var result = await _service.CanDownloadSds835(1, CancellationToken.None);

                result.Exists.Should().BeFalse();
            }

            [Fact]
            public async Task CanDownloadSds835Document_ReturnsFalseOnRefitOtherThanNotFound()
            {
                var expectedStatusCode = System.Net.HttpStatusCode.BadRequest;
                Mock.Get(_storageService)
                    .Setup(r => r.DownloadEdi835(1, It.IsAny<CancellationToken>()))
                    .Throws(await ApiException.Create(
                    new HttpRequestMessage(),
                    HttpMethod.Get,
                    new HttpResponseMessage(expectedStatusCode),
                    new RefitSettings()));
                var result = await _service.CanDownloadSds835(1, CancellationToken.None);
                result.Exists.Should().BeFalse();
            }
        }

        public class CanDownloadPrs835 : StpPaymentServiceTests

        {
            [Fact]
            public async Task CanDownloadPrs835Document_ReturnsTrueOnSuccess()
            {
                var prs835Exists = new Outbound835ExistsDto { TransactionId = 1, Exists = true };
                Mock.Get(_paymentRoutingApi)
                    .Setup(r => r.CanDownloadOutbound835Document(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(prs835Exists);

                var result = await _service.CanDownloadPrs835(1, CancellationToken.None);

                result.Exists.Should().BeTrue();
            }

            [Fact]
            public async Task CanDownloadPrs835Document_ReturnsFalseOnRefitNotFound()
            {
                var expectedStatusCode = System.Net.HttpStatusCode.NotFound;
                Mock.Get(_paymentRoutingApi)
                    .Setup(r => r.CanDownloadOutbound835Document(1, It.IsAny<CancellationToken>()))
                    .Throws(await ApiException.Create(
                    new HttpRequestMessage(),
                    HttpMethod.Get,
                    new HttpResponseMessage(expectedStatusCode),
                    new RefitSettings()));

                var result = await _service.CanDownloadPrs835(1, CancellationToken.None);

                result.Exists.Should().BeFalse();
            }

            [Fact]
            public async Task CanDownloadPrs835Document_ReturnsFalseOnException()
            {
                Mock.Get(_paymentRoutingApi)
                    .Setup(r => r.CanDownloadOutbound835Document(1, It.IsAny<CancellationToken>()))
                    .Throws(new Exception("fail"));

                var result = await _service.CanDownloadPrs835(1, CancellationToken.None);

                result.Exists.Should().BeFalse();
            }

            [Fact]
            public async Task CanDownloadPrs835Document_ReturnsFalseOnRefitOtherThanNotFound()
            {
                var expectedStatusCode = System.Net.HttpStatusCode.BadRequest;
                Mock.Get(_paymentRoutingApi)
                    .Setup(r => r.CanDownloadOutbound835Document(1, It.IsAny<CancellationToken>()))
                    .Throws(await ApiException.Create(
                    new HttpRequestMessage(),
                    HttpMethod.Get,
                    new HttpResponseMessage(expectedStatusCode),
                    new RefitSettings()));
                var result = await _service.CanDownloadPrs835(1, CancellationToken.None);
                result.Exists.Should().BeFalse();
            }
        }

        #endregion

        #region Helpers

        private static IList<StpPayment> MockStpPayments()
        {
            return
            [
                MockStpPayment(1, NetworkName, B2BEnrollType, SdsEdi835Type),
                MockStpPayment(2, NetworkName, B2BEnrollType, PrsEdi835Type, remitValid: true),
                MockStpPayment(3, NetworkName, B2BEnrollType, PrsEdi835Type, remitValid: true, edi835Avail: true),
                MockStpPayment(4, NetworkName, B2BEnrollType, PrsEdi835Type, remitValid: true, edi835Avail: true, paymentSent: true)
            ];
        }

        private static StpPayment MockStpPayment(long transactionId, string networkName, string enrollType, string edit835Type, bool remitValid = false, bool edi835Avail = false, bool paymentSent = false)
        {
            return new StpPayment
            {
                TransactionId = transactionId,
                NetworkName = networkName,
                EnrollType = enrollType,
                Edi835Type = edit835Type,
                PaymentSent = paymentSent,
                Edi835Avail = edi835Avail,
                RemitValid = remitValid
            };
        }

        #endregion
    }
}
