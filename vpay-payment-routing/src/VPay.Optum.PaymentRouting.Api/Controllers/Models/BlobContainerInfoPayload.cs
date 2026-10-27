using System.Collections.Generic;
using Azure.Storage.Blobs.Models;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public record BlobContainerInfoPayload(
        string BlobEndpoint,
        string BlobContainer,
        IEnumerable<BlobItem> BlobNames);
}
