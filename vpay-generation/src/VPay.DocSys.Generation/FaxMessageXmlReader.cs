using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation
{
    public class FaxMessageXmlReader : IFaxMessageXmlReader
    {
        private readonly ILogger<FaxMessageXmlReader> _logger;

        public FaxMessageXmlReader(
            ILogger<FaxMessageXmlReader> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
        }

        public FaxMessageData ReadXml(string xmlString)
        {
            _logger.LogInformation("Removing pilcrows from xml");
            var xml = xmlString.Replace("\u00B6", null);

            var xDocument = XDocument.Parse(xml);

            using (var xr = xDocument.CreateReader())
            {
                var notificationRequest = (NotificationRequestXmlDto)new XmlSerializer(typeof(NotificationRequestXmlDto)).Deserialize(xr);

                var faxMessageData = new FaxMessageData(notificationRequest)
                {
                    Xml = xDocument
                };

                ValidateFaxMessage(faxMessageData);

                return faxMessageData;
            }
        }

        private void ValidateFaxMessage(FaxMessageData faxMessageData)
        {

            if (!"FAX".Equals(faxMessageData.Type, StringComparison.InvariantCultureIgnoreCase))
            {
                var message = $"Type: {faxMessageData.Type} is invalid, only supporting 'FAX'";

                _logger.LogAndThrowError(message);
            }
        }
    }
}
