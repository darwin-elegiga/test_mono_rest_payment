using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public interface IInventoryDriverRecordMessageProcessor
    {
        Task Process(InventoryDriverRecordMessage recordMessage);
    }

    public class InventoryDriverRecordMessageProcessor : IInventoryDriverRecordMessageProcessor
    {
        private readonly IArchiveService _archiveService;
        private readonly IPdfTools _pdfService;
        private readonly IFileSystem _fileSystem;
        private readonly IRedCardRepository _redCardRepository;
        private readonly IEdi835MessagePublisher _edi835MessagePublisher;
        private readonly ILogger<InventoryDriverRecordMessageProcessor> _logger;

        private static readonly string[] LimitedFileDetailTypes =
        {
            "CLEAR",
            "CHECK"
        };

        public InventoryDriverRecordMessageProcessor(
            IArchiveService archiveService,
            IPdfTools pdfService,
            IFileSystem fileSystem,
            IRedCardRepository redCardRepository,
            IEdi835MessagePublisher edi835MessagePublisher,
            ILogger<InventoryDriverRecordMessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(archiveService, nameof(archiveService));
            ArgumentHelpers.ThrowIf.IsNull(pdfService, nameof(pdfService));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(redCardRepository, nameof(redCardRepository));
            ArgumentHelpers.ThrowIf.IsNull(edi835MessagePublisher, nameof(edi835MessagePublisher));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _archiveService = archiveService;
            _pdfService = pdfService;
            _fileSystem = fileSystem;
            _redCardRepository = redCardRepository;
            _edi835MessagePublisher = edi835MessagePublisher;
            _logger = logger;
        }

        public async Task Process(InventoryDriverRecordMessage recordMessage)
        {
            using (_logger.BeginScope(new Dictionary<string, object>()
            {
                {"TPA", recordMessage.Tpa },
                {"InventoryDriverFilePath", recordMessage.InventoryDriverFilePath },
                {"DocumentType", recordMessage.DocumentType },
                {"TransactionId", recordMessage.TransactionId },
                {"Guid", recordMessage.Guid },
                {"PaymentId", recordMessage.PaymentId},
            }))
            {
                _logger.LogInformation($"Processing record: {recordMessage.Guid}");

                try
                {
                    switch (recordMessage.DocumentType)
                    {
                        case RedCardDocumentType.CO:
                            _ = await ProcessCoRecord(recordMessage);
                            break;

                        case RedCardDocumentType.EP:
                            _ = await ProcessEpRecord(recordMessage);
                            break;

                        default:
                            throw new ArgumentException($"Cannot process Document Type: {recordMessage.DocumentType}");
                    }

                    await UpdateTransactionStatus(recordMessage.TransactionId, recordMessage.DocumentType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw;
                }
            }
        }

        private async Task<IArchiveFile> ProcessEpRecord(InventoryDriverRecordMessage record)
        {
            var archiveFile = await _archiveService.GetArchiveForTransactionID(record.TransactionId);

            if (archiveFile == null)
            {
                _logger.LogAndThrowError($"Could not find archive for transaction id {record.TransactionId}");
            }

            var docFileName = record.DocumentFileNames.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(docFileName))
            {
                _logger.LogAndThrowError($"No document file name given for transaction id {record.TransactionId}");
            }

            var base64String = record.PdfFiles.FirstOrDefault().Value;
            if (base64String == null)
            {
                _logger.LogAndThrowError($"No tar file content given for transaction id {record.TransactionId}");
            }

            var plainEobBytes = Convert.FromBase64String(base64String);
            var coverFileName = await GetCoverFileName(archiveFile);

            if (archiveFile.GetArchiveDetailByName(docFileName) != null)
            {
                _logger.LogInformation($"Pdf file {docFileName} already in archive {archiveFile.FileName} for transaction id {record.TransactionId}");
            }
            else
            {
                archiveFile = await SaveFileToArchive(archiveFile, docFileName, "base_pdf document", FileDetailType.Pdf, plainEobBytes, $"Failed to write {docFileName} to archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");
            }

            archiveFile = await CreateAndSaveEobFlagFileToArchive(archiveFile, docFileName);

            if (coverFileName.Contains("CLEAR"))
            {
                var packetFileName = coverFileName.Replace("_cover", "_eob");
                var maskedPacketFileName = packetFileName.Replace("CLEAR", "MASKED");

                if (archiveFile.AnyArchiveDetailByName(packetFileName))
                {
                    _logger.LogInformation($"CLEAR packet {packetFileName} already in archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");
                }
                else
                {
                    archiveFile = await CreateAndSavePacketToArchive(archiveFile, coverFileName, plainEobBytes, packetFileName, "Clear-PAN + EOB", FileDetailType.Limited);
                }

                if (archiveFile.AnyArchiveDetailByName(maskedPacketFileName))
                {
                    _logger.LogInformation($"MASKED packet {maskedPacketFileName} already in archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");
                }
                else
                {
                    var maskedCoverFileName = coverFileName.Replace("CLEAR", "MASKED");

                    archiveFile = await CreateAndSavePacketToArchive(archiveFile, maskedCoverFileName, plainEobBytes, maskedPacketFileName, "Masked-PAN + EOB", FileDetailType.Pdf);
                }
            }
            else
            {
                var packetFileName = coverFileName.Replace("_cover.", "_cover_eob.");

                if (archiveFile.AnyArchiveDetailByName(packetFileName))
                {
                    _logger.LogInformation($"COVER packet {packetFileName} already in archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");
                }
                else
                {
                    string description;
                    FileDetailType fileDetailType;

                    if (coverFileName.Contains("EFT"))
                    {
                        description = "EFT + EOB";
                        fileDetailType = FileDetailType.Pdf;
                    }
                    else if (coverFileName.Contains("NPF"))
                    {
                        description = "NPF + EOB";
                        fileDetailType = FileDetailType.Pdf;
                    }
                    else
                    {
                        description = "Check + EOB";
                        fileDetailType = FileDetailType.Limited;
                    }

                    archiveFile = await CreateAndSavePacketToArchive(archiveFile, coverFileName, plainEobBytes, packetFileName, description, fileDetailType);
                }
            }

            return archiveFile;
        }

        private async Task<IArchiveFile> ProcessCoRecord(InventoryDriverRecordMessage message)
        {
            var archiveFile = await _archiveService.GetArchiveForTransactionID(message.TransactionId);

            if (archiveFile == null)
            {
                _logger.LogAndThrowError($"Could not find archive for transaction id {message.TransactionId}");
            }

            var record = new CoRecord(message);
            var pdfFilesFromTar = message.PdfFiles.ToDictionary(k => k.Key, v => Convert.FromBase64String(v.Value));
            var archiveDetailFileNames = archiveFile.GetArchiveDetailFileNames();

            if (record.DocumentFileNames.Except(pdfFilesFromTar.Keys).Any())
            {
                _logger.LogAndThrowError($"The following Pdfs are missing from the archive {archiveFile.FileName} with guid {archiveFile.DocumentUniqueId}: {string.Join(", ", record.DocumentFileNames.Except(archiveDetailFileNames))}");
            }

            _logger.LogInformation($"The Following {pdfFilesFromTar.Count} file(s): are being stapled together: {string.Join(", ", pdfFilesFromTar.Keys)}");

            var stapledPdf = await _pdfService.MergeAsync(pdfFilesFromTar.Values.ToArray());
            var pageCount = await _pdfService.GetNumberOfPagesAsync(stapledPdf);

            _logger.LogInformation($"{RedCardDocumentType.CO} new items created by stapling {pdfFilesFromTar.Count} documents with {pageCount} total pages.");

            var fileDetailType = archiveDetailFileNames.ContainsStrings(LimitedFileDetailTypes)
                ? FileDetailType.Limited
                : FileDetailType.Pdf;

            var metadata = record.GetRevisionMetadata(archiveDetailFileNames);

            // This should be impossible to hit, since the meta-data
            // should be created with the latest revision number
            if (record.DocumentFileNames.Contains(metadata.FileName))
            {
                _logger.LogWarning($"{metadata.FileName} Pdf already in {record.TransactionId}-{archiveFile.DocumentUniqueId}");
            }

            archiveFile = await SaveFileToArchive(archiveFile, metadata.FileName, metadata.Description, fileDetailType, stapledPdf, $"Failed to write {metadata.FileName} to archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");

            archiveDetailFileNames.Append(metadata.FileName);

            if (archiveDetailFileNames.ContainsString("CHECK"))
            {
                metadata.FileName = metadata.FileName.Replace("_stapled", "_stapled_masked_CLCHK");
                metadata.Description += " w/Masked Check";
                fileDetailType = FileDetailType.Pdf;

                var maskedPdf = await _pdfService.MicrStampAsync(stapledPdf);

                archiveFile = await SaveFileToArchive(archiveFile, metadata.FileName, metadata.Description, fileDetailType, maskedPdf, $"Failed to write {metadata.FileName} to archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");

                archiveDetailFileNames.Append(metadata.FileName);
            }

            var revisionString = metadata.Revision.PrependToRevisionString("_");

            foreach (var (pdfName, pdfBytes) in pdfFilesFromTar)
            {
                var fileName = $"{_fileSystem.Path.GetFileNameWithoutExtension(pdfName)}{revisionString}{_fileSystem.Path.GetExtension(pdfName)}";

                if (archiveDetailFileNames.ContainsString(fileName))
                {
                    _logger.LogInformation($"'{fileName}' is already in the archive {archiveFile.FileName} for guid {archiveFile.DocumentUniqueId}");
                }
                else
                {
                    archiveFile = await SaveFileToArchive(archiveFile, fileName, "original mailed pdf document", FileDetailType.Hidden, pdfBytes, $"Failed to write {fileName} to archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");
                }
            }

            return archiveFile;
        }

        /// <summary>
        /// Merge and save the <paramref name="archiveFileName"/> from <paramref name="archiveFile"/> with <paramref name="eobBytes"/> back into the same archive file
        /// </summary>
        /// <param name="archiveFile">archive to retrieve and save files</param>
        /// <param name="archiveFileName">PDF file to retrieve</param>
        /// <param name="eobBytes">EOB PDF as bytes</param>
        /// <param name="packetFileName">new packet file name</param>
        /// <param name="description">new packet description</param>
        /// <param name="fileDetailType">file detail type</param>
        /// <returns>updated <paramref name="archiveFile"/></returns>
        private async Task<IArchiveFile> CreateAndSavePacketToArchive(IArchiveFile archiveFile, string archiveFileName, byte[] eobBytes, string packetFileName, string description, FileDetailType fileDetailType)
        {
            var archiveDetail = await _archiveService.GetArchiveDetailForTransactionIDAndFileName(archiveFile.TransactionId, archiveFileName);

            var newPdf = await _pdfService.MergeAsync(archiveDetail.Data, eobBytes);

            return await SaveFileToArchive(archiveFile, packetFileName, description, fileDetailType, newPdf,
                $"Failed to add {GetPayTypeFromDescription(description)} packet {packetFileName} to archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");
        }

        /// <summary>
        /// Add has_plain_eob flag to archive file if one does not exist
        /// </summary>
        /// <param name="archiveFile">archive file</param>
        /// <param name="documentFileName">file name contained in flag</param>
        /// <returns></returns>
        private async Task<IArchiveFile> CreateAndSaveEobFlagFileToArchive(IArchiveFile archiveFile, string documentFileName)
        {
            var documentType = VPaygeDocumentType.Has_Plain_Eob;
            var fileName = documentType.GetAttribute<DocFileExtensionAttribute>().FileExtension;

            var archiveDetail = archiveFile.GetArchiveDetailByName(fileName);

            if (archiveDetail != null)
            {
                _logger.LogInformation($"'{fileName}' flag already exists in archive with {archiveFile.DocumentUniqueId}");
            }
            else
            {
                var bytes = $"{documentFileName}"
                    .ToCharArray()
                    .Select(Convert.ToByte)
                    .ToArray();

                archiveFile = await SaveFileToArchive(archiveFile, fileName, documentType.GetDescription(), FileDetailType.Hidden, bytes, $"Could not create flag {fileName} to archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}", 1);
            }

            return archiveFile;
        }

        /// <summary>
        /// Return cover file name from archive file
        /// using the has_cover flag
        /// </summary>
        /// <param name="archiveFile">archive file</param>
        /// <returns></returns>
        private async Task<string> GetCoverFileName(IArchiveFile archiveFile)
        {
            var fileName = VPaygeDocumentType.Has_Cover.GetAttribute<DocFileExtensionAttribute>().FileExtension;
            var archiveDetail = archiveFile.GetArchiveDetailByName(fileName);

            if (archiveDetail == null)
            {
                _logger.LogAndThrowError($"'{fileName}' flag missing from archive {archiveFile.FileName} with {archiveFile.DocumentUniqueId}");
            }

            using (var archiveDetailStream = await _archiveService.GetArchiveDetailStream(archiveDetail.ArchiveDetailId))
            using (var stream = archiveDetailStream.Open())
            using (var streamReader = new StreamReader(stream))
            {
                var coverFileName = streamReader.ReadToEnd();

                return _fileSystem.Path.GetFileName(coverFileName);
            }
        }

        /// <summary>
        /// Save a file with the given meta-data into the archive file
        /// </summary>
        /// <param name="archiveFile">archive file containing saved file</param>
        /// <param name="fileName">file name</param>
        /// <param name="description">file description</param>
        /// <param name="fileDetailType"><see cref="FileDetailType"/></param>
        /// <param name="bytes">file content</param>
        /// <param name="errorMessage">error message to log if save failed</param>
        /// <param name="pageCount">file page count, if not supplied, assume pdf file and call method</param>
        /// <returns>archive file containing saved file</returns>
        private async Task<IArchiveFile> SaveFileToArchive(IArchiveFile archiveFile, string fileName, string description, FileDetailType fileDetailType, byte[] bytes, string errorMessage, int? pageCount = null)
        {
            IArchiveFile _ = null;

            var pages = pageCount ?? await _pdfService.GetNumberOfPagesAsync(bytes);

            try
            {
                _ = await _archiveService.SaveFile(archiveFile.ArchiveFileId, fileName, description, fileDetailType, bytes, pages, archiveFile.TransactionId.ToString());

                _logger.LogInformation($"Add {description} packet {fileName} in archive {archiveFile.FileName} from guid {archiveFile.DocumentUniqueId}");
            }
            catch (Exception ex)
            {
                _logger.LogAndThrowError(errorMessage, ex: ex);
            }

            return _;
        }

        private string GetPayTypeFromDescription(string description)
        {
            return description
                .Split(new[] { '-', ' ', '+' }, StringSplitOptions.RemoveEmptyEntries)
                .First()
                .Trim()
                .ToUpper();
        }

        private async Task UpdateTransactionStatus(long transactionId, RedCardDocumentType documentType)
        {
            // all documents are stapled
            await _redCardRepository.UpdateTransactionStatus(transactionId, RedCardTransactionStatusCode.DocumentsStapled);

            if (documentType == RedCardDocumentType.CO)
            {
                await _redCardRepository.UpdateTransactionStatus(transactionId, RedCardTransactionStatusCode.TransactionComplete);
                await _edi835MessagePublisher.PublishEdi835Message(transactionId, CancellationToken.None);
            }
            else
            {
                await _redCardRepository.UpdateTransactionStatus(transactionId, RedCardTransactionStatusCode.ReadyForDistribution);
            }
        }
    }
}
