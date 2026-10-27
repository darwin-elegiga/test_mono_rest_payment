using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Kubera.Models;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Application;

public class PayerSubPayerAndPaymentNumberToTransactionId
{
    /// <summary>
    /// Payment Number To Transaction Id Request Object
    /// </summary>
    /// <param name="SubPayerId">Optum Sub Payer Id</param>
    /// <param name="PaymentId">Optum Payment Id</param>
    public record Query(string PayerId, string SubPayerId, string PaymentId) : IRequest<Response>;

    /// <summary>
    /// Payment Number To Transaction Id Response Object
    /// </summary>
    /// <param name="ClientCode">VPay Client Code</param>
    /// <param name="BillingEntityCode">VPay Billing Entity Code</param>
    /// <param name="TransactionId">VPay Transaction Id</param>
    public record Response(string ClientCode, string BillingEntityCode, long TransactionId);

    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Handler> _logger;

        public Handler(IMediator mediator, ILogger<Handler> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            /* We'll convert the Unconsolidated Payment Number/UserKey to the transaction id by
             * 1: Load the Optum Sub-Payer mapping to get the Client the payment is associated with
             * 2: Use the Client + Unconsolidated Payment Number/UserKey to look up the transaction details
             * 3: Get the most-recent record, and get the transaction id for that record
            */

            var optumSubPayer = await _mediator.Send(new OptumSubPayerQuery(request.SubPayerId), cancellationToken).ConfigureAwait(false);
            if (optumSubPayer == null || optumSubPayer.OptumPayorId != request.PayerId)
            {
                throw new ItemNotFoundException("Sub-payer cannot be found.");
            }

            // Get the payment information from kubera
            var allTransactions = await _mediator.Send(new GetTransactionsByPaymentIdQuery(optumSubPayer.ClientCode, request.PaymentId), cancellationToken).ConfigureAwait(false);
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            var transactions = (allTransactions ?? Array.Empty<Transaction>()).Where(x =>
                    x.BillingEntityCode == optumSubPayer.BillingEntityCode && x.ClientCode == optumSubPayer.ClientCode)
                .ToArray();

            if (transactions == null || transactions.Length == 0)
            {
                _logger.LogInformation("Transactions not found for PaymentId = {PaymentId}, Client '{ClientCode}'", request.PaymentId, optumSubPayer.ClientCode);
                throw new ItemNotFoundException("Could not find payment for the sub-payer.");
            }

            return new Response(optumSubPayer.ClientCode, optumSubPayer.BillingEntityCode,
                transactions.OrderByDescending(x => x.DateLoaded).First().TransactionId);
        }
    }
}
