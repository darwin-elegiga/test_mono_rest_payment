using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.FinSeg;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.FileTransfer;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Helpers;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands
{
    /// <summary>
    /// Takes payments marked as CLCFT and builds an 835 and a FINSEG TXT file
    /// </summary>
    /// <remarks>
    /// This should be called only periodically, as a new sequence number will need to be generated
    /// and Optum can only accept 99 files per day (01-99)
    /// </remarks>
    public class CreateOutboundFinSegFile
    {
        public record Command(long OutboundFinSegTxtFileId) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly ILogger<Handler> _logger;
            private readonly IMediator _mediator;
            private readonly IFileSystem _fileSystem;
            private readonly IFinSegFileWriter _fileWriter;

            public Handler(ILogger<Handler> logger, IMediator mediator, IFileSystem fileSystem, IFinSegFileWriter fileWriter)
            {
                _logger = logger;
                _mediator = mediator;
                _fileSystem = fileSystem;
                _fileWriter = fileWriter;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Getting outbound file information to build FinSeg");
                var finSegInfo = (await _mediator.Send(new GetOutbound835FileFinSegInfo.Query(request.OutboundFinSegTxtFileId),
                        cancellationToken).ConfigureAwait(false)).ToList();

                if (finSegInfo == null)
                {
                    _logger.LogError("Could not find FINSEG file info with Outbound835FileId of: {FileId}", request.OutboundFinSegTxtFileId);
                    throw new ItemNotFoundException();
                }

                if (finSegInfo.First().Status == Outbound835FileStatus.FinSegTxtQueued)
                {
                    _logger.LogInformation("Found {Count} FINSEG records for {FileId}", finSegInfo.Count(), request.OutboundFinSegTxtFileId);

                    await SetOutbound835FileStatus(request.OutboundFinSegTxtFileId, Outbound835FileStatus.FinSegTxtStarted, cancellationToken).ConfigureAwait(false);

                    var fileName = $"{finSegInfo.First().PayerAlias.OutboundFinSegPayerAliasConverter()}_835FINSEG_{finSegInfo.First().DateStamp}_{finSegInfo.First().SequenceNumber:00}.txt";
                    var tempPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), fileName);

                    var finSegFile = new FinSegFile(fileName,
                        finSegInfo.Select(x => new FinSegFileDetail
                        {
                            PayerId = x.PayerId,
                            Amount = x.PaymentAmount,
                            ProcessDate = DateTime.ParseExact(x.DateStamp, "yyyyMMdd", CultureInfo.InvariantCulture)
                        }).ToList()
                    );

                    await using (var writer = new StreamWriter(tempPath))
                    {
                        await _fileWriter.BuildFile(writer, finSegFile).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                    }

                    try
                    {
                        //Update file path
                        await _mediator.Send(new UpdateOutbound835FilePath.Command
                        {
                            Outbound835FileId = request.OutboundFinSegTxtFileId,
                            FilePath = fileName,
                            ModifiedBy = Environment.MachineName
                        }, cancellationToken).ConfigureAwait(false);

                        //Update status
                        await SetOutbound835FileStatus(request.OutboundFinSegTxtFileId, Outbound835FileStatus.FinSegTxtBuilt, cancellationToken).ConfigureAwait(false);

                        //Send file to transfer service
                        await _mediator.Send(
                            new SendToFileTransferService.Command(_fileSystem.FileInfo.FromFileName(tempPath),
                                request.OutboundFinSegTxtFileId), cancellationToken).ConfigureAwait(false);

                        await SetOutbound835FileStatus(request.OutboundFinSegTxtFileId, Outbound835FileStatus.Transferring, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        _fileSystem.File.Delete(tempPath);
                    }
                }
                else
                {
                    throw new BadRequestException("Outbound file not in proper status");
                }

                return Unit.Value;
            }

            private Task SetOutbound835FileStatus(long fileId, Outbound835FileStatus newStatus, CancellationToken cancellationToken)
            {
                return _mediator.Send(new UpdateOutbound835FileStatus.Command(fileId, newStatus, Environment.MachineName), cancellationToken);
            }
        }
    }
}
