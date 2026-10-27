using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Refit;

namespace VPay.Gateway.Common.RestPayments;
public static class AddBankAccount
{
    public record Command(string origin, string clientCode, string billingEntityCode, string clientMemberId, string zetaMemberId, string bankRoutingNumber, string bankAccountNumber) : IRequest<IApiResponse<AddBankAccountResponse>>;

    /// <summary>
    /// StopPayment Response Object
    /// </summary>
    /// <param name="memBankAcct">Member Bank Account Code</param>    
    /// <param name="successCode">Success Code</param>
    /// <param name="successDesc">Success Description</param>
    /// <param name="vpMemberID">Vpay Member ID</param>

    public record AddBankAccountResponse(string id, string vPayMemberId, string successCode, string successDesc);

    public class Handler : IRequestHandler<Command, IApiResponse<AddBankAccountResponse>>
    {
        private readonly IRestPaymentApi _restPaymentApi;

        public Handler(IRestPaymentApi restPaymentApi) => _restPaymentApi =
            restPaymentApi ?? throw new ArgumentNullException(nameof(restPaymentApi));

        public async Task<IApiResponse<AddBankAccountResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var apiResponse = await _restPaymentApi.AddBankAccount(
                request,
                cancellationToken
            ).ConfigureAwait(false);
            return apiResponse;

        }
    }
}
