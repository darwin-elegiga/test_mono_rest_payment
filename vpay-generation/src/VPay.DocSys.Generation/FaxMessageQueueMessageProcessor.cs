using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;
using VPay.Queuing;

namespace VPay.DocSys.Generation
{
    public class FaxMessageQueueMessageProcessor : IFaxMessageQueueMessageProcessor
    {
        private readonly IQueueService _queueService;
        private readonly IFaxMessageService _faxMessageService;
        private readonly IFaxMessageXmlReader _faxMessageXmlReader;
        private readonly QueueConfigurationSettings _correspondenceQueueConfigurationSettings;
        private readonly ITiffMessageQueuer _tiffMessageQueuer;
        private readonly ILogger _logger;

        public FaxMessageQueueMessageProcessor(
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            IFaxMessageService faxMessageService,
            IFaxMessageXmlReader faxMessageXmlReader,
            ITiffMessageQueuer tiffMessageQueuer,
            ILogger<FaxMessageQueueMessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(queueConfigurations, nameof(queueConfigurations));
            ArgumentHelpers.ThrowIf.IsChildPropertyNull(queueConfigurations.CorrespondenceQueue, nameof(queueConfigurations), nameof(queueConfigurations.CorrespondenceQueue));

            ArgumentHelpers.ThrowIf.IsNull(queueService, nameof(queueService));
            ArgumentHelpers.ThrowIf.IsNull(tiffMessageQueuer, nameof(tiffMessageQueuer));
            ArgumentHelpers.ThrowIf.IsNull(faxMessageService, nameof(faxMessageService));
            ArgumentHelpers.ThrowIf.IsNull(faxMessageXmlReader, nameof(faxMessageXmlReader));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _correspondenceQueueConfigurationSettings = queueConfigurations.CorrespondenceQueue;

            _queueService = queueService;
            _faxMessageService = faxMessageService;
            _faxMessageXmlReader = faxMessageXmlReader;
            _tiffMessageQueuer = tiffMessageQueuer;
            _logger = logger;
        }

        public async Task ProcessMessageString(string message)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message, nameof(message));

            var payloadString = message.Substring(275);
            var jsonDictionary = JsonConvert.DeserializeObject<JObject>(payloadString).ToDictionaryExcept("xml");

            using (_logger.BeginScope(jsonDictionary))
            {
                try
                {
                    var pmbIndex = ParseFaxMessageIndex(message);

                    switch (pmbIndex)
                    {
                        case FaxMessageIndex.CreateFax:
                            await ProcessFaxMessageMessage(payloadString, FaxMessageQueueMessage.ParsePriority(message));
                            break;

                        case FaxMessageIndex.CreateEftFax:
                            await ProcessEftFaxMessage(payloadString, FaxMessageQueueMessage.ParsePriority(message));
                            break;

                        default:
                            throw new Exception($"Invalid Index {pmbIndex} in Message Header: {message.Substring(0, 275).Trim()}");
                    }
                }
                catch (Exception)
                {
                    var payload = FaxMessageCorrespondencePayload.CreateErrorPayload(jsonDictionary);
                    var correspondenceMessage = new FaxMessageCorrespondenceMessage(payload);

                    _queueService.PublishQueueMessage(_correspondenceQueueConfigurationSettings, correspondenceMessage.ToString());

                    throw;
                }
            }
        }

        private static FaxMessageIndex ParseFaxMessageIndex(string pmbMessage) => (FaxMessageIndex)Enum.Parse(typeof(FaxMessageIndex), pmbMessage.Substring(89, 6).Trim(), true);

        private async Task ProcessFaxMessageMessage(string payloadString, byte priority)
        {
            var payload = JsonConvert.DeserializeObject<FaxMessagePayload>(payloadString);
            var faxMessageXmlString = Encoding.UTF8.GetString(Convert.FromBase64String(payload.VPaygeXmlBase64));
            var faxMessageXml = _faxMessageXmlReader.ReadXml(faxMessageXmlString);

            using (_logger.BeginScope(faxMessageXml.ToDictionaryExcept(nameof(faxMessageXml.Xml))))
            {
                var archiveFile = await _faxMessageService.CreateArchiveForWarrantyXmlString(faxMessageXmlString);

                PublishCorrespondenceMessage(payload);
                PublishTiffMessage(payload, priority, FaxReserveName.WARRANTY, faxMessageXml, archiveFile);
            }
        }

        private async Task ProcessEftFaxMessage(string payloadString, byte priority)
        {
            var payload = JsonConvert.DeserializeObject<FaxMessagePayload>(payloadString);
            var faxMessageXmlString = Encoding.UTF8.GetString(Convert.FromBase64String(payload.VPaygeXmlBase64));
            var faxMessageXml = _faxMessageXmlReader.ReadXml(faxMessageXmlString);

            using (_logger.BeginScope(faxMessageXml.ToDictionaryExcept(nameof(faxMessageXml.Xml))))
            {
                var archiveFile = await _faxMessageService.CreateArchiveForEftFaxXmlString(faxMessageXmlString);

                PublishTiffMessage(payload, priority, FaxReserveName.VPFEE, faxMessageXml, archiveFile);
            }
        }

        private void PublishCorrespondenceMessage(FaxMessagePayload payload)
        {
            var message = new FaxMessageCorrespondenceMessage(new FaxMessageCorrespondencePayload
            {
                CorrespondenceId = payload.CorrespondenceId.ToString(),
                Status = "GENERATED",
                FaxNumber = payload.FaxNumber,
                TransactionIds = payload.TransactionIds.Select(p => p.ToString()).ToArray(),
            });

            _queueService.PublishQueueMessage(_correspondenceQueueConfigurationSettings, message.ToString());
            _logger.LogInformation("Published PMB Message for Warranty Fax Generated");
        }

        private void PublishTiffMessage(
            FaxMessagePayload payload,
            byte priority,
            FaxReserveName faxReserveName,
            FaxMessageData warrantyFaxData,
            IArchiveFile archiveFile)
        {
            var message = new TiffQueueMessage
            {
                CorrespondenceId = payload.CorrespondenceId,
                FaxNumber = payload.FaxNumber,
                PayType = payload.PaymentType,
                TransactionIds = payload.TransactionIds,
                Origin = payload.Origin,

                // nothing coming from the pmb fax queue will drop to print
                PrintVendor = "",

                Priority = priority,

                BillingEntity = warrantyFaxData.BillingEntity,
                Tpa = warrantyFaxData.Tpa,

                ArchiveFileId = archiveFile.ArchiveFileId,
                PdfFileName = GetPdfFileName(archiveFile),

                ReserveName = faxReserveName,
            };

            _tiffMessageQueuer.QueueTiffMessage(message);
        }

        private static string GetPdfFileName(IArchiveFile archiveFile)
        {
            try
            {
                // there should be only one file of this type for a fax archive
                return archiveFile.ArchiveDetails
                    .Single(_ => _.FileDetailType == FileDetailType.FaxDocument)
                    .FileName;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Could not find Fax Document in {archiveFile.GetFullFilePath()}", e);
            }
        }
    }
}
