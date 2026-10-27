using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public static class Inbound835SettingsLogScopeExtensions
    {
        /// <summary>
        /// Begins a logical operation scope including the values of members of <paramref name="message"/> and <paramref name="settings"/>.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public static IDisposable BeginScope(this ILogger logger, Inbound835FileMessage message, Inbound835FileServiceSettings settings)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            return logger.BeginScope(message.ToDictionary());
        }

        /// <summary>
        /// Begins a logical operation scope including the values of members of <paramref name="message"/> and <paramref name="settings"/>.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public static IDisposable BeginScope(this ILogger logger, Inbound835FileMessage message, AcknowledgmentFileSettings settings)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            return logger.BeginScope(Concat(message.ToDictionary(), settings.ToDictionary()));
        }

        /// <summary>
        /// Begins a logical operation scope including the values of members of <paramref name="message"/> and <paramref name="settings"/>.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public static IDisposable BeginScope(this ILogger logger, Inbound835FileMessage message, PaymentPostedFileSettings settings)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            return logger.BeginScope(Concat(message.ToDictionary(), settings.ToDictionary()));
        }

        /// <summary>
        /// Begins a logical operation scope including the values of members of <paramref name="message"/> and <paramref name="settings"/>.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public static IDisposable BeginScope(this ILogger logger, Inbound835FileMessage message, RemittanceAdviceFileSettings settings)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            return logger.BeginScope(Concat(message.ToDictionary(), settings.ToDictionary()));
        }

        private static IDictionary<string, object> ToDictionary(this Inbound835FileMessage message)
        {
            return new Dictionary<string, object>()
            {
                { "FilePath", message?.FilePath },
            };
        }

        private static IDictionary<string, object> ToDictionary(this RemittanceAdviceFileSettings settings)
        {
            return Concat(new Dictionary<string, object>()
            {
                { "RemittanceAdviceFileSettings_IgnoreMissingTransactions", settings?.IgnoreMissingTransactions },

            }, settings?.SendToAvaility.ToDictionary());
        }

        private static IDictionary<string, object> ToDictionary(this SendToAvailitySettings settings)
        {
            return Concat(new Dictionary<string, object>()
            {
                { "SendToAvailitySettings_Enabled", settings?.Enabled },

            }, settings?.TransferSettings.ToDictionary());
        }

        private static IDictionary<string, object> ToDictionary(this SendToAvailityTransferSettings settings)
        {
            return new Dictionary<string, object>()
            {
                { "SendToAvailityTransferSettings_Host", settings?.Host },
                { "SendToAvailityTransferSettings_UserName", settings?.UserName },
                { "SendToAvailityTransferSettings_UseRsaKey", settings?.UseRsaKey },
                { "SendToAvailityTransferSettings_TransferPath", settings?.TransferPath },
            };
        }

        private static IDictionary<string, object> ToDictionary(this AcknowledgmentFileSettings settings)
        {
            return Concat(new Dictionary<string, object>()
            {
                { "AcknowledgmentFileSettings_IgnoreMissingTransactions", settings?.IgnoreMissingTransactions },

            }, settings?.SendAckPmbMessage.ToDictionary());
        }

        private static IDictionary<string, object> ToDictionary(this SendAckPmbMessageSettings settings)
        {
            return new Dictionary<string, object>()
            {
                { "SendAckPmbMessageSettings_Enabled", settings?.Enabled },
                { "SendAckPmbMessageSettings_Retries", settings?.Retries },
                { "SendAckPmbMessageSettings_SleepBetweenRetries", settings?.SleepBetweenRetries },
            };
        }

        private static IDictionary<string, object> ToDictionary(this PaymentPostedFileSettings settings)
        {
            return Concat(new Dictionary<string, object>()
            {
                { "PaymentPostedFileSettings_IgnoreMissingTransactions", settings?.IgnoreMissingTransactions },

            }, settings?.iSeriesTransfer.ToDictionary());
        }

        private static IDictionary<string, object> ToDictionary(this iSeriesTransferSettings settings)
        {
            return new Dictionary<string, object>()
            {
                { "iSeriesTransferSettings_Enabled", settings?.Enabled },
                { "iSeriesTransferSettings_Host", settings?.Host },
                { "iSeriesTransferSettings_UserName", settings?.UserName },
                { "iSeriesTransferSettings_UseRsaKey", settings?.UseRsaKey },
                { "iSeriesTransferSettings_TransferPath", settings?.TransferPath },
            };
        }

        private static IDictionary<TKey, TValue> Concat<TKey, TValue>(IDictionary<TKey, TValue> dictionary, params IEnumerable<KeyValuePair<TKey, TValue>>[] items)
        {
            foreach(var kvp in items.Where(n => n != null).SelectMany(a => a))
            {
                dictionary.Add(kvp);
            }

            return dictionary;
        }
    }
}
