using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using Unidecode.NET;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;

// ReSharper disable UseStringInterpolation
// ReSharper disable InconsistentNaming
#pragma warning disable 4014

namespace VPay.DocSys.Generation
{
    public class DocSysOperations : IDocSysOperations
    {
        private readonly IEobXmlParser _eobXmlParser;
        private readonly IFileSystem _fileSystem;
        private readonly IStorageManager _storageOperations;
        private readonly ILogger _logger;
        private readonly DocSysSettings _configuration;
        private readonly CleanDocsService _cleanDocsService;
        private readonly IRedCardRepository _redCardRepository;
        private readonly ICustomSendBackDocumentGeneratorService _customSendBackDocumentGeneratorService;
        private readonly ICoverVPaygeGeneratorService _coverVPaygeGeneratorService;
        private readonly IEobDocumentGeneratorService _eobDocumentGeneratorService;
        private readonly IPacketDocumentGeneratorService _packetDocumentGeneratorService;
        private readonly IFssiTracManager _fssiTracManager;
        private readonly IStpRemittanceDocumentService _stpRemittanceDocumentService;

        public DocSysOperations(
            IEobXmlParser eobXmlParser,
            IFileSystem fileSystem,
            IStorageManager storageOperations,
            ILogger<DocSysOperations> logger,
            DocSysSettings configuration,
            CleanDocsService cleanDocsService,
            IRedCardRepository redCardRepository,
            ICustomSendBackDocumentGeneratorService customSendBackDocumentGeneratorService,
            ICoverVPaygeGeneratorService coverVPaygeGeneratorService,
            IEobDocumentGeneratorService eobDocumentGeneratorService,
            IPacketDocumentGeneratorService packetDocumentGeneratorService,
            IFssiTracManager fssiTracManager,
            IStpRemittanceDocumentService stpRemittanceDocumentService)
        {
            ArgumentHelpers.ThrowIf.IsNull(eobXmlParser, nameof(eobXmlParser));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(storageOperations, nameof(storageOperations));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));
            ArgumentHelpers.ThrowIf.IsNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIf.IsNull(cleanDocsService, nameof(cleanDocsService));
            ArgumentHelpers.ThrowIf.IsNull(redCardRepository, nameof(redCardRepository));
            ArgumentHelpers.ThrowIf.IsNull(customSendBackDocumentGeneratorService, nameof(customSendBackDocumentGeneratorService));
            ArgumentHelpers.ThrowIf.IsNull(coverVPaygeGeneratorService, nameof(coverVPaygeGeneratorService));
            ArgumentHelpers.ThrowIf.IsNull(eobDocumentGeneratorService, nameof(eobDocumentGeneratorService));
            ArgumentHelpers.ThrowIf.IsNull(packetDocumentGeneratorService, nameof(packetDocumentGeneratorService));
            ArgumentHelpers.ThrowIf.IsNull(fssiTracManager, nameof(fssiTracManager));
            ArgumentHelpers.ThrowIf.IsNull(stpRemittanceDocumentService, nameof(stpRemittanceDocumentService));

