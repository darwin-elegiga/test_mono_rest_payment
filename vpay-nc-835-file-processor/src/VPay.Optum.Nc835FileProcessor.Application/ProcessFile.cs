using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Optum.Data.Sql.InboundFiles;
using VPay.Optum.Nc835FileProcessor.Application.Services;
using VPay.Optum.Nc835Segments;

namespace VPay.Optum.Nc835FileProcessor.Application
{
    public static class ProcessFile
    {
        public record Command(long InboundFileId, Guid? QueueMessageId) : IRequest<bool>;

        public class Handler : IRequestHandler<Command, bool>
        {
            private readonly IFileSystem _fileSystem;
            private readonly IX12Parser _parser;
            private readonly IMediator _mediator;
            private readonly ILogger<Handler> _logger;

            public Handler(IFileSystem fileSystem, IMediator mediator, ILogger<Handler> logger, IX12Parser x12Parser)
            {
                _fileSystem = fileSystem;
                _mediator = mediator;
                _logger = logger;
                _parser = x12Parser;
            }

            private record ParseErrors(string recordNumber, string errorMessage);

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                var record = await _mediator.Send(new GetInboundFile.Query(request.InboundFileId), cancellationToken).ConfigureAwait(false);

                if (record == null)
                {
                    throw new ItemNotFoundException($"Could not located request {request.InboundFileId}");
                }

                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["InboundFileId"] = record.Id,
                    ["FilePath"] = record.FilePath,
                    ["FileType"] = record.FileType
                });

                if (record.Status == InboundFileStatusEnum.Completed)
                {
                    _logger.LogWarning("Inbound File {InboundFileId} has already been completed", request.InboundFileId);
                    return false;
                }

                if (record.QueueMessageId.HasValue && record.QueueMessageId != request.QueueMessageId)
                {
                    _logger.LogError("Inbound File {InboundFileId} is currently being processed with Id of {QueueMessageGuid}", record.Id, record.QueueMessageId);
                    return false;
                }

                var currentStatus = record.Status;

                currentStatus = await UpdateStatus(request.InboundFileId, InboundFileStatusEnum.Started, currentStatus, request.QueueMessageId, cancellationToken).ConfigureAwait(false);

                List<ParseErrors> parseErrs = new();

                var allRecords = await _mediator.Send(new GetInboundFileRecordsByFileId.Query(request.InboundFileId), cancellationToken).ConfigureAwait(false);

                using var fileStream = _fileSystem.File.OpenRead(record.FilePath);

                var reader = _parser.GetReader(fileStream);

