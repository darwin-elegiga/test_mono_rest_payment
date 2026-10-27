using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class ReminderFaxService : IReminderFaxService
    {
        private readonly IPlatformConfigurationApiClient _platformConfigurationApiClient;
        private readonly IParsingApiClient _parsingApiClient;
        private readonly IPayorDetailResolver _payorDetailResolver;
        private readonly IStorageManager _storageManager;
        private readonly IPdfTools _pdfTools;
        private readonly IReminderFaxMessageQueuer _reminderFaxMessageQueuer;
        private readonly IReminderFaxErrorReportBuilder _reminderFaxErrorReportBuilder;
        private readonly IGreenArrowApiClient _greenArrowApiClient;
        private readonly ReminderFaxEmailSettings _reminderFaxEmailSettings;
        private readonly ILogger<ReminderFaxService> _logger;

        public ReminderFaxService(
            IPlatformConfigurationApiClient platformConfigurationApiClient,
            IParsingApiClient parsingApiClient,
            IPayorDetailResolver payorDetailResolver,
            IStorageManager storageManager,
            IPdfTools pdfTools,
            IReminderFaxMessageQueuer reminderFaxMessageQueuer,
            IReminderFaxErrorReportBuilder reminderFaxErrorReportBuilder,
            IGreenArrowApiClient greenArrowApiClient,
            ReminderFaxEmailSettings reminderFaxEmailSettings,
            ILogger<ReminderFaxService> logger)
        {
            ThrowIf.IsNull(platformConfigurationApiClient, nameof(platformConfigurationApiClient));
            ThrowIf.IsNull(parsingApiClient, nameof(parsingApiClient));
            ThrowIf.IsNull(payorDetailResolver, nameof(payorDetailResolver));
            ThrowIf.IsNull(storageManager, nameof(storageManager));
            ThrowIf.IsNull(pdfTools, nameof(pdfTools));
            ThrowIf.IsNull(reminderFaxMessageQueuer, nameof(reminderFaxMessageQueuer));
            ThrowIf.IsNull(reminderFaxErrorReportBuilder, nameof(reminderFaxErrorReportBuilder));
            ThrowIf.IsNull(greenArrowApiClient, nameof(greenArrowApiClient));
            ThrowIf.IsNull(reminderFaxEmailSettings, nameof(reminderFaxEmailSettings));
            ThrowIf.IsNull(logger, nameof(logger));

            _platformConfigurationApiClient = platformConfigurationApiClient;
            _parsingApiClient = parsingApiClient;
            _payorDetailResolver = payorDetailResolver;
            _storageManager = storageManager;
            _pdfTools = pdfTools;
            _reminderFaxMessageQueuer = reminderFaxMessageQueuer;
            _reminderFaxErrorReportBuilder = reminderFaxErrorReportBuilder;
            _greenArrowApiClient = greenArrowApiClient;
            _reminderFaxEmailSettings = reminderFaxEmailSettings;
            _logger = logger;
        }

        public async Task ProcessReminderFaxes(ReminderFaxQueueMessage reminderFaxQueueMessage)
        {
            ThrowIf.IsNull(reminderFaxQueueMessage, nameof(reminderFaxQueueMessage));

            await ProcessReminderFaxes(
                reminderFaxQueueMessage.StartDate,
                reminderFaxQueueMessage.EndDate,
                reminderFaxQueueMessage.ReminderFaxes ?? Array.Empty<ReminderFax>()).ConfigureAwait(false);
        }

        public Task ProcessReminderFaxRequest(ReminderFaxRequest reminderFaxRequest)
        {
            ThrowIf.IsNull(reminderFaxRequest, nameof(reminderFaxRequest));

            var reminderFax = reminderFaxRequest.ToReminderFax();

            return ProcessReminderFaxes(DateTime.Now.Date, DateTime.Now.Date, new[] { reminderFax });
        }

        private async Task ProcessReminderFaxes(DateTime startDate, DateTime endDate, params ReminderFax[] reminderFaxes)
        {
            // remove all nulls
            var reminderFaxList = reminderFaxes
                .Where(_ => _ != null);

            if (!reminderFaxList.Any())
            {
                _logger.LogInformation("No Reminder Faxes to process");
                return;
            }

            var errors = new Dictionary<ReminderFaxErrorType, IList<ReminderFaxError>>
            {
                { ReminderFaxErrorType.MissingDistributionPacket, new List<ReminderFaxError>{ } },
                { ReminderFaxErrorType.MissingOtbInformation, new List<ReminderFaxError>{ } },
                { ReminderFaxErrorType.CannotResolvePayorName, new List<ReminderFaxError>{ } },
                { ReminderFaxErrorType.GenericError, new List<ReminderFaxError>{ } },
            };

            var rfGroups = reminderFaxList
                .GroupBy(_ => { (string TPACode, string BillingEntity) k = (_.TPACode, _.BillingEntity); return k; });

            foreach (var group in rfGroups)
            {
                await ProcessGroup(group, errors).ConfigureAwait(false);
            }

            await EmailErrorReport(errors, startDate, endDate, reminderFaxes.Length).ConfigureAwait(false);
        }

        private async Task ProcessGroup(
            IGrouping<(string TPACode, string BillingEntity), ReminderFax> group,
            Dictionary<ReminderFaxErrorType, IList<ReminderFaxError>> errors)
        {
            // get tpa/be info
            var otbDetail = await _platformConfigurationApiClient.GetOtbDetail(
                group.Key.TPACode,
                group.Key.BillingEntity).ConfigureAwait(false);

            // only happens if no OTB Details found from platform configuration
            if (otbDetail == null)
            {
                foreach (var reminderFax in group)
                {
                    _logger.LogWarning("No client details for TPA:{TPA}, BE:{BE} could be found, skipping reminder fax for TransactionId:{TransactionId}", group.Key.TPACode, group.Key.BillingEntity, reminderFax.SeTransactionId);
                }

                errors[ReminderFaxErrorType.MissingOtbInformation] = errors[ReminderFaxErrorType.MissingOtbInformation]
                    .Union(group.Select(_ => new ReminderFaxError(_)))
                    .ToList();

                return;
            }

            FieldDictionary otbFieldConfiguration = null;

            // only medical clients need to create a configuration field dictionary
            if (group.First().ClientType == ClientType.MEDICAL)
            {
                var configuration = await _parsingApiClient.GetXmlConfiguration(group.Key.TPACode, group.Key.BillingEntity).ConfigureAwait(false);
                otbFieldConfiguration = _payorDetailResolver.CreateFieldDictionary(otbDetail, configuration);
            }

            // reset the PayorDetail for each new tpa/be group
            PayorDetail payorDetail = null;

            foreach (var reminderFax in group)
            {
                try
                {
                    using (_logger.BeginScope(reminderFax.ToDictionary()))
                    using (_logger.BeginScope(otbDetail?.ToDictionary() ?? new Dictionary<string, dynamic>(0)))
                    using (_logger.BeginScope(new
                    {
                        TPA = reminderFax.TPACode,
                        ClientCode = reminderFax.TPACode,
                        BE = reminderFax.BillingEntity,
                        TransactionId = reminderFax.SeTransactionId,
                        PayType = reminderFax.PaymentTypeCode,
                        Origin = reminderFax.OriginCode,
                    }.ToDictionary()))
                    {
                        // if this is null or
                        // the resolution is EOB dependent
                        // we need to resolve again
                        if (payorDetail?.IsEobDependent ?? true)
                        {
                            payorDetail = await _payorDetailResolver.ResolvePayorDetails(
                                reminderFax.ClientType,
                                reminderFax.TPACode,
                                reminderFax.BillingEntity,
                                reminderFax.SeTransactionId,
                                otbDetail,
                                otbFieldConfiguration).ConfigureAwait(false);
                        }

                        // other PayorDetail fields are filled by the otbDetail object
                        // if this is null, then we could not resolve the name
                        if (payorDetail.PayorName == null)
                        {
                            _logger.LogWarning("No Payor Name for ClientType:{ClientType} TPA:{TPA}, BE:{BE} could be found, skipping reminder fax for TransactionId:{TransactionId}",
                                reminderFax.ClientType.ToString(),
                                reminderFax.TPACode,
                                reminderFax.BillingEntity,
                                reminderFax.SeTransactionId);

                            errors[ReminderFaxErrorType.CannotResolvePayorName].Add(new ReminderFaxError(reminderFax));

                            continue;
                        }

                        using (_logger.BeginScope(payorDetail.ToDictionary()))
                        {
                            // get distribution packet
                            var (distPacketBytes, distFileName, distPageCount) = await GetDistributionPacket(reminderFax.SeTransactionId, reminderFax.PaymentTypeCode).ConfigureAwait(false);

                            if (distPacketBytes == null)
                            {
                                _logger.LogWarning("No distribution packet found for TransactionId:{TransactionId}, PayType:{PayType}", reminderFax.SeTransactionId, reminderFax.PaymentTypeCode);

                                errors[ReminderFaxErrorType.MissingDistributionPacket].Add(new ReminderFaxError(reminderFax));

                                continue;
                            }

                            var payload = new ReminderFaxMessage
                            {
                                FileName = distFileName,
                                DistributionPacketBytes = distPacketBytes,
                                PageCount = distPageCount,
                                ReminderFax = reminderFax,
                                PayorDetail = payorDetail,
                            };

                            _reminderFaxMessageQueuer.QueueReminderFaxMessage(payload);
                        }
                    }
                }
                catch (Exception e) when (e.LogWithScope(_logger))
                {
                    errors[ReminderFaxErrorType.GenericError].Add(new ReminderFaxError(reminderFax, e));
                }
            }
        }

        private async Task<(byte[], string, int)> GetDistributionPacket(long transactionId, string paymentType)
        {
            using var distributionPacket = await _storageManager.GetDistributionPacketStream(transactionId, paymentType, false).ConfigureAwait(false);

            if (distributionPacket == null) { return (null, null, 0); }

            using var distPacketStream = distributionPacket.Open();
            using var ms = new MemoryStream();

            await distPacketStream.CopyToAsync(ms).ConfigureAwait(false);

            var pageCount = distributionPacket.PageCount;
            var bytes = ms.ToArray();

            if (pageCount <= 0)
            {
                pageCount = await _pdfTools.GetNumberOfPagesAsync(bytes).ConfigureAwait(false);
            }

            return (bytes, distributionPacket.FileName, pageCount);
        }

        private async Task EmailErrorReport(
            IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>> reminderFaxErrors,
            DateTime startDate,
            DateTime endDate,
            int totalReminders)
        {
            var message = _reminderFaxErrorReportBuilder.BuildReport(reminderFaxErrors, totalReminders);

            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogInformation("No Reminder Fax Errors");
                return;
            }

            var request = new ReminderFaxEmailRequest(_reminderFaxEmailSettings, startDate, endDate)
            {
                MessageHtml = message
            };

            await _greenArrowApiClient.SendEmail(request).ConfigureAwait(false);
        }
    }
}
