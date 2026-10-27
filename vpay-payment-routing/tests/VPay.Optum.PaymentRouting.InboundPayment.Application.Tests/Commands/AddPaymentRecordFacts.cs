using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Optum.Http.ConfigApi.Queries;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Commands;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Constants;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Executors;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Tests.Commands
{
    public class AddPaymentRecordFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public AddPaymentRecordFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddMock<ILogger<AddPaymentRecord.Handler>>()
                    .AddMock<IMediator>()
                    .AddSingleton<AddPaymentRecord.Handler>()
                    .AddMock<ICheckBatchCompleteExecutor>();
            });

        [Fact]
        public async Task Handle_ShouldNotThrowException_WhenValid()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture)));

            var command = new AddPaymentRecord.Command {PaymentRecord = paymentRecord};
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "OriginCode")
                    .With(mapping => mapping.ClientCode, "ClientCode"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(new ConfigurationDto());

            PayerPraConfigurationDto fakePraConfigResult =
                _container.Fake<PayerPraConfigurationDto>(x =>
                    x.With(pra => pra.IsPayerPraEnabled, true)
                    .With(pra => pra.RemitMethod, "PDF"));

            mediator.Send(Arg.Any<GetPayerPraConfigurationQuery.Query>())
                .ReturnsForAnyArgs(fakePraConfigResult);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Handle_ShouldNotAddPayment_WhenDuplicate()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture)));
            var command = new AddPaymentRecord.Command {PaymentRecord = paymentRecord};
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "OriginCode")
                    .With(mapping => mapping.ClientCode, "ClientCode"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(new ConfigurationDto());

            GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result fakeGetPaymentResult =
                _container.Fake<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>(x =>
                    x.With(payment => payment.OptumBatchId, command.PaymentRecord.BatchId)
                        .With(payment => payment.PaymentClob, paymentRecord.PaymentClob));
            mediator.Send(Arg.Any<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>())
                .ReturnsForAnyArgs(fakeGetPaymentResult);

            await handler.Handle(command, CancellationToken.None);

            await mediator.DidNotReceive().Send(Arg.Any<InsertInboundPaymentSql.Command>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldCallInsertInboundHeaderSql_WhenDuplicate()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(_ =>
                        _.With(_ => _.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture))
                        .With(_ => _.TransactionType, TransactionTypes.Eof)
                    );
            var command = new AddPaymentRecord.Command { PaymentRecord = paymentRecord };
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            GetInboundHeaderByOptumBatchIdSql.Result fakeGetHeaderResult =
                _container.Fake<GetInboundHeaderByOptumBatchIdSql.Result>(x =>
                    x.With(header => header.OptumBatchId, command.PaymentRecord.BatchId)
                        .With(header => header.HeaderData, paymentRecord.PaymentClob));
            mediator.Send(Arg.Any<GetInboundHeaderByOptumBatchIdSql.Query>()).ReturnsForAnyArgs(fakeGetHeaderResult);

            await handler.Handle(command, CancellationToken.None);

            await mediator.DidNotReceive().Send(Arg.Any<InsertInboundHeaderSql.Command>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotAddHeader_WhenDuplicate()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(_ =>
                        _.With(_ => _.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture))
                    );
            var command = new AddPaymentRecord.Command {PaymentRecord = paymentRecord};
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "OriginCode")
                    .With(mapping => mapping.ClientCode, "ClientCode"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(new ConfigurationDto());

            PayerPraConfigurationDto fakePraConfigResult =
                _container.Fake<PayerPraConfigurationDto>(x =>
                    x.With(pra => pra.IsPayerPraEnabled, true)
                    .With(pra => pra.RemitMethod, "PDF"));

            mediator.Send(Arg.Any<GetPayerPraConfigurationQuery.Query>())
                .ReturnsForAnyArgs(fakePraConfigResult);

            GetInboundHeaderByOptumBatchIdSql.Result fakeGetHeaderResult =
                _container.Fake<GetInboundHeaderByOptumBatchIdSql.Result>(x =>
                    x.With(header => header.OptumBatchId, command.PaymentRecord.BatchId)
                        .With(header => header.HeaderData, paymentRecord.PaymentClob));
            mediator.Send(Arg.Any<GetInboundHeaderByOptumBatchIdSql.Query>()).ReturnsForAnyArgs(fakeGetHeaderResult);

            await handler.Handle(command, CancellationToken.None);

            await mediator.DidNotReceive().Send(Arg.Any<InsertInboundHeaderSql.Command>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldThrowBadRequestException_WhenPaymentIsDuplicate()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture)));
            var command = new AddPaymentRecord.Command { PaymentRecord = paymentRecord };
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "OriginCode")
                    .With(mapping => mapping.ClientCode, "ClientCode"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(new ConfigurationDto());

            GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result fakeGetPaymentResult =
                _container.Fake<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>(x =>
                    x.With(header => header.OptumBatchId, command.PaymentRecord.BatchId));
            mediator.Send(Arg.Any<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>())
                .ReturnsForAnyArgs(fakeGetPaymentResult);

            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<VPayAlertableException>();
            await mediator.DidNotReceive().Send(Arg.Any<InsertInboundPaymentSql.Command>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldThrowBadRequest_WhenEofIsDuplicate()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture)).With(pr => pr.TransactionType, TransactionTypes.Eof ));
            var command = new AddPaymentRecord.Command { PaymentRecord = paymentRecord };
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            GetInboundHeaderByOptumBatchIdSql.Result fakeGetHeaderResult =
                _container.Fake<GetInboundHeaderByOptumBatchIdSql.Result>(x =>
                    x.With(header => header.OptumBatchId, command.PaymentRecord.BatchId));
            mediator.Send(Arg.Any<GetInboundHeaderByOptumBatchIdSql.Query>()).ReturnsForAnyArgs(fakeGetHeaderResult);

            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<VPayAlertableException>();
            await mediator.DidNotReceive().Send(Arg.Any<InsertInboundHeaderSql.Command>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldAddPaymentWhenNotDuplicate()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture)));
            var command = new AddPaymentRecord.Command {PaymentRecord = paymentRecord};
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "OriginCode")
                    .With(mapping => mapping.ClientCode, "ClientCode"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(new ConfigurationDto());

            PayerPraConfigurationDto fakePraConfigResult =
                _container.Fake<PayerPraConfigurationDto>(x =>
                    x.With(pra => pra.IsPayerPraEnabled, true)
                    .With(pra => pra.RemitMethod, "PDF"));

            mediator.Send(Arg.Any<GetPayerPraConfigurationQuery.Query>())
                .ReturnsForAnyArgs(fakePraConfigResult);

            mediator.Send(Arg.Any<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>())
                .ReturnsForAnyArgs((GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result)null);

            await handler.Handle(command, CancellationToken.None);

            await mediator.Received().Send(Arg.Is<InsertInboundPaymentSql.Command>(x => x.OptumBatchId == command.PaymentRecord.BatchId), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldAddHeaderWhenNotDuplicate()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture))
                        .With(x => x.TransactionType, TransactionTypes.Eof));
            var command = new AddPaymentRecord.Command {PaymentRecord = paymentRecord};
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            InsertInboundHeaderSql.Command fakeInsertHeaderCommand = _container.Fake<InsertInboundHeaderSql.Command>();

            mediator.Send(Arg.Any<GetInboundHeaderByOptumBatchIdSql.Query>())
                .ReturnsForAnyArgs((GetInboundHeaderByOptumBatchIdSql.Result)null);

            await handler.Handle(command, CancellationToken.None);

            await mediator.Received().Send(Arg.Is<InsertInboundHeaderSql.Command>(x => x.OptumBatchId == command.PaymentRecord.BatchId), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotAddPaymentWhenEof()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture))
                        .With(x => x.TransactionType, TransactionTypes.Eof));
            var command = new AddPaymentRecord.Command { PaymentRecord = paymentRecord };
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result fakeGetPaymentResult =
                _container.Fake<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>(x =>
                    x.With(header => header.OptumBatchId, command.PaymentRecord.BatchId));
            mediator.Send(Arg.Any<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>())
                .ReturnsForAnyArgs(fakeGetPaymentResult);

            await handler.Handle(command, CancellationToken.None);

            await mediator.DidNotReceive().Send(Arg.Any<InsertInboundPaymentSql.Command>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotAddHeaderWhenNotEof()
        {
            IMediator mediator = _container.Get<IMediator>();
            decimal amount = _container.Fake<decimal>();
            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture))
                        .With(x => x.TransactionType, "Not EOF"));
            var command = new AddPaymentRecord.Command { PaymentRecord = paymentRecord };
            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "OriginCode")
                    .With(mapping => mapping.ClientCode, "ClientCode"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(new ConfigurationDto());

            PayerPraConfigurationDto fakePraConfigResult =
                _container.Fake<PayerPraConfigurationDto>(x =>
                    x.With(pra => pra.IsPayerPraEnabled, true)
                    .With(pra => pra.RemitMethod, "PDF"));

            mediator.Send(Arg.Any<GetPayerPraConfigurationQuery.Query>())
                .ReturnsForAnyArgs(fakePraConfigResult);

            GetInboundHeaderByOptumBatchIdSql.Result fakeGetHeaderResult =
                _container.Fake<GetInboundHeaderByOptumBatchIdSql.Result>(x =>
                    x.With(header => header.OptumBatchId, command.PaymentRecord.BatchId));
            mediator.Send(Arg.Any<GetInboundHeaderByOptumBatchIdSql.Query>()).ReturnsForAnyArgs(fakeGetHeaderResult);

            await handler.Handle(command, CancellationToken.None);

            await mediator.DidNotReceive().Send(Arg.Any<InsertInboundHeaderSql.Command>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldPrefixUserkey_WhenConfigEnabled()
        {
            IMediator mediator = _container.Get<IMediator>();

            decimal amount = _container.Fake<decimal>();

            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture)));

            var expectedUserKey = string.Concat(paymentRecord.PayerSubpayerId, "_", paymentRecord.UnconsolidatedPaymentNbr);

            var command = new AddPaymentRecord.Command { PaymentRecord = paymentRecord };

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "SEMED")
                    .With(mapping => mapping.ClientCode, "MHP"));

            ConfigurationDto fakeConfigResult =
                _container.Fake<ConfigurationDto>(x =>
                    x.With(config => config.Data1, "Y")
                    .With(config => config.Status, "ACTIVE"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(fakeConfigResult);

            PayerPraConfigurationDto fakePraConfigResult =
                _container.Fake<PayerPraConfigurationDto>(x =>
                    x.With(pra => pra.IsPayerPraEnabled, true)
                    .With(pra => pra.RemitMethod, "PDF"));

            mediator.Send(Arg.Any<GetPayerPraConfigurationQuery.Query>())
                .ReturnsForAnyArgs(fakePraConfigResult);

            mediator.Send(Arg.Any<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>())
                .ReturnsForAnyArgs((GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result)null);

            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();
            await handler.Handle(command, CancellationToken.None);

            var expectedInsertCommand = new InsertInboundPaymentSql.Command(paymentRecord.BatchId, paymentRecord.Payer835Id, paymentRecord.PayerSubpayerId,
                        paymentRecord.FileProcId, paymentRecord.PayerAlias, paymentRecord.ProviderTin, paymentRecord.ZipCode, expectedUserKey,
                        Convert.ToDecimal(paymentRecord.UnconsolidatedPaymentAmount), paymentRecord.PaymentClob, fakePraConfigResult.RemitMethod);

            await mediator.Received().Send(Arg.Is<InsertInboundPaymentSql.Command>(expectedInsertCommand), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotPrefixUserkey_WhenConfigDisabled()
        {
            IMediator mediator = _container.Get<IMediator>();

            decimal amount = _container.Fake<decimal>();

            PaymentRecord paymentRecord =
                _container.Fake<PaymentRecord>(x =>
                    x.With(pr => pr.UnconsolidatedPaymentAmount, amount.ToString(CultureInfo.InvariantCulture)));

            var expectedUserKey = paymentRecord.UnconsolidatedPaymentNbr;

            var command = new AddPaymentRecord.Command { PaymentRecord = paymentRecord };

            PrsPayerAliasMappingDto fakeMappingResult =
                _container.Fake<PrsPayerAliasMappingDto>(x =>
                    x.With(mapping => mapping.OriginCode, "SEMED")
                    .With(mapping => mapping.ClientCode, "MHP"));

            ConfigurationDto fakeConfigResult =
                _container.Fake<ConfigurationDto>(x =>
                    x.With(config => config.Data1, "N"));

            mediator.Send(Arg.Any<GetPrsPayerAliasMapping.Query>())
                .ReturnsForAnyArgs(fakeMappingResult);

            mediator.Send(Arg.Any<GetClientBoardingConfiguration.Query>())
                .ReturnsForAnyArgs(fakeConfigResult);

            PayerPraConfigurationDto fakePraConfigResult =
                _container.Fake<PayerPraConfigurationDto>(x =>
                    x.With(pra => pra.IsPayerPraEnabled, true)
                    .With(pra => pra.RemitMethod, "PDF"));

            mediator.Send(Arg.Any<GetPayerPraConfigurationQuery.Query>())
                .ReturnsForAnyArgs(fakePraConfigResult);

            mediator.Send(Arg.Any<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Query>())
                .ReturnsForAnyArgs((GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result)null);

            AddPaymentRecord.Handler handler = _container.Get<AddPaymentRecord.Handler>();
            await handler.Handle(command, CancellationToken.None);

            var expectedInsertCommand = new InsertInboundPaymentSql.Command(paymentRecord.BatchId, paymentRecord.Payer835Id, paymentRecord.PayerSubpayerId,
                        paymentRecord.FileProcId, paymentRecord.PayerAlias, paymentRecord.ProviderTin, paymentRecord.ZipCode, expectedUserKey,
                        Convert.ToDecimal(paymentRecord.UnconsolidatedPaymentAmount), paymentRecord.PaymentClob, fakePraConfigResult.RemitMethod);

            await mediator.Received().Send(Arg.Is<InsertInboundPaymentSql.Command>(expectedInsertCommand), Arg.Any<CancellationToken>());
        }
    }
}
