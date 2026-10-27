using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    public class ReminderFaxMessageProcessor : IReminderFaxMessageProcessor
    {
        private readonly string _vpayFaxNumber;
        private readonly IReminderFaxPdfCreator _reminderFaxPdfCreator;
        private readonly IPdfTools _pdfTools;
        private readonly IStorageManager _storageManager;
        private readonly ITiffMessageQueuer _tiffMessageQueuer;
        private readonly ILogger _logger;

        public ReminderFaxMessageProcessor(
            ResourceManager bundleFaxResourceManager,
            IStorageManager storageManager,
            IReminderFaxPdfCreator reminderFaxPdfCreator,
            IPdfTools pdfTools,
            ITiffMessageQueuer tiffMessageQueuer,
            ILogger<ReminderFaxMessageProcessor> logger)
        {
            if (bundleFaxResourceManager == null) { throw new ArgumentNullException(nameof(bundleFaxResourceManager)); }

            _reminderFaxPdfCreator = reminderFaxPdfCreator ?? throw new ArgumentNullException(nameof(reminderFaxPdfCreator));
            _pdfTools = pdfTools ?? throw new ArgumentNullException(nameof(pdfTools));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _tiffMessageQueuer = tiffMessageQueuer ?? throw new ArgumentNullException(nameof(tiffMessageQueuer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _vpayFaxNumber = bundleFaxResourceManager.GetString("DefaultFromFax");
        }

        public async Task ProcessMessage(ReminderFaxMessage reminderFaxMessage)
        {
            using (_logger.BeginScope(reminderFaxMessage.PayorDetail.ToDictionary()))
            using (_logger.BeginScope(reminderFaxMessage.ReminderFax.ToDictionary()))
            using (_logger.BeginScope(new { reminderFaxMessage.FileName }.ToDictionary()))
            {
                try
                {
                    var coverSheetParams = new CoverSheetParameters(
                        reminderFaxMessage.ReminderFax,
                        reminderFaxMessage.PayorDetail,
                        _vpayFaxNumber)
                    {
                        PageCount = reminderFaxMessage.PageCount + 1 // +1 for itself
                    };

                    // create cover sheet
                    var coverSheetBytes = await CreateCoverSheet(coverSheetParams);

                    // staple files
                    var stapledPdf = await _pdfTools.MergeAsync(coverSheetBytes, reminderFaxMessage.DistributionPacketBytes);

                    // archive file
                    var (archiveFileId, pdfFileName) = await ArchiveReminderFax(stapledPdf, reminderFaxMessage.ReminderFax, reminderFaxMessage.FileName, coverSheetParams);

                    if (string.IsNullOrWhiteSpace(pdfFileName))
                    {
                        throw new ArgumentException($"Cannot obtain a valid {nameof(pdfFileName)} for {reminderFaxMessage.ReminderFax.SeTransactionId}");
                    }
                    var tempTransactionList = new List<long> { reminderFaxMessage.ReminderFax.SeTransactionId };
                    _tiffMessageQueuer.QueueTiffMessage(new TiffQueueMessage
                    {
                        ArchiveFileId = archiveFileId,
                        BillingEntity = reminderFaxMessage.ReminderFax.BillingEntity,
                        CorrespondenceId = reminderFaxMessage.ReminderFax.CorrespondenceId,
                        FaxNumber = reminderFaxMessage.ReminderFax.UseFaxNumber,
                        Origin = reminderFaxMessage.ReminderFax.OriginCode,
                        PayType = reminderFaxMessage.ReminderFax.PaymentTypeCode,
                        PdfFileName = pdfFileName,
                        PrintVendor = "", // not for drop to print
                        ReserveName = FaxReserveName.RFAX,
                        Priority = 100, // lowest possible value
                        Tpa = reminderFaxMessage.ReminderFax.TPACode,
                        TransactionIds = tempTransactionList,
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    throw;
                }
            }
        }

        private async Task<byte[]> CreateCoverSheet(CoverSheetParameters coverSheetParameters)
        {
            using (var ps = await _reminderFaxPdfCreator.CreatePdfStream(coverSheetParameters))
            {
                return ps.ToArray();
            }
        }

        private async Task<(long, string)> ArchiveReminderFax(byte[] stapledPdf, ReminderFax reminderFax, string fileName, CoverSheetParameters coverSheetParameters)
        {
            var description = $"{(coverSheetParameters.IsFinal ? "Final " : "")}Reminder Fax";
            var name = fileName.Replace(".pdf", $".{description.ToLower().Replace(' ', '_')}.pdf");
            var pdfFileStream = new MemoryStream(stapledPdf);

            var file = new GeneratedFileStream
            {
                FileDetailType = FileDetailType.Pdf,
                Description = description,
                PageCount = coverSheetParameters.PageCount,
                TransactionId = reminderFax.SeTransactionId.ToString("D20"),
                FileName = name,
                Stream = pdfFileStream,
                Length = pdfFileStream.Length
            };

            var archiveId = await _storageManager.CreateArchive(
                new FaxArchiveFile(reminderFax.TPACode, reminderFax.SeTransactionId, reminderFax.CorrespondenceId.ToString("D20")),
                new List<IGeneratedFile> { file },
                false);

            return (archiveId, file.FileName);
        }
    }
}
