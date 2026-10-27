using System.Xml.Serialization;

namespace VPay.DocSys.Generation.Common
{
    [XmlRoot(ElementName = "notificationRequest")]
    public class NotificationRequestXmlDto
    {
        [XmlElement(ElementName = "control")]
        public ControlXmlDto Control { get; set; }

        [XmlElement(ElementName = "card")]
        public CardXmlDto Card { get; set; }

        [XmlElement(ElementName = "sendTo")]
        public SendToXmlDto SendTo { get; set; }
    }

    public class ControlXmlDto
    {
        [XmlElement(ElementName = "type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "clientCode")]
        public string ClientCode { get; set; }

        [XmlElement(ElementName = "Billing_Entity")]
        public string BillingEntity { get; set; }

        [XmlElement(ElementName = "application")]
        public string Application { get; set; }

        [XmlElement(ElementName = "notificationID")]
        public string NotificationId { get; set; }
    }

    public class SendToXmlDto
    {
        [XmlElement(ElementName = "fax")]
        public string Fax { get; set; }
    }

    public class CardXmlDto
    {
        [XmlElement(ElementName = "loadTransactionID")]
        public string LoadTransactionId { get; set; }
    }
}
