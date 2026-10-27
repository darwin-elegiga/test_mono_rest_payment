using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using VPay.Gateway.Common.PlatformConfig;
using Xunit;

namespace VPay.Gateway.Application.Tests;

/// <summary>
/// Tests for <see cref="CreateEnrollmentCommandHandler"/>
/// </summary>
public class PayerSubPayerAndPaymentNumberToTransactionIdTests
{
    private readonly ILogger<PayerSubPayerAndPaymentNumberToTransactionId.Handler> _logger = new NullLogger<PayerSubPayerAndPaymentNumberToTransactionId.Handler>();
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
    private readonly PayerSubPayerAndPaymentNumberToTransactionId.Handler _handler;

    public PayerSubPayerAndPaymentNumberToTransactionIdTests() =>
        _handler = new PayerSubPayerAndPaymentNumberToTransactionId.Handler(_mediatorMock.Object, _logger);

#pragma warning disable CA1806
    public class Constructor : PayerSubPayerAndPaymentNumberToTransactionIdTests
    {
        [Fact]
        public void WhenMediatorNull_ThrowsArgumentNullException() =>
            new Action(() => new PayerSubPayerAndPaymentNumberToTransactionId.Handler(null!, _logger))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("mediator");

        [Fact]
        public void WhenLoggerIsNull_ThrowsArgumentNullException() =>
            new Action(() => new PayerSubPayerAndPaymentNumberToTransactionId.Handler(_mediatorMock.Object, null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");
    }
#pragma warning restore CA1806

    public class Handle : PayerSubPayerAndPaymentNumberToTransactionIdTests
    {
        private const string PayerId = "OPId";
        private const string SubPayerId = "Payer";
        private const string PaymentId = "payment1";
        private const int TransactionId = 12345;

        private readonly OptumSubPayerDto _subPayerDto = new()
        {
            BillingEntityCode = "BE1",
            ClientCode = "CC2",
            OptumSubPayorId = SubPayerId,
            CreatedBy = "Test",
            CreatedDateTime = DateTime.Now,
            LastModifiedBy = "Test2",
            LastModifiedDateTime = DateTime.Now.AddHours(1),
            OptumPayorId = "OPId"
        };

        [Fact]
        public async Task WorksAsExpected()
        {
            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == SubPayerId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_subPayerDto)
                .Verifiable();

            _mediatorMock.Setup(x =>
                    x.Send(
                        It.Is<GetTransactionsByPaymentIdQuery>(q =>
                            q.ClientCode == _subPayerDto.ClientCode && q.PaymentId == PaymentId),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Transaction[]
                {
                    new()
                    {
                        ClientCode = _subPayerDto.ClientCode,
                        BillingEntityCode = _subPayerDto.BillingEntityCode,
                        PaymentId = PaymentId,
                        Amount = 100m,
                        TransactionId = TransactionId
                    }
                })
                .Verifiable();

            var response = await _handler.Handle(new PayerSubPayerAndPaymentNumberToTransactionId.Query(PayerId, SubPayerId, PaymentId),
                CancellationToken.None);
            response.Should().NotBeNull();
            response.BillingEntityCode.Should().Be(_subPayerDto.BillingEntityCode);
            response.ClientCode.Should().Be(_subPayerDto.ClientCode);
            response.TransactionId.Should().Be(TransactionId);
        }

        [Fact]
        public async Task WhenOptumSubPayerQueryReturnsNull_ThrowsItemNotFoundException()
        {
            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == SubPayerId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptumSubPayerDto)null)
                .Verifiable();

            Func<Task<PayerSubPayerAndPaymentNumberToTransactionId.Response>> func = async () => await _handler.Handle(new PayerSubPayerAndPaymentNumberToTransactionId.Query(PayerId, SubPayerId, PaymentId),
                CancellationToken.None);

            (await func.Should().ThrowAsync<ItemNotFoundException>().ConfigureAwait(false)).And.Message.Should().Be("Sub-payer cannot be found.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenNoTransactions_ThrowsItemNotFoundException(bool nullTransaction)
        {
            _mediatorMock.Setup(x => x.Send(It.Is<OptumSubPayerQuery>(q => q.OptumSubPayerId == SubPayerId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_subPayerDto)
                .Verifiable();

            _mediatorMock.Setup(x =>
                    x.Send(
                        It.Is<GetTransactionsByPaymentIdQuery>(q =>
                            q.ClientCode == _subPayerDto.ClientCode && q.PaymentId == PaymentId),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(nullTransaction ? null : Array.Empty<Transaction>())
                .Verifiable();

            Func<Task<PayerSubPayerAndPaymentNumberToTransactionId.Response>> func = async () => await _handler.Handle(new PayerSubPayerAndPaymentNumberToTransactionId.Query(PayerId, SubPayerId, PaymentId),
                CancellationToken.None);

            (await func.Should().ThrowAsync<ItemNotFoundException>().ConfigureAwait(false)).And.Message.Should().Be("Could not find payment for the sub-payer.");
        }
    }
}
