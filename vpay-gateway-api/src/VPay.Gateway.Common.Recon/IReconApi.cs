using Refit;
using VPay.Gateway.Common.Recon.Models;

namespace VPay.Gateway.Common.Recon;

/// <summary>
/// Interface defining the shape of the Recon API.
/// </summary>
public interface IReconApi
{
    [Get("/api/client-recons/{originCode}/{clientCode}")]
    Task<HttpResponseMessage> GetClientRecons(string originCode, string clientCode, [Query] string paymentId, DateRangeQuery dateRange, CancellationToken token);
}
