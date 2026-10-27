using System.Collections.Generic;
using Azure.Storage.Blobs.Models;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public record BlobServiceInfoPayload(
        string BlobEndpoint,
        IEnumerable<BlobContainerItem> BlobContainerNames);
}
