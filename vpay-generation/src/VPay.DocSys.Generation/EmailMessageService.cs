using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public class EmailMessageService : IEmailMessageService
    {
        private IEmailMessageXmlReader _emailMessageXmlReader;
        private readonly IWarrantyPdfCreator _warrantyPdfCreator;
        private readonly IFileSystem _fileSystem;
        private readonly IStorageManager _storageManager;
        private readonly IPdfTools _pdfTools;
        private readonly DocSysSettings _docSysSettings;
        private readonly ILogger<EmailMessageService> _logger;

        public EmailMessageService(IEmailMessageXmlReader emailMessageXmlReader,
            IWarrantyPdfCreator warrantyPdfCreator,
            IFileSystem fileSystem,
            IStorageManager storageManager,
            IPdfTools pdfTools,
            DocSysSettings docSysSettings,
            ILogger<EmailMessageService> logger
            )
        {
            ArgumentHelpers.ThrowIf.IsNull(emailMessageXmlReader, nameof(emailMessageXmlReader));
            ArgumentHelpers.ThrowIf.IsNull(warrantyPdfCreator, nameof(warrantyPdfCreator));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(storageManager, nameof(storageManager));
            ArgumentHelpers.ThrowIf.IsNull(pdfTools, nameof(pdfTools));
            ArgumentHelpers.ThrowIf.IsNull(docSysSettings, nameof(docSysSettings));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _emailMessageXmlReader = emailMessageXmlReader;
            _warrantyPdfCreator = warrantyPdfCreator;
            _storageManager = storageManager;
            _pdfTools = pdfTools;
            _docSysSettings = docSysSettings;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public async Task<IArchiveFile> CreateArchiveForWarrantyXmlString(string xmlString)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlString, nameof(xmlString));

            var emailMessageEmailData = _emailMessageXmlReader.ReadXml(xmlString);

            using (GetLoggerScope(emailMessageEmailData))
            {
                var baseFilePath = _fileSystem.Directory.CreateDirectory(
                    _fileSystem.Path.Combine(
                        _docSysSettings.ResultPath,
                        Guid.NewGuid().ToString(),
                        emailMessageEmailData.TransactionId))
                    .FullName;

                var pdfFilename = $"{emailMessageEmailData.CorrespondenceId}.pdf";
                var pdfFilePath = _fileSystem.Path.Combine(baseFilePath, pdfFilename);
                var pdfStream = await CreateWarrantyPdfStream(emailMessageEmailData);
                CreateFile(pdfFilePath, pdfStream);

                // archive files
                var archiveFileId = await CreateArchive(emailMessageEmailData, pdfFilePath);
                _logger.LogInformation($"Create emailMessage Email Archive");

                var archiveFile = await _storageManager.GetArchiveForArchiveFileId(archiveFileId);
                return archiveFile;
            }
        }

        private async Task<MemoryStream> CreateWarrantyPdfStream(EmailMessageData emailMessageData)
        {
            return await _warrantyPdfCreator.CreatePdfStream(emailMessageData.XslUrlPath, emailMessageData.Xml);
        }

        private async Task<long> CreateArchive(EmailMessageData warrantyEmailData, string pdfFilePath)
        {
            var pdfGeneratedFile = new GeneratedFile
            {
                Description = "VPayge",
                FileDetailType = FileDetailType.Limited,
                FileName = pdfFilePath,
                PageCount = await _pdfTools.GetNumberOfPagesAsync(pdfFilePath),
                TransactionId = warrantyEmailData.TransactionId,
            };

            return await _storageManager.CreateArchive(
                new FaxArchiveFile(
                    warrantyEmailData.Tpa,
                    Convert.ToInt64(warrantyEmailData.TransactionId),
                    warrantyEmailData.CorrespondenceId)
                {
                    DocumentSystemType = DocumentSystemType.Generic
                },
                new List<IGeneratedFile>
                {
                    pdfGeneratedFile,
                }, true);
        }

        private string CreateFile(string filePath, Stream stream)
        {
            using (stream)
            using (var destinationStream = _fileSystem.File.OpenWrite(filePath))
            {
                stream.CopyTo(destinationStream);
                destinationStream.Close();
                _logger.LogInformation($"Created {filePath}");
            }

            return filePath;
        }

        private IDisposable GetLoggerScope(EmailMessageData warrantyEmailDataMeta)
        {
            return _logger.BeginScope(new Dictionary<string, object>
            {
                {"TPA", warrantyEmailDataMeta.Tpa},
                {"BE", warrantyEmailDataMeta.BillingEntity},
                {"CorrespondenceId", warrantyEmailDataMeta.CorrespondenceId},
                {"TransactionId", warrantyEmailDataMeta.TransactionId},
                {"Application", warrantyEmailDataMeta.Application},
                {"Type", warrantyEmailDataMeta.Type},
            });
        }
    }
}
