using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Common.Models;

namespace VPay.DocSys.Generation;

public class EmailMessageXmlReader : IEmailMessageXmlReader
{
    private readonly ILogger<EmailMessageXmlReader> _logger;

    public EmailMessageXmlReader(ILogger<EmailMessageXmlReader> logger)
    {
        ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

        _logger = logger;
    }

    public EmailMessageData ReadXml(string xmlString)
    {
        var xml = xmlString.Replace("\u00B6", null);

        var xDocument = XDocument.Parse(xml);

        using (var xr = xDocument.CreateReader())
        {
            var notificationRequest = (NotificationRequestXmlDto)new XmlSerializer(typeof(NotificationRequestXmlDto)).Deserialize(xr);

            var emailMessageData = new EmailMessageData(notificationRequest)
            {
                Xml = xDocument
            };

            ValidateEmailMessage(emailMessageData);

            return emailMessageData;
        }
    }

    private void ValidateEmailMessage(EmailMessageData emailMessageData)
    {
        if (!"EMAIL".Equals(emailMessageData.Type, StringComparison.InvariantCultureIgnoreCase))
        {
            var message = $"Type: {emailMessageData.Type} is invalid, only supporting 'EMAIL'";

            _logger.LogAndThrowError(message);
        }
    }
}
