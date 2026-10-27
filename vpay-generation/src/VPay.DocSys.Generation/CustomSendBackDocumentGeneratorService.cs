using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Pdf.Tools;
using static VPay.DocSys.Common.ArgumentHelpers;
using VPay.DocSys.Generation.Common;
using System;

namespace VPay.DocSys.Generation
{
    public class CustomSendBackDocumentGeneratorService : ICustomSendBackDocumentGeneratorService
    {
        private static VPaygeDocumentType[] SendBackDocumentTypes => new[]
        {
            VPaygeDocumentType.CustomSendBackCheck_Masked,
            VPaygeDocumentType.CustomSendBackCheck_Clear,
        };

        private readonly IPythonRunner _pythonRunner;
        private readonly IFileSystem _fileSystem;
        private readonly IPdfTools _pdfTools;
        private readonly ISendBackCheckXmlFileWriter _sendBackCheckXmlFileWriter;
        private readonly ILogger<CustomSendBackDocumentGeneratorService> _logger;

        public CustomSendBackDocumentGeneratorService(
            IPythonRunner pythonRunner,
            IFileSystem fileSystem,
            IPdfTools pdfTools,
            ISendBackCheckXmlFileWriter sendBackCheckXmlFileWriter,
            ILogger<CustomSendBackDocumentGeneratorService> logger)
        {
            ThrowIf.IsNull(pythonRunner, nameof(pythonRunner));
            ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ThrowIf.IsNull(pdfTools, nameof(pdfTools));
            ThrowIf.IsNull(sendBackCheckXmlFileWriter, nameof(sendBackCheckXmlFileWriter));
            ThrowIf.IsNull(logger, nameof(logger));

            _pythonRunner = pythonRunner;
            _fileSystem = fileSystem;
            _pdfTools = pdfTools;
            _sendBackCheckXmlFileWriter = sendBackCheckXmlFileWriter;
            _logger = logger;
        }

        public async Task<IEnumerable<CustomSendBackGeneratedFile>> GenerateDocuments(DocGenParamInternal param)
        {
            ThrowIf.IsNull(param, nameof(param));

            var tasks = new List<Task<CustomSendBackGeneratedFile>>(2);

            var transactionId = param.DocGenParam.CheckInfo.SETransactionId;
            var path = param.SavePath;
            var xmlFilePath = _sendBackCheckXmlFileWriter.CreateXmlFile(path, param.DocGenParam.CheckInfo);

            _ = await _pythonRunner.CreateCustomSendBacks(xmlFilePath);

            foreach (var documentType in SendBackDocumentTypes)
            {
                var checkFileName = documentType.GetCustomSendCheckFileName(long.Parse(transactionId));

                var checkFilePath = _fileSystem.Path.Combine(path, checkFileName);

                if (!_fileSystem.File.Exists(checkFilePath))
                {
                    var error = $"Could not locate: {checkFilePath}";
                    _logger.LogAndThrowError(error, ex: new FileNotFoundException(error));
                }

                tasks.Add(CreateCheckVPaygeFile(checkFilePath, documentType, transactionId));
            }

            return await Task.WhenAll(tasks);
        }

        private async Task<CustomSendBackGeneratedFile> CreateCheckVPaygeFile(string filePath, VPaygeDocumentType documentType, string transactionId)
        {
            FileDetailType fileDetailType;
            string description;

            if (documentType == VPaygeDocumentType.CustomSendBackCheck_Clear)
            {
                description = "Clear check";
                fileDetailType = FileDetailType.Hidden;
            }
            else
            {
                description = "Masked check";
                fileDetailType = FileDetailType.Limited;
            }

            _logger.LogInformation($"Create Custom SendBack {description.Replace(Environment.NewLine, "")} file for {transactionId.Replace(Environment.NewLine, "")}");

            return new CustomSendBackGeneratedFile
            {
                Description = description,
                FileDetailType = fileDetailType,
                FileName = filePath,
                PageCount = await _pdfTools.GetNumberOfPagesAsync(filePath),
                TransactionId = transactionId,
                VPaygeDocumentType = documentType
            };
        }
    }
}
