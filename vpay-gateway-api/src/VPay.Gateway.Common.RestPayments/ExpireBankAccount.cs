using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Refit;

namespace VPay.Gateway.Common.RestPayments;
public static class ExpireBankAccount
{
    public record ExpireBankAccountCommand(string origin, string payerCode, string subpayerCode, string clientMemberId, string zetaMemberId) : IRequest<IApiResponse<ExpireBankAccountResponse>>;

    /// <summary>
    /// Expire Bank Account Response Object
    /// </summary>
    /// <param name="memBankAcct">Member Bank Account Code</param>    
    /// <param name="successCode">Success Code</param>
    /// <param name="successDesc">Success Description</param>
    /// <param name="optumMemberId">Vpay Member ID</param>

    public record ExpireBankAccountResponse(string status, string optumMemberId, string successDesc);

    public class Handler : IRequestHandler<ExpireBankAccountCommand, IApiResponse<ExpireBankAccountResponse>>
    {
        private readonly IRestPaymentApi _restPaymentApi;

        public Handler(IRestPaymentApi restPaymentApi) => _restPaymentApi =
            restPaymentApi ?? throw new ArgumentNullException(nameof(restPaymentApi));

        public async Task<IApiResponse<ExpireBankAccountResponse>> Handle(ExpireBankAccountCommand request, CancellationToken cancellationToken)
        {
            var apiResponse = await _restPaymentApi.ExpireBankAccount(
                request,
                cancellationToken
            ).ConfigureAwait(false);
            return apiResponse;

        }
    }
}
