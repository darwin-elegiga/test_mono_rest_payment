using System;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Used for sending email via <see cref="IGreenArrowApiClient.SendEmail"/>
    /// </summary>
    public interface IEmailRequest
    {
        Guid Id { get; }
        DateTime CreatedAt { get; }
        string EmailEventNotificationUrl { get; }

        string VPTransId { get; }
        string ReferenceNumber { get; }

        string MessageText { get; }
        string MessageHtml { get; }

        string EmailType { get; }
        string EmailSubject { get; }

        string EmailAddress { get; }
        string EmailName { get; }

        string FromEmailAddress { get; }
        string FromName { get; }
    }

}
