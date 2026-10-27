using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Optum.PaymentRouting.Application.Outbound.Queries;
using VPay.Optum.PaymentRouting.Common.Interfaces;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using Xunit;

namespace VPay.Optum.PaymentRouting.Application.Tests.Outbound.Queries
{
    /// <summary>
    /// Tests for <see cref="GetOutbound835Document"/>
    /// </summary>
    public class GetOutbound835DocumentTests
    {
        private readonly IMediator _mediator;
        private readonly GetOutbound835Document.Handler _handler;
        private readonly ILogger<GetOutbound835Document.Handler> _logger;
        private readonly ISystemClock _systemClock;

        public sealed class Constructor : GetOutbound835DocumentTests
        {
#pragma warning disable CA1806

            [Fact]
            public void MissingMediator_ThrowsException()
            {
                // ReSharper disable once ObjectCreationAsStatement
                Action ctor = () => new GetOutbound835Document.Handler(null!, _logger, _systemClock);
                ctor.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("mediator");
            }

            [Fact]
            public void WhenLoggerIsNull_ThrowsException()
            {
                Action act = () => new GetOutbound835Document.Handler(_mediator, null!, _systemClock);
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("logger");
            }

            [Fact]
            public void WhenSystemClockIsNull_ThrowsException()
            {
                Action act = () => new GetOutbound835Document.Handler(_mediator, _logger, null!);
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("systemClock");
            }

#pragma warning restore CA1806
        }

        public GetOutbound835DocumentTests()
        {
            _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
            _logger = new NullLogger<GetOutbound835Document.Handler>();
            _systemClock = _systemClock = Mock.Of<ISystemClock>();
            _handler = new GetOutbound835Document.Handler(_mediator, _logger, _systemClock);
        }

        [Fact]
        public async Task WhenTransactionIdIsFound_ReturnsEdi835Document()
        {
            var transactionId = 1;
            var query = new GetOutbound835Document.Query(transactionId);
            var outbound835Data = new Outbound835Data
            {
                TransactionId = 1,
                OutboundRefEO = "841162789",
                HeaderData = "ISA*00*          *00*          *ZZ*999999999      *ZZ*411858498      *180308*0235*^*00501*000043565*0*P*:~ GS*HP*999999999*411858498*20180308*023536*92618*X*005010X221A1~",
                PaymentData = "ST*835*161999004~BPR*I*1.00*C*CHK************20240525~TRN*1*VP2151551503*1942788907*CREDITCAR~REF*EV*841162789~DTM*405*20220110~N1*PR*UMR SutterSelect~N3*PO Box 30541~N4*Salt Lake City*UT*841300541~REF*2U*UMR01~PER*CX*UMR EDI Team*TE*8442518318~PER*BL*UMR EDI Team*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*CONNER DAVID M*XX*1588906812~N3*PO BOX 95000 LBX 7650~N4*PHILADELPHIA*PA*19195~REF*TJ*639123284~LX*1~CLP*0178779*1*1650*19.09**12*21194705343~NM1*QC*1*BUSSELL*WILLIAM~NM1*IL*1*BUSSELL*WILLIAM****MI*704000587833~NM1*82*1*CONNER*DAVID****XX*1871589614~REF*1L*78800037~AMT*AU*19.09~SVC*HC:00142:QK:QS:P2*1650*19.09~DTM*472*20210707~CAS*CO*45*1611.82**223*19.09~AMT*B6*19.09~LQ*HE*N830~SE*28*161999004~"
            };

            Mock.Get(_mediator).Setup(x => x.Send(It.Is<GetOutbound835FileDataByTransactionId.Query>(q => q.TransactionId == query.TransactionId), default))
                .ReturnsAsync(outbound835Data);

            Mock.Get(_systemClock).Setup(x => x.Now)
                .Returns(new DateTime(2024, 1, 1, 0, 0, 0));

            var expectedFileName = "edi-1.835";
            var expectedContentType = "text/plain";
            var expected835Document = "ISA*00*          *00*          *ZZ*999999999      *ZZ*841162789      *240101*0000*^*00501*000043565*0*P*:~GS*HP*999999999*841162789*20240101*0000*92618*X*005010X221A1~ST*835*0001~BPR*I*1.00*C*CHK************20240525~TRN*1*VP2151551503*1942788907*CREDITCAR~REF*EV*841162789~DTM*405*20220110~N1*PR*UMR SutterSelect~N3*PO Box 30541~N4*Salt Lake City*UT*841300541~REF*2U*UMR01~PER*CX*UMR EDI Team*TE*8442518318~PER*BL*UMR EDI Team*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*CONNER DAVID M*XX*1588906812~N3*PO BOX 95000 LBX 7650~N4*PHILADELPHIA*PA*19195~REF*TJ*639123284~LX*1~CLP*0178779*1*1650*19.09**12*21194705343~NM1*QC*1*BUSSELL*WILLIAM~NM1*IL*1*BUSSELL*WILLIAM****MI*704000587833~NM1*82*1*CONNER*DAVID****XX*1871589614~REF*1L*78800037~AMT*AU*19.09~SVC*HC:00142:QK:QS:P2*1650*19.09~DTM*472*20210707~CAS*CO*45*1611.82**223*19.09~AMT*B6*19.09~LQ*HE*N830~SE*28*0001~GE*1*92618~IEA*1*000043565~";

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Status.Should().Be(ResultStatus.Ok);
            result.Value.FileName.Should().Be(expectedFileName);
            result.Value.ContentType.Should().Be(expectedContentType);

            using var streamReader = new StreamReader(result.Value.Stream);
            var edi835Data = await streamReader.ReadToEndAsync();

            edi835Data.Should().Be(expected835Document);
        }

        [Fact]
        public async Task WhenTransactionIdIsNotFound_ReturnsNotFound()
        {
            var transactionId = 1;
            var query = new GetOutbound835Document.Query(transactionId);

            Mock.Get(_mediator).Setup(x => x.Send(It.Is<GetOutbound835FileDataByTransactionId.Query>(q => q.TransactionId == query.TransactionId), default))
               .ReturnsAsync((Outbound835Data)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Status.Should().Be(ResultStatus.NotFound);
        }
    }
}
