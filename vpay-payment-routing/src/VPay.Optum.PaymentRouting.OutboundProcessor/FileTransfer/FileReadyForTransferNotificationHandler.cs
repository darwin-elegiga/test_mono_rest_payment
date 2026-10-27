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

namespace VPay.Optum.PaymentRouting.OutboundProcessor.FileTransfer
{
    public class FileReadyForTransferNotificationHandler : MassTransitFileReadyForTransferNotificationHandlerBase
    {
        private readonly IMediator _mediator;

        public FileReadyForTransferNotificationHandler(
            IMediator mediator,
            IFileTransferServiceBus fileTransferServiceBus,
            ILogger<FileReadyForTransferNotificationHandler> logger)
            : base(fileTransferServiceBus, logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        protected override async Task UpdateFileStatus(FileReadyForTransferNotification notification, CancellationToken cancellationToken)
        {
            var fileId = notification.AdditionalProperties!.Value<long>("fileId");

            var record = await _mediator.Send(new GetOutbound835FileById.Query(fileId), cancellationToken).ConfigureAwait(false);

            // NOTE: This *should not* happen, but we'll handle it just in case
            if (record is null)
            {
                // if the file can't be found, that's a problem
                Logger.LogError("File with ID '{FileId}' could not be found", fileId);
                throw new ItemNotFoundException($"File with ID '{fileId}' could not be found");
            }

            // update the status to "transferring" *only* if it's currently in the "Built" state
            if (record.Status == Outbound835FileStatus.Built)
            {
                _ = await _mediator.Send(
                    new UpdateOutbound835FileStatus.Command(fileId, Outbound835FileStatus.Transferring),
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }
    }
}
