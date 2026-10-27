using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Common
{
    public class DocumentTransactionMessageListBuilder : IDocumentTransactionMessageListBuilder
    {
        private static readonly string[] NonCheckPayTypes =
        {
            PayType.CLEFT, PayType.CLNPF, PayType.CLVCP, PayType.CLCCK, PayType.CLCFT, PayType.CLSDA, PayType.OPACH
        };

        private readonly ISubFileParserService _subFileParserService;
        private readonly ICheckFileParser _checkFileParser;
        private readonly IOtbService _otbService;
        private readonly ILogger<DocumentTransactionMessageListBuilder> _logger;

        public DocumentTransactionMessageListBuilder(
            ISubFileParserService subFileParserService,
            ICheckFileParser checkFileParser,
            IOtbService otbService,
            ILogger<DocumentTransactionMessageListBuilder> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(subFileParserService, nameof(subFileParserService));
            ArgumentHelpers.ThrowIf.IsNull(checkFileParser, nameof(checkFileParser));
            ArgumentHelpers.ThrowIf.IsNull(otbService, nameof(otbService));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _subFileParserService = subFileParserService;
            _checkFileParser = checkFileParser;
            _otbService = otbService;
            _logger = logger;
        }

        public async Task<List<DocumentTransactionMessage>> BuildMessagesAsync(
            DocSysConfiguration tpaConfig,
            StatusFile statusFile,
            string statusFilePath,
            IList<long> transactionIds,
            bool isRegenRequest,
            bool isReIssue,
            bool isCustomSendBackTpa,
            bool isRedCard)
        {
            ArgumentHelpers.ThrowIf.IsNull(tpaConfig, nameof(tpaConfig));
            ArgumentHelpers.ThrowIf.IsNull(statusFile, nameof(statusFile));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(statusFilePath, nameof(statusFilePath));

            var checkFilePath = statusFilePath.Replace(".sts", ".xml");
            var subFilePath = statusFilePath.Replace(".sts", ".sub");
            var tpaConfigJson = JsonConvert.SerializeObject(tpaConfig);

            var messages =
                (from record in statusFile.Records
                 where transactionIds.Any(t => t == long.Parse(record.TXID.RemoveLeadingZeroes())) && record.DESC != "DENIED"
                 select new DocumentTransactionMessage
                 {
                     //Re-issue uses EOB from the original transaction
                     GenerateEOB = !isReIssue,

                     StatusFilePath = statusFilePath,
                     RegeneratePackage = isRegenRequest,
                     InputFile = statusFile.Header.InputFile,
                     TransactionId = record.TXID,
                     TPA = record.TPA,
                     IsRedCard = isRedCard,
                     GUID = record.GUID,
                     BillingEntity = record.BILENT,
                     CDate = statusFile.Header.CDate,
                     StatusRecord = record,
                     ConfigData = tpaConfigJson,
                 })
                .ToList();
            _logger.LogInformation("STS Parse Complete.");

            SetCheckInfo(messages, checkFilePath, isCustomSendBackTpa, isRedCard);

            SetSubstituteDataInfo(messages, subFilePath);

            await SetOtbData(messages).ConfigureAwait(false);

            SetMissingDistributionType(messages);

            return messages;
        }

        private void SetCheckInfo(IList<DocumentTransactionMessage> messages, string checkFilePath, bool isCustomSendBackTpa, bool isRedCard)
        {
            _logger.LogInformation("Check file Started.");

            // will only allow unique transaction ids
            var checkInfoMessages = new HashSet<DocumentTransactionMessage>(new DocumentTransactionMessageEqualityComparer());

            // process for non-redcard with no check pay types
            if (!isRedCard)
            {
                checkInfoMessages.UnionWith(messages.Where(m => !NonCheckPayTypes.Contains(m.StatusRecord.PAYTYPE)));
            }

            // process for custom send backs, for only dist type SendBack
            if (isCustomSendBackTpa)
            {
                checkInfoMessages.UnionWith(messages.Where(m => m.StatusRecord.DIST == DistType.SendBack));
            }

            if (checkInfoMessages.Any())
            {
                var checkInfo = _checkFileParser.ParseCheckFileAsync(checkFilePath, isCustomSendBackTpa, isRedCard);

                foreach (var checkInfoMessage in checkInfoMessages)
                {
                    var check = checkInfo
                        .SingleOrDefault(i => long.Parse(i.SETransactionId) == long.Parse(checkInfoMessage.TransactionId));

                    if (check == null)
                    {
                        _logger.LogError($"Could not find corresponding check for status record with transaction id: {checkInfoMessage.TransactionId}");
                    }
                    else
                    {
                        checkInfoMessage.CheckInfo = check.ToJsonStringSnakeCase();
                        checkInfoMessage.IsCustomSendBack = isCustomSendBackTpa && checkInfoMessage.StatusRecord.DIST == DistType.SendBack;
                    }
                }

                _logger.LogInformation("Check file Complete.");
            }
            else
            {
                _logger.LogInformation("No check file required.");
            }
        }

        private void SetSubstituteDataInfo(IEnumerable<DocumentTransactionMessage> messages, string subFilePath)
        {
            _logger.LogInformation("Sub file Started.");

            var sendBackMessages = messages
                .Where(x => x.StatusRecord.DIST == DistType.SendBack)
                .ToList();

            if (sendBackMessages.Any())
            {
                var subRecords = _subFileParserService.ParseFile(subFilePath);

                foreach (var message in sendBackMessages)
                {
                    message.SubstituteDataInfo = subRecords[message.TransactionId.RemoveLeadingZeroes()];
                }

                _logger.LogInformation("Sub file Complete.");
            }
            else
            {
                _logger.LogInformation("No Sub file required.");
            }
        }

        private async Task SetOtbData(IEnumerable<DocumentTransactionMessage> messages)
        {
            var clientCode = messages.First().TPA.ToUpper();

            var beCodes = messages
                .Select(rec => rec.BillingEntity)
                .Distinct()
                .ToArray();

            var records = await _otbService.GetOtbRecordsAsync(clientCode, beCodes).ConfigureAwait(false);

            foreach (var msg in messages)
            {
                msg.OTBData = JsonConvert.SerializeObject(records[msg.BillingEntity]);
            }

            _logger.LogInformation("OTB Data Parse Complete.");
        }

        private void SetMissingDistributionType(IEnumerable<DocumentTransactionMessage> messages)
        {
            foreach (var msg in messages.Where(_ => string.IsNullOrEmpty(_.StatusRecord.DIST)))
            {
                _logger.LogInformation($"Update missing DIST value for status record guid: {msg.StatusRecord.GUID}");

                msg.StatusRecord.DIST = "M";
            }
        }

        /// <summary>
        /// This is a quick and dirty way to get distinct <see cref="DocumentTransactionMessage"/> by TransactionIds
        /// </summary>
        /// <remarks>
        /// This class is kept private and internal to disallow abuse
        /// </remarks>
        private class DocumentTransactionMessageEqualityComparer : IEqualityComparer<DocumentTransactionMessage>
        {
            public bool Equals(DocumentTransactionMessage x, DocumentTransactionMessage y)
            {
                return x?.TransactionId.Equals(y?.TransactionId) ?? false;
            }

            public int GetHashCode(DocumentTransactionMessage obj)
            {
                return obj.TransactionId.GetHashCode();
            }
        }
    }
}
