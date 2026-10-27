using System.Threading;
using System.Threading.Tasks;
using Refit;
using VPay.Optum.Http.ConfigApi.Models;

namespace VPay.Optum.Http.ConfigApi
{
    public interface IConfigApi
    {
        [Get("/api/billing-entities/{clientCode}/{billingEntityCode}")]
        Task<BillingEntity> GetBillingEntity(string clientCode, string billingEntityCode, CancellationToken token = default);
    }
}
