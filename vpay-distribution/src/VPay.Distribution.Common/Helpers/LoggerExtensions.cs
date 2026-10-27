using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using VPay.DocSys.Common;
using DS = VPay.DocSys.Data;

namespace VPay.Distribution.Common.Helpers
{
    public static class LoggerExtensions
    {
        public static IDisposable CreateLogScope(this ILogger logger, DS.Edi835 message)
        {
            var dictionary = message.ToDictionaryExcept(nameof(DocDistMsg.Transactions));

            dictionary["OriginCode"] = message.OriginCode;
            dictionary["ClientCode"] = message.ClientCode;
            dictionary["TransactionId"] = message.TransactionId;
            dictionary["PaymentStatusFile"] = message.PaymentStatusFile;
            dictionary["Edi835Id"] = message.Edi835Id;
            dictionary["Edi835StatusCode"] = message.StatusCode;

            return logger.BeginScope(dictionary.SanitizeNewlines());
        }

        public static IDisposable CreateLogScope(this ILogger logger, DocDistMsg message)
        {
            var dictionary = message.ToDictionaryExcept(nameof(DocDistMsg.Transactions));

            dictionary["StatusFileId"] = message.PaymentStatusFileId;
            dictionary["OriginCode"] = message.Origin;
            dictionary["BillingEntityCode"] = message.BillingEntity;
            dictionary["ClientCode"] = message.TPA;
            dictionary["NextWarningTime"] = message.WarningTime;
            dictionary["Transactions"] = message.Transactions?.Count ?? 0;
            dictionary["IsRedCard"] = message.IsRedCard;

            return logger.BeginScope(dictionary.SanitizeNewlines());
        }

        public static IDisposable CreateLogScope(this ILogger logger, DistTxnData distTxnData)
        {
            var scope = distTxnData.ToDictionary();

            scope.Add("TransactionId", distTxnData.TxnId);

            var logScopeDict = scope.SanitizeNewlines();

            return logger.BeginScope(logScopeDict);
        }

        public static IDisposable CreateLogScope(this ILogger logger, EventPublishQueueMessage eventPublishRequest)
        {
            return logger.BeginScope(eventPublishRequest.ToDictionary().SanitizeNewlines());
        }

        public static IDisposable CreateLogScope(this ILogger logger, Edi835CreationMessage message)
        {
            var scope = message.ToDictionaryExcept(nameof(message.FetchPdfInNonProd)).SanitizeNewlines();
            return logger.BeginScope(scope);
        }

        /// <summary>
        /// Used with a <see langword="when"/> on a <see langword="try"/>/<see langword="catch"/> to log the error with scope
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool LogException(this ILogger logger, Exception e)
        {
            logger.LogException(e, null);

            return true;
        }

        private static Dictionary<string, object> SanitizeNewlines(this Dictionary<string, object> input)
        {
            var sanitized = new Dictionary<string, object>();

            foreach (var kvp in input)
            {
                sanitized.Add(kvp.Key.Replace(Environment.NewLine, ""), kvp.Value?.ToString().Replace(Environment.NewLine, ""));
            }

            return sanitized;
        }
    }
}
