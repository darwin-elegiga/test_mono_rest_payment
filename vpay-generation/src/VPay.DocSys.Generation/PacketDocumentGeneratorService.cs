using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class PacketDocumentGeneratorService : IPacketDocumentGeneratorService
    {
        private static readonly VPaygeDocumentType[] HasCoverFlagTypes =
        {
            VPaygeDocumentType.VCard_Clear,
            VPaygeDocumentType.EFT,
            VPaygeDocumentType.SDA,
            VPaygeDocumentType.NoPay,
            VPaygeDocumentType.Check,
            VPaygeDocumentType.OptumAch
        };

        private readonly DocSysSettings _configuration;
        private readonly IPdfTools _pdfTools;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<PacketDocumentGeneratorService> _logger;

        public PacketDocumentGeneratorService(
            DocSysSettings configuration,
            IPdfTools pdfTools,
            IFileSystem fileSystem,
            ILogger<PacketDocumentGeneratorService> logger)
        {
            ThrowIf.IsNull(configuration, nameof(configuration));
            ThrowIf.IsNull(logger, nameof(logger));
            ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ThrowIf.IsNull(pdfTools, nameof(pdfTools));

            _configuration = configuration;
            _pdfTools = pdfTools;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<IEnumerable<PacketGeneratedFile>> GeneratePacketDocuments(IEnumerable<PacketDocument> packetDocs, DocGenParamInternal param)
        {
            var result = await Task.WhenAll(packetDocs.Select(x => GeneratePacketDocInternal(x, param)));
            return result.SelectMany(n => n).Where(_ => _ != null); // remove null values
        }

        private async Task<IEnumerable<PacketGeneratedFile>> GeneratePacketDocInternal(PacketDocument packetDoc, DocGenParamInternal param)
        {
            var docGenParam = param.DocGenParam;
            List<PacketGeneratedFile> allDocs = new List<PacketGeneratedFile>();
            PacketGeneratedFile result = GetPacketInfo(packetDoc, param);

            List<string> combineFiles = new List<string>();

            foreach (var docNeeded in packetDoc.DocumentsNeeded)
            {
                switch (docNeeded)
                {
                    case PacketIncludedDocumentType.EOB:
                        var eobFileName = param.GetSaveFileName(Constants.FileExtensions.EobPdf);

                        _logger.LogInformation("Add to Packet, EOB file = {FileName}", eobFileName);

                        if (!_fileSystem.File.Exists(eobFileName))
                        {
                            throw new FileNotFoundException("Could not locate EOB file", eobFileName);
                        }

                        combineFiles.Add(eobFileName);

                        if (packetDoc.ForceEobToEvenPages)
                        {
                            _logger.LogInformation("Force EOB to be Even Pages, EOB file = {FileName}", eobFileName);
                            if (string.IsNullOrWhiteSpace(_configuration.WhiteBoxPdf))
                            {
                                throw new ArgumentException($"{nameof(packetDoc.ForceEobToEvenPages)} cannot be true, if DocSysOperations. '{nameof(_configuration.WhiteBoxPdf)}' is null or empty", nameof(packetDoc));
                            }

                            var pdfPageCount = await _pdfTools.GetNumberOfPagesAsync(eobFileName);
                            _logger.LogInformation("EOB page count " + pdfPageCount + ", EOB file = {FileName}", eobFileName);
                            if (pdfPageCount % 2 == 1)
                            {
                                _logger.LogInformation("Add to Packet, Blank Page = {FileName}", _configuration.WhiteBoxPdf);
                                combineFiles.Add(_configuration.WhiteBoxPdf);
                            }
                        }
                        break;

                    case PacketIncludedDocumentType.CustomPdf:
                        foreach (var pdf in packetDoc.CustomPdfs)
                        {
                            _logger.LogInformation("Add to Packet, Custom Pdf = {FileName}", pdf);

                            if (!_fileSystem.File.Exists(pdf))
                            {
                                throw new FileNotFoundException("Could not locate CustomPdf file", pdf);
                            }
                            combineFiles.Add(pdf);
                        }
                        break;

                    case PacketIncludedDocumentType.Cover:
                        var vpaygeFileName = param.GetVPaygeFileName(packetDoc.DocumentType, docGenParam.IsRedCard);

                        _logger.LogInformation("Add to Packet, VPayge = {FileName}", vpaygeFileName.Replace(Environment.NewLine, ""));

                        if (!_fileSystem.File.Exists(vpaygeFileName))
                        {
                            throw new FileNotFoundException("Could not locate VPayge Cover file", vpaygeFileName);
                        }

                        combineFiles.Add(vpaygeFileName);
                        break;
                }
            }

            // only return cover flag packet marker
            // points to already created cover file
            // actual eob is given via 02s in .inv.drv and .tar file
            // and not generated / stapled
            if (docGenParam.IsRedCard)
            {
                return new List<PacketGeneratedFile>(){ GeneratePacketMarker(
                         result.IncludedVPaygeDocumentType,
                         combineFiles[0], // should always be the cover pdf
                         param.DocGenParam.StatusRecord.TXID) };
            }

            //merge combine pdfs using the combine method into fileName
            if (!await _pdfTools.MergeAsync(result.FileName, combineFiles))
            {
                _logger.LogError("Could not Merge files to {FileName}", result.FileName);
                return null;
            }

            if (File.Exists(result.FileName))
            {
                result.PageCount = await _pdfTools.GetNumberOfPagesAsync(result.FileName);
            }
            allDocs.Add(result);
            //RedCard does their own masking.
            if (!docGenParam.IsRedCard && (docGenParam.StatusRecord.PAYTYPE == "CLCHK" ||
                                            docGenParam.StatusRecord.PAYTYPE == "CLCCK"))
            {
                result.FileDetailType = FileDetailType.Limited;
                //We make a masked copy of the check when the vpayge is created
                //So if packet doc is just the check anyway we don't need another copy of the masked check
                if (combineFiles.Count > 1)
                {
                    string maskedFileName = result.FileName.Replace(Constants.FileExtensions.PacketCheckPdf, ".MASKED_cover_eob.pdf");
                    string tempFile;
                    File.Copy(result.FileName, tempFile = Path.Combine(Path.GetDirectoryName(result.FileName), Path.GetTempFileName()), true);
                    File.WriteAllBytes(tempFile, await _pdfTools.MicrStampAsync(File.ReadAllBytes(tempFile)));
                    //Since this can be called from API or consumer the PDF needs to be deployed to a common folder
                    string pdfPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources/Images/tiltedcopy.pdf");
                    await _pdfTools.StampAsync(maskedFileName, tempFile, pdfPath);
                    File.Delete(tempFile);
                    PacketGeneratedFile maskedCheck = new PacketGeneratedFile()
                    {
                        Description = "Masked " + result.Description,
                        FileName = maskedFileName,
                        FileDetailType = FileDetailType.Pdf,
                        IncludedVPaygeDocumentType = VPaygeDocumentType.Check,
                        PageCount = result.PageCount,
                        TransactionId = result.TransactionId
                    };
                    allDocs.Add(maskedCheck);
                }
            }
            // non-RC process does not create any cover flag packet marker
            return allDocs;
        }

        private PacketGeneratedFile GetPacketInfo(PacketDocument packetDoc, DocGenParamInternal param)
        {
            var eobDescription = packetDoc.DocumentsNeeded.Contains(PacketIncludedDocumentType.EOB) ? " + EOB" : "";
            switch (packetDoc.DocumentType)
            {
                case VPaygeDocumentType.VCard_Clear:
                    return new PacketGeneratedFile
                    {
                        FileDetailType = FileDetailType.Limited,
                        Description = $"Clear-PAN{eobDescription}",
                        TransactionId = param.DocGenParam.StatusRecord.TXID,
                        FileName = param.GetSaveFileName(Constants.FileExtensions.PacketVCardClearPdf),
                        IncludedVPaygeDocumentType = packetDoc.DocumentType
                    };

                case VPaygeDocumentType.VCard_Masked:
                    return new PacketGeneratedFile
                    {
                        FileDetailType = FileDetailType.Pdf,
                        Description = $"Masked-PAN{eobDescription}",
                        TransactionId = param.DocGenParam.StatusRecord.TXID,
                        FileName = param.GetSaveFileName(Constants.FileExtensions.PacketVCardMaskedPdf),
                        IncludedVPaygeDocumentType = packetDoc.DocumentType
                    };

                case VPaygeDocumentType.NoPay:
                    return new PacketGeneratedFile
                    {
                        FileDetailType = FileDetailType.Pdf,
                        Description = $"NPF{eobDescription}",
                        TransactionId = param.DocGenParam.StatusRecord.TXID,
                        FileName = param.GetSaveFileName(Constants.FileExtensions.PacketNoPayPdf),
                        IncludedVPaygeDocumentType = packetDoc.DocumentType
                    };

                case VPaygeDocumentType.EFT:
                case VPaygeDocumentType.Client_EFT:
                case VPaygeDocumentType.SDA:
                case VPaygeDocumentType.OptumAch:
                    return new PacketGeneratedFile
                    {
                        FileDetailType = FileDetailType.Pdf,
                        Description = $"EFT{eobDescription}",
                        TransactionId = param.DocGenParam.StatusRecord.TXID,
                        FileName = param.GetSaveFileName(Constants.FileExtensions.PacketEftPdf),
                        IncludedVPaygeDocumentType = packetDoc.DocumentType
                    };

                case VPaygeDocumentType.Check:
                case VPaygeDocumentType.Client_Check:
                    return new PacketGeneratedFile
                    {
                        FileDetailType = FileDetailType.Pdf,
                        Description = $"Check{eobDescription}",
                        TransactionId = param.DocGenParam.StatusRecord.TXID,
                        FileName = param.GetSaveFileName(Constants.FileExtensions.PacketCheckPdf),
                        IncludedVPaygeDocumentType = packetDoc.DocumentType
                    };
            }

            return null;
        }

        private PacketGeneratedFile GeneratePacketMarker(VPaygeDocumentType coverPageDocumentType, string coverpageFilePath, string TxId)
        {
            if (HasCoverFlagTypes.Contains(coverPageDocumentType))
            {
                _logger.LogInformation($"Add to Packet, VPayge = {VPaygeDocumentType.Has_Cover}");

                var fileName = VPaygeDocumentType.Has_Cover.GetAttribute<DocFileExtensionAttribute>().FileExtension;
                fileName = _fileSystem.CreateTextFile(Path.GetDirectoryName(coverpageFilePath), fileName, Path.GetFileName(coverpageFilePath));

                return new PacketGeneratedFile
                {
                    FileDetailType = FileDetailType.Hidden,
                    Description = VPaygeDocumentType.Has_Cover.GetDescription(),
                    TransactionId = TxId,
                    FileName = fileName,
                    IncludedVPaygeDocumentType = VPaygeDocumentType.Has_Cover,
                    PageCount = 1,
                };
            }

            return null;
        }
    }
}
