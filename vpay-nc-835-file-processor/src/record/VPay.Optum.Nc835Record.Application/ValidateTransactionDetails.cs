using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Optum.Http.Transactions;
using VPay.Optum.Nc835Record.Application.Queries;

namespace VPay.Optum.Nc835Record.Application
{
    public static class ValidateTransactionDetails
    {
        public record Command(long TransactionId, string PayerTaxId) : IRequest;

        public class Handler : IRequestHandler<Command>
        {
            private readonly ITransactionsApi _transactionsApi;
            private readonly IMediator _mediator;
            private readonly ILogger<Handler> _logger;

            public Handler(ITransactionsApi transactionsApi, IMediator mediator, ILogger<Handler> logger)
            {
                _transactionsApi = transactionsApi;
                _mediator = mediator;
                _logger = logger;
            }

            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var findTransaction = await _transactionsApi.GetByTransactionId(request.TransactionId, cancellationToken).ConfigureAwait(false);

                if (findTransaction.Items == null || findTransaction.Items.Count == 0)
                {
                    throw new ItemNotFoundException("Could not locate transaction in the iSeries");
                }

                var trx = findTransaction.Items[0];

                var subPayerId = await _mediator.Send(new GetSubPayerId.Query(trx.ClientCode, trx.BillingEntityCode), cancellationToken).ConfigureAwait(false);

                if (subPayerId == null)
                {
                    //should not happen
                    throw new ItemNotFoundException($"Could not locate BE [{trx.ClientCode} {trx.BillingEntityCode}] in the iSeries");
                }

                var strSubPayer = $"1{subPayerId:000000000}";

                if (request.PayerTaxId == strSubPayer)
                {
                    _logger.LogCritical(10001, "TRN03 is the same as the VPay's SubPayerId {VPaySubPayerId} and should not be", strSubPayer);
                }

                return;
            }
        }
    }
}