                int i = 0;
                await foreach (var obj in reader.GetNext())
                {
                    if (obj is X12Record rec)
                    {
                        i++;

                        var fileRecord = allRecords.FirstOrDefault(x => x.RecordNumber == i);

                        if (fileRecord != null)
                        {
                            if (fileRecord.Status == InboundFileRecordStatusEnum.Completed)
                            {
                                _logger.LogInformation("Skipping record {RecordNumber} because it has already been completed", i);
                                continue;
                            }
                            else if (fileRecord.Status == InboundFileRecordStatusEnum.ParseError)
                            {
                                _logger.LogInformation("Skipping record {RecordNumber} because it has already been marked as ParseError", i);
                                continue;
                            }
                        }

                        var inboundFileRecord = new InsertInboundFileRecord.Command(record.Id, i, rec.ST?.ST02 ?? $"{i}", InboundFileRecordStatusEnum.Queued);

                        bool hasError = false;
                        //TODO: do some sort of logging. May need to add validation of file itself
                        if (rec.TRN == null && rec.BPR == null)
                        {
                            hasError = true;
                            inboundFileRecord = inboundFileRecord with { Status = InboundFileRecordStatusEnum.ParseError, StatusDescription = "Missing BPR and TRN segments" };
                            parseErrs.Add(new ParseErrors(inboundFileRecord.RecordKey, inboundFileRecord.StatusDescription));
                        }
                        else if (rec.BPR == null)
                        {
                            hasError = true;
                            inboundFileRecord = inboundFileRecord with { Status = InboundFileRecordStatusEnum.ParseError, StatusDescription = "Missing BPR segments" };
                            parseErrs.Add(new ParseErrors(inboundFileRecord.RecordKey, inboundFileRecord.StatusDescription));
                        }
                        else if (rec.TRN == null)
                        {
                            hasError = true;
                            inboundFileRecord = inboundFileRecord with { Status = InboundFileRecordStatusEnum.ParseError, StatusDescription = "Missing TRN segments" };
                            parseErrs.Add(new ParseErrors(inboundFileRecord.RecordKey, inboundFileRecord.StatusDescription));
                        }

                        if (fileRecord == null)
                        {
                            fileRecord = await _mediator.Send(inboundFileRecord, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            fileRecord = await _mediator.Send(new UpdateInboundFileRecordStatus.Command(fileRecord.Id, inboundFileRecord.Status, fileRecord.Status, inboundFileRecord.StatusDescription, fileRecord.QueueMessageId), cancellationToken).ConfigureAwait(false);
                        }

                        if (hasError)
                        {
                            _logger.LogWarning("Marking record {RecordNumber} as ParseError", i);
                            continue;
                        }

                        var recPublish = new Nc835RecordNotification(fileRecord!.Id, rec.TRN!.TRN02, rec.BPR!.BPR04, rec.BPR.BPR07, rec.BPR.BPR09, rec.BPR.BPR13, rec.BPR.BPR15, rec.TRN.TRN03);

                        await _mediator.Send(new StartRecordProcess.Command(recPublish), cancellationToken).ConfigureAwait(false);
                    }
                }

                if (parseErrs.Count > 0)
                {
                    var email = new SendEmail.Notification(
                        CreateMessage(record.FilePath, BuildErrorString(parseErrs, record.FilePath)),
                        "Critical Alert - NC 835 File Warnings");

                    await _mediator.Publish(email, cancellationToken).ConfigureAwait(false);
                }

                currentStatus = await UpdateStatus(request.InboundFileId, InboundFileStatusEnum.Completed, currentStatus, null, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Completed Parsing of file");

                return true;
            }

            private async Task<InboundFileStatusEnum> UpdateStatus(long id, InboundFileStatusEnum newStatus, InboundFileStatusEnum oldStatus, Guid? queueMessageId, CancellationToken cancellationToken)
            {
                var updatedRecord = await _mediator.Send(new UpdateInboundFileStatus.Command(id, newStatus, oldStatus, queueMessageId), cancellationToken).ConfigureAwait(false);
                if (updatedRecord == null)
                {
                    throw new Exception("Unexpected error occured when updating the status");
                }

                return updatedRecord.Status;
            }

            private static string BuildErrorString(List<ParseErrors> errors, string? fileName)
            {
                var rows = errors.Select(x => $@"
                        <tr>
                          <td align='right'>{x.recordNumber}</td>
                          <td>{x.errorMessage}</td>
                        </tr>");

                return $"The following records were sent back as warnings for: <b>{fileName}</b><br/>" +
                    "<br/>" +
                    @"<table border='1' style='margin: auto; border-collapse: collapse'>
                        <thead style='font-weight:bold;'>
                        <tr>
                          <th colspan='2'>PRF Errors</th>
                        </tr>
                        <tr>
                          <th>Record Number</th>
                          <th>Error</th>
                        </tr>
                        </thead>" +
                    $"{string.Join("", rows)}" +
                    @$" <tfoot>
                          <tr>
                            <td>Count:</td>
                            <td align='right'>{errors.Count}</td>
                          </tr>
                        </tfoot>
                      </table>" +
                    "<br/>";
            }

            private static string CreateMessage(string? filePath, string message) =>
                $"<b>Current Time</b>: {DateTime.Now:F}<br/>" +
                $"<b>File Name</b>: {filePath}<br/>" +
                $"<b>Error</b>: {message}";
        }
    }
}
