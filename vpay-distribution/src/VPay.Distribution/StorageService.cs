using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.Distribution.Common;
using VPay.DocSys.Storage;

namespace VPay.Distribution
{
    public class StorageService : IStorageService
    {
        private readonly IStorageManager _storageManager;
        private readonly ILogger<StorageService> _logger;

        public StorageService(
            IStorageManager storageManager,
            ILogger<StorageService> logger)
        {
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get the list of ArchiveFiles from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The list of ArchiveFiles.</returns>
        public async Task<IEnumerable<IArchiveFile>> GetArchiveFiles(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _storageManager.GetArchivesForTransactionId(transactionId);
                return result;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR retrieving Archive Files for TransactionId [{TransactionId}]", transactionId);
                _logger.LogError(ex, "ERROR retrieving Archive Files");
                throw;
            }
        }

        /// <summary>
        /// Get the ArchiveDetail for the STP Remittance JSON file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The ArchiveDetail for the STP Remittance JSON file.</returns>
        public async Task<IArchiveDetail> GetRemitJsonDetail(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var archiveFiles = await _storageManager
                    .GetArchivesForTransactionId(transactionId)
                    .ConfigureAwait(false);

                if (!archiveFiles.Any())
                {
                    //_logger.LogWarning("NOT FOUND locating Remittance archive files for TransactionId [{TransactionId}]", transactionId);
                    _logger.LogWarning("NOT FOUND locating Remittance archive files");
                    return null;
                }

                var archiveFile = archiveFiles
                    .SelectMany(x => x.ArchiveDetails.Where(y => y.Description == "STP Remittance Json"))
                    .FirstOrDefault();

                if (archiveFile == null)
                {
                    //_logger.LogWarning("NOT FOUND locating Remittance JSON file for TransactionId [{TransactionId}]", transactionId);
                    _logger.LogWarning("NOT FOUND locating Remittance JSON file");
                    return null;
                }

                //_logger.LogInformation("FOUND Remittance JSON file for TransactionId [{TransactionId}]", transactionId);
                _logger.LogInformation("FOUND Remittance JSON file");
                return archiveFile;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR retrieving Remittance JSON file for TransactionId [{TransactionId}]", transactionId);
                _logger.LogError(ex, "ERROR retrieving Remittance JSON file");
                throw;
            }
        }

        /// <summary>
        /// Get the contents of the STP Remittance JSON file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The contents of the STP Remittance JSON file.</returns>
        public async Task<string> GetRemitJson(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            var archiveDetail = await GetRemitJsonDetail(transactionId, cancellationToken);
            if (archiveDetail == null)
            {
                return null;
            }

            try
            {
                using var archiveDetailStream = await _storageManager.GetArchiveDetailStream(archiveDetail.ArchiveDetailId);
                if (archiveDetailStream == null)
                {
                    return null;
                }

                var memoryStream = new MemoryStream();
                using (var fileStream = archiveDetailStream.Open())
                {
                    fileStream.CopyTo(memoryStream);
                    fileStream.Close();
                }
                memoryStream.Position = 0;

                using var reader = new StreamReader(memoryStream);
                var rawJson = await reader.ReadToEndAsync(cancellationToken);
                var json = FormatRemitJson(rawJson);
                return json;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR retrieving Remittance JSON file for TransactionId [{TransactionId}]", transactionId);
                _logger.LogError(ex, "ERROR retrieving Remittance JSON file");
                throw;
            }
        }

        /// <summary>
        /// Format the remittance JSON string by ensuring each key-value pair is on its own line.
        /// </summary>
        /// <param name="value">The raw JSON string.</param>
        /// <returns>The formatted JSON string.</returns>
        public string FormatRemitJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            string[] parts = value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int size = parts.Length;
            for (int i = 0; i < size; i++)
            {
                sb.AppendFormat("{0}", parts[i].Replace("\\\"", "\""));
                sb.AppendLine();
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Verify the existence of the SDS EDI 835 file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The contents of the SDS EDI 835 file.</returns>
        public async Task<bool> CanDownloadEdi835(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var archiveDetailStream = await _storageManager.Get835File(transactionId);
                if (archiveDetailStream == null)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR finding SDS EDI 835 file for TransactionId [{TransactionId}]", transactionId);
                _logger.LogError(ex, "ERROR finding SDS EDI 835 file");
                throw;
            }
        }

        /// <summary>
        /// Get the contents of the SDS EDI 835 file from Document System Storage for the given transaction Id.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>The contents of the SDS EDI 835 file.</returns>
        public async Task<string> DownloadEdi835(
            long transactionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var archiveDetailStream = await _storageManager.Get835File(transactionId);
                if (archiveDetailStream == null)
                {
                    return null;
                }

                var memoryStream = new MemoryStream();
                using (var fileStream = archiveDetailStream.Open())
                {
                    fileStream.CopyTo(memoryStream);
                    fileStream.Close();
                }
                memoryStream.Position = 0;

                using var reader = new StreamReader(memoryStream);
                var edi835 = await reader.ReadToEndAsync(cancellationToken);
                return edi835;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "ERROR retrieving SDS EDI 835 file for TransactionId [{TransactionId}]", transactionId);
                _logger.LogError(ex, "ERROR retrieving SDS EDI 835 file");
                throw;
            }
        }
    }
}
