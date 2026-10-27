using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Connection;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Logic;
using Xunit;

namespace VPay.Gateway.PaymentProcessor.Consumer.Tests.Logic;

public class RecordRelatedPaymentEventsCommandHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mockMediator = new Mock<IMediator>(MockBehavior.Strict);
    private readonly Mock<IDbUnitOfWork> _mockDbUnitOfWork = new Mock<IDbUnitOfWork>();
    private readonly Mock<IDataConnectionTransaction> _mockDataTransaction = new Mock<IDataConnectionTransaction>();
    private readonly ILogger<RecordRelatedPaymentEventsCommandHandler> _logger = new NullLogger<RecordRelatedPaymentEventsCommandHandler>();
    private readonly RecordRelatedPaymentEventsCommandHandler _handler;

    public RecordRelatedPaymentEventsCommandHandlerTests()
    {
        _handler = new RecordRelatedPaymentEventsCommandHandler(_mockMediator.Object, _mockDbUnitOfWork.Object, _logger);
    }

    public void Dispose()
    {
        _mockMediator.VerifyAll();
        _mockDbUnitOfWork.VerifyAll();
    }

    public class Handle : RecordRelatedPaymentEventsCommandHandlerTests
    {
        [Fact]
        public async Task WhenCannotFindExternalClientConfig_ThenShouldReturnFalse()
        {
            var query = new RecordRelatedPaymentEventsCommand("VPA1", "PAY1");

            ExternalClientConfigurationQuery actQuery = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync((ExternalClientConfigurationDto)null)
                .Callback<IRequest<ExternalClientConfigurationDto>, CancellationToken>((q, t) =>
                {
                    actQuery = (ExternalClientConfigurationQuery)q;
                })
                .Verifiable();

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeFalse();
                actQuery.Should().NotBeNull().And
                    .BeEquivalentTo(new ExternalClientConfigurationQuery(query.ClientCode, DateTime.Today));
            }
        }

        [Fact]
        public async Task WhenExtenalClientIsReturnedAndTransactionApiReturnsNull_ThenShouldReturnFalse()
        {
            var query = new RecordRelatedPaymentEventsCommand("VPA1", "PAY1");

            _mockMediator.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync(new ExternalClientConfigurationDto { ClientCode = query.ClientCode, ImplementationType = "Guidewire", Id = 1 })
                .Verifiable();

            GetTransactionsByPaymentIdQuery actQuery = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<GetTransactionsByPaymentIdQuery>(), default))
                .ReturnsAsync((Transaction[])null)
                .Callback<IRequest<Transaction[]>, CancellationToken>((q, t) =>
                {
                    actQuery = (GetTransactionsByPaymentIdQuery)q;
                })
                .Verifiable();

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeFalse();
                actQuery.Should().NotBeNull().And
                    .BeEquivalentTo(new GetTransactionsByPaymentIdQuery(query.ClientCode, query.ClientPaymentId));
            }
        }

        [Fact]
        public async Task WhenExtenalClientIsReturnedAndTransactionApiReturnsAnEmptyArray_ThenShouldReturnFalse()
        {
            var query = new RecordRelatedPaymentEventsCommand("VPA1", "PAY1");

            _mockMediator.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync(new ExternalClientConfigurationDto { ClientCode = query.ClientCode, ImplementationType = "Guidewire", Id = 1 })
                .Verifiable();

            GetTransactionsByPaymentIdQuery actQuery = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<GetTransactionsByPaymentIdQuery>(), default))
                .ReturnsAsync(Array.Empty<Transaction>())
                .Callback<IRequest<Transaction[]>, CancellationToken>((q, t) =>
                {
                    actQuery = (GetTransactionsByPaymentIdQuery)q;
                })
                .Verifiable();

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeFalse();
                actQuery.Should().NotBeNull().And
                    .BeEquivalentTo(new GetTransactionsByPaymentIdQuery(query.ClientCode, query.ClientPaymentId));
            }
        }

        [Fact]
        public async Task WhenExtenalClientIsReturnedAndTransactionApiReturnsTransactions_ThenShouldInsertPaymentAndEventsAndReturnTrue()
        {
            var query = new RecordRelatedPaymentEventsCommand("VPA1", "PAY1");

            var transactions = new List<Transaction>
                {
                    new Transaction
                    {
                        TransactionId = 1,
                        ClientCode = query.ClientCode,
                        PaymentId = query.ClientPaymentId,
                        PaymentTypeCode = "CLVCP",
                        CurrentBalance = 0m,
                        DateLoaded = DateTime.Now.AddMinutes(-100),
                        Events = new List<TransactionEvent>
                        {
                            new TransactionEvent
                            {
                                Id = 1,
                                TransactionId = 1,
                                Status = "REQUESTED",
                                ProcessedDate = DateTime.Now.AddDays(-10),
                                ReasonCode = "OK",
                                ReasonDescription = ""
                            },
                            new TransactionEvent
                            {
                                Id = 2,
                                TransactionId = 1,
                                Status = "LOADED",
                                ProcessedDate = DateTime.Now.AddDays(-10),
                                ReasonCode = "",
                                ReasonDescription = ""
                            },
                            new TransactionEvent
                            {
                                Id = 3,
                                TransactionId = 1,
                                Status = "UNLOAD",
                                ProcessedDate = DateTime.Now.AddDays(-10).AddMinutes(1),
                                ReasonCode = "1052",
                                ReasonDescription = "Unloaded through object"
                            }
                        }
                    },
                    new Transaction
                    {
                        TransactionId = 2,
                        ClientCode = query.ClientCode,
                        PaymentId = query.ClientPaymentId,
                        PaymentTypeCode = "CLVCP",
                        CurrentBalance = 23.14m,
                        DateLoaded = DateTime.Now.AddMinutes(-1),
                        Events = new List<TransactionEvent>
                        {
                            new TransactionEvent
                            {
                                Id = 10,
                                TransactionId = 2,
                                Status = "REQUESTED",
                                ProcessedDate = DateTime.Now.AddDays(-9),
                                ReasonCode = "OK",
                                ReasonDescription = ""
                            },
                            new TransactionEvent
                            {
                                Id = 11,
                                TransactionId = 2,
                                Status = "LOADED",
                                ProcessedDate = DateTime.Now.AddDays(-8),
                                ReasonCode = "",
                                ReasonDescription = ""
                            }
                        }
                    }
                };

            var expectedEventInserts = transactions
                .SelectMany(x => x.Events ?? new List<TransactionEvent>())
                .OrderBy(x => x.ProcessedDate)
                .Select(x => new PaymentEventInsertCommand(
                    1,
                    x.TransactionId,
                    x.Id,
                    x.Status,
                    x.ProcessedDate,
                    x.ReasonCode,
                    x.ReasonDescription,
                    "CLVCP",
                    x.TrackingNumber
                    ));

            _mockMediator.Setup(x => x.Send(It.IsAny<ExternalClientConfigurationQuery>(), default))
                .ReturnsAsync(new ExternalClientConfigurationDto { ClientCode = query.ClientCode, ImplementationType = "Guidewire", Id = 1 })
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.IsAny<GetTransactionsByPaymentIdQuery>(), default))
                .ReturnsAsync(transactions.ToArray())
                .Verifiable();

            PaymentUpsertCommand actQuery = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentUpsertCommand>(), default))
                .ReturnsAsync(new Payment { Id = 1 })
                .Callback<IRequest<Payment>, CancellationToken>((q, t) =>
                {
                    actQuery = (PaymentUpsertCommand)q;
                })
                .Verifiable();

            var paymentEvents = new List<PaymentEventInsertCommand>();
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventInsertCommand>(), default))
                .ReturnsAsync(new PaymentEvent())
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    paymentEvents.Add((PaymentEventInsertCommand)q);
                })
                .Verifiable();

            _mockDbUnitOfWork.Setup(x => x.BeginTransaction()).ReturnsAsync(_mockDataTransaction.Object);

            var act = await _handler.Handle(query, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeTrue();
                actQuery.Should().NotBeNull().And
                    .BeEquivalentTo(new PaymentUpsertCommand(query.ClientCode, query.ClientPaymentId, 23.14m));

                paymentEvents.Should().BeEquivalentTo(expectedEventInserts);
            }

            _mockDbUnitOfWork.Verify(x => x.BeginTransaction(), Times.Once);
            _mockDataTransaction.Verify(x => x.Commit(), Times.Once);
        }
    }
}
