using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Optum.Data.Sql.InboundFiles;
using VPay.Optum.Http.Loaf.Models;

namespace VPay.Optum.Nc835Record.Application
{
    public static class ProcessIndividualRecord
    {
        public record Command(Nc835RecordNotification Record, Guid? QueueMessageId) : IRequest<bool>;

        public class Handler : IRequestHandler<Command, bool>
        {
            private readonly IMediator _mediator;
            private readonly ILogger<Handler> _logger;

            public Handler(IMediator mediator, ILogger<Handler> logger)
            {
                _mediator = mediator;
                _logger = logger;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                var message = request.Record;
                var record = await _mediator.Send(new GetInboundFileRecord.Query(message.InboundFileRecordId), cancellationToken).ConfigureAwait(false);

                if (record == null)
                {
                    throw new ItemNotFoundException($"Could not located request {message.InboundFileRecordId}");
                }

                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["InboundFileRecordId"] = record.Id,
                    ["InboundFileId"] = record.InboundFileId,
                    ["RecordNumber"] = record.RecordNumber,
                    ["RecordKey"] = record.RecordKey,
                    ["TransactionId"] = message.TransactionId
                });

                if (record.Status == InboundFileStatusEnum.Completed)
                {
                    _logger.LogWarning("Inbound File Record {InboundFileRecordId} has already been successfully completed", message.InboundFileRecordId);
                    return false;
                }

                if (record.QueueMessageId.HasValue && record.QueueMessageId != request.QueueMessageId)
                {
                    _logger.LogError("Inbound File Record {InboundFileRecordId} is currently being processed with Id of {QueueMessageGuid}", record.Id, record.QueueMessageId);
                    return false;
                }

                var currentStatus = record.Status;

                _logger.LogInformation("Record found for processing with status {Status}", currentStatus);

                currentStatus = await UpdateStatus(request.Record.InboundFileRecordId, InboundFileRecordStatusEnum.Started, currentStatus, request.QueueMessageId, cancellationToken).ConfigureAwait(false);

                TransactionBankInfoUpsert trans = new()
                {
                    ReferenceId = message.TransactionId,
                    FromRoutingNumber = message.FromRouting,
                    FromAccountNumber = message.FromAccount,
                    ToRoutingNumber = message.ToRouting,
                    ToAccountNumber = message.ToAccount
                };

                await _mediator.Send(new CreateLoafTransaction.Command(trans), cancellationToken).ConfigureAwait(false);

                if (long.TryParse(message.TransactionId, out var transactionId))
                {
                    var value = await _mediator.Send(new TieTransactionTo835Record.Command(record.Id, transactionId), cancellationToken).ConfigureAwait(false);
                    if (value != null)
                    {
                        //Check if we are tracking.
                        await _mediator.Send(new ValidateTransactionDetails.Command(transactionId, request.Record.PayerTaxId), cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    _logger.LogError("Could not convert {ReferenceId} to number", message.TransactionId);
                }

                currentStatus = await UpdateStatus(request.Record.InboundFileRecordId, InboundFileRecordStatusEnum.Completed, currentStatus, null, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Completed Update for Record {ReferenceId}", message.TransactionId);

                return true;
            }

            private async Task<InboundFileRecordStatusEnum> UpdateStatus(long id, InboundFileRecordStatusEnum newStatus, InboundFileRecordStatusEnum oldStatus, Guid? queueMessageId, CancellationToken cancellationToken)
            {
                var updatedRecord = await _mediator.Send(new UpdateInboundFileRecordStatus.Command(id, newStatus, oldStatus, "", queueMessageId), cancellationToken).ConfigureAwait(false);
                if (updatedRecord == null)
                {
                    throw new Exception("Unexpected error occured when updating the status");
                }

                return updatedRecord.Status;
            }
        }
    }
}
