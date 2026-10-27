using MediatR;
using Newtonsoft.Json;
using VPay.Gateway.Common.Recon.Models;

namespace VPay.Gateway.Common.Recon;

public class ClientReconQuery : IRequest<(HttpResponseMessage? Error, ClientReconDto? Item)>
{
    public ClientReconQuery(
        string originCode,
        string clientCode,
        string paymentId,
        DateTime? startTime,
        DateTime? endTime)
    {
        OriginCode = originCode;
        ClientCode = clientCode;
        PaymentId = paymentId;
        StartTime = startTime;
        EndTime = endTime;
    }

    public string OriginCode { get; }

    public string ClientCode { get; }

    public string PaymentId { get; }

    public DateTime? StartTime { get; }

    public DateTime? EndTime { get; }
}

public class ClientReconQueryHandler : IRequestHandler<ClientReconQuery, (HttpResponseMessage?, ClientReconDto?)>
{
    private readonly IReconApi _reconApi;

    public ClientReconQueryHandler(IReconApi reconApi) =>
        _reconApi = reconApi ?? throw new ArgumentNullException(nameof(reconApi));

    public async Task<(HttpResponseMessage?, ClientReconDto?)> Handle(ClientReconQuery request, CancellationToken cancellationToken)
    {
        var response = await _reconApi.GetClientRecons(
            request.OriginCode,
            request.ClientCode,
            request.PaymentId,
            new DateRangeQuery
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime
            },
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            // if the result is a success, we'll return the deserialized object
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (null, JsonConvert.DeserializeObject<ClientReconDto>(content));
        }

        // for a non-success, just pass the response directly down to the caller
        return (response, null);
    }
}
