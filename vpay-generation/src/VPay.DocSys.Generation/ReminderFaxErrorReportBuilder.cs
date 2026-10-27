using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class ReminderFaxErrorReportBuilder : IReminderFaxErrorReportBuilder
    {
        private readonly ILogger<ReminderFaxErrorReportBuilder> _logger;

        public ReminderFaxErrorReportBuilder(
            ILogger<ReminderFaxErrorReportBuilder> logger)
        {
            ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
        }

        public string BuildReport(IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>> reminderFaxErrorDictionary, int totalReminders)
        {
            ThrowIf.IsNull(reminderFaxErrorDictionary, nameof(reminderFaxErrorDictionary));

            var sb = new StringBuilder();

            var totalErrors = reminderFaxErrorDictionary.Sum(_ => _.Value.Count);

            if (totalErrors > 0)
            {
                sb.Append($"{totalErrors} of {totalReminders} reminder faxes failed.<br/><br/>");
            }

            foreach (ReminderFaxErrorType rfet in Enum.GetValues(typeof(ReminderFaxErrorType)))
            {
                var reminderFaxes = reminderFaxErrorDictionary.TryGetValueDefault(rfet, Array.Empty<ReminderFaxError>());

                if (reminderFaxes.Any())
                {
                    _logger.LogWarning("Found {Count} Reminder Faxes that are {ReminderFaxErrorType}", reminderFaxes.Count, rfet);

                    sb.Append($"The following {reminderFaxes.Count} Reminder Faxes could not be sent due to '{rfet.GetDescription()}':");
                    sb.AppendLine("<br/><br/>");

                    sb.Append("<table style='white-space: nowrap'>");
                    sb.AppendLine("<tr style='text-align: center'><th>Client Type</th><th>Reminder Type</th><th>Origin Code</th><th>TPA Code</th><th>Billing Entity</th><th>Payment Type Code</th><th>SE Transaction Id</th><th>Error Detail</th></tr>");

                    GetSortedValues(reminderFaxes)
                        .ToList()
                        .ForEach(_ => sb.AppendLine(GetLine(_)));

                    sb.Append("</table>");
                    sb.AppendLine("<br/>");
                }
            }

            return sb.ToString();
        }

        private IEnumerable<ReminderFaxError> GetSortedValues(IList<ReminderFaxError> reminderFaxeErrors)
        {
            return reminderFaxeErrors
                .OrderBy(_ => _.ClientType)
                .ThenBy(_ => _.ReminderType)
                .ThenBy(_ => _.OriginCode)
                .ThenBy(_ => _.ClientCode)
                .ThenBy(_ => _.BillingEntity)
                .ThenBy(_ => _.PaymentTypeCode)
                .ThenBy(_ => _.TransactionId);
        }

        private string GetLine(ReminderFaxError reminderFaxError)
        {
            return $"<tr><td>{reminderFaxError.ClientType}</td><td>{reminderFaxError.ReminderType}</td><td>{reminderFaxError.OriginCode}</td><td>{reminderFaxError.ClientCode}</td><td>{reminderFaxError.BillingEntity}</td><td>{reminderFaxError.PaymentTypeCode}</td><td style='text-align:right'>{reminderFaxError.TransactionId}</td><td>{reminderFaxError.ExceptionMessage}</td></tr>";
        }
    }
}
