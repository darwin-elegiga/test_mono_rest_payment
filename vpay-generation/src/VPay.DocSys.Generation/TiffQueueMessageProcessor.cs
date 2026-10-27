using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;
using VPay.Queuing;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class TiffQueueMessageProcessor : ITiffQueueMessageProcessor
    {
        public static IList<FaxReserveName> CreateNewArchiveTypes => new[] { FaxReserveName.MEDICAL, FaxReserveName.CSS };

        private readonly QueueConfigurationSettings _faxQueueConfigurationSettings;
        private readonly IFileSystem _fileSystem;
        private readonly ITiffOperations _tiffOperations;
        private readonly IStorageManager _storageManager;
        private readonly IQueueService _queueService;
        private readonly string _faxBasePath;
        private readonly FaxJobSettings _faxJobSettings;
        private readonly ILogger _logger;

        public TiffQueueMessageProcessor(
            QueueConfigurations queueConfigurations,
            IFileSystem fileSystem,
            ITiffOperations tiffOperations,
            IStorageManager storageManager,
            IQueueService queueService,
            FaxJobSettings faxJobSettings,
            FaxBase faxBase,
            ILogger<TiffQueueMessageProcessor> logger)
        {
            _faxQueueConfigurationSettings = queueConfigurations.FaxJobDataQueue ?? throw new ArgumentNullException(nameof(queueConfigurations.FaxJobDataQueue));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _tiffOperations = tiffOperations ?? throw new ArgumentNullException(nameof(tiffOperations));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            _faxJobSettings = faxJobSettings ?? throw new ArgumentNullException(nameof(faxJobSettings));
            _faxBasePath = faxBase.Path ?? throw new ArgumentNullException(nameof(faxBase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessMessageString(string message)
        {
            var tiffMessage = JsonConvert.DeserializeObject<TiffQueueMessage>(message);
            var logDictionary = tiffMessage.ToDictionary();
            logDictionary[nameof(tiffMessage.TransactionIds)] = string.Join(", ", tiffMessage.TransactionIds);

            using (_logger.BeginScope(logDictionary))
            {
                try
                {
                    ValidateMessage(tiffMessage);

                    var tempDirectory = CreateTemporaryDirectory();

                    var (pdfFilePath, pdfPageCount) = await ExtractPdfFromArchive(tiffMessage, tempDirectory);

                    var tiffFilePath = await _tiffOperations.CreateTiffFromPdf(pdfFilePath);

                    PublishFaxJob(tiffMessage, tiffFilePath, pdfPageCount);

                    await ArchiveFaxFiles(tiffMessage, tiffFilePath, pdfFilePath, pdfPageCount);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    throw;
                }
            }
        }

        private void ValidateMessage(TiffQueueMessage tiffMessage)
        {
            ThrowIf.IsNullOrEmpty(tiffMessage.TransactionIds, nameof(tiffMessage.TransactionIds));

            switch (tiffMessage.ReserveName)
            {
                case FaxReserveName.WARRANTY:
                case FaxReserveName.RFAX:
                case FaxReserveName.VPFEE:
                    ThrowIf.IsNull(tiffMessage.ArchiveFileId, nameof(tiffMessage.ArchiveFileId));
                    ThrowIf.IsNullOrWhiteSpace(tiffMessage.PdfFileName, nameof(tiffMessage.PdfFileName));
                    break;

                case FaxReserveName.CSS:
                    if (!tiffMessage.ArchiveDetailId.HasValue &&
                        string.IsNullOrWhiteSpace(tiffMessage.PdfFileName))
                    {
                        throw new ArgumentException($"{tiffMessage.ReserveName} must contain either a {nameof(tiffMessage.ArchiveDetailId)} or {nameof(tiffMessage.PdfFileName)}");
                    }
                    break;

                case FaxReserveName.BFAX:
                    ThrowIf.IsNull(tiffMessage.ArchiveFileId, nameof(tiffMessage.ArchiveFileId));
                    ThrowIf.IsNull(tiffMessage.ArchiveDetailId, nameof(tiffMessage.ArchiveDetailId));
                    break;

                case FaxReserveName.MEDICAL:
                    break;

                default:
                    throw new ArgumentException(
                        $"Cannot process {nameof(TiffQueueMessage)} with {tiffMessage.ReserveName}",
                        nameof(tiffMessage.ReserveName));
            }
        }

        private string CreateTemporaryDirectory()
        {
            var tempDirectory = $"/tmp/{Guid.NewGuid()}";

            _fileSystem.Directory.CreateDirectory(tempDirectory);
            _logger.LogDebug("Create {Directory}", tempDirectory);

            return tempDirectory;
        }

        private async Task<(string, int)> ExtractPdfFromArchive(TiffQueueMessage tiffMessage, string tempDirectory)
        {
            IArchiveDetail archive;

            switch (tiffMessage.ReserveName)
            {
                // provides both the filename and archive file id
                /// <see cref="FaxMessageQueueMessageProcessor"/>
                case FaxReserveName.WARRANTY:
                /// <see cref="ReminderFaxMessageProcessor"/>
                case FaxReserveName.RFAX:
                case FaxReserveName.VPFEE:
                    archive = await GetFile(tiffMessage.ArchiveFileId, tiffMessage.PdfFileName);
                    break;

                // can come from red screen or vpay 360
                case FaxReserveName.CSS:
                    archive = tiffMessage.ArchiveDetailId.HasValue
                        ? await _storageManager.GetArchiveDetailStream(tiffMessage.ArchiveDetailId.Value) // vpay 360
                        : await GetFileStream(tiffMessage.TransactionIds[0], tiffMessage.PdfFileName); // red screen
                    break;

                // from vpay 360
                /// <see cref="BundledFaxService"/>
                case FaxReserveName.BFAX:
                    archive = await _storageManager.GetArchiveDetailStream(tiffMessage.ArchiveDetailId.Value);
                    break;

                // from distribution
                case FaxReserveName.MEDICAL:
                    archive = await _storageManager.GetDistributionPacketStream(tiffMessage.TransactionIds[0], tiffMessage.PayType, false);
                    break;

                default:
                    throw new ArgumentException($"Cannot handle {tiffMessage.ReserveName}", nameof(tiffMessage.ReserveName));
            }

            var pdfFilePath = WriteFile(archive, tempDirectory);

            return (pdfFilePath, archive.PageCount);
        }

        private async Task<IArchiveDetailStream> GetFileStream(long transactionId, string fileName)
        {
            var archiveDetailId = (await _storageManager.GetArchiveForTransactionId(transactionId, fileName))
                .ArchiveDetails
                .First()
                .ArchiveDetailId;

            return await _storageManager.GetArchiveDetailStream(archiveDetailId);
        }

        private async Task<IArchiveDetail> GetFile(long? archiveFileId, string fileName) =>
            (await _storageManager.GetArchiveDetailStreams(archiveFileId.Value, new MatchingCriteria(fileName))).ArchiveDetails.First();

        private string WriteFile(IArchiveDetail archiveDetail, string tempDirectory)
        {
            var pdfFilePath = _fileSystem.Path.Combine(tempDirectory, archiveDetail.FileName);

            if (archiveDetail is IArchiveDetailStream ads)
            {

                using (ads)
                using (var outputStream = _fileSystem.File.Create(pdfFilePath))
                using (var inputStream = ads.Open())
                {
                    inputStream.CopyTo(outputStream);
                }

                //using (ads)
                //using (var output = _fileSystem.FileStream.Create(pdfFilePath, FileMode.CreateNew))
                //using (var stream = ads.Open())
                //{
                //    stream.CopyTo(output);
                //}
            }
            else
            {
                _fileSystem.File.WriteAllBytes(pdfFilePath, archiveDetail.Data);
            }

            _logger.LogInformation("Extracted {FileName} to {FaxDocumentPath}", archiveDetail.FileName, pdfFilePath);

            return pdfFilePath;
        }

        private void PublishFaxJob(TiffQueueMessage tiffMessage, string tiffFilePath, int pageCount)
        {
            var faxFilePath = GetFaxFilePath(_fileSystem.Path.GetFileName(tiffFilePath));

            _fileSystem.File.Copy(tiffFilePath, faxFilePath);
            ulong size = (ulong)_fileSystem.FileInfo.New(faxFilePath).Length;

            _logger.LogInformation("Copied tiff file to: {FaxFilePath}", faxFilePath);

            var payload = new FaxJobRequest(tiffMessage, pageCount, size, _faxJobSettings)
            {
                DocumentPath = faxFilePath,
            };

            var message = new FaxJobQueueMessage(payload);
            var rmqPriority = payload.Priority.ConvertAndScalePriority(100, _faxQueueConfigurationSettings.MaxPriority);

            using (_logger.BeginScope(payload.ToDictionary()))
            {
                _queueService.PublishQueueMessage(_faxQueueConfigurationSettings, JsonConvert.SerializeObject(message), rmqPriority);
            }
        }

        private async Task ArchiveFaxFiles(TiffQueueMessage tiffMessage, string tiffFilePath, string pdfFilePath, int? pdfPageCount)
        {
            /* NOTE: medical and css faxes will 
             * create a new FaxArchive to add the pdf Fax
             * Document this is to maintain visual consistency
             * with VPay360 in displaying fax documents
             */
            if (CreateNewArchiveTypes.Contains(tiffMessage.ReserveName))
            {
                if (pdfFilePath == null) throw new ArgumentNullException(nameof(pdfFilePath));
                if (pdfPageCount == null) throw new ArgumentNullException(nameof(pdfPageCount));

                tiffMessage.ArchiveFileId = await CreateFaxArchive(tiffMessage, pdfFilePath, pdfPageCount.Value);
            }

            // bundled fax uses 0 for the combined fax file
            var transactionIdString = (tiffMessage.ReserveName == FaxReserveName.BFAX
                ? 0
                : tiffMessage.TransactionIds.First())
                .ToString("D20");

            // always save tiff to archive
            var gf = new GeneratedFile
            {
                Description = "Fax Tiff",
                FileDetailType = FileDetailType.Hidden,
                TransactionId = transactionIdString,
                FileName = tiffFilePath,
                PageCount = 0,
            };

            await _storageManager.AppendArchive(tiffMessage.ArchiveFileId.Value, new List<IGeneratedFile> { gf }, true);

            var path = _fileSystem.Path.GetDirectoryName(tiffFilePath);
            _fileSystem.Directory.Delete(path, true);
            _logger.LogDebug("Delete {Directory}", path);
        }

        private string GetFaxFilePath(string faxFileName)
        {
            var now = DateTime.Now;

            var faxDailyDirectory = _fileSystem.Path.Combine(
                _faxBasePath,
                now.Year.ToString("D4"),
                now.Month.ToString("D2"),
                now.Day.ToString("D2"));

            if (!_fileSystem.Directory.Exists(faxDailyDirectory))
            {
                _fileSystem.Directory.CreateDirectory(faxDailyDirectory);
                _logger.LogInformation("Created {Directory}", faxDailyDirectory);
            }

            return _fileSystem.Path.Combine(
                faxDailyDirectory,
                _fileSystem.Path.ChangeExtension(faxFileName, $"{Guid.NewGuid()}.tiff"));
        }

        private async Task<long> CreateFaxArchive(TiffQueueMessage tiffMessage, string pdfFilePath, int pdfPageCount)
        {
            return await _storageManager.CreateArchive(
                new FaxArchiveFile(
                    tiffMessage.Tpa,
                    tiffMessage.TransactionIds[0],
                    tiffMessage.CorrespondenceId.ToString("D20")),
                new List<IGeneratedFile>
                {
                    new GeneratedFile
                    {
                        Description = "Fax",
                        FileDetailType = FileDetailType.FaxDocument,
                        FileName = pdfFilePath,
                        PageCount = pdfPageCount,
                        TransactionId = tiffMessage.TransactionIds[0].ToString("D20"),
                    },
                },
                false);
        }
    }
}