            _eobXmlParser = eobXmlParser;
            _fileSystem = fileSystem;
            _storageOperations = storageOperations;
            _logger = logger;
            _configuration = configuration;
            _cleanDocsService = cleanDocsService;
            _redCardRepository = redCardRepository;
            _customSendBackDocumentGeneratorService = customSendBackDocumentGeneratorService;
            _coverVPaygeGeneratorService = coverVPaygeGeneratorService;
            _eobDocumentGeneratorService = eobDocumentGeneratorService;
            _packetDocumentGeneratorService = packetDocumentGeneratorService;
            _fssiTracManager = fssiTracManager;
            _stpRemittanceDocumentService = stpRemittanceDocumentService;
        }

        public async Task<DocGenManifest> GetDocumentManifest(DocGenParam docGenParam)
        {
            var param = await SetupParam(docGenParam);

            return GetDocumentManifestInternal(param);
        }

        public async Task<IEnumerable<EobGeneratedFile>> GenerateEobDoc(DocGenParam docGenParam)
        {
            var param = await SetupParam(docGenParam);
            var manifest = GetDocumentManifestInternal(param);

            return await _eobDocumentGeneratorService.GenerateEobDocuments(manifest.EobGenerateType, param);
        }

        public async Task<IEnumerable<VPaygeGeneratedFile>> GenerateVPaygeDocs(DocGenParam docGenParam)
        {
            var param = await SetupParam(docGenParam);
            var manifest = GetDocumentManifestInternal(param);

            return await _coverVPaygeGeneratorService.GenerateCoverVPayges(manifest.CoverSheetDocuments, param);
        }

        public async Task<IEnumerable<IGeneratedFile>> GenerateAllDocs(DocGenParam docGenParam)
        {
            long transactionId = 0;
            if (!string.IsNullOrEmpty(docGenParam?.StatusRecord?.TXID ?? ""))
            {
                transactionId = Convert.ToInt64(docGenParam?.StatusRecord?.TXID);
                _logger.LogDebug(
                      $"processing DocSysOperations for {transactionId}");
            }
            var result = new List<IGeneratedFile>();
            using (_logger.BeginScope(new Dictionary<string, object>()
                {
                    {"TransactionId", transactionId}
                }))
            {
                if (transactionId > 0)
                {
                    await _storageOperations
                        .InsertTransactionStatus(transactionId, TransactionStatusCode.Dequeued)
                        .ConfigureAwait(false);
                }

                try
                {
                    if (docGenParam.RegeneratePackage && !await RegeneratePackage(docGenParam))
                    {
                        // Attempt to regen the package. Throw error if unable.
                        throw new InvalidOperationException("Unable to regenerate transaction");
                    }

                    var docParam = await SetupParam(docGenParam);

                    var docGenSet = GetDocumentManifestInternal(docParam);

                    if (docGenSet == null)
                    {
                        _logger.LogWarning("Document Manifest is null");

                        await _storageOperations
                            .InsertTransactionStatus(transactionId, TransactionStatusCode.Error)
                            .ConfigureAwait(false);
                        return result;
                    }

                    var eobDocTask = _eobDocumentGeneratorService.GenerateEobDocuments(docGenSet.EobGenerateType, docParam);
                    var coversTask = _coverVPaygeGeneratorService.GenerateCoverVPayges(docGenSet.CoverSheetDocuments, docParam);

                    // runs the eob and covers generation in parallel
                    await Task.WhenAll(eobDocTask, coversTask);

                    // gets the result from eobDocTask (does not re-run it)
                    var eobDoc = await eobDocTask;
                    if (eobDoc != null)
                    {
                        result.AddRange(eobDoc);
                    }

                    // gets the result from coversTask (does not re-run it)
                    var covers = await coversTask;
                    if (covers != null)
                    {
                        result.AddRange(covers);
                        _logger.LogDebug(
                            $"Generate Covers in DocSysOperations for {transactionId}");
                    }

                    // packets must be run after the eob and cover generation occurs.
                    var packets = await _packetDocumentGeneratorService.GeneratePacketDocuments(docGenSet.PacketDocuments, docParam);

                    result.AddRange(packets);

                    //Re-issue. Load previous EOB data
                    if (docParam.IsReIssue())
                    {
                        var sourceFile = await WriteReIssueTxtFiles(docParam);
                        if (sourceFile != null)
                        {
                            result.Add(sourceFile);
                        }
                    }

                    string stageFileKey = docGenParam.InputFile;

                    var guid = docParam.IsReIssue()
                        ? docParam.ReIssueData.ReplacementGuid
                        : docGenParam.StatusRecord.GUID;

                    CheckFaxSizeCutoff(result, docGenParam);

                    if (docParam.DocGenParam.IsCustomSendBack)
                    {
                        var sendbackFiles = await _customSendBackDocumentGeneratorService.GenerateDocuments(docParam);

                        result.AddRange(sendbackFiles);
                    }

                    if (docParam.IsStraightThruProcessing())
                    {
                        using var eobStream = await GetEobXmlStream(docParam.EobXmlFilePath);
                        var eobXml = new XPathDocument(docParam.EobXmlFilePath);

                        (var remitDocs, bool remittanceAccepted) = await _stpRemittanceDocumentService.GenerateAndSendRemittanceDocument(docParam, transactionId, eobXml);

                        result.AddRange(remitDocs);

                        // if remittance was accepted by the api then change payment's distribution type to "O"
                        if (remittanceAccepted)
                        {
                            docParam.DocGenParam.StatusRecord.DIST = "O";
                        }
                    }

                    long archiveFileId = await _storageOperations.CreateArchive(
                        new ArchiveFile(
                            stageFileKey,
                            docGenParam.StatusRecord.TPA,
                            transactionId,
                            Guid.Parse(guid),
                            docGenParam.StatusRecord.TRANID),
                        result,
                        true);

                    _logger.LogInformation("Saved Documents for {transactionId} to Id = {ArchiveFileId}", transactionId, archiveFileId);

                    if (docParam.IsReIssue())
                    {
                        await _storageOperations.SaveReissueData(
                            Convert.ToInt64(docParam.ReIssueData.OriginalTransactionId.RemoveLeadingZeroes()),
                            Convert.ToInt64(docParam.ReIssueData.ReissuedTransactionId.RemoveLeadingZeroes()));
                    }

                    bool successFullyInsertedDisposition =
                        await _storageOperations.InsertDocumentDisposition(transactionId,
                            docGenParam.StatusRecord.DIST);
                    if (!successFullyInsertedDisposition)
                    {
                        _logger.LogWarning(
                            $"Failed to InsertDocumentDisposition({transactionId}, {docGenParam.StatusRecord.DIST}");
                        //TODO -JAH 2018-09-21- We need to have some sort of queue to place these on... but that's not necessarily an October 8th concern.
                    }

                    if (docGenParam.IsRedCard)
                    {
                        try
                        {
                            await _redCardRepository.UpdateTransactionStatus(transactionId,
                                RedCardTransactionStatusCode.DocumentsGenerated);
                            _logger.LogDebug(
                             $"UpdateTransactionStatus in DocSysOperations for {transactionId}");
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception,
                                $"Failed to update red card transaction status to {RedCardTransactionStatusCode.DocumentsGenerated} for transaction id {transactionId}.");
                        }
                    }

                    await _storageOperations
                        .InsertTransactionStatus(transactionId, TransactionStatusCode.GenerationComplete)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "GenerateAllDocs failed. See exception for details.");
                    await _storageOperations
                        .InsertTransactionStatus(transactionId, TransactionStatusCode.Error)
                        .ConfigureAwait(false);
                    throw;
                }
            }
            return result;
        }

        private void CheckFaxSizeCutoff(IList<IGeneratedFile> result, DocGenParam docGenParam)
        {
            if (!(docGenParam?.Configuration?.EnvironmentSettings?.ContainsKey("fax_size_cutoff") ?? false))
            { return; }
            try
            {
                string faxSizeCutoff = docGenParam.Configuration.EnvironmentSettings["fax_size_cutoff"];
                if (string.IsNullOrEmpty(faxSizeCutoff) || !Int32.TryParse(faxSizeCutoff, out int numPagesMax))
                {
                    // if the fax_size_cutoff variable isn't present, or can't be coerced into
                    // an integer, just return without changing anything
                    return;
                }
                // Check if the package's distribution type is Fax
                // AND the number of pages of any generated document is greater than the
                // the numPagesMax (fax_size_cutoff),
                // then, change the package's distribution to Mail
                if (docGenParam.StatusRecord.DIST == DistType.Fax
                    && result.Any(r => r.PageCount >= numPagesMax))
                {
                    docGenParam.StatusRecord.DIST = DistType.Mail;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// If a transaction (package) is requested to be regenerated,
        /// there are a few things that need to be checked prior to
        /// removing the existing documents and generating the new ones.
        /// </summary>
        /// <param name="docGenParam"></param>
        private async Task<bool> RegeneratePackage(DocGenParam docGenParam)
        {
            if (docGenParam.StatusRecord.DIST == DistType.Mail)
            {
                var isInFssi = await _fssiTracManager.SentToFssi(string.Format("{0}_{1}_{2}_{3}.zip"
                    , docGenParam.StatusRecord.PRTVENDOR
                    , docGenParam.StatusRecord.TPA
                    , docGenParam.InputFile
                    , docGenParam.StatusRecord.PAYTYPE
                ));

                if (isInFssi)
                {
                    docGenParam.StatusRecord.DIST = DistType.DisplayOnly;
                }
            }

            try
            {
                await _cleanDocsService
                    .CleanSingleTransaction(docGenParam.StatusFilePath,
                        long.Parse(docGenParam.StatusRecord.TXID.RemoveLeadingZeroes()))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting cleandocs");
                // Still ok to run,even if cleandocs failed although
                // probably want to change this at some point. Inspect why it fails,
                // if it ever does.
            }
            return true;
        }

        // ReSharper disable once InconsistentNaming
        private async Task<ReissueEOBData> GetReissueEOBData(DocGenParamInternal paramInternal)
        {
            var docGenParam = paramInternal.DocGenParam;

            var eobData = new ReissueEOBData();

            //The re-issue comes in with the same guid as the original so we can relate them
            eobData.OriginalGuid = docGenParam.StatusRecord.GUID;
            eobData.ReissuedTransactionId = docGenParam.StatusRecord.TXID;

            //We need a new guid so we don't create identical file names for the new re-issue documents
            eobData.ReplacementGuid = Guid.NewGuid().ToString();

            paramInternal.EobXmlFilePath = _fileSystem.Path.Combine(paramInternal.SavePath, $"{docGenParam.InputFile}.{eobData.ReplacementGuid}{Constants.FileExtensions.EobXml}");

            //Gather the EOB data from the original transaction archive
            var archiveFiles = await _storageOperations.GetArchivesForGuid(new Guid(docGenParam.StatusRecord.GUID), true);
            var archive = archiveFiles?.FirstOrDefault();
            if (archive == null)
            {
                archiveFiles = await _storageOperations.GetArchivesForGuid(new Guid(docGenParam.StatusRecord.GUID), true);
                eobData.ArchiveSource = DbSystemType.SqlServer;
                archive = archiveFiles?.FirstOrDefault();
                if (archive == null)
                {
                    _logger.LogError("Reissue - Could not locate original archive file");
                    throw new ApplicationException("Reissue - Could not locate original archive file.");
                }
            }

            eobData.ArchiveFileId = archive.ArchiveFileId;
            eobData.OriginalTransactionId = archive.TransactionId.ToString();

            //How many times has this transaction been re-issued already
            eobData.ReissueCount = archive.ArchiveDetails.Select(d => d.FileName).Distinct().Count(n => n.Contains("reissued_to_"));

            var xmlDetail = archive.ArchiveDetails.FirstOrDefault(d => d.FileName.EndsWith(Constants.FileExtensions.EobXml, StringComparison.OrdinalIgnoreCase));
            var pdfDetail = archive.ArchiveDetails.FirstOrDefault(d => d.FileName.EndsWith(Constants.FileExtensions.EobPdf, StringComparison.OrdinalIgnoreCase));

            if (xmlDetail == null)
            {
                _logger.LogError("Reissue - Could not locate eob.xml file");
                throw new ApplicationException("Reissue - Could not locate eob.xml file.");
            }

            eobData.OriginalXmlFileName = xmlDetail.FileName;

            var copyDetails = new List<CopyArchiveFileDetail> { new CopyArchiveFileDetail(xmlDetail, paramInternal.EobXmlFilePath) };

            if (pdfDetail != null)
            {
                paramInternal.EobPdfFilePath = paramInternal.GetSaveFileName(Constants.FileExtensions.EobTempBase64Pdf);
                copyDetails.Add(new CopyArchiveFileDetail(pdfDetail, paramInternal.EobPdfFilePath));
            }

            await _storageOperations.CopyArchiveDetailFiles(archive, copyDetails);

            return eobData;
        }

        private async Task<IGeneratedFile> WriteReIssueTxtFiles(DocGenParamInternal paramInternal)
        {
            var reissueEobData = paramInternal.ReIssueData;

            string timeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            //Create a text file with information that points to the original archive and transaction
            string reissueMsg = string.Format(REISSUE_TARGET_MSG, timeStamp, reissueEobData.OriginalTransactionId.RemoveLeadingZeroes(), reissueEobData.OriginalGuid);

            var fromFile = paramInternal.GetSaveFileName($".reissued_from_{reissueEobData.ReissueCount + 1:00}.txt");

            _fileSystem.File.WriteAllText(fromFile, reissueMsg);

            if (reissueEobData.ArchiveSource == DbSystemType.SqlServer)
            {
                //Create a text file to add to the original archive to show the transaction was re-issued pointing to the new GUID and transaction ID
                string originalArchiveMsg = string.Format(REISSUE_SOURCE_MSG, timeStamp, reissueEobData.ReissuedTransactionId.RemoveLeadingZeroes(), reissueEobData.ReplacementGuid);
                var fileToName = reissueEobData.OriginalXmlFileName.Replace(Constants.FileExtensions.EobXml, $".reissued_to_{reissueEobData.ReissueCount + 1:00}.txt");

                var toFilePath = _fileSystem.Path.Combine(paramInternal.SavePath, fileToName);

                _fileSystem.File.WriteAllText(toFilePath, originalArchiveMsg);

                var genFile = new EobGeneratedFile()
                {
                    FileDetailType = FileDetailType.Text,
                    FileName = toFilePath,
                    TransactionId = reissueEobData.OriginalTransactionId.ToString(),
                    Description = "unload_reissue documentation for source document"
                };

                var originalArchive = await _storageOperations
                    .GetArchiveForArchiveFileId(reissueEobData.ArchiveFileId);
                await _storageOperations.CreateArchive(
                    archiveFile: new ArchiveFile
                    {
                        StageFileKey = originalArchive.StageFileKey,
                        CreateLogin = originalArchive.CreateLogin,
                        Tpa = originalArchive.Tpa,
                        TransactionId = originalArchive.TransactionId,
                        DocumentUniqueId = originalArchive.DocumentUniqueId,
                        ReferenceCode = originalArchive.ReferenceCode,
                        DocumentSystemType = DocumentSystemType.Transaction,
                    },
                    generatedFiles: new List<IGeneratedFile> { genFile },
                    cleanUp: false);
            }

            return new EobGeneratedFile()
            {
                FileName = fromFile,
                Description = "unload_reissue documentation for target document",
                TransactionId = reissueEobData.ReissuedTransactionId,
                FileDetailType = FileDetailType.Text
            };
        }

        #region Internal Methods

        private DocGenManifest GetDocumentManifestInternal(DocGenParamInternal param)
        {
            var config = param.Config;
            var docGenParam = param.DocGenParam;

            bool NoEobRptFlag = String.IsNullOrWhiteSpace(config.NoEobRptFlag) ? false : true;

            var result = new DocGenManifest();

            var customPdf = GetCustomPdfElement(param.EobAsDictionary);
            ValidateCustomPdf(customPdf);

            var documentOrderForPacket = GetPacketDocumentOrder(customPdf);

            List<string> customPdfs = null;
            var forceEobToEvenPages = false;

            string eobMethod;
            if (param.IsReIssue())
            {
                if (!string.IsNullOrEmpty(param.EobPdfFilePath))
                {
                    result.EobGenerateType = EobGenerateType.PdfOnly;
                }
                else
                {
                    eobMethod = GetEobMethod(param.EobAsDictionary, !String.IsNullOrWhiteSpace(config.EobMakerProgramName), !String.IsNullOrWhiteSpace(param.EobPdfFilePath));
                    result.EobGenerateType = GetEobDocument(eobMethod, NoEobRptFlag);
                }
            }
            else
            {
                eobMethod = GetEobMethod(param.EobAsDictionary, !String.IsNullOrWhiteSpace(config.EobMakerProgramName), !String.IsNullOrWhiteSpace(param.EobPdfFilePath));
                result.EobGenerateType = GetEobDocument(eobMethod, NoEobRptFlag);
            }

            if (result.EobGenerateType == EobGenerateType.None ||
                result.EobGenerateType == EobGenerateType.NoEobFromPayer)
            {
                documentOrderForPacket.Remove(PacketIncludedDocumentType.EOB);
            }

            // If it is status record is distribution type is UNLOAD, then only the EOB needs to be generated. This returns after eob document type is setup and before setting up Coversheets and Packets.
            if (docGenParam.StatusRecord.DIST == DistType.Unload)
            {
                return result;
            }

            if (customPdf != null)
            {
                customPdfs = customPdf.Files.Select(x => x.File).ToList();
                forceEobToEvenPages = customPdf.Method == "APPEND" && customPdf.DuplexPrint == "YES";
            }

            var coverSheets = new List<VPaygeDocumentType>();

            if (param.DocGenParam.IsRedCard &&
                new[] { PayType.CLCFT, PayType.CLCCK }.Contains(docGenParam.StatusRecord.PAYTYPE))
            {
                _logger.LogInformation($"Red Card TPA {docGenParam.StatusRecord.TPA} of PayType {docGenParam.StatusRecord.PAYTYPE} changed to default PayType {PayType.CLCHK}");

                docGenParam.StatusRecord.PAYTYPE = PayType.CLCHK;
            }

            switch (docGenParam.StatusRecord.PAYTYPE)
            {
                case PayType.CLVCP:
                    coverSheets.Add(VPaygeDocumentType.VCard_Masked);
                    coverSheets.Add(VPaygeDocumentType.VCard_Clear);
                    break;

                case PayType.CLEFT:
                    coverSheets.Add(VPaygeDocumentType.EFT);
                    break;

                case PayType.CLSDA:
                    coverSheets.Add(VPaygeDocumentType.SDA);
                    break;

                case PayType.CLCFT:
                    coverSheets.Add(VPaygeDocumentType.Client_EFT);
                    break;

                case PayType.CLNPF:
                    coverSheets.Add(VPaygeDocumentType.NoPay);
                    break;

                case PayType.CLCCK:
                    coverSheets.Add(VPaygeDocumentType.Client_Check);
                    break;

                case PayType.OPACH:
                    coverSheets.Add(VPaygeDocumentType.OptumAch);
                    break;

                default:
                    coverSheets.Add(VPaygeDocumentType.Check);
                    break;
            }

            if (documentOrderForPacket.Contains(PacketIncludedDocumentType.Cover))
            {
                result.CoverSheetDocuments = coverSheets;
            }

            result.PacketDocuments = coverSheets.Select(x =>
                new PacketDocument
                {
                    CustomPdfs = customPdfs,
                    DocumentsNeeded = documentOrderForPacket,
                    ForceEobToEvenPages = forceEobToEvenPages,
                    DocumentType = x
                }
                ).ToList();

            // RedCard EOBs come in via the .inv.drv file
            if (docGenParam.IsRedCard)
            {
                result.EobGenerateType = EobGenerateType.None;
            }

            return result;
        }

        internal virtual List<PacketIncludedDocumentType> GetPacketDocumentOrder(EobXmlCustomPdfs customPdfs)
        {
            if (customPdfs == null)
            {
                return new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.EOB };
            }

            if (customPdfs.Method == "APPEND")
            {
                return new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.EOB, PacketIncludedDocumentType.CustomPdf };
            }

            if (customPdfs.Order == 1)
            {
                return new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.CustomPdf, PacketIncludedDocumentType.EOB };
            }

            return new List<PacketIncludedDocumentType> { PacketIncludedDocumentType.Cover, PacketIncludedDocumentType.CustomPdf };
        }

        internal virtual EobGenerateType GetEobDocument(string eobMethod, bool NoEobRptFlag)
        {
            EobGenerateType result = EobGenerateType.None;
            if (eobMethod == "NONE" || NoEobRptFlag)
            {
                result = EobGenerateType.NoEobFromPayer;
            }
            else
            {
                var hasData = eobMethod.Contains("DATA");
                var hasPdf = eobMethod.Contains("PDF");

                if (hasData && hasPdf)
                {
                    result = (eobMethod == "PDF,DATA") ? EobGenerateType.PdfThenData : EobGenerateType.DataThenPdf;
                }
                else if (hasData)
                {
                    result = EobGenerateType.DataOnly;
                }
                else if (hasPdf)
                {
                    result = EobGenerateType.PdfOnly;
                }
            }

            return result;
        }

        internal static EobXmlCustomPdfs GetCustomPdfElement(IDictionary<string, dynamic> eobAsDict)
        {
            if (eobAsDict == null)
            {
                return null;
            }

            var xCustomPdf = eobAsDict.ContainsKey("custom_pdfs") ? eobAsDict["custom_pdfs"] as Dictionary<string, dynamic> : null;

            if (xCustomPdf == null)
            {
                return null;
            }

            var result = new EobXmlCustomPdfs()
            {
                Method = xCustomPdf.ContainsKey("method") ? xCustomPdf["method"] as string : null,
                DuplexPrint = xCustomPdf.ContainsKey("duplex_print") ? xCustomPdf["duplex_print"] as string : null,
                OrderAsString = xCustomPdf.ContainsKey("order") ? xCustomPdf["order"] as string : null,
            };

            var files = xCustomPdf.ContainsKey("files") ? xCustomPdf["files"] as Dictionary<string, dynamic> : null;

            if (files != null && files.ContainsKey("file"))
            {
                if (files["file"] is List<object> fileObjList && fileObjList.All(i => i is string))
                {
                    result.Files = fileObjList
                        .Select(x => new CustomPdfFile { File = x.ToString() })
                        .ToArray();
                }
                else if (files["file"] is string s)
                {
                    result.Files = new[] { new CustomPdfFile { File = s } };
                }
            }
            return result;
        }

        internal string GetEobMethod(IDictionary<string, dynamic> eobAsDictionary, bool hasEobMaker, bool hasPdf)
        {
            if (!hasEobMaker)
            {
                return hasPdf ? "PDF" : "NONE";
            }
            var eobMethod = eobAsDictionary.ContainsKey("eob_method") ? eobAsDictionary["eob_method"] as string : null;
            if (eobMethod == null)
            {
                return "DATA";
            }
            return eobMethod.ToUpper();
        }

        private async Task<DocGenParamInternal> SetupParam(DocGenParam docGenParam)
        {
            ArgumentHelpers.ThrowIf.IsNull(docGenParam, nameof(docGenParam));
            ArgumentHelpers.ThrowIf.IsChildPropertyNull(docGenParam.Configuration, nameof(docGenParam), nameof(docGenParam.Configuration));
            ArgumentHelpers.ThrowIf.IsChildPropertyNull(docGenParam.StatusRecord, nameof(docGenParam), nameof(docGenParam.StatusRecord));
            ArgumentHelpers.ThrowIf.IsChildPropertyNull(docGenParam.OtbRecord, nameof(docGenParam), nameof(docGenParam.OtbRecord));
            if (docGenParam.GenerateEOB)
            {
                ArgumentHelpers.ThrowIf.IsChildPropertyNull(docGenParam.EOBData, nameof(docGenParam), nameof(docGenParam.EOBData));
                ArgumentHelpers.ThrowIf.IsChildPropertyNullOrWhiteSpace(docGenParam.EOBData.Name, nameof(docGenParam), nameof(docGenParam.EOBData), nameof(docGenParam.EOBData.Name));
                ArgumentHelpers.ThrowIf.IsChildPropertyNullOrWhiteSpace(docGenParam.EOBData.XML, nameof(docGenParam), nameof(docGenParam.EOBData), nameof(docGenParam.EOBData.XML));
            }

            var result = new DocGenParamInternal()
            {
                DocGenParam = docGenParam,
                Config = new DocSysConfig(docGenParam.Configuration),
                SavePath = GetDocumentPath(docGenParam),
            };

            if (result.IsReIssue())
            {
                result.ReIssueData = await GetReissueEOBData(result);
                docGenParam.EOBData = null;
            }
            else
            {
                if (_configuration.AllowUnicode == false)
                {
                    CleanUnicodeFromEobXml(docGenParam);
                }

                result.EobXmlFilePath = _fileSystem.Path.Combine(result.SavePath, docGenParam.EOBData.Name);

                //write out the eob.xml file to be included in the archive
                if (!string.IsNullOrEmpty(docGenParam.EOBData.Name) && !string.IsNullOrEmpty(docGenParam.EOBData.XML))
                {
                    var fullFileName = result.EobXmlFilePath;

                    if (_fileSystem.File.Exists(docGenParam.EOBData.XML))
                    {
                        var eobXml = _fileSystem.File.ReadAllText(docGenParam.EOBData.XML);
                        if (!string.IsNullOrEmpty(eobXml))
                        {
                            eobXml = eobXml.Unidecode();

                            _fileSystem.File.WriteAllText(fullFileName, eobXml);
                        }
                    }
                    else
                    {
                        _fileSystem.File.WriteAllText(fullFileName, docGenParam.EOBData.XML);
                    }
                }

                if (!string.IsNullOrEmpty(docGenParam.EOBData.PDF))
                {
                    string base64Pdf;

                    if (_fileSystem.File.Exists(docGenParam.EOBData.PDF))
                    {
                        base64Pdf = _fileSystem.File.ReadAllText(docGenParam.EOBData.PDF);
                    }
                    else
                    {
                        base64Pdf = docGenParam.EOBData.PDF;
                    }
                    result.EobPdfFilePath = await SavePdfFileFromBase64(base64Pdf, result.GetSaveFileName(Constants.FileExtensions.EobTempBase64Pdf));
                }

                docGenParam.EOBData = null;
            }

            // read the xml from the eob.xml file saved onto the disk drive
            using (var fileReader = _fileSystem.File.OpenRead(result.EobXmlFilePath))
            {
                result.EobAsDictionary = await _eobXmlParser.EobToDictionaryAsync(fileReader);
            }

            GC.Collect();
            return result;
        }

        #endregion Internal Methods

        internal virtual void CleanUnicodeFromEobXml(DocGenParam docGenParam)
        {
            _logger.LogInformation("Removing Unicode");

            if (docGenParam.EOBData != null)
            {
                docGenParam.EOBData.XML = docGenParam.EOBData.XML.Unidecode();
            }
        }

        private async Task<string> SavePdfFileFromBase64(string pdfAsBase64, string fileName)
        {
            var bytes = Convert.FromBase64String(pdfAsBase64);

            using (var stream = _fileSystem.File.Create(fileName))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }

            return fileName;
        }

        private void ValidateCustomPdf(EobXmlCustomPdfs customPdf)
        {
            if (customPdf == null)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(customPdf.Method))
            {
                throw new InvalidFileFormatException("no <method> element in <custom_pdfs>");
            }
            if (customPdf.Files == null)
            {
                throw new InvalidFileFormatException("no <files> element in <custom_pdfs>");
            }
            if (customPdf.Files.Length == 0)
            {
                throw new InvalidFileFormatException("no <file> element in <custom_pdfs>");
            }

            switch (customPdf.Method)
            {
                case "REPLACE":
                    if (customPdf.Order == null)
                    {
                        throw new InvalidFileFormatException("no <order> element in <custom_pdfs>");
                    }
                    else if (customPdf.Order != 1 && customPdf.Order != 2)
                    {
                        throw new InvalidFileFormatException("valid order in <custom_pdfs> is 1 or 2");
                    }
                    break;

                case "APPEND":
                    break;

                default:
                    throw new InvalidFileFormatException("valid method in <custom_pdfs> is Replace or Append");
            }
        }

        public string GetDocumentPath(DocGenParam docGenParam)
        {
            var result = Path.Combine(_configuration.ResultPath, docGenParam.StatusRecord.GUID);
            _fileSystem.Directory.CreateDirectory(result);
            return result;
        }

        private static async Task<Stream> GetEobXmlStream(string eobXmlFilePath)
        {
            byte[] fileBytes;
            using (var fileStream = new FileStream(eobXmlFilePath, FileMode.Open, FileAccess.Read))
            {
                fileBytes = new byte[fileStream.Length];
                await fileStream.ReadAsync(fileBytes, 0, (int)fileStream.Length);
            }

            return new MemoryStream(fileBytes);
        }

        private const string REISSUE_SOURCE_MSG = @"The documents in this container were unloaded & reissued
{0}.

new_setxid: {1}
new_guid: {2}
";

        private const string REISSUE_TARGET_MSG = @"This document was created by the unload/re-issue process
on {0}.

original_setxid: {1}
original_guid: {2}
";
    }
}
