using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.FileTransfer;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Tests.Commands
{
    public class CreateOutbound835FileTests
    {
        private readonly PaymentRoutingTestingContainer _container;

        public CreateOutbound835FileTests()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<CreateOutbound835File.Handler>()
                    .AddMock<IMediator>()
                    .AddMock<ILogger<CreateOutbound835File.Handler>>()
                    .AddMock<IFileSystem>();
            });
        }

        [Fact]
        public async Task ShouldThrowIfOutboundFileNotFound()
        {
            var command = _container.Fake<CreateOutbound835File.Command>();

            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<GetOutbound835FileById.Query>(x => x.Outbound835FileId == command.Outbound835FileId),
                Arg.Any<CancellationToken>()).ReturnsNull();

            var handler = _container.Get<CreateOutbound835File.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ItemNotFoundException>();
        }

        private class TestDataGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new()
            {
                new object[] { Outbound835FileStatus.Built },
                new object[] { Outbound835FileStatus.Completed },
                new object[] { Outbound835FileStatus.FailedSend },
                new object[] { Outbound835FileStatus.Transferring },
                new object[] { Outbound835FileStatus.Started }
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(TestDataGenerator))]
        public async Task ShouldHandleBadStatus(Outbound835FileStatus status)
        {
            var command = _container.Fake<CreateOutbound835File.Command>();

            var outboundRecord =
                _container.Fake<Outbound835File>(x =>
                    x.With(y => y.Id, command.Outbound835FileId).With(y => y.Status, status).Without(y => y.PaymentIds));

            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<GetOutbound835FileById.Query>(x => x.Outbound835FileId == command.Outbound835FileId),
                Arg.Any<CancellationToken>()).Returns(outboundRecord);

            var handler = _container.Get<CreateOutbound835File.Handler>();
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BadRequestException>();
            await mediator.Received().Send(Arg.Any<GetOutbound835FileById.Query>(), Arg.Any<CancellationToken>());
            await mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<GetInboundHeaderByBatchId.Query>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldBuild835()
        {
            var command = _container.Fake<CreateOutbound835File.Command>();

            var paymentIds = _container.FakeMany<long>().ToList();
            var outboundRecord =
                _container.Fake<Outbound835File>(x =>
                    x.With(y => y.Id, command.Outbound835FileId).With(y => y.Status, Outbound835FileStatus.Queued)
                        .With(y => y.PaymentIds, paymentIds));

            var mediator = _container.Get<IMediator>();
            mediator.Send(Arg.Is<GetOutbound835FileById.Query>(x => x.Outbound835FileId == command.Outbound835FileId),
                Arg.Any<CancellationToken>()).Returns(outboundRecord);

            foreach (var paymentId in paymentIds)
            {
                var paymentWithClob = _container.Fake<GetOutboundPaymentClobByPaymentId.Result>(x => x.With(y => y.Clob,
                        "ST*835*54411001~BPR*I*360*C*ACH*CCP*01*000*DA*000*1627395711*0000UMR01*01*000*DA*000*20170321~TRN*1*DPM1821000012507080000008*1627395711*0000UMR01~REF*EV*841162789~DTM*405*20170321~N1*PR*UMR CLARKSTON   POTOMAC GROUP INC~N3*115 W WAUSAU AVE~N4*WAUSAU*WI*54402~REF*2U*UMR01~REF*EO*1627395711~PER*BL*UMR EDI TEAM*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*DENTAL HEALTH ASSOCIATES PA*FI*222692491~LX*1~CLP*62032819*1*160*85*75*12*17076130021~NM1*QC*1*POTOMAC*CLARKSTON1~NM1*IL*1*POTOMAC*CLARKSTON1****MI*62032819~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*85~SVC*AD:D0120*75*0~DTM*472*20170313~CAS*PR*119*75~LQ*HE*N130~SVC*AD:D1110*85*85~DTM*472*20170313~AMT*B6*85~LX*2~CLP*63145082*1*160*85*75*12*17076130023~NM1*QC*1*YEAR*POLICY~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*85~SVC*AD:D0120*75*0~DTM*472*20170309~CAS*PR*119*75~LQ*HE*N130~SVC*AD:D1110*85*85~DTM*472*20170309~AMT*B6*85~LX*3~CLP*63145082*1*160*30*130*12*17076130024~NM1*QC*1*YEAR*SPOUSE~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*75~SVC*AD:D0120*75*30~DTM*472*20170313~CAS*PR*1*45~AMT*B6*75~SVC*AD:D1110*85*0~DTM*472*20170313~CAS*PR*119*85~LQ*HE*N130~LX*4~CLP*63145082*1*75*75**12*17076130025~NM1*QC*1*YEAR*CHILD2~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*75~SVC*AD:D0120*75*75~DTM*472*20170313~AMT*B6*75~LX*5~CLP*63145082*1*85*85**12*17076130026~NM1*QC*1*YEAR*CHILD2~NM1*IL*1*YEAR*POLICY****MI*63145082~NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~REF*1L*76560010~AMT*AU*85~SVC*AD:D0272*85*85~DTM*472*20170309~AMT*B6*85~SE*76*54411001~"));

                mediator.Send(
                    Arg.Is<GetOutboundPaymentClobByPaymentId.Query>(x => x.InboundPaymentId == paymentId),
                    Arg.Any<CancellationToken>()).Returns(paymentWithClob);
            }

            var firstPaymentId = paymentIds.First();
            var firstPayment = _container.Fake<GetInboundPaymentWithClobByIdSql.Result>();

            var header = new Data.Sql.InboundHeader.InboundHeader()
            {
                HeaderData = "ISA*00*          *00*          *ZZ*391995276      *ZZ*411858498      *170321*1225*^*00501*000000870*0*T*:~GS*HP*391995276*411858498*20170321*122536*54411*X*005010X221A1~",
                Status = PaymentStatus.Out835Built
            };

            mediator.Send(Arg.Is<GetInboundHeaderByPaymentIdSql.Query>(x => x.InboundPaymentId == firstPaymentId),
                Arg.Any<CancellationToken>()).Returns(header);

            var stream = new MemoryStream();
            var fileSystem = _container.Get<IFileSystem>();
            fileSystem.File.Create(Arg.Any<string>()).Returns(stream);

            var handler = _container.Get<CreateOutbound835File.Handler>();
            await handler.Handle(command, CancellationToken.None);

            await mediator.Received().Send(Arg.Any<GetOutbound835FileById.Query>(), Arg.Any<CancellationToken>());
            await mediator.Received().Send(Arg.Any<GetInboundHeaderByPaymentIdSql.Query>(),
                Arg.Any<CancellationToken>());
            await mediator.Received(paymentIds.Count).Send(Arg.Any<GetOutboundPaymentClobByPaymentId.Query>(),
                Arg.Any<CancellationToken>());
            await mediator.Received().Send(Arg.Any<SendToFileTransferService.Command>(), Arg.Any<CancellationToken>());
            await mediator.Received().Send(Arg.Is<UpdateOutbound835FilePath.Command>(x => x.Outbound835FileId == outboundRecord.Id &&
                !string.IsNullOrWhiteSpace(x.FilePath) &&
                x.ModifiedBy == Environment.MachineName), Arg.Any<CancellationToken>());
            await mediator.Received()
                .Send(
                    Arg.Is<UpdateOutbound835FileStatus.Command>(x =>
                        x.Id == outboundRecord.Id && x.Status == Outbound835FileStatus.Built &&
                        x.UpdatedBy == Environment.MachineName), Arg.Any<CancellationToken>());
        }
    }
}
