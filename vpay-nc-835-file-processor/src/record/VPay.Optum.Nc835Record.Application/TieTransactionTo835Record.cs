using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.Data.Connection;
using VPay.Optum.Data.Sql.PrfFiles;

namespace VPay.Optum.Nc835Record.Application
{
    public static class TieTransactionTo835Record
    {
        public record Command(long InboundFileRecordId, long TransactionId) : IRequest<PrfTransaction?>;

        public class Handler : IRequestHandler<Command, PrfTransaction?>
        {
            private readonly IMediator _mediator;
            private readonly IDbUnitOfWork _uow;
            private readonly ILogger<Handler> _logger;

            public Handler(IMediator mediator, IDbUnitOfWork uow, ILogger<Handler> logger)
            {
                _mediator = mediator;
                _uow = uow;
                _logger = logger;
            }

            public async Task<PrfTransaction?> Handle(Command request, CancellationToken cancellationToken)
            {
                var file = await _mediator.Send(new GetPrfTransactionByTransactionId.Query(request.TransactionId), cancellationToken).ConfigureAwait(false);
                if (file == null)
                {
                    _logger.LogWarning("Could not locate transaction [{TransactionId}] in one sent to Optum", request.TransactionId);
                    return null;
                }

                using var trans = await _uow.BeginTransaction();

                await _mediator.Send(new MarkTransactionAs835Received.Command(file.Id), cancellationToken).ConfigureAwait(false);

                await _mediator.Send(new LinkTransactionToFileRecord.Command(file.Id, request.InboundFileRecordId), cancellationToken).ConfigureAwait(false);

                trans.Commit();

                return file;
            }
        }
    }
}
