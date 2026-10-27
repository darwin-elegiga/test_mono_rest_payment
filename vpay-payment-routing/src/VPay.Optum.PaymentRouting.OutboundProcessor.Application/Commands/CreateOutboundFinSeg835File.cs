using System;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdiFabric.Core.Model.Edi.X12;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.EdiFabric;
using VPay.EdiFabric.Helpers;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.OutboundPayment.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.FileTransfer;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Helpers;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands
{
    /// <summary>
    /// Takes payments marked as CLCFT and builds an 835 file
    /// </summary>
    /// <remarks>
    /// This should be called only periodically, as a new sequence number will need to be generated
    /// and Optum can only accept 99 files per day (01-99)
    /// </remarks>
    public class CreateOutboundFinSeg835File
    {
        public record Command(long OutboundFinSeg835FileId, long OutboundFinSegTxtFileId) : IRequest<bool>;

        public class Handler : IRequestHandler<Command, bool>
        {
            private readonly IFileSystem _fileSystem;
            private readonly IPublishEndpoint _publishEndpoint;
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;

            public Handler(ILogger<Handler> logger, IMediator mediator, IFileSystem fileSystem, IPublishEndpoint publishEndpoint)
            {
                _logger = logger;
                _mediator = mediator;
                _fileSystem = fileSystem;
                _publishEndpoint = publishEndpoint;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Getting outbound file information");
                var outboundFile = await _mediator.Send(new GetOutbound835FileById.Query(request.OutboundFinSeg835FileId), cancellationToken);

                if (outboundFile == null)
                {
                    _logger.LogError("Could not find outbound file with id: {FileId}", request.OutboundFinSeg835FileId);
                    throw new ItemNotFoundException();
                }

                if (outboundFile.Status == Outbound835FileStatus.FinSeg835Queued)
                {
                    _logger.LogInformation("Found {Count} payments for batch id {FileId}", outboundFile.PaymentIds.Count, outboundFile.Id);
                    await SetOutbound835FileStatus(request.OutboundFinSeg835FileId, Outbound835FileStatus.FinSeg835Started,
                        cancellationToken).ConfigureAwait(false);

                    var header = await _mediator.Send(new GetInboundHeaderByPaymentIdSql.Query(outboundFile.PaymentIds[0]), cancellationToken);
                    var dateStamp = string.IsNullOrWhiteSpace(outboundFile.DateStamp)
                        ? $"{DateTime.Now:yyyyMMdd}"
                        : outboundFile.DateStamp;

                    var fileName = $"{outboundFile.PayerAlias.OutboundFinSegPayerAliasConverter()}_{dateStamp}_{(outboundFile.SequenceNumber ?? 0):00}";
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

                                await vPay835.WriteISARecordAsync(inboundIsa).ConfigureAwait(false);
                                await vPay835.WriteGSRecordAsync(inboundGs).ConfigureAwait(false);
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
                            Outbound835FileId = request.OutboundFinSeg835FileId,
                            FilePath = fileName,
                            ModifiedBy = Environment.MachineName
                        }, cancellationToken).ConfigureAwait(false);


                        //Update status
                        await SetOutbound835FileStatus(request.OutboundFinSeg835FileId, Outbound835FileStatus.FinSeg835Built, cancellationToken).ConfigureAwait(false);

                        //Send file to transfer service
                        await _mediator.Send(
                            new SendToFileTransferService.Command(_fileSystem.FileInfo.FromFileName(tempPath),
                                request.OutboundFinSeg835FileId), cancellationToken).ConfigureAwait(false);

                        await SetOutbound835FileStatus(request.OutboundFinSeg835FileId, Outbound835FileStatus.Transferring, cancellationToken).ConfigureAwait(false);

                        //Trigger next step to create the TXT summary file for the FinSeg835 file that was just built.
                        //Important: the file should have the same sequence number as the 835 file.
                        await _publishEndpoint.Publish(new OutboundFinSegTxtFileMessage
                        {
                            OutboundFinSegTxtFileId = request.OutboundFinSegTxtFileId
                        }, cancellationToken).ConfigureAwait(false);
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

                return true;
            }

            private Task SetOutbound835FileStatus(long fileId, Outbound835FileStatus newStatus, CancellationToken cancellationToken)
            {
                return _mediator.Send(new UpdateOutbound835FileStatus.Command(fileId, newStatus, Environment.MachineName), cancellationToken);
            }
        }
    }
}
