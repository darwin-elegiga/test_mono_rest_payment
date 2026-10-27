using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Storage;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class FaxArchiveMessageProcessor : IFaxArchiveMessageProcessor
    {
        private readonly IFileSystem _fileSystem;
        private readonly IStorageManager _storageManager;
        private readonly ILogger<FaxArchiveMessageProcessor> _logger;

        public FaxArchiveMessageProcessor(
            IFileSystem fileSystem,
            IStorageManager storageManager,
            ILogger<FaxArchiveMessageProcessor> logger)
        {
            ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ThrowIf.IsNull(storageManager, nameof(storageManager));
            ThrowIf.IsNull(logger, nameof(logger));

            _fileSystem = fileSystem;
            _storageManager = storageManager;
            _logger = logger;
        }

        public async Task ProcessMessage(FaxRetryArchiveMessage faxRetryArchiveMessage)
        {
            ThrowIf.IsNull(faxRetryArchiveMessage, nameof(faxRetryArchiveMessage));

            var logScopeDictionary = faxRetryArchiveMessage.ToDictionaryExcept(nameof(FaxRetryArchiveMessage.TransactionIds));
            logScopeDictionary.Add("TransactionIds", string.Join(", ", faxRetryArchiveMessage.TransactionIds));

            using (_logger.BeginScope(logScopeDictionary))
            {
                if (!faxRetryArchiveMessage.TransactionIds.Any())
                {
                    _logger.LogInformation("No transactions to process for CorrespondenceId:{CorrespondenceId} fax", faxRetryArchiveMessage.CorrespondenceId);

                    return;
                }

                var tempDirectory = CreateTemporaryDirectory();

                foreach (var tid in faxRetryArchiveMessage.TransactionIds)
                {
                    using (_logger.BeginScope(new { TransactionId = tid }))
                    {
                        try
                        {
                            var filePath = WriteFile(faxRetryArchiveMessage, tid, tempDirectory);

                            var retryArchive = new FaxArchiveFile(
                                faxRetryArchiveMessage.TpaCode,
                                tid,
                                faxRetryArchiveMessage.CorrespondenceId.ToString("D20"));

                            var files = new List<IGeneratedFile>
                            {
                                new GeneratedFile
                                {
                                    Description = faxRetryArchiveMessage.Description,
                                    FileDetailType = FileDetailType.Text,
                                    FileName = filePath,
                                    PageCount = 1,
                                    TransactionId = tid.ToString("D20"),
                                },
                            };

                            _ = await _storageManager.CreateArchive(retryArchive, files, false, faxRetryArchiveMessage.UpdateDateTime);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, e.Message);

                            throw;
                        }
                    }
                }

                _fileSystem.Directory.Delete(tempDirectory, true);
            }
        }

        private string CreateTemporaryDirectory()
        {
            var tempDirectory = $"/tmp/{Guid.NewGuid()}";

            _fileSystem.Directory.CreateDirectory(tempDirectory);
            _logger.LogDebug("Create {Directory}", tempDirectory);

            return tempDirectory;
        }

        private string WriteFile(FaxRetryArchiveMessage faxRetryArchiveMessage, long transactionId, string tempDirectory)
        {
            var fileName = faxRetryArchiveMessage.GetRetryFileName(_fileSystem);
            var filePath = _fileSystem.Path.Combine(tempDirectory, fileName);

            _fileSystem.File.WriteAllText(filePath, faxRetryArchiveMessage.GetFileContents(transactionId));

            _logger.LogInformation("Created {FileName} to {FaxRetryDocumentPath}", fileName, filePath);

            return filePath;
        }
    }
}
