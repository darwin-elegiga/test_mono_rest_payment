using Microsoft.AspNetCore.Mvc;
using Moq;
using VPay.Distribution.Api.Controllers;
using VPay.Distribution.Common;
using VPay.Distribution.Http.Abstractions;
using VPay.Distribution.Http.Abstractions.Dto;
using VPay.DocSys.Data;
using VPay.DocSys.Storage;

namespace VPay.Distribution.Api.Tests.Controllers
{
    public class StpPaymentsControllerTests
    {
        private readonly Mock<IStpPaymentService> _stpPaymentServiceMock = new(MockBehavior.Strict);
        private readonly Mock<IRemittanceService> _remittanceServiceMock = new(MockBehavior.Strict);
        private readonly Mock<IStorageService> _storageServiceMock = new(MockBehavior.Strict);
        private readonly Mock<IPlatformConfigurationApi> _platformConfigApiMock = new(MockBehavior.Strict);
        private readonly Mock<IPaymentRoutingApi> _paymentRoutingApiMock = new(MockBehavior.Strict);
        private readonly StpPaymentsController _controller;

        private const string NetworkName = "MY-NETWORK";
        private const string B2BEnrollType = "B2B";
        private const string C2BEnrollType = "C2B";
        private const string SdsEdi835Type = "SDS";
        private const string PrsEdi835Type = "PRS";

        public StpPaymentsControllerTests()
        {
            _controller = new StpPaymentsController(
                _stpPaymentServiceMock.Object,
                _remittanceServiceMock.Object,
                _storageServiceMock.Object,
                _platformConfigApiMock.Object,
                _paymentRoutingApiMock.Object);
        }

