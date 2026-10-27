using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using VPay.MassTransit.Abstractions;

namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseBlobStorageContainer(this IApplicationBuilder appBuilder, BlobServiceClient blobServiceClient, StorageConfig storageConfig)
        {
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(storageConfig.BlobContainerName);

            // Explicitly checking for the container's existence before calling CreateIfNotExists is a workaround for a known SDK issue.
            if (!container.Exists())
            {
                container.CreateIfNotExists();
            }

            return appBuilder;
        }
    }
}
