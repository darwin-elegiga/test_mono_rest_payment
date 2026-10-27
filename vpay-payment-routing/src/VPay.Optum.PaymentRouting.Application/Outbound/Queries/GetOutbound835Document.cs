using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using EdiFabric.Core.Model.Edi.X12;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.EdiFabric;
using VPay.EdiFabric.Helpers;
using VPay.Optum.PaymentRouting.Application.Outbound.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.Common.Interfaces;

namespace VPay.Optum.PaymentRouting.Application.Outbound.Queries
{
    public class GetOutbound835Document
    {
        public record Query(long TransactionId) : IRequest<Result<Outbound835Document>>;

        public class Handler : IRequestHandler<Query, Result<Outbound835Document>>
        {
            private readonly IMediator _mediator;
            private readonly ILogger<Handler> _logger;
            private readonly ISystemClock _systemClock;

            public Handler(IMediator mediator, ILogger<Handler> logger, ISystemClock systemClock)
            {
                _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            }

            public async Task<Result<Outbound835Document>> Handle(Query request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Getting outbound 835 data for transaction id: {TransactionId}", request.TransactionId);
                    var data = await _mediator.Send(new GetOutbound835FileDataByTransactionId.Query(request.TransactionId));

                    if (data == null)
                    {
                        _logger.LogError("Could not find outbound 835 data for transaction id: {TransactionId}", request.TransactionId);
                        return Result<Outbound835Document>.NotFound();
                    }

                    var stream835 = new MemoryStream();

                    await using (var headerStream = data.HeaderData.ToStream())
                    {
                        using var vPay835 = new VPay835Writer(stream835, encoding: new UTF8Encoding(false));
                        using var inbound835 = new VPay835Reader(headerStream);
                        var inboundIsa = await inbound835.ReadISAAsync().ConfigureAwait(false);
                        var inboundGs = await inbound835.ReadAsync().ConfigureAwait(false) as GS;

                        var now = _systemClock.Now;
                        var outboundIsa = GenerateIsa(data.OutboundRefEO, inboundIsa, now);
                        var outboundGs = GenerateGs(outboundIsa, inboundGs, now);
                        await vPay835.WriteISARecordAsync(outboundIsa).ConfigureAwait(false);
                        await vPay835.WriteGSRecordAsync(outboundGs).ConfigureAwait(false);

                        await using var paymentStream = data.PaymentData.ToStream();
                        using var payment835 = new VPay835Reader(paymentStream, noEnvelope: true);
                        var payment = await payment835.Read835Async().ConfigureAwait(false);
                        payment.ST.TransactionSetControlNumber_02 = "0001";
                        await vPay835.Write835TransactionAsync(payment).ConfigureAwait(false);
                    }

                    stream835.Position = 0;

                    return Result<Outbound835Document>.Success(
                      new Outbound835Document()
                      {
                          Stream = stream835,
                          FileName = $"edi-{request.TransactionId}.835",
                          ContentType = "text/plain"
                      });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to build outbound 835 Document for transaction id: {TransactionId}", request.TransactionId);
                    return Result<Outbound835Document>.Error(ex.Message);
                }
            }

            private static ISA GenerateIsa(string outboundRefEO, ISA inboundIsa, DateTime now)
            {
                return new ISA
                {
                    AuthorizationInformationQualifier_1 = "00",
                    AuthorizationInformation_2 = new string(' ', 10),
                    SecurityInformationQualifier_3 = "00",
                    SecurityInformation_4 = new string(' ', 10),
                    SenderIDQualifier_5 = "ZZ",
                    InterchangeSenderID_6 = inboundIsa.InterchangeSenderID_6.LimitLength(15, 15),
                    ReceiverIDQualifier_7 = "ZZ",
                    InterchangeReceiverID_8 = outboundRefEO.LimitLength(15, 15),
                    InterchangeDate_9 = now.ToString("yyMMdd"),
                    InterchangeTime_10 = now.ToString("HHmm"),
                    InterchangeControlStandardsIdentifier_11 = "^",
                    InterchangeControlVersionNumber_12 = "00501",
                    InterchangeControlNumber_13 = inboundIsa.InterchangeControlNumber_13.LimitLength(9, 9),
                    AcknowledgementRequested_14 = "0",
                    UsageIndicator_15 = inboundIsa.UsageIndicator_15,
                    ComponentElementSeparator_16 = ":"
                };
            }

            private static GS GenerateGs(ISA outboundIsa, GS inboundGs, DateTime now)
            {
                return new GS
                {
                    CodeIdentifyingInformationType_1 = "HP",
                    SenderIDCode_2 = outboundIsa.InterchangeSenderID_6.Trim().LimitLength(2, 15),
                    ReceiverIDCode_3 = outboundIsa.InterchangeReceiverID_8.Trim().LimitLength(2, 15),
                    Date_4 = now.ToString("yyyyMMdd"),
                    Time_5 = now.ToString("HHmm"),
                    GroupControlNumber_6 = (inboundGs?.GroupControlNumber_6 ?? "").Trim().LimitLength(1, 9),
                    TransactionTypeCode_7 = "X",
                    VersionAndRelease_8 = "005010X221A1"
                };
            }
        }
    }
}
