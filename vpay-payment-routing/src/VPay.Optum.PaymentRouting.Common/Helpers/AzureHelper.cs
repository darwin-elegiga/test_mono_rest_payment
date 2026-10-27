using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace VPay.Optum.PaymentRouting.Common.Helpers
{
    public enum EndpointProtocol
    {
        Http,
        Https
    }

    public enum EndpointType
    {
        BlobEndpoint,
        QueueEndpoint,
        TableEndpoint,
        FileEndpoint
    }

    [ExcludeFromCodeCoverage]
    public static class AzureHelper
    {
        public static string BuildServiceBusConnectionString(
            string hostName,
            string accountName,
            string accountKey)
        {
            var sb = new StringBuilder();
            sb.Append($"Endpoint=sb://{hostName};");
            sb.Append($"SharedAccessKeyName={accountName};");
            sb.Append($"SharedAccessKey={accountKey};");

            return sb.ToString();
        }

        public static string BuildStorageConnectionString(
            string accountName,
            string accountKey,
            EndpointType endpointType,
            string endpointUrl,
            EndpointProtocol protocol = EndpointProtocol.Https)
        {
            var sb = new StringBuilder();
            sb.Append($"DefaultEndpointsProtocol={protocol.ToString().ToLower()};");
            sb.Append($"AccountName={accountName};");
            sb.Append($"AccountKey={accountKey};");
            sb.Append($"{endpointType}={endpointUrl};");

            return sb.ToString();
        }

        public static string BuildStorageConnectionString(
            string sasToken,
            EndpointType endpointType,
            string endpointUrl,
            bool sasTokenEscaped = false)
        {
            var sb = new StringBuilder();
            sb.Append($"{endpointType}={endpointUrl};");
            if (!string.IsNullOrEmpty(sasToken))
            {
                sb.Append($"SharedAccessSignature={sasToken}");
            }
            return sasTokenEscaped ? sb.ToString() : Uri.EscapeDataString(sb.ToString());
        }

        public static string BuildStorageConnectionString(
            string accountName,
            string accountKey,
            Dictionary<EndpointType, string> endpointUrlList,
            EndpointProtocol protocol = EndpointProtocol.Https)
        {
            var sb = new StringBuilder();
            sb.Append($"DefaultEndpointsProtocol={protocol.ToString().ToLower()};");
            sb.Append($"AccountName={accountName};");
            sb.Append($"AccountKey={accountKey};");

            foreach (var endpoint in endpointUrlList)
            {
                sb.Append($"{endpoint.Key}={endpoint.Value};");
            }

            return sb.ToString();
        }

        public static string BuildStorageConnectionString(
            string sasToken,
            Dictionary<EndpointType, string> endpointUrlList,
            EndpointProtocol protocol = EndpointProtocol.Https,
            bool sasTokenEscaped = false)
        {
            var sb = new StringBuilder();
            sb.Append($"DefaultEndpointsProtocol={protocol.ToString().ToLower()};");
            foreach (var endpoint in endpointUrlList)
            {
                sb.Append($"{endpoint.Key}={endpoint.Value};");
            }
            if (!string.IsNullOrEmpty(sasToken))
            {
                sb.Append($"SharedAccessSignature={sasToken}");
            }
            return sasTokenEscaped ? sb.ToString() : Uri.EscapeDataString(sb.ToString());
        }

        public static string BuildServiceBusUrl(string hostName, EndpointProtocol protocol = EndpointProtocol.Https)
        {
            var queueUri = $"{protocol}://{hostName}";
            return queueUri;
        }

        public static string BuildServiceBusQueueUrl(string hostName, string queueName, EndpointProtocol protocol = EndpointProtocol.Https)
        {
            var queueUri = $"{protocol}://{hostName}/{queueName}";
            return queueUri;
        }

        public static string BuildServiceBusQueueUrl(string serviceBusUrl, string queueName)
        {
            var queueUri = $"{serviceBusUrl}/{queueName}";
            return queueUri;
        }

        public static string BuildBlobStorageUri(string endpointUrl, string blobContainerName, string fileName)
        {
            var fileUri = $"{endpointUrl}/{blobContainerName}/{fileName}";
            return fileUri;
        }

        /// <summary>
        /// Use a shared access signature to access a Storage Account.
        /// A shared access signature (SAS) is a URI that grants restricted
        /// access rights to Azure Storage resources. You can provide a shared
        /// access signature to clients who should not be trusted with your
        /// storage account key but to whom you wish to delegate access to
        /// certain storage account resources. By distributing a shared access
        /// signature URI to these clients, you can grant them access to a
        /// resource for a specified period of time, with a specified set of
        /// permissions.
        /// </summary>
        public static Uri BuildBlobEndpointSasUri(string blobEndpoint, string sasQuery)
        {
            // Build a SAS URI
            var blobSasUri = new UriBuilder($"{blobEndpoint}");
            blobSasUri.Query = sasQuery;

            // Return the URI
            return blobSasUri.Uri;
        }

        /// <summary>
        /// Use a shared access signature to access a Storage Account.
        /// A shared access signature (SAS) is a URI that grants restricted
        /// access rights to Azure Storage resources. You can provide a shared
        /// access signature to clients who should not be trusted with your
        /// storage account key but to whom you wish to delegate access to
        /// certain storage account resources. By distributing a shared access
        /// signature URI to these clients, you can grant them access to a
        /// resource for a specified period of time, with a specified set of
        /// permissions.
        /// </summary>
        public static Uri BuildFileEndpointSasUri(string fileEndpoint, string shareName, string sasQuery)
        {
            // Build a SAS URI
            var fileSasUri = new UriBuilder($"{fileEndpoint}/{shareName}");
            fileSasUri.Query = sasQuery;

            // Return the URI
            return fileSasUri.Uri;
        }
    }
}
