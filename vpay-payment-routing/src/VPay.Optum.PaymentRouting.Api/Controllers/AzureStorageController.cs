using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using VPay.MassTransit.Abstractions;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ExcludeFromCodeCoverage]
    public class AzureStorageController : ControllerBase
    {
        private readonly BlobServiceClient _storage;
        private readonly StorageConfig _storageConfig;

        public AzureStorageController(
            BlobServiceClient storage,
            StorageConfig storageConfig)
        {
            _storage = storage;
            _storageConfig = storageConfig;
        }

        [HttpGet("download-blob-with-sas")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult> DownloadFileFromBlobStorageWithSas(
        [Required(AllowEmptyStrings = false)][StringLength(255)] string blobUri,
        [Required(AllowEmptyStrings = false)][StringLength(255)] string fileName,
        string blobContainerName = null,
        bool useSharedAccessKey = true,
        bool deleteBlobAfterDownload = false,
        CancellationToken token = default)
        {
            blobContainerName ??= _storageConfig.BlobContainerName;
            await using var content = new MemoryStream();

            Azure.Response response;
            BlobClient blobClient;
            if (useSharedAccessKey)
            {
                var b = new UriBuilder(blobUri)
                {
                    // Azure REST interface for blobs protected with SAS
                    Query = _storageConfig.SharedAccessSignature
                };
                blobClient = new BlobClient(b.Uri);

                // Retrieve file URI from Blob Storage
                response = await blobClient.DownloadToAsync(content, token);
            }
            else
            {
                var b = new UriBuilder(blobUri);
                BlobContainerClient blobContainerClient = _storage.GetBlobContainerClient(blobContainerName);
                string blobName = b.Uri.Segments[^1];
                blobClient = blobContainerClient.GetBlobClient(blobName);

                response = await blobClient.DownloadToAsync(content, token);
            }

            if (response == null || content == null)
            {
                return BadRequest("Unable to download file.");
            }

            byte[] fileBytes = content.ToArray();

            if (fileBytes.Length == 0)
            {
                return NotFound();
            }

            if (deleteBlobAfterDownload)
            {
                await blobClient.DeleteAsync(cancellationToken: token);
            }

            return File(fileBytes, "text/plain", fileName);
        }
    }
}
