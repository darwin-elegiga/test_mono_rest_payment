using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public class FaxMessageService : IFaxMessageService
    {
        private readonly IFaxMessageXmlReader _faxMessageXmlReader;
        private readonly IWarrantyPdfCreator _warrantyPdfCreator;
        private readonly IEftFaxPdfCreator _eftFaxPdfCreator;
        private readonly IFileSystem _fileSystem;
        private readonly IStorageManager _storageManager;
        private readonly IPdfTools _pdfTools;
        private readonly DocSysSettings _docSysSettings;
        private readonly ILogger<FaxMessageService> _logger;

        public FaxMessageService(
            IFaxMessageXmlReader faxMessageXmlReader,
            IWarrantyPdfCreator warrantyPdfCreator,
            IEftFaxPdfCreator eftFaxPdfCreator,
            IFileSystem fileSystem,
            IStorageManager storageManager,
            IPdfTools pdfTools,
            DocSysSettings docSysSettings,
                ILogger<FaxMessageService> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(faxMessageXmlReader, nameof(faxMessageXmlReader));
            ArgumentHelpers.ThrowIf.IsNull(warrantyPdfCreator, nameof(warrantyPdfCreator));
            ArgumentHelpers.ThrowIf.IsNull(eftFaxPdfCreator, nameof(eftFaxPdfCreator));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(storageManager, nameof(storageManager));
            ArgumentHelpers.ThrowIf.IsNull(pdfTools, nameof(pdfTools));
            ArgumentHelpers.ThrowIf.IsNull(docSysSettings, nameof(docSysSettings));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _faxMessageXmlReader = faxMessageXmlReader;
            _warrantyPdfCreator = warrantyPdfCreator;
            _eftFaxPdfCreator = eftFaxPdfCreator;
            _fileSystem = fileSystem;
            _storageManager = storageManager;
            _pdfTools = pdfTools;
            _docSysSettings = docSysSettings;
            _logger = logger;
        }

            public async Task<string> CreateWarrantyPdfFromXmlFile(string xmlFilePath)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                {"xmlFilePath", xmlFilePath.Sanitize()}
            }))
            {
                ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

                var xmlString = _fileSystem.File.ReadAllText(xmlFilePath);

                ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlString, nameof(xmlString));

                var faxMessageData = _faxMessageXmlReader.ReadXml(xmlString);

                using (GetLoggerScope(faxMessageData))
                {
                    var pdfStream = await CreateWarrantyPdfStream(faxMessageData);
                    var pdfFilePath = _fileSystem.Path.ChangeExtension(xmlFilePath, "pdf");
                    _logger.LogInformation("Create {PdfFilePath} from {XmlFilePath}", pdfFilePath.Sanitize(), xmlFilePath.Sanitize());
                    return CreateFile(pdfFilePath, pdfStream);
                }
            }
        }
        public async Task<string> CreateEftFaxPdfFromXmlFile(string xmlFilePath)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                {"xmlFilePath", xmlFilePath.Sanitize()}
            }))
            {
                ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

                var xmlString = _fileSystem.File.ReadAllText(xmlFilePath);

                ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlString, nameof(xmlString));

                var faxMessageData = _faxMessageXmlReader.ReadXml(xmlString);

                using (GetLoggerScope(faxMessageData))
                {
                    var pdfStream = await CreateEftFaxPdfStream(faxMessageData);
                    var pdfFilePath = _fileSystem.Path.ChangeExtension(xmlFilePath, "pdf");
                    _logger.LogInformation("Create {PdfFilePath} from {XmlFilePath}", pdfFilePath.Sanitize(), xmlFilePath.Sanitize());
                    return CreateFile(pdfFilePath, pdfStream);
                }
            }
        }

        public async Task<IArchiveFile> CreateArchiveForWarrantyXmlString(string xmlString)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlString, nameof(xmlString));

            var faxMessageFaxData = _faxMessageXmlReader.ReadXml(xmlString);

            using (GetLoggerScope(faxMessageFaxData))
            {
                var baseFilePath = _fileSystem.Directory.CreateDirectory(
                    _fileSystem.Path.Combine(
                        _docSysSettings.ResultPath,
                        Guid.NewGuid().ToString(),
                        faxMessageFaxData.TransactionId))
                    .FullName;

                // save xml file
                var xmlFilePath = _fileSystem.Path.Combine(baseFilePath, $"{faxMessageFaxData.CorrespondenceId}.xml");

                _fileSystem.File.WriteAllText(xmlFilePath, xmlString);
                _logger.LogInformation("Created {XmlFilePath}", xmlFilePath.Sanitize());

                // create pdf in same location
                var pdfFilePath = xmlFilePath.Replace(".xml", ".pdf");
                var pdfStream = await CreateWarrantyPdfStream(faxMessageFaxData);
                CreateFile(pdfFilePath, pdfStream);

                // archive files
                var archiveFileId = await CreateArchive(faxMessageFaxData, xmlFilePath, pdfFilePath);
                _logger.LogInformation($"Create faxMessage Fax Archive");

                return await _storageManager.GetArchiveForArchiveFileId(archiveFileId);
            }
        }
        public async Task<IArchiveFile> CreateArchiveForEftFaxXmlString(string xmlString)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(xmlString, nameof(xmlString));

            var faxMessageFaxData = _faxMessageXmlReader.ReadXml(xmlString);

            using (GetLoggerScope(faxMessageFaxData))
            {
                var baseFilePath = _fileSystem.Directory.CreateDirectory(
                    _fileSystem.Path.Combine(
                        _docSysSettings.ResultPath,
                        Guid.NewGuid().ToString(),
                        faxMessageFaxData.TransactionId))
                    .FullName;

                // save xml file
                var xmlFilePath = _fileSystem.Path.Combine(baseFilePath, $"{faxMessageFaxData.CorrespondenceId}.xml");

                _fileSystem.File.WriteAllText(xmlFilePath, xmlString);
                _logger.LogInformation("Created {XmlFilePath}", xmlFilePath.Sanitize());

                // create pdf in same location
                var pdfFilePath = xmlFilePath.Replace(".xml", ".pdf");
                var pdfStream = await CreateEftFaxPdfStream(faxMessageFaxData);
                CreateFile(pdfFilePath, pdfStream);

                // archive files
                var archiveFileId = await CreateArchive(faxMessageFaxData, xmlFilePath, pdfFilePath);
                _logger.LogInformation($"Create faxMessage Fax Archive");

                return await _storageManager.GetArchiveForArchiveFileId(archiveFileId);
            }
        }


        private async Task<MemoryStream> CreateWarrantyPdfStream(FaxMessageData faxMessageData)
        {
            return await _warrantyPdfCreator.CreatePdfStream(faxMessageData.XslUrlPath, faxMessageData.Xml);
        }
        private async Task<MemoryStream> CreateEftFaxPdfStream(FaxMessageData faxMessageData)
        {
            return await _eftFaxPdfCreator.CreatePdfStream(faxMessageData.Xml);
        }

        private async Task<long> CreateArchive(FaxMessageData warrantyFaxData, string xmlFilePath, string pdfFilePath)
        {
            var xmlGeneratedFile = new GeneratedFile
            {
                Description = "Fax Xml",
                FileDetailType = FileDetailType.Hidden,
                FileName = xmlFilePath,
                TransactionId = warrantyFaxData.TransactionId,
            };

            var pdfGeneratedFile = new GeneratedFile
            {
                Description = "Fax",
                FileDetailType = FileDetailType.FaxDocument,
                FileName = pdfFilePath,
                PageCount = await _pdfTools.GetNumberOfPagesAsync(pdfFilePath),
                TransactionId = warrantyFaxData.TransactionId,
            };

            return await _storageManager.CreateArchive(
                new FaxArchiveFile(
                    warrantyFaxData.Tpa,
                    Convert.ToInt64(warrantyFaxData.TransactionId),
                    warrantyFaxData.CorrespondenceId),
                new List<IGeneratedFile>
                {
                    xmlGeneratedFile,
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
                _logger.LogInformation("Created {FilePath}", filePath.Sanitize());
            }

            return filePath;
        }

        private IDisposable GetLoggerScope(FaxMessageData warrantyFaxDataMeta)
        {
            return _logger.BeginScope(new Dictionary<string, object>
            {
                {"TPA", warrantyFaxDataMeta.Tpa.Sanitize()},
                {"BE", warrantyFaxDataMeta.BillingEntity.Sanitize()},
                {"CorrespondenceId", warrantyFaxDataMeta.CorrespondenceId.Sanitize()},
                {"TransactionId", warrantyFaxDataMeta.TransactionId.Sanitize()},
                {"Application", warrantyFaxDataMeta.Application.Sanitize()},
                {"Type", warrantyFaxDataMeta.Type.Sanitize()},
            });
        }
    }
}
