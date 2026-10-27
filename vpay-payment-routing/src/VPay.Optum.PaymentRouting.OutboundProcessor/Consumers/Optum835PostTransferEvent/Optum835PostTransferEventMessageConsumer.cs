#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.FileTransfer.Messaging;
using VPay.FileTransfer.Messaging.Abstractions;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Consumers.Optum835PostTransferEvent
{
    public class Optum835PostTransferEventMessageConsumer : PostTransferEventMessageConsumer
    {
        private readonly IMediator _mediator;

        public Optum835PostTransferEventMessageConsumer(
            IMediator mediator,
            IFileReadyForTransferNotificationPublisher fileReadyForTransferNotificationPublisher,
            ILogger<Optum835PostTransferEventMessageConsumer> logger)
            : base(fileReadyForTransferNotificationPublisher, logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        protected override async Task UpdateFileTrackingStatus(PostTransferEventMessage message, object? trackingRecord, TransferStatus status, CancellationToken cancellationToken)
        {
 
            var record = (Data.Sql.Models.Outbound835File)trackingRecord!;

            var newStatus = status == TransferStatus.Success ? Outbound835FileStatus.Completed : Outbound835FileStatus.FailedSend;
            var updateStatusCommand = new UpdateOutbound835FileStatus.Command(record.Id, newStatus);

            if (status == TransferStatus.Success)
            {
                updateStatusCommand = updateStatusCommand with
                {
                    SentDateTime = message.TransferredDateTime
                };
            }

            _ = await _mediator.Send(updateStatusCommand, cancellationToken).ConfigureAwait(false);

            var count = await _mediator.Send(new CreateCtrStatusPublishCommand.Command(record.Id), cancellationToken);
            base.Logger.LogInformation($"Optum835PostTransferEventMessageConsumer: Published  {count} transactions for {{OutboundFileId}}", record.Id);
        }

        protected override Task<bool> ShouldMessageBeProcessed(PostTransferEventMessage message, object? trackingRecord, CancellationToken cancellationToken)
        {
            var record = (Data.Sql.Models.Outbound835File)trackingRecord!;
            return Task.FromResult(record.Status != Outbound835FileStatus.Completed);
        }

        protected override async Task<object?> LoadFileTrackingRecord(PostTransferEventMessage message, CancellationToken cancellationToken)
        {
            var fileId = message.OriginalMessage.Parameters.Value<long>("fileId");
            var record = await _mediator.Send(new GetOutbound835FileById.Query(fileId), cancellationToken).ConfigureAwait(false);
            if (record is null)
            {
                // if the file can't be found, that's a problem
                Logger.LogError("File with ID '{FileId}' could not be found", fileId);
                throw new ItemNotFoundException($"File with ID '{fileId}' could not be found");
            }

            return record;
        }
    }
}
