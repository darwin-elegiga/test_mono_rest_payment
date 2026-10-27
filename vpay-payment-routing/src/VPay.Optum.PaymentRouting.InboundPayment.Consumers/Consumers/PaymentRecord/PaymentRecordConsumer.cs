using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.MassTransit.Abstractions;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Commands;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.PaymentRecord
{
    public class PaymentRecordConsumer : IConsumer<Common.Serialization.PaymentRecord>
    {
        private readonly ILogger<PaymentRecordConsumer> _logger;
        private readonly IMediator _mediator;
        private readonly BlobServiceClient _storage;
        private readonly StorageConfig _storageConfig;

        // Used for Unit Testing
        internal static Func<Uri, BlobClient> NewBlobClient = (Uri url) => new BlobClient(url);

        public PaymentRecordConsumer(
            ILogger<PaymentRecordConsumer> logger,
            IMediator mediator,
            BlobServiceClient storage,
            StorageConfig storageConfig)
        {
            _logger = logger;
            _mediator = mediator;
            _storage = storage;
            _storageConfig = storageConfig;
        }

        [ExcludeFromCodeCoverage]
        public async Task Consume(ConsumeContext<Common.Serialization.PaymentRecord> context)
        {
            await Consume(context.Message, context.CancellationToken).ConfigureAwait(false);
        }

        public async Task Consume(Common.Serialization.PaymentRecord paymentRecord, CancellationToken cancellationToken)
        {
            await ClaimCheck(paymentRecord, cancellationToken).ConfigureAwait(false);
            await _mediator.Send(new AddPaymentRecord.Command { PaymentRecord = paymentRecord }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a file from storage if the Payment Record message contains a URL in the Blob Storage URI property.
        /// The Claim Check converts the blob contents to string and copies the blob contents to the Payment Clob property.
        /// Payment records with a null or "null" value in the Blob Storage property already have the contents
        /// in-lined in the Payment Clob property and skip the Claim Check operation.
        /// </summary>
        /// <param name="payment">The payment record that holds the blob storage URI.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        private async Task ClaimCheck(Common.Serialization.PaymentRecord payment, CancellationToken token = default)
        {
            if (payment?.PaymentClob?.Equals("null", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                payment.PaymentClob = null;
            }

            if (payment?.BlobStorageUri?.Equals("null", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                payment.BlobStorageUri = null;
            }

            if (string.IsNullOrWhiteSpace(payment.PaymentClob) &&
                !string.IsNullOrWhiteSpace(payment.BlobStorageUri))
            {
                var watch = Stopwatch.StartNew();

                byte[] fileBytes = await GetBlobContent(payment, token);

                if (fileBytes.Length > 0)
                {
                    // Set the Payment Clob with the contents for the Blob Storage URI
                    payment.PaymentClob = System.Text.Encoding.UTF8.GetString(fileBytes);
                }

                watch.Stop();
                _logger.LogInformation("Downloaded blob for payment request with OptumBatchId: {OptumBatchId}, UserKey: {UserKey}, AzureBlobFileSize: {AzureBlobFileSize}, AzureBlobDownloadDuration: {Duration} ms.",
                payment.BatchId, payment.UnconsolidatedPaymentNbr, fileBytes.Length, watch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Gets the blob storage file contents directly from the Azure or Azurite blob storage repository.
        /// Uses the Azure REST interface if the Azure configuration includes a Shared Access Signature (SAS);
        /// uses the Azure SDK interface otherwise. Azure connection supports SAS and non-SAS blob access modes;
        /// Azurite connections only support blob access without SAS.
        /// </summary>
        /// <param name="blobUri">The full URL of the blob to download including storage host, blob container name,
        /// and blob name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The byte array containing the file contents.</returns>
        private async Task<byte[]> GetBlobContent(Common.Serialization.PaymentRecord payment, CancellationToken token = default)
        {
            UriBuilder uriBuilder = new UriBuilder(payment.BlobStorageUri);
            await using MemoryStream content = new();

            if (!string.IsNullOrWhiteSpace(_storageConfig.SharedAccessSignature))
            {
                // Azure REST interface for blobs protected with SAS
                uriBuilder.Query = _storageConfig.SharedAccessSignature;
                var blobClient = NewBlobClient(uriBuilder.Uri);

                _logger.LogInformation("Downloading blob for payment request with OptumBatchId: {OptumBatchId}, UserKey: {UserKey}, AzureBlobStorageUri: {AzureBlobStorageUri}",
                    payment.BatchId, payment.UnconsolidatedPaymentNbr, uriBuilder.Uri);

                // Retrieve file URI from Blob Storage
                var result = await blobClient.DownloadToAsync(content, token);
            }
            else if (uriBuilder.Uri.Segments.Length > 1)
            {
                // Azure SDK interface for blobs not protected with SAS
                string blobName = uriBuilder.Uri.Segments[^1];
                string blobContainerName = _storageConfig.BlobContainerName;
                BlobContainerClient blobContainerClient = _storage.GetBlobContainerClient(blobContainerName);
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                _logger.LogInformation("Downloading blob for payment request (No-SAS) with OptumBatchId: {OptumBatchId}, UserKey: {UserKey}, AzureBlobStorageUri: {AzureBlobStorageUri}",
                payment.BatchId, payment.UnconsolidatedPaymentNbr, payment.BlobStorageUri);

                // Retrieve file URI from Blob Storage
                var result = await blobClient.DownloadToAsync(content, token);
            }
            else
            {
                return Array.Empty<byte>();
            }

            byte[] fileBytes = content.ToArray();
            return fileBytes;
        }
    }
}
