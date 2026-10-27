using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Common
{
    public class DistributionMessageBuilder : IDistributionMessageBuilder
    {
        private readonly ILogger<DistributionMessageBuilder> _logger;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;

        public DistributionMessageBuilder(
            Edi835ConfigurationCache edi835ConfigurationCache,
            ILogger<DistributionMessageBuilder> logger)
        {
            _edi835ConfigurationCache = edi835ConfigurationCache ?? throw new ArgumentNullException(nameof(edi835ConfigurationCache));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));
            _logger = logger;
        }

        public DocDistMsg CreateDistributionMessage(StatusFile statusFile, string statusFilePath, long statusFileId, bool isCustomSendBackTpa, int warningDelayInHours)
        {
            ArgumentHelpers.ThrowIf.IsNull(statusFile, nameof(statusFile));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(statusFile.Records, nameof(statusFile.Records));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(statusFilePath, nameof(statusFilePath));

            var firstRecord = statusFile.Records.First();

            var faxComplete = IsFaxComplete(statusFile);
            var nonFaxComplete = IsNonFaxComplete(statusFile, isCustomSendBackTpa);
            var customSendBackComplete = IsCustomSendBackComplete(statusFile, isCustomSendBackTpa);

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                { "BillingEntity", firstRecord.BILENT },
                { "Origin", firstRecord.ORIGIN },
                { "BatchSize", statusFile.Records.Count },
                { "FaxComplete", faxComplete },
                { "NonFaxComplete", nonFaxComplete },
                { "CustomSendBackComplete", customSendBackComplete },
            }))
            {
                _logger.LogInformation("Create Document Distribution Message.");

                return new DocDistMsg
                {
                    TPA = firstRecord.TPA,
                    BillingEntity = firstRecord.BILENT,
                    Origin = firstRecord.ORIGIN,
                    StatusFilePath = statusFilePath,
                    PaymentStatusFileId = statusFileId,
                    BatchSize = statusFile.Records.Count,
                    IsCustomSendBackTpa = isCustomSendBackTpa,
                    CreateDate = DateTime.Now,
                    FaxComplete = faxComplete,
                    NonFaxComplete = nonFaxComplete,
                    CustomSendBackComplete = customSendBackComplete,
                    NextStatusCheck = DateTime.Now.AddSeconds(20),
                    WarningTime = DateTime.Now.AddHours(warningDelayInHours),
                    WarningsIssued = 0,
                    Transactions = CreateDistributionMessages(statusFile.Records, firstRecord.TPA),
                    BoardUploadTimeStamp = statusFile.Header.FileUploadTimeStamp ?? statusFile.Header.RecordDate
                };
            }
        }

        private bool IsFaxComplete(StatusFile statusFile)
        {
            // true if no records are of dist type F or R
            return statusFile.Records.All(r => r.DIST != "F" && r.DIST != "R");
        }

        private bool IsNonFaxComplete(StatusFile statusFile, bool isCustomSendBackTpa)
        {
            // if isCustomSendBackTpa, NOT true if ANY records are NOT F, R or S
            if (isCustomSendBackTpa)
                return !statusFile.Records.Any(r => r.DIST != "F" && r.DIST != "R" && r.DIST != "S");

            // if not isCustomSendBackTpa, NOT true if ANY records are F or R
            return !statusFile.Records.Any(r => r.DIST != "F" && r.DIST != "R");
        }

        private bool IsCustomSendBackComplete(StatusFile statusFile, bool isCustomSendBackTpa)
        {
            // NOT true if isCustomSendBackTpa and ANY records are of dist type S
            return !(isCustomSendBackTpa && statusFile.Records.Any(r => r.DIST == "S"));
        }

        private List<DistTxnData> CreateDistributionMessages(List<StatusRecord> records, string tpa)
        {
            var priority = GetPriority(tpa);

            _logger.LogInformation("Use Priority {Priority} For {TPA}", priority, tpa);

            return records
                .Select(_ => new DistTxnData
                {
                    CorrespondenceId = _.CORRESP_ID,
                    FaxNumber = _.FAX_NUMBER,
                    Priority = priority,
                    TxnId = long.Parse(_.TXID.RemoveLeadingZeroes()),
                    PayType = _.PAYTYPE,
                    DistType = _.DIST[0],
                    PrintVendor = _.PRTVENDOR,
                    ClientPaymentId = _.USERKEY,
                    IsEdi835Needed =  _.IsEdi835Needed(_edi835ConfigurationCache)
                })
                .ToList();
        }

        /// <summary>
        /// Business Rules for setting priority for medical faxes, a smaller
        /// number means higher priority
        /// SWK - 25
        /// *   - 50
        /// </summary>
        /// <param name="tpa"></param>
        /// <returns>priority of medical fax</returns>
        private static int GetPriority(string tpa)
        {
            return tpa.Equals("SWK", StringComparison.InvariantCultureIgnoreCase)
                ? 25
                : 50;
        }
    }
}