        [Fact]
        public async Task Constructor_Throws_WhenStpPaymentServiceIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(new StpPaymentsController(
                null!,
                _remittanceServiceMock.Object,
                _storageServiceMock.Object,
                _platformConfigApiMock.Object,
                _paymentRoutingApiMock.Object)));
        }

        [Fact]
        public async Task Constructor_Throws_WhenRemittanceServiceIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(new StpPaymentsController(
                _stpPaymentServiceMock.Object,
                null!,
                _storageServiceMock.Object,
                _platformConfigApiMock.Object,
                _paymentRoutingApiMock.Object)));
        }

        [Fact]
        public async Task Constructor_Throws_WhenStorageServiceIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(new StpPaymentsController(
                _stpPaymentServiceMock.Object,
                _remittanceServiceMock.Object,
                null!,
                _platformConfigApiMock.Object,
                _paymentRoutingApiMock.Object)));
        }

        [Fact]
        public async Task Constructor_Throws_WhenPlatformConfigApiIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(new StpPaymentsController(
                _stpPaymentServiceMock.Object,
                _remittanceServiceMock.Object,
                _storageServiceMock.Object,
                null!,
                _paymentRoutingApiMock.Object)));
        }

        [Fact]
        public async Task Constructor_Throws_WhenPaymentRoutingApiIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(new StpPaymentsController(
                _stpPaymentServiceMock.Object,
                _remittanceServiceMock.Object,
                _storageServiceMock.Object,
                _platformConfigApiMock.Object,
                null!)));
        }

        [Fact]
        public async Task GetStpPayment_ReturnsOk_WhenFound()
        {
            var payment = new StpPayment { TransactionId = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.GetStpPayment(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task GetStpPayment_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.GetStpPayment(0);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetStpPayment_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.GetStpPayment(2);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task SearchStpPayments_ReturnsOk_WhenFound()
        {
            var searchDto = new StpPaymentSearchDto { NetworkName = "Net" };
            var payments = MockStpPayments();

            _stpPaymentServiceMock.Setup(s => s.SearchStpPayments(
                searchDto.NetworkName, searchDto.EnrollType, searchDto.Edi835Type,
                searchDto.Edi835Avail, searchDto.RemitValid, searchDto.PaymentSent, searchDto.PmbMessageSent,
                searchDto.PaymentSentRetries, searchDto.MaxResults, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payments);

            var result = await _controller.SearchStpPayments(searchDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<StpPaymentDto>>(okResult.Value);
        }

        [Fact]
        public async Task SearchStpPayments_ReturnsOk_WhenEmpty()
        {
            var searchDto = new StpPaymentSearchDto { NetworkName = "Net" };
            var payments = new List<StpPayment>();

            _stpPaymentServiceMock.Setup(s => s.SearchStpPayments(
                searchDto.NetworkName, searchDto.EnrollType, searchDto.Edi835Type,
                searchDto.Edi835Avail, searchDto.RemitValid, searchDto.PaymentSent, searchDto.PmbMessageSent,
                searchDto.PaymentSentRetries, searchDto.MaxResults, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payments);

            var result = await _controller.SearchStpPayments(searchDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<StpPaymentDto>>(okResult.Value);
        }

        [Fact]
        public async Task SearchStpPayments_ReturnsBadRequest_WhenNull()
        {
            var result = await _controller.SearchStpPayments(null);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentRemitValid_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.MarkStpPaymentRemitValid(0);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentRemitValid_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentRemitValid(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentRemitValid_ReturnsNoContent_WhenNotUpdated()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StpPayment { TransactionId = 1 });
            _stpPaymentServiceMock.Setup(s => s.MarkRemitValid(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentRemitValid(1);

            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentRemitValid_ReturnsOk_WhenUpdated()
        {
            var payment = new StpPayment { TransactionId = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.MarkRemitValid(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.MarkStpPaymentRemitValid(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task MarkStpPaymentEdi835Avail_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.MarkStpPaymentEdi835Avail(0);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentEdi835Avail_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentEdi835Avail(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentEdi835Avail_ReturnsNoContent_WhenNotUpdated()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StpPayment { TransactionId = 1 });
            _stpPaymentServiceMock.Setup(s => s.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentEdi835Avail(1);

            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentEdi835Avail_ReturnsOk_WhenUpdated()
        {
            var payment = new StpPayment { TransactionId = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.MarkEdi835Avail(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.MarkStpPaymentEdi835Avail(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentSent_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.MarkStpPaymentPaymentSent(0);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentSent_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPaymentSent(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentSent_ReturnsNoContent_WhenNotUpdated()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StpPayment { TransactionId = 1 });
            _stpPaymentServiceMock.Setup(s => s.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPaymentSent(1);

            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentSent_ReturnsOk_WhenUpdated()
        {
            var payment = new StpPayment { TransactionId = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.MarkPaymentSent(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.MarkStpPaymentPaymentSent(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentNotSent_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.MarkStpPaymentPaymentNotSent(0);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentNotSent_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPaymentNotSent(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentNotSent_ReturnsNoContent_WhenNotUpdated()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StpPayment { TransactionId = 1 });
            _stpPaymentServiceMock.Setup(s => s.MarkPaymentNotSent(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPaymentNotSent(1);

            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPaymentNotSent_ReturnsOk_WhenUpdated()
        {
            var payment = new StpPayment { TransactionId = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.MarkPaymentNotSent(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.MarkStpPaymentPaymentNotSent(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task CanDownloadPrs835Document_ReturnsOk()
        {
            var existsDto = new Outbound835ExistsDto { TransactionId = 1, Exists = true };
            _paymentRoutingApiMock.Setup(s => s.CanDownloadOutbound835Document(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existsDto);

            var result = await _controller.CanDownloadPrs835(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            TransactionExistsDto dtoResult = Assert.IsType<TransactionExistsDto>(okResult.Value);
            Assert.Equal(existsDto.TransactionId, dtoResult.TransactionId);
            Assert.Equal(existsDto.Exists, dtoResult.Exists);
        }

        [Fact]
        public async Task GetNetworkPartner_ReturnsBadRequest_WhenNullOrWhitespace()
        {
            var result = await _controller.GetNetworkPartner(" ");

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetNetworkPartner_ReturnsNotFound_WhenNull()
        {
            NetworkDto? network = null!;
            _platformConfigApiMock.Setup(s => s.GetNetworkPartner("Net", It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(network);

            var result = await _controller.GetNetworkPartner("Net");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetNetworkPartner_ReturnsOk_WhenFound()
        {
            var network = new NetworkDto { NetworkPartner = "Net" };
            _platformConfigApiMock.Setup(s => s.GetNetworkPartner("Net", It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(network);

            var result = await _controller.GetNetworkPartner("Net");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(network, okResult.Value);
        }

        [Fact]
        public async Task PublishPmbMessage_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.PublishPmbMessage(0);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PublishPmbMessage_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.PublishPmbMessage(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PublishPmbMessage_ReturnsBadRequest_WhenNotB2B()
        {
            var payment = MockStpPayment(1, NetworkName, C2BEnrollType, PrsEdi835Type);

            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.PublishPmbMessage(1);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PublishPmbMessage_ReturnsNoContent_WhenPaymentSent()
        {
            var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type, paymentSent: true);

            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.PublishPmbMessage(1);

            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task PublishPmbMessage_ReturnsAccepted_WhenPublished()
        {
            var payment = MockStpPayment(1, NetworkName, B2BEnrollType, SdsEdi835Type);

            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            _stpPaymentServiceMock.Setup(s => s.PublishPmbMessage(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.PublishPmbMessage(1);

            Assert.IsType<AcceptedResult>(result.Result);
        }

        [Fact]
        public async Task GetPendingStpPayments_ReturnsOk_WhenFound()
        {
            var filterDto = new StpPaymentFilterDto { NetworkName = "Net" };
            var payments = MockStpPayments();

            _stpPaymentServiceMock.Setup(s => s.SearchStpPayments(
                filterDto.NetworkName, filterDto.EnrollType, filterDto.Edi835Type,
                null, true, false, null, null, filterDto.MaxResults, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payments);

            var result = await _controller.GetPendingStpPayments(filterDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<StpPaymentDto>>(okResult.Value);
        }

        [Fact]
        public async Task GetPendingStpPayments_ReturnsOk_WhenEmpty()
        {
            var filterDto = new StpPaymentFilterDto { NetworkName = "Net" };
            var payments = new List<StpPayment>();

            _stpPaymentServiceMock.Setup(s => s.SearchStpPayments(
                filterDto.NetworkName, filterDto.EnrollType, filterDto.Edi835Type,
                null, true, false, null, null, filterDto.MaxResults, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payments);

            var result = await _controller.GetPendingStpPayments(filterDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<StpPaymentDto>>(okResult.Value);
        }

        [Fact]
        public async Task GetPendingStpPayments_ReturnsBadRequest_WhenNull()
        {
            var result = await _controller.GetPendingStpPayments(null);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task StartPendingToSend_ReturnsNoContent_WhenNone()
        {
            var payments = new List<StpPayment>();
            _stpPaymentServiceMock.Setup(s => s.SearchStpPayments(
                null, EnrollTypes.B2B, null, null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payments);

            var result = await _controller.StartPendingToSend();

            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task StartPendingToSend_ReturnsAccepted_WhenFound()
        {
            var payments = new List<StpPayment> { new() { TransactionId = 1 } };
            _stpPaymentServiceMock.Setup(s => s.SearchStpPayments(
                null, EnrollTypes.B2B, null, null, true, false, null, null, 10000, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payments);
            _stpPaymentServiceMock.Setup(s => s.StartPendingToSend(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.StartPendingToSend();

            var accepted = Assert.IsType<AcceptedResult>(result.Result);
            Assert.Equal(payments.Count, accepted.Value);
        }

        [Theory]
        [InlineData(EnrollTypes.B2B)]
        [InlineData(EnrollTypes.C2B)]
        public async Task ValidateB2BRemitDocument_ReturnsBadRequest_WhenNull(string enrollType)
        {
            var result = await _controller.ValidateRemitDocument(1, enrollType, null, default);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Theory]
        [InlineData(EnrollTypes.B2B)]
        [InlineData(EnrollTypes.C2B)]
        public async Task ValidateB2BRemitDocument_ReturnsBadRequest_WhenInvalidId(string enrollType)
        {
            var doc = new Mock<StpB2BRemittanceDocument>();
            var result = await _controller.ValidateRemitDocument(0, enrollType, doc.Object, default);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Theory]
        [InlineData(EnrollTypes.B2B)]
        [InlineData(EnrollTypes.C2B)]
        public async Task ValidateB2BRemitDocument_ReturnsBadRequest_WhenNullDoc(string enrollType)
        {
            var result = await _controller.ValidateRemitDocument(1, enrollType, null, default);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Theory]
        [InlineData(EnrollTypes.B2B)]
        [InlineData(EnrollTypes.C2B)]
        public async Task ValidateB2BRemitDocument_ReturnsOk_WhenValid(string enrollType)
        {
            var doc = new Mock<StpB2BRemittanceDocument>();
            doc.Setup(d => d.ToJsonString()).Returns("{}");
            _remittanceServiceMock.Setup(s => s.ValidateRemitDocument(1, enrollType, "{}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.ValidateRemitDocument(1, enrollType, doc.Object, default);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var validResult = Assert.IsType<TransactionValidDto>(okResult.Value);
            Assert.True(validResult.IsValid);
        }

        [Fact]
        public async Task SendRemitDocument_ReturnsBadRequest_WhenNull()
        {
            var result = await _controller.SendRemitDocument(0, null, default);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task SendRemitDocument_ReturnsBadRequest_WhenInvalidId()
        {
            var doc = new Mock<StpB2BRemittanceDocument>();
            var result = await _controller.SendRemitDocument(0, EnrollTypes.B2B, doc.Object, default);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task SendRemitDocument_ReturnsBadRequest_WhenNullDoc()
        {
            var result = await _controller.SendRemitDocument(1, null, default);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task SendRemitDocument_ReturnsOk_WhenSent()
        {
            var doc = new Mock<StpB2BRemittanceDocument>();
            doc.Setup(d => d.ToJsonString()).Returns("{}");

            _remittanceServiceMock.Setup(s => s.PostRemitDocument(1, EnrollTypes.B2B, "{}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.SendRemitDocument(1, EnrollTypes.B2B, doc.Object, default);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var sentResult = Assert.IsType<TransactionValidDto>(okResult.Value);
            Assert.True(sentResult.IsValid);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageSent_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.MarkStpPaymentPmbMessageSent(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageSent_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPmbMessageSent(1);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageSent_ReturnsNoContent_WhenNotUpdated()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StpPayment { TransactionId = 1 });
            _stpPaymentServiceMock.Setup(s => s.MarkPmbMessageSent(1, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPmbMessageSent(1);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageSent_ReturnsOk_WhenUpdated()
        {
            var payment = new StpPayment { TransactionId = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.MarkPmbMessageSent(1, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.MarkStpPaymentPmbMessageSent(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageNotSent_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.MarkStpPaymentPmbMessageNotSent(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageNotSent_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPmbMessageNotSent(1);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageNotSent_ReturnsNoContent_WhenNotUpdated()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StpPayment { TransactionId = 1 });
            _stpPaymentServiceMock.Setup(s => s.MarkPmbMessageNotSent(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.MarkStpPaymentPmbMessageNotSent(1);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task MarkStpPaymentPmbMessageNotSent_ReturnsOk_WhenUpdated()
        {
            var payment = new StpPayment { TransactionId = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.MarkPmbMessageNotSent(1, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.MarkStpPaymentPmbMessageNotSent(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task ResetStpPaymentSentRetries_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.ResetStpPaymentSentRetries(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ResetStpPaymentSentRetries_ReturnsNotFound_WhenNotFound()
        {
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.ResetStpPaymentSentRetries(1);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ResetStpPaymentSentRetries_ReturnsNoContent_WhenRetriesZero()
        {
            var payment = new StpPayment { TransactionId = 1, PaymentSentRetries = 0 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.ResetStpPaymentSentRetries(1);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task ResetStpPaymentSentRetries_ReturnsNoContent_WhenNotUpdated()
        {
            var payment = new StpPayment { TransactionId = 1, PaymentSentRetries = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.ResetStpRemitSentRetries(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StpPayment?)null);

            var result = await _controller.ResetStpPaymentSentRetries(1);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task ResetStpPaymentSentRetries_ReturnsOk_WhenUpdated()
        {
            var payment = new StpPayment { TransactionId = 1, PaymentSentRetries = 1 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayment(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
            _stpPaymentServiceMock.Setup(s => s.ResetStpRemitSentRetries(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            var result = await _controller.ResetStpPaymentSentRetries(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<StpPaymentDto>(okResult.Value);
        }

        [Fact]
        public async Task CanDownloadSds835_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.CanDownloadSds835(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CanDownloadSds835_ReturnsOk_WhenExists()
        {
            _storageServiceMock.Setup(s => s.CanDownloadEdi835(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.CanDownloadSds835(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TransactionExistsDto>(okResult.Value);
            Assert.True(dto.Exists);
            Assert.Equal(1, dto.TransactionId);
        }

        [Fact]
        public async Task CanDownloadSds835_ReturnsNotFound_OnException()
        {
            _storageServiceMock.Setup(s => s.CanDownloadEdi835(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            var result = await _controller.CanDownloadSds835(1);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CanDownloadPrs835_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.CanDownloadPrs835(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CanDownloadPrs835_ReturnsNotFound_OnNotFoundApiException()
        {
            var ex = await Refit.ApiException.Create(
                "Not Found",
                new HttpRequestMessage(HttpMethod.Get, "http://localhost"),
                HttpMethod.Get,
                new HttpResponseMessage(System.Net.HttpStatusCode.NotFound),
                null);

            _paymentRoutingApiMock.Setup(s => s.CanDownloadOutbound835Document(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(ex);

            var result = await _controller.CanDownloadPrs835(1);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CanDownloadPrs835_ReturnsServerError_OnOtherApiException()
        {
            var ex = await Refit.ApiException.Create(
                "Internal Server Error",
                new HttpRequestMessage(HttpMethod.Get, "http://localhost"),
                HttpMethod.Get,
                new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError),
                null);

            _paymentRoutingApiMock.Setup(s => s.CanDownloadOutbound835Document(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(ex);

            var result = await _controller.CanDownloadPrs835(1);
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetArchiveFiles_ReturnsOk()
        {
            var files = new List<IArchiveFile> { Mock.Of<IArchiveFile>() };
            _storageServiceMock.Setup(static s => s.GetArchiveFiles(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(files);

            var result = await _controller.GetArchiveFiles(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(files, okResult.Value);
        }

        [Fact]
        public async Task GetRemittanceJson_ReturnsBadRequest_WhenInvalidId()
        {
            var result = await _controller.GetRemittanceJson(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetRemittanceJson_ReturnsNotFound_WhenNullOrWhitespace()
        {
            _storageServiceMock.Setup(s => s.GetRemitJson(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            var result = await _controller.GetRemittanceJson(1);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetRemittanceJson_ReturnsOk_WhenFound()
        {
            _storageServiceMock.Setup(s => s.GetRemitJson(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{ \"remit\": true }");

            var result = await _controller.GetRemittanceJson(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("{ \"remit\": true }", okResult.Value);
        }

        [Fact]
        public async Task ProcessPendingToSend_ReturnsNoContent_WhenEmpty()
        {
            var ids = Array.Empty<long>();
            var result = await _controller.ProcessPendingToSend(ids);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task ProcessPendingToSend_ReturnsNoContent_WhenNoPending()
        {
            var ids = new long[] { 1, 2 };
            _stpPaymentServiceMock.Setup(s => s.GetStpPayments(ids, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StpPayment>());

            var result = await _controller.ProcessPendingToSend(ids);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task ProcessPendingToSend_ReturnsBadRequest_WhenNotB2B()
        {
            var ids = new long[] { 1 };
            var payment = MockStpPayment(1, NetworkName, C2BEnrollType, PrsEdi835Type);
            _stpPaymentServiceMock.Setup(s => s.GetStpPayments(ids, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StpPayment> { payment });

            var result = await _controller.ProcessPendingToSend(ids);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ProcessPendingToSend_ReturnsBadRequest_WhenPaymentSent()
        {
            var ids = new long[] { 1 };
            var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type, paymentSent: true, remitValid: true);
            _stpPaymentServiceMock.Setup(s => s.GetStpPayments(ids, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StpPayment> { payment });

            var result = await _controller.ProcessPendingToSend(ids);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ProcessPendingToSend_ReturnsBadRequest_WhenRemitInvalid()
        {
            var ids = new long[] { 1 };
            var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type, remitValid: false);
            _stpPaymentServiceMock.Setup(s => s.GetStpPayments(ids, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StpPayment> { payment });

            var result = await _controller.ProcessPendingToSend(ids);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ProcessPendingToSend_ReturnsBadRequest_WhenRetriesExceeded()
        {
            var ids = new long[] { 1 };
            var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type, remitValid: true);
            payment.PaymentSentRetries = 6;
            _stpPaymentServiceMock.Setup(s => s.GetStpPayments(ids, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StpPayment> { payment });

            var result = await _controller.ProcessPendingToSend(ids);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ProcessPendingToSend_ReturnsAccepted_WhenProcessed()
        {
            var ids = new long[] { 1 };
            var payment = MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type, remitValid: true);
            _stpPaymentServiceMock.Setup(s => s.GetStpPayments(ids, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StpPayment> { payment });
            _stpPaymentServiceMock.Setup(s => s.ProcessPendingStpPayment(payment, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.ProcessPendingToSend(ids);
            var accepted = Assert.IsType<AcceptedResult>(result.Result);
            Assert.Equal(1, accepted.Value);
        }

        private static IList<StpPayment> MockStpPayments()
        {
            return
            [
                MockStpPayment(1, NetworkName, B2BEnrollType, PrsEdi835Type),
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
    }
}
