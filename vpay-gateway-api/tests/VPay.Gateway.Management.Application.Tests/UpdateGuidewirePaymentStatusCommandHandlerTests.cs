using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.Management.Application.GuidewirePayments;
using Xunit;

namespace VPay.Gateway.Management.Application.Tests;

public class UpdateGuidewirePaymentStatusCommandHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
    private readonly ILogger<UpdateGuidewirePaymentStatusCommandHandler> _logger = new NullLogger<UpdateGuidewirePaymentStatusCommandHandler>();
    private readonly UpdateGuidewirePaymentStatusCommandHandler _handler;

    public UpdateGuidewirePaymentStatusCommandHandlerTests() =>
        _handler = new UpdateGuidewirePaymentStatusCommandHandler(_mediatorMock.Object, _logger);

    public void Dispose()
    {
        _mediatorMock.VerifyAll();
    }

    public class Handle : UpdateGuidewirePaymentStatusCommandHandlerTests
    {
        private const string ClientCode = "SWK";
        private const string ClientPaymentId = "PAY1";

        [Fact]
        public async Task WhenCannotFindExternalClientConfig_ThenItShouldThrowAnArgumentException()
        {
            var query = new UpdateGuidewirePaymentStatusCommand(ClientCode, ClientPaymentId, Common.Guidewire.Models.GuidewirePaymentStatus.Issued, "Note");

            ExternalClientConfigurationQuery actQuery = null;
            _mediatorMock.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync((ExternalClientConfigurationDto)null)
                .Callback<IRequest<ExternalClientConfigurationDto>, CancellationToken>((q, t) =>
                {
                    actQuery = (ExternalClientConfigurationQuery)q;
                })
                .Verifiable();

            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                var exception = await act.Should().ThrowExactlyAsync<ArgumentException>().ConfigureAwait(false);
                exception.And.ParamName.Should().Be(nameof(query.ClientCode));

                actQuery.Should().NotBeNull().And
                    .BeEquivalentTo(new ExternalClientConfigurationQuery(query.ClientCode, DateTime.Today));
            }
        }

        [Fact]
        public async Task WhenExternalClientConfigIsConfiguredAndPaymentIsFound_ThenItShouldSendPaymentInfoToGuidewire()
        {
            var query = new UpdateGuidewirePaymentStatusCommand(ClientCode, ClientPaymentId, GuidewirePaymentStatus.Issued, "Note");
            var externalConfig = new ExternalClientConfigurationDto { ClientCode = query.ClientCode, ImplementationType = "Guidewire", Id = 1 };
            var payment = new Payment
            {
                Id = 1,
                ClientCode = query.ClientCode,
                ClientPaymentId = query.ClientPaymentId,
                LastSentStatus = "Requested"
            };

            var expectedUpdate = new SendPaymentStatusChangeCommand(query.Status, query.ClientPaymentId, query.Note, externalConfig.ToGuidewireSettings());

            _mediatorMock.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync(externalConfig)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<PaymentQuery>(x => x.ClientCode == query.ClientCode && x.ClientPaymentId == query.ClientPaymentId), default))
                .ReturnsAsync(payment)
                .Verifiable();

            var paymentUpdateResult = new GuidewireResult { Status = "SUCCESS" };
            SendPaymentStatusChangeCommand actPaymentStatusCodeCommand = null;
            _mediatorMock.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.IsAny<PaymentUpdateStatusCommand>(), default))
                .ReturnsAsync(payment)
                .Verifiable();

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().Be(paymentUpdateResult);
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedUpdate);
            }
        }

        [Fact]
        public async Task WhenExternalClientConfigIsConfiguredAndPaymentIsFoundAndFailsOnGuidewiresend_ThenItShouldNotSendPaymentStatusUpdate()
        {
            var query = new UpdateGuidewirePaymentStatusCommand(ClientCode, ClientPaymentId, GuidewirePaymentStatus.Issued, "Note");
            var externalConfig = new ExternalClientConfigurationDto { ClientCode = query.ClientCode, ImplementationType = "Guidewire", Id = 1 };
            var payment = new Payment
            {
                Id = 1,
                ClientCode = query.ClientCode,
                ClientPaymentId = query.ClientPaymentId,
                LastSentStatus = "Requested"
            };

            var expectedUpdate = new SendPaymentStatusChangeCommand(query.Status, query.ClientPaymentId, query.Note, externalConfig.ToGuidewireSettings());

            _mediatorMock.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync(externalConfig)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<PaymentQuery>(x => x.ClientCode == query.ClientCode && x.ClientPaymentId == query.ClientPaymentId), default))
                .ReturnsAsync(payment)
                .Verifiable();

            var paymentUpdateResult = new GuidewireResult { Status = "FAILED" };
            _mediatorMock.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Verifiable();

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().Be(paymentUpdateResult);
                _mediatorMock.Verify(x => x.Send(It.IsAny<PaymentUpdateStatusCommand>(), default), Times.Never);
            }
        }

        [Fact]
        public async Task WhenExternalClientConfigIsConfiguredAndPaymentIsNotFound_AndPaymentDoesntExistInVPay_ThenItShouldTryToLookUpPaymentAndReturnFailed()
        {
            var query = new UpdateGuidewirePaymentStatusCommand(ClientCode, ClientPaymentId, GuidewirePaymentStatus.Issued, "Note");
            var externalConfig = new ExternalClientConfigurationDto { ClientCode = query.ClientCode, ImplementationType = "Guidewire", Id = 1 };
            var payment = new Payment
            {
                Id = 1,
                ClientCode = query.ClientCode,
                ClientPaymentId = query.ClientPaymentId,
                LastSentStatus = "Requested"
            };

            var expectedUpdate = new SendPaymentStatusChangeCommand(query.Status, query.ClientPaymentId, query.Note, externalConfig.ToGuidewireSettings());

            _mediatorMock.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync(externalConfig)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<PaymentQuery>(x => x.ClientCode == query.ClientCode && x.ClientPaymentId == query.ClientPaymentId), default))
                .ReturnsAsync((Payment)null)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<GetTransactionsByPaymentIdQuery>(x => x.ClientCode == query.ClientCode && x.PaymentId == query.ClientPaymentId), default))
                .ReturnsAsync(Array.Empty<Transaction>())
                .Verifiable();

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new GuidewireResult { Status = "FAILED", Message = "Could not Locate Payment" });
            }
        }

        [Fact]
        public async Task WhenExternalClientConfigIsConfiguredAndPaymentIsNotFound_AndPaymentDoesExistInVPay_ThenItShouldTryToLookUpPaymentAndRecordItThenTryToSendToGuidewireUpdate()
        {
            var query = new UpdateGuidewirePaymentStatusCommand(ClientCode, ClientPaymentId, GuidewirePaymentStatus.Issued, "Note");
            var externalConfig = new ExternalClientConfigurationDto { ClientCode = query.ClientCode, ImplementationType = "Guidewire", Id = 1 };
            var payment = new Payment
            {
                Id = 1,
                ClientCode = query.ClientCode,
                ClientPaymentId = query.ClientPaymentId,
                LastSentStatus = "",
                CurrentBalance = 1.0m
            };

            var expectedUpdate = new SendPaymentStatusChangeCommand(query.Status, query.ClientPaymentId, query.Note, externalConfig.ToGuidewireSettings());

            _mediatorMock.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync(externalConfig)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<PaymentQuery>(x => x.ClientCode == query.ClientCode && x.ClientPaymentId == query.ClientPaymentId), default))
                .ReturnsAsync((Payment)null)
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<GetTransactionsByPaymentIdQuery>(x => x.ClientCode == query.ClientCode && x.PaymentId == query.ClientPaymentId), default))
                .ReturnsAsync(new Transaction[] { new Transaction { PaymentId = query.ClientPaymentId, ClientCode = query.ClientCode, CurrentBalance = payment.CurrentBalance } })
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.Is<PaymentUpsertCommand>(x => x.ClientCode == query.ClientCode && x.ClientPaymentId == query.ClientPaymentId && x.CurrentBalance == payment.CurrentBalance), default))
                .ReturnsAsync(payment)
                .Verifiable();

            var paymentUpdateResult = new GuidewireResult { Status = "SUCCESS" };
            SendPaymentStatusChangeCommand actPaymentStatusCodeCommand = null;
            _mediatorMock.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            _mediatorMock.Setup(x => x.Send(It.IsAny<PaymentUpdateStatusCommand>(), default))
                .ReturnsAsync(payment)
                .Verifiable();

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().Be(paymentUpdateResult);
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedUpdate);
            }
        }
    }
}
