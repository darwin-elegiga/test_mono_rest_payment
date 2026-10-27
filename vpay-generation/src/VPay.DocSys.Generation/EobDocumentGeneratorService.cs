using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class EobDocumentGeneratorService : IEobDocumentGeneratorService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IPdfTools _pdfTools;
        private readonly IPythonRunner _pythonRunner;
        private readonly ILogger<EobDocumentGeneratorService> _logger;

        public EobDocumentGeneratorService(
            IFileSystem fileSystem,
            IPdfTools pdfTools,
            IPythonRunner pythonRunner,
            ILogger<EobDocumentGeneratorService> logger)
        {
            ThrowIf.IsNull(logger, nameof(logger));
            ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ThrowIf.IsNull(pdfTools, nameof(pdfTools));
            ThrowIf.IsNull(pythonRunner, nameof(pythonRunner));

            _fileSystem = fileSystem;
            _pdfTools = pdfTools;
            _pythonRunner = pythonRunner;
            _logger = logger;
        }

        public async Task<IEnumerable<EobGeneratedFile>> GenerateEobDocuments(EobGenerateType eobGenerateType, DocGenParamInternal param)
        {
            var result = new List<EobGeneratedFile>();
            var docGenParam = param.DocGenParam;

            _logger.LogInformation($"EobGenerateType = {eobGenerateType.ToString().Replace(Environment.NewLine, "")}");
            _logger.LogInformation($"Document Generated Path = {param.SavePath.Replace(Environment.NewLine, "")}");

            result.Add(new EobGeneratedFile
            {
                FileName = param.EobXmlFilePath,
                Description = "eob.xml",
                FileDetailType = FileDetailType.Hidden,
                TransactionId = docGenParam.StatusRecord.TXID
            });

            if (eobGenerateType == EobGenerateType.None)
            {
                return result;
            }

            var config = param.Config;

            var fileName = param.GetSaveFileName(Constants.FileExtensions.EobPdf);

            EobGeneratedFile eobPdfFile = new EobGeneratedFile
            {
                FileName = null,
                FileDetailType = FileDetailType.Pdf,
                EobGenerateType = eobGenerateType,
                TransactionId = docGenParam.StatusRecord.TXID
            };

            result.Add(eobPdfFile);

            switch (eobGenerateType)
            {
                case EobGenerateType.NoEobFromPayer:
                    eobPdfFile.FileName = _fileSystem.CreateTextFile(param.SavePath, Constants.FileExtensions.NoEobFromPayerFileName);
                    eobPdfFile.FileDetailType = FileDetailType.Text;
                    eobPdfFile.Description = "No EOB is provided by the payer.";

                    break;
                case EobGenerateType.DataOnly:
                    eobPdfFile.FileName = await CreateEob(param, fileName).ConfigureAwait(false);
                    eobPdfFile.Description = "plain_eob.pdf";

                    break;
                case EobGenerateType.PdfOnly:
                    if (_fileSystem.File.Exists(fileName))
                    {
                        _fileSystem.File.Delete(fileName);
                    }

                    _fileSystem.File.Move(param.EobPdfFilePath, fileName);
                    var overlayer = param.Config.EobOverlayer;

                    if (!string.IsNullOrEmpty(overlayer))
                    {
                        var (isCheck, overlayId, overlayDate) = GetOverlayValues(param);

                        fileName = await _pythonRunner.OverlayEob(
                            overlayer,
                            fileName,
                            isCheck,
                            overlayId,
                            overlayDate)
                            .ConfigureAwait(false);
                    }

                    eobPdfFile.FileName = fileName;
                    eobPdfFile.Description = param.IsReIssue() ? "plain_eob.pdf" : "Original PDF";
                    break;
                case EobGenerateType.DataThenPdf:
                    _logger.LogInformation(
                        $"Eob Python Script = {config.EobMakerProgramName.Replace(Environment.NewLine, "")}, Python Version = {config.RptPythonVersion.Replace(Environment.NewLine, "")}");

                    eobPdfFile.FileName = await SavePdfFileAndData(true, param, fileName);
                    eobPdfFile.Description = "Original plus composed PDF";

                    break;
                case EobGenerateType.PdfThenData:
                    _logger.LogInformation(
                        $"Eob Python Script = {config.EobMakerProgramName.Replace(Environment.NewLine, "")}, Python Version = {config.RptPythonVersion.Replace(Environment.NewLine, "")}");

                    eobPdfFile.FileName = await SavePdfFileAndData(false, param, fileName);
                    eobPdfFile.Description = "Original plus composed PDF";

                    break;
            }

            //just in case something did not generated right return null, may need to throw error instead.
            if (string.IsNullOrWhiteSpace(eobPdfFile.FileName))
            {
                return null;
            }

            if (Path.GetExtension(eobPdfFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(eobPdfFile.FileName))
                {
                    eobPdfFile.PageCount = await _pdfTools.GetNumberOfPagesAsync(eobPdfFile.FileName);
                }
            }

            return result;
        }

        /// <summary>
        /// This will generate both the Original PDF for the pdf base 64 string and
        /// the pdf from the eobXML data and then merge it
        /// and then it will cleanup the temporary files needed to merge the document to 1 doc.
        /// </summary>
        /// <param name="isDataThenPdf"></param>
        /// <param name="paramInternal"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private async Task<string> SavePdfFileAndData(bool isDataThenPdf, DocGenParamInternal paramInternal, string fileName)
        {
            var dataPdfName = paramInternal.GetSaveFileName(Constants.FileExtensions.EobTempDataPdf);
            var dataResult = await CreateEob(paramInternal, dataPdfName).ConfigureAwait(false);
            var pdfResult = paramInternal.EobPdfFilePath;

            var combineList = new List<string>();
            if (isDataThenPdf)
            {
                combineList.Add(dataResult);
                combineList.Add(pdfResult);
            }
            else
            {
                combineList.Add(pdfResult);
                combineList.Add(dataResult);
            }

            var mergeSuccessful = await _pdfTools.MergeAsync(fileName, combineList);

            _fileSystem.File.Delete(dataResult);
            _fileSystem.File.Delete(pdfResult);

            if (mergeSuccessful)
            {
                return fileName;
            }

            throw new ProcessRunnerException(200, "Could not merge files during eob generation");
        }

        /// <summary>
        /// MTM has a different method signature than other TPAs.  Needed to check for and
        /// pass aux_files from the config.ini
        /// </summary>
        /// <param name="docGenParam"></param>
        /// <returns></returns>
        private string GetAuxFiles(DocGenParam docGenParam)
        {
            return docGenParam.Configuration.EnvironmentSettings.ContainsKey("aux_files")
                ? docGenParam.Configuration.EnvironmentSettings["aux_files"]
                : string.Empty;
        }

        private (bool IsCheck, string OverlayId, DateTime OverLayDate) GetOverlayValues(DocGenParamInternal param)
        {
            var overlayId = param.DocGenParam.StatusRecord.TXID;
            DateTime overlayDate = param.DocGenParam.StatusRecord.CLAIMDATE;
            bool isCheck = param.DocGenParam.CheckInfo != null;

            if (isCheck)
            {
                overlayId = param.DocGenParam.CheckInfo.CheckNumber;
                overlayDate = DateTime.ParseExact(param.DocGenParam.CheckInfo.CheckDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            }

            return (isCheck, overlayId, overlayDate);
        }

        private async Task<string> CreateEob(DocGenParamInternal param, string dataPdfName)
        {
            _logger.LogInformation(
                $"Eob Python Script = {param.Config.EobMakerProgramName.Replace(Environment.NewLine, "")}, Python Version = {param.Config.RptPythonVersion.Replace(Environment.NewLine, "")}");

            var (isCheck, overlayId, overlayDate) = GetOverlayValues(param);

            return await _pythonRunner.CreateEob(
                param.Config.EobMakerProgramName,
                dataPdfName,
                param.EobXmlFilePath,
                GetAuxFiles(param.DocGenParam),
                param.DocGenParam.StatusRecord.PAYTYPE,
                param.Config.RptPythonVersion,
                overlayId,
                overlayDate,
                isCheck)
                .ConfigureAwait(false);
        }
    }
}
