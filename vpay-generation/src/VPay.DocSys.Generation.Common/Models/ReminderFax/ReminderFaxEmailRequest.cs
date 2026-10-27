using System;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Common
{
    public class ReminderFaxEmailRequest : IEmailRequest
    {
        private ReminderFaxEmailRequest()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }

        public ReminderFaxEmailRequest(ReminderFaxEmailSettings reminderFaxEmailSettings, DateTime startDate, DateTime endDate)
            : this()
        {
            ThrowIf.IsNull(reminderFaxEmailSettings, nameof(reminderFaxEmailSettings));
            ThrowIf.IsChildPropertyNullOrWhiteSpace(reminderFaxEmailSettings.FromEmail, nameof(reminderFaxEmailSettings.FromEmail));
            ThrowIf.IsChildPropertyNullOrWhiteSpace(reminderFaxEmailSettings.FromName, nameof(reminderFaxEmailSettings.FromName));
            ThrowIf.IsChildPropertyNullOrWhiteSpace(reminderFaxEmailSettings.ToEmail, nameof(reminderFaxEmailSettings.ToEmail));
            ThrowIf.IsChildPropertyNullOrWhiteSpace(reminderFaxEmailSettings.ToName, nameof(reminderFaxEmailSettings.ToName));

            EmailName = reminderFaxEmailSettings.ToName;
            EmailAddress = reminderFaxEmailSettings.ToEmail;
            FromName = reminderFaxEmailSettings.FromName;
            FromEmailAddress = reminderFaxEmailSettings.FromEmail;

            var dateString = startDate.Date == endDate.Date
                ? $"{startDate:yyyy-MM-dd}"
                : $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";

            EmailSubject = $"Reminder Fax Error Report for {dateString} ran on {CreatedAt:yyyy-MM-dd}";
        }

        public Guid Id { get; }
        public DateTime CreatedAt { get; }

        public string EmailEventNotificationUrl => null;
        public string VPTransId => null;
        public string ReferenceNumber => null;

        public string MessageHtml { get; set; }
        public string MessageText { get; set; }

        public string EmailType => "ReminderFaxErrorReport";
        public string EmailSubject { get; }

        public string EmailAddress { get; }
        public string EmailName { get; }

        public string FromEmailAddress { get; }
        public string FromName { get; }
    }
}
