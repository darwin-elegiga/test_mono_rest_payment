using System;
using Azure.Storage.Blobs.Models;

namespace VPay.Optum.PaymentRouting.Api.Controllers.Models
{
    public record BlobInfoPayload(
        BlobContentInfo BlobInfo,
        Uri BlobUri,
        string BlobContainerName,
        string BlobName);
}
