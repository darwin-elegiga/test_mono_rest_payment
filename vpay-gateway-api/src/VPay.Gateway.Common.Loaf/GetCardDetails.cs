using MediatR;
using Refit;

namespace VPay.Gateway.Common.Loaf;

public class GetCardDetails
{
    /// <summary>
    /// GetCardDetails Request Object
    /// </summary>
    /// <param name="ClientCode">VPay Client Code</param>
    /// <param name="PaymentId">Client User Id / Payment Id</param>
    /// <param name="BillingEntityCode">VPay Billing Entity Code</param>
    /// <param name="TransactionId">VPay Transaction Id</param>
    public record Query(string PaymentId, string ClientCode, string BillingEntityCode, long TransactionId) : IRequest<IApiResponse<Response?>>;

    /// <summary>
    /// GetCardDetails Response Object
    /// </summary>
    /// <param> name="PaymentId">The Payment Id used to search</param>
    /// <param name="TransactionId">The Transaction Identifier</param>
    /// <param name="CreditCardNumber">The Credit Card Number issued to the client</param>
    /// <param name="ExpirationDate">The Expiration Date of the Credit Card</param>
    /// <param name="CVCCode">The CVC Code of the Credit Card</param>
    /// <param name="AddressLine1">The Address Line 1 of the Credit Card</param>
    /// <param name="AddressLine2">The Address Line 2 of the Credit Card</param>
    /// <param name="ZipCode">The Zip Code of the Credit Card</param>
    /// <param name="CardBalance">The current balance of the card</param>
    public record Response(
        string PaymentId,
        string TransactionId,
        string CreditCardNumber,
        DateTime ExpirationDate,
        string CVCCode,
        string AddressLine1,
        string AddressLine2,
        string ZipCode,
        decimal CardBalance);

    public class Handler : IRequestHandler<Query, IApiResponse<Response?>>
    {
        private readonly ILoafApi _loafApi;

        public Handler(ILoafApi loafApi) => _loafApi = loafApi ?? throw new ArgumentNullException(nameof(loafApi));

        public async Task<IApiResponse<Response?>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = await _loafApi.TransactionInfo(request.TransactionId, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<Response?>(new HttpResponseMessage(), new Response(request.PaymentId, response.Content.TransactionId,
                    response.Content.CreditCardNumber,
                    response.Content.EndDateTime ?? DateTime.MinValue, response.Content.CVC2,
                    response.Content.AddressLine1, response.Content.AddressLine2,
                    response.Content.PostalCode, response.Content.LoadedAmount ?? 0), new RefitSettings(), null);
            }

            return new ApiResponse<Response?>(new HttpResponseMessage(response.StatusCode), null, new RefitSettings(), response.Error);
        }
    }
}
