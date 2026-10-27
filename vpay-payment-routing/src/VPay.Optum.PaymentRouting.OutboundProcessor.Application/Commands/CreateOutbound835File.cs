using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdiFabric.Core.Model.Edi.X12;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.EdiFabric;
using VPay.EdiFabric.Helpers;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.FileTransfer;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands
{
    public class CreateOutbound835File
    {
        public record Command(long Outbound835FileId) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;
            private readonly IFileSystem _fileSystem;

            public Handler(ILogger<Handler> logger, IMediator mediator, IFileSystem fileSystem)
            {
                _logger = logger;
                _mediator = mediator;
                _fileSystem = fileSystem;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Getting outbound file information");
                var outboundFile = await _mediator.Send(new GetOutbound835FileById.Query(request.Outbound835FileId), cancellationToken);

                if (outboundFile == null)
                {
                    _logger.LogError("Could not find outbound file with id: {FileId}", request.Outbound835FileId);
                    throw new ItemNotFoundException();
                }

                if (outboundFile.Status == Outbound835FileStatus.Queued)
                {
                    _logger.LogInformation("Found {Count} payments for batch id {FileId}", outboundFile.PaymentIds.Count, outboundFile.Id);

                    var header = await _mediator.Send(new GetInboundHeaderByPaymentIdSql.Query(outboundFile.PaymentIds[0]), cancellationToken);

                    var fileName = $"{outboundFile.PayerAlias}_{DateTime.Now:yyyyMMddHHmmssffff}.txt";
                    var tempPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), fileName);

                    try
                    {
                        await using (var fileStream = _fileSystem.File.Create(tempPath))
                        {
                            using var vPay835 = new VPay835Writer(fileStream, encoding: new UTF8Encoding(false));

                            await using (var inboundStream = header.HeaderData.ToStream())
                            {
                                using var inbound835 = new VPay835Reader(inboundStream);
                                var inboundIsa = await inbound835.ReadISAAsync().ConfigureAwait(false);
                                var inboundGs = await inbound835.ReadAsync().ConfigureAwait(false) as GS;

                                var now = DateTime.Now;
                                var outboundIsa = GenerateIsa(outboundFile, inboundIsa, now);
                                var outboundGs = GenerateGs(outboundIsa, inboundGs, now);

                                await vPay835.WriteISARecordAsync(outboundIsa).ConfigureAwait(false);
                                await vPay835.WriteGSRecordAsync(outboundGs).ConfigureAwait(false);
                            }

                            var count = 1;
                            foreach (var paymentId in outboundFile.PaymentIds)
                            {
                                var clobPayment = await _mediator.Send(
                                    new GetOutboundPaymentClobByPaymentId.Query(paymentId), cancellationToken);
                                await using var clobStream = clobPayment.Clob.ToStream();
                                using var clob835 = new VPay835Reader(clobStream, noEnvelope: true);

                                var payment = await clob835.Read835Async().ConfigureAwait(false);
                                payment.ST.TransactionSetControlNumber_02 = $"{count:0000}";
                                count++;
                                await vPay835.Write835TransactionAsync(payment).ConfigureAwait(false);
                            }
                        }

                        //Update file path
                        await _mediator.Send(new UpdateOutbound835FilePath.Command
                        {
                            Outbound835FileId = request.Outbound835FileId,
                            FilePath = fileName,
                            ModifiedBy = Environment.MachineName
                        });

                        //Update status
                        await _mediator.Send(
                            new UpdateOutbound835FileStatus.Command(request.Outbound835FileId,
                                Outbound835FileStatus.Built, Environment.MachineName), cancellationToken);

                        //Send file to transfer service
                        await _mediator.Send(
                            new SendToFileTransferService.Command(_fileSystem.FileInfo.FromFileName(tempPath),
                                outboundFile.Id), cancellationToken);
                    }
                    finally
                    {
                        _fileSystem.File.Delete(tempPath);
                    }
                }
                else
                {
                    _logger.LogError("Outbound file in improper status {status}", outboundFile.Status.Name);
                    throw new BadRequestException("Outbound file not in proper status");
                }

                return Unit.Value;
            }

            private static ISA GenerateIsa(Outbound835File outboundFile, ISA inboundIsa, DateTime now)
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
                    InterchangeReceiverID_8 = outboundFile.OutboundRefEO.LimitLength(15, 15),
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
