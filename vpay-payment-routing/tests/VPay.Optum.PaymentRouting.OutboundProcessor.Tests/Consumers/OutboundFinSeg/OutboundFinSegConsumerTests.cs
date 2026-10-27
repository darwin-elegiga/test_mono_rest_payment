using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EdiFabric.Templates.Hipaa5010;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.ReconReportsApi.Models;
using VPay.Optum.Http.ReconReportsApi.Queries;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Commands;
using VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.OutboundFinSeg;
using VPay.Optum.PaymentRouting.OutboundProcessor.Helpers;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Tests.Consumers.OutboundFinSeg
{
    public class OutboundFinSegConsumerTests
    {
        private readonly PaymentRoutingTestingContainer _container;

        public OutboundFinSegConsumerTests() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddMock<ILogger<OutboundFinSegConsumer>>()
                    .AddMock<IMediator>()
                    .AddMock<IUserInfo>()
                    .AddMock<IClobConverter>()
                    .AddSingleton<OutboundFinSegConsumer>();
            });

        [Fact]
        public async Task ConsumeWithNoPaymentMessage_ThrowsException()
        {
            var provider = new ServiceCollection()
                .AddLogging(l => l.AddProvider(NullLoggerProvider.Instance))
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<OutboundFinSegConsumer>();
                    cfg.AddConsumerTestHarness<OutboundFinSegConsumer>();
                })
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<InMemoryTestHarness>();
            try
            {
                await harness.Start();
                var bus = provider.GetRequiredService<IBus>();

                IRequestClient<BuildOutboundFinSeg> client = bus.CreateRequestClient<BuildOutboundFinSeg>();

                Func<Task> act = () => client.GetResponse<BuildOutboundFinSeg>(null);

                await act.Should().ThrowAsync<RequestException>();
            }
            finally
            {
                await harness.Stop();
                await provider.DisposeAsync();
            }
        }

        [Fact]
        public async Task ConsumeWithNullPaymentMessage_ThrowsException()
        {
            var consumer = _container.Get<OutboundFinSegConsumer>();

            Func<Task> act = () => consumer.Consume(null);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ConsumePaymentWithNullPaymentMessage_ThrowsException()
        {
            var consumer = _container.Get<OutboundFinSegConsumer>();

            Func<Task> act = () => consumer.Consume(null);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInFinSegNeededStatus_WorksAsExpected()
        {
            var clobConverterMock = _container.Get<IClobConverter>();
            var fake835 = CreateFake835();

            clobConverterMock.ClobTo835(Arg.Any<string>()).ReturnsForAnyArgs(fake835);
            clobConverterMock.Ts835ToClob(Arg.Any<TS835>()).ReturnsForAnyArgs(_container.Fake<string>());

            var mediatorMock = _container.Get<IMediator>();
            mediatorMock.Send(Arg.Any<TransactionBankAccountsQuery>(), Arg.Any<CancellationToken>()).Returns(_container.Fake<TransactionBankAccounts>());

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.FinSegNeeded)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLCFT"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            await mediatorMock.Send(new UpdateInboundPaymentRefEoSql.Command
            {
                InboundPaymentId = apMessageTracking.InboundPaymentId,
                OutboundRefEo = "paymentRevEv",
                ModifiedBy = "modifiedBy"
            });

            await mediatorMock.Send(new InsertOutboundPaymentClobSql.Command
            {
                InboundPaymentId = apMessageTracking.InboundPaymentId,
                Clob = "clob"
            });

            await mediatorMock.Send(new UpdateInboundPaymentStatusSql.Command
            {
                InboundPaymentId = apMessageTracking.InboundPaymentId,
                Status = PaymentStatus.FinSegBuilt,
                ModifiedBy = "modifiedBy"
            });

            await mediatorMock.Send(new UpdateApMessageTrackingStatus.Command
            {
                ApMessageTrackingId = apMessageTracking.ApMessageTrackingId,
                Status = ApMessageTrackingStatus.Completed,
                ModifiedBy = "modifiedBy"
            });

            var consumer = _container.Get<OutboundFinSegConsumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutboundFinSeg(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await clobConverterMock.Received(1).ClobTo835(Arg.Any<string>());
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInStartFinSegBuildStatus_WorksAsExpected()
        {
            var clobConverterMock = _container.Get<IClobConverter>();
            var fake835 = CreateFake835();

            clobConverterMock.ClobTo835(Arg.Any<string>()).ReturnsForAnyArgs(fake835);
            clobConverterMock.Ts835ToClob(Arg.Any<TS835>()).ReturnsForAnyArgs(_container.Fake<string>());

            var mediatorMock = _container.Get<IMediator>();
            mediatorMock.Send(Arg.Any<TransactionBankAccountsQuery>(), Arg.Any<CancellationToken>()).Returns(_container.Fake<TransactionBankAccounts>());

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.StartFinSegBuild)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLCFT"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            await mediatorMock.Send(new UpdateInboundPaymentRefEoSql.Command
            {
                InboundPaymentId = apMessageTracking.InboundPaymentId,
                OutboundRefEo = "paymentRevEv",
                ModifiedBy = "modifiedBy"
            });

            await mediatorMock.Send(new InsertOutboundPaymentClobSql.Command
            {
                InboundPaymentId = apMessageTracking.InboundPaymentId,
                Clob = "clob"
            });

            await mediatorMock.Send(new UpdateInboundPaymentStatusSql.Command
            {
                InboundPaymentId = apMessageTracking.InboundPaymentId,
                Status = PaymentStatus.FinSegBuilt,
                ModifiedBy = "modifiedBy"
            });

            await mediatorMock.Send(new UpdateApMessageTrackingStatus.Command
            {
                ApMessageTrackingId = apMessageTracking.ApMessageTrackingId,
                Status = ApMessageTrackingStatus.Completed,
                ModifiedBy = "modifiedBy"
            });

            var consumer = _container.Get<OutboundFinSegConsumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutboundFinSeg(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().NotThrowAsync();
            await clobConverterMock.Received(1).ClobTo835(Arg.Any<string>());
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInCompletedStatus_SkipsProcessing()
        {
            var fake835 = CreateFake835();

            var mediatorMock = _container.Get<IMediator>();

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.Completed)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLCFT"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var consumer = _container.Get<OutboundFinSegConsumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutboundFinSeg(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ConsumePayment_WithPaymentMessageInOtherStatus_ThrowsBadRequestException()
        {
            var fake835 = CreateFake835();

            var mediatorMock = _container.Get<IMediator>();

            var apMessageTracking = _container.Fake<ApMessageTracking>(x => x
                .With(amt => amt.Status, ApMessageTrackingStatus.Queued)
                .With(amt => amt.TrnSegment, "fake0*fake1*fake2*fake3*fake4")
                .With(amt => amt.TransactionId, 12345)
                .With(amt => amt.PaymentTypeCode, "CLCFT"));
            mediatorMock.Send(Arg.Any<GetApMessageTrackingByIdSql.Query>())
                .ReturnsForAnyArgs(apMessageTracking);

            var fakeInboundPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();
            mediatorMock.Send(Arg.Any<GetInboundPaymentWithClobByIdSql.Query>())
                .ReturnsForAnyArgs(fakeInboundPayment);

            var consumer = _container.Get<OutboundFinSegConsumer>();

            Func<Task> act = () => consumer.ConsumePayment(new BuildOutboundFinSeg(apMessageTracking.ApMessageTrackingId, _container.Fake<string>()), CancellationToken.None);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        private static TS835 CreateFake835()
        {
            var fake835 = new TS835
            {
                AllN1 = new All_N1_835
                {
                    Loop1000A = new Loop_1000A_835
                    {
                        REF_AdditionalPayerIdentification = new List<REF_AdditionalPayerIdentification>
                        {
                            new()
                            {
                                ReferenceIdentificationQualifier_01 = "EO"
                            }
                        }
                    }
                }
            };

            return fake835;
        }
    }
}
