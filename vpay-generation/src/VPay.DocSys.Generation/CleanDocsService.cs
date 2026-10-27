using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public class CleanDocsService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICleanDocsOperations _cleandocsOps;
        private readonly IStorageManager _storageManager;
        private readonly ILogger _logger;

        public CleanDocsService(IFileSystem fileSystem, IStorageManager storageManager, ICleanDocsOperations cleanDocsOperations, ILogger<CleanDocsService> logger)
        {
            _fileSystem = fileSystem;
            _storageManager = storageManager;
            _cleandocsOps = cleanDocsOperations;
            _logger = logger;
        }

        /// <summary>
        /// Run cleandocs for all transactions in status file
        /// </summary>
        /// <param name="statusFilePath">status file with transaction information records</param>
        /// <param name="confirmDeletes">if false, will return the expected counts without performing clean operations</param>
        /// <param name="recordCount">expected count of files to clean. must match actual number of operations</param>
        /// <returns>counts of each operation</returns>
        /// <exception cref="ArgumentException">when a drop to print file is passed</exception>
        public async Task<CleanDocsServiceResponse> CleanDocs(string statusFilePath, bool confirmDeletes, int recordCount)
        {
            var request = ValidateServiceRequest(statusFilePath);
            var response = new CleanDocsServiceResponse();
            if (confirmDeletes && recordCount == 0)
            {
                response.Message = "Can't run delete if expected count is not set";
            }
            CleanParams cleanParams = new CleanParams()
            {
                MasterArchiveSearch = request.MasterArchiveSearch,
                Guids = request.StatusFile.Records.Select(r => r.GUID).ToList(),
                TransactionIds = request.StatusFile.Records.Select(r => Convert.ToInt64(r.TXID.RemoveLeadingZeroes())).ToList()
            };

            var targets = await _storageManager.GetArchivesToDelete(cleanParams, request.IsReissue);
            response.ExpectedArchiveCount = targets.Count;
            response.ExpectedDetailCount = targets.Sum(t => t.detailCount);
            if (!confirmDeletes)
            {
                return response;
            }
            else
            {
                if (recordCount != response.ExpectedArchiveCount)
                {
                    response.Message = "Counts do not match. Aborting.";
                    return response;
                }

                var reissueResult = await CleanReissueReferencesFromOriginalArchives(
                    targets.Select(x => x.TransactionId), false);
                response.DeletedReissueTransactionCount = reissueResult.DeletedReissueTransactionCount;

                response.DeletedFileCount = await DeleteArchiveFiles(
                    targets.Select(
                        t => _fileSystem.Path.Combine(t.archive_path, t.archive_name)).ToList());

                var dbDeletes = await DeleteDatabaseRecords(targets.Select(t => t.archive_file_id).ToList());
                response.DeletedArchiveCount = dbDeletes["archives"];
                response.DeletedDetailCount = dbDeletes["details"];
            }
            return response;
        }

        /// <summary>
        /// Runs cleandocs for one transaction
        /// </summary>
        /// <param name="statusFilePath">status file with transaction information records</param>
        /// <param name="transactionId">transactionid to clean</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<CleanDocsServiceResponse> CleanSingleTransaction(string statusFilePath, long transactionId)
        {
            var request = ValidateServiceRequest(statusFilePath);
            var response = new CleanDocsServiceResponse();
            CleanParams cleanParams = new CleanParams()
            {
                TransactionIds = new List<long>() { transactionId },
                Guids = request.StatusFile.Records.Where(r => r.TXID.RemoveLeadingZeroes() == transactionId.ToString()).Select(r => r.GUID).ToList()
            };

            //One archive per transaction plus the master archive
            var potentialTargets = await _storageManager.GetArchivesToDelete(cleanParams, request.IsReissue);
            var targets = potentialTargets.Where(t => t.TransactionId == transactionId).ToList();
            response.ExpectedArchiveCount = targets.Count;
            response.ExpectedDetailCount = targets.Sum(t => t.detailCount);

            response.DeletedReissueTransactionCount = (await CleanReissueReferencesFromOriginalArchives(
                targets.Select(x => x.TransactionId), false)).DeletedReissueTransactionCount;
            response.DeletedFileCount = await DeleteArchiveFiles(targets.Select(t => _fileSystem.Path.Combine(t.archive_path, t.archive_name)).ToList());
            var dbDeletes = await DeleteDatabaseRecords(targets.Select(t => t.archive_file_id).ToList());
            response.DeletedArchiveCount = dbDeletes["archives"];
            response.DeletedDetailCount = dbDeletes["details"];

            return response;
        }

        /// <summary>
        /// Inserts cleandocs request for transactions
        /// </summary>
        /// <param name="statusFilePath">status file with transaction information records</param>
        /// <param name="csvTransactionIds">comma-delimited transactionids to clean</param>
        /// <returns><see cref="ICleanDocsOperations.CleanDocsRequestInsert"/></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<IList<CleanDocsRequestResult>> CleanDocsRequestInsert(string statusFilePath, string csvTransactionIds)
        {
            var request = ValidateServiceRequest(statusFilePath);
            // If we passed an empty string for csvTransactionIds, take all the transactions,
            // otherwise, only try to get those transactions passed.
            List<long> transactions = string.IsNullOrEmpty(csvTransactionIds)
                ? request.StatusFile.Records.Select(r => Convert.ToInt64(r.TXID.RemoveLeadingZeroes())).ToList()
                : csvTransactionIds.Split(',').Select(r => Convert.ToInt64(r)).ToList();

            CleanParams cleanParams = new CleanParams()
            {
                MasterArchiveSearch = request.MasterArchiveSearch,
                Guids = request.StatusFile.Records.Select(r => r.GUID).ToList(),
                TransactionIds = transactions
            };

            var targets = await _storageManager.GetArchivesToDelete(cleanParams, request.IsReissue);
            var requestResults = await _cleandocsOps.CleanDocsRequestInsert(statusFilePath, targets.Select(_ => _.archive_file_id).ToList());

            return requestResults;
        }

        /// <summary>
        /// Wrapper for <see cref="ICleanDocsOperations.CleanDocsRequestGet(int)"/>
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public Task<IList<CleanDocsRequestResult>> CleanDocsRequestGet(int requestId)
        {
            return _cleandocsOps.CleanDocsRequestGet(requestId);
        }

        /// <summary>
        /// Performs delete operation for existing clean_docs_request
        /// </summary>
        /// <param name="requestedDeletions"></param>
        /// <param name="deleteDistribution"></param>
        /// <param name="removeAuxFile"></param>
        /// <param name="removeClaimCheckFiles"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<CleanDocsServiceResponse> CleanDocsRequestDelete(IList<CleanDocsRequestResult> requestedDeletions, bool deleteDistribution, bool removeAuxFile, bool removeClaimCheckFiles)
        {
            var firstRecord = requestedDeletions?.FirstOrDefault();
            if (firstRecord == null)
            {
                throw new ApplicationException(
                    "requestedDeletions object cannot be null, have 0 records, or have a requestId < 0");
            }

            int requestId = firstRecord.RequestId;
            string statusFileFullPath = firstRecord.StatusFilePath;
            string statusFilePath = _fileSystem.Path.GetDirectoryName(statusFileFullPath);
            if (!(statusFilePath?.EndsWith(_fileSystem.Path.DirectorySeparatorChar.ToString()) ?? true))
            {
                statusFilePath += _fileSystem.Path.DirectorySeparatorChar;
            }
            string statusFileName = _fileSystem.Path.GetFileName(statusFileFullPath);
            if (requestId < 1)
            {
                throw new ApplicationException(
                    "requestedDeletions object cannot have a requestId < 0");
            }

            var response = new CleanDocsServiceResponse();

            // get transaction ids before document_system_archive_file records are deleted
            var transactionIds = await GetTransactionIds(requestedDeletions);

            // Clean the SqlServer tables
            var smResult = await _storageManager.CleanPaymentStatusFile(statusFilePath, statusFileName, deleteDistribution);
            if (smResult.Success)
            {
                response.DeletedDocumentTransactionRows = smResult.DocumentTransactionRowsDeleted;
                response.DeletedDocumentDispositonRows = smResult.DocumentDispositionRowsDeleted;
            }

            var rrResult = await CleanReissueReferencesFromOriginalArchives(
                transactionIds, false);
            response.DeletedReissueTransactionCount = rrResult.DeletedReissueTransactionCount;

            // Clean the legacy document_ssystem tables
            var dbDeletes = await _cleandocsOps.CleanDocsRequestDelete(requestId);
            response.DeletedArchiveCount = dbDeletes["archives"];
            response.DeletedDetailCount = dbDeletes["details"];

            // Delete the files from the file system.
            response.DeletedFileCount = await DeleteArchiveFiles(requestedDeletions
                .Where(_ => _.ArchiveFileId > 0)
                .Select(_ => _.PathToDelete).ToList());

            if (removeAuxFile)
            {
                RemoveAuxiliaryFile(statusFileFullPath);
            }

            if (removeClaimCheckFiles)
            {
                RemoveClaimCheckFiles(statusFileFullPath, transactionIds);
            }

            return response;
        }

        /// <summary>
        /// Wrapper for <see cref="IStorageManager.GetArchiveDetailsForReissuedToFiles"/> \
        /// and <see cref="IStorageManager.DeleteArchiveDetailFromArchiveFile"/>
        /// and <see cref="IStorageManager.DeleteReissueDataByReissue"/>
        /// </summary>
        /// <param name="reissuedTransactionIds">
        /// List of <see cref="ReissueTransaction.ReissueTransactionId"/> values
        /// </param>
        /// <param name="cleanOrphanedReissueReferences">
        /// Whether to delete reissue_to detail records and zip entries with no matching reissue_from detail record.
        /// Set to true if reissue records have already been deleted.
        /// </param>
        /// <remarks>if reissuedTransactionIds do not correspond to ReissueTransaction records, nothing will happen</remarks>
        /// <returns>ExpectedDetailCount, DeletedDetailCount, DeletedFileCount, DeletedReissueTransactionCount</returns>
        public async Task<CleanDocsServiceResponse> CleanReissueReferencesFromOriginalArchives(
            IEnumerable<long> reissuedTransactionIds, bool cleanOrphanedReissueReferences)
        {
            var response = new CleanDocsServiceResponse();

            var reissuedTransactionIdList = reissuedTransactionIds.ToList();

            var reissuedToTxtDetails = await _storageManager.GetArchiveDetailsForReissuedToFiles(
                reissuedTransactionIdList, cleanOrphanedReissueReferences);

            response.ExpectedDetailCount = reissuedToTxtDetails.Count();

            foreach (var txtDetail in reissuedToTxtDetails)
            {
                var deleted = await _storageManager.DeleteArchiveDetailFromArchiveFile(txtDetail);

                if (deleted)
                {
                    response.DeletedDetailCount++;
                    response.DeletedFileCount++;
                }
            }

            foreach (var reissuedTransactionId in reissuedTransactionIdList)
            {
                var deleted = await _storageManager.DeleteReissueDataByReissue(reissuedTransactionId);

                if (deleted > 0)
                {
                    response.DeletedReissueTransactionCount++;
                }
            }

            return response;
        }

        /// <summary>
        /// Wrapper for <see cref="IStorageManager.ExpungeArchive(Guid, long)"/>
        /// </summary>
        /// <remarks>Logs any exception as a warning</remarks>
        /// <param name="transactionGuid">must not equal <see cref="Guid.Empty"/></param>
        /// <param name="archiveId">must exceed 0</param>
        public async Task<bool> ExpungeArchive(Guid transactionGuid, long archiveId)
        {
            var result = false;

            try
            {
                if (transactionGuid != Guid.Empty && archiveId > 0)
                {
                    result = await _storageManager.ExpungeArchive(transactionGuid, archiveId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to expunge archiveId: {archiveId} Guid: {transactionGuid}");
            }

            return result;
        }

        /// <summary>
        /// Sets <see cref="CleanDocsServiceRequest.FileName"/> via <see cref="IPath.GetFileNameWithoutExtension(string)"/>
        /// Throws <see cref="ArgumentException"/> if file prefix is PF
        /// Sets <see cref="CleanDocsServiceRequest.IsReissue"/> if file prefix is R
        /// Sets <see cref="CleanDocsServiceRequest.StatusFile"/> via <see cref="IFile.ReadAllLines(string)"/> and constructor
        /// Sets <see cref="CleanDocsServiceRequest.MasterArchiveSearch"/> via <see cref="CleanDocsServiceRequest.FileName"/> for reissues,
        ///   otherwise via <see cref="StatusFile"/>.<see cref="StatusHeader.InputFile"/>
        /// </summary>
        /// <param name="statusFilePath">.sts file to parse</param>
        /// <returns>status file is for a reissue</returns>
        /// <exception cref="ArgumentException"></exception>
        private CleanDocsServiceRequest ValidateServiceRequest(string statusFilePath)
        {
            var input = new CleanDocsServiceRequest
            {
                FileName = _fileSystem.Path.GetFileNameWithoutExtension(statusFilePath)
            };

            if (input.FileName.StartsWith("PF"))
            {
                throw new ArgumentException("Drop to print can not be cleaned", nameof(statusFilePath));
            }
            else if (input.FileName.StartsWith("R"))
            {
                input.IsReissue = true;
            }

            input.StatusFile = new StatusFile(_fileSystem.File.ReadAllLines(statusFilePath));

            if (input.IsReissue)
            {
                input.MasterArchiveSearch = "MASTER-REISSUE." + input.FileName.ToUpper();
            }
            else
            {
                input.MasterArchiveSearch = "MASTER-ARCHIVE.%." + input.StatusFile.Header.InputFile;
            }

            return input;
        }

        /// <summary>
        /// Deletes files with <see cref="System.IO.File"/>
        /// </summary>
        /// <param name="filePaths">absolute paths to delete</param>
        /// <returns>count of successfully deleted files</returns>
        private Task<int> DeleteArchiveFiles(List<string> filePaths)
        {
            int deleted = 0;
            foreach (string path in filePaths
                // avoid DirectoryNotFoundException from Delete
                .Where(_fileSystem.File.Exists))
            {
                _fileSystem.File.Delete(path);

                if (!_fileSystem.File.Exists(path))
                {
                    deleted++;
                }
            }

            return Task.FromResult(deleted);
        }

        /// <summary>
        /// wrapper for <see cref="ICleanDocsOperations.DeleteDatabaseRecords(List{long})"/>
        /// </summary>
        /// <param name="archiveIds">archive_file_id</param>
        /// <returns>
        /// "archives" key for deleted document_system_archive_file records
        /// "details" key for deleted document_system_archive_details records
        /// </returns>
        private Task<Dictionary<string, int>> DeleteDatabaseRecords(List<long> archiveIds)
        {
            return _cleandocsOps.DeleteDatabaseRecords(archiveIds);
        }

        /// <summary>
        /// Deletes file from extension <see cref="StatusFileHelper.GetAuxiliaryFilePathFromStatusFilePath(string)"/>
        /// </summary>
        /// <param name="statusFilePath">status file with transaction information records</param>
        private void RemoveAuxiliaryFile(string statusFileFullPath)
        {
            string auxFilePath = statusFileFullPath.GetAuxiliaryFilePathFromStatusFilePath();

            if (_fileSystem.File.Exists(auxFilePath))
            {
                _fileSystem.File.Delete(auxFilePath);
            }
        }

        private void RemoveClaimCheckFiles(string statusFilePath, HashSet<long> transactionIds)
        {
            // Remove the claim check files if they exist.
            var directoryPath = _fileSystem.Path.GetDirectoryName(statusFilePath);
            var messageDirectory = _fileSystem.Path.Combine(directoryPath, "messages", _fileSystem.Path.GetFileNameWithoutExtension(statusFilePath));
            _logger.LogInformation("Removing claim check files from {MessageDirectory}", messageDirectory);
            if (_fileSystem.Directory.Exists(messageDirectory))
            {
                foreach (var transactionId in transactionIds)
                {
                    var fileNamePattern = $"DocumentTransaction_{transactionId}*";
                    _logger.LogInformation("Removing claim check files with pattern {FileNamePattern}", fileNamePattern);
                    foreach (var file in _fileSystem.Directory.GetFiles(messageDirectory, fileNamePattern))
                    {
                        _fileSystem.File.Delete(file);
                    }
                }
            }
        }

        /// <summary>
        /// Gets transactionIds from archiveFileIds via
        /// <see cref="IStorageManager.GetArchiveForArchiveFileId(long)"/>
        /// </summary>
        /// <param name="requestResults">list of ArchiveFileIds</param>
        /// <returns>list of TransactionIds associated with ArchiveFile</returns>
        private async Task<HashSet<long>> GetTransactionIds(IList<CleanDocsRequestResult> requestResults)
        {
            var transactionIds = new HashSet<long>();

            foreach (var archiveFileId in requestResults.Select(r => r.ArchiveFileId).Where(i => i > 0))
            {
                try
                {
                    var archive = await _storageManager.GetArchiveForArchiveFileId(archiveFileId, includeDetails: false);

                    if (archive == null)
                    {
                        continue;
                    }

                    transactionIds.Add(archive.TransactionId);
                }
                catch (ArchiveFileNotFoundException)
                {
                    // intentionally ignored, thrown when archive is null
                }
            }

            return transactionIds;
        }
    }
}
