using System;
using System.Xml.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class FaxMessageData
    {
        public FaxMessageData(NotificationRequestXmlDto notificationRequest)
        {
            ArgumentHelpers.ThrowIf.IsNull(notificationRequest, nameof(notificationRequest));
            ArgumentHelpers.ThrowIf.IsNull(notificationRequest.Control, nameof(notificationRequest.Control));
            ArgumentHelpers.ThrowIf.IsNull(notificationRequest.Card, nameof(notificationRequest.Card));
            ArgumentHelpers.ThrowIf.IsNull(notificationRequest.SendTo, nameof(notificationRequest.SendTo));

            Type = notificationRequest.Control.Type;
            Tpa = notificationRequest.Control.ClientCode;
            Application = notificationRequest.Control.Application;
            TransactionId = notificationRequest.Card.LoadTransactionId;
            CorrespondenceId = notificationRequest.Control.NotificationId;
            BillingEntity = notificationRequest.Control.BillingEntity;
            FaxNumber = notificationRequest.SendTo.Fax;
        }

        public XNode Xml { get; set; }

        public string Type { get; set; }
        public string Tpa { get; set; }
        public string Application { get; set; }
        public string TransactionId { get; set; }
        public string CorrespondenceId { get; set; }
        public string BillingEntity { get; set; }
        public string FaxNumber { get; set; }

        public string XslUrlPath => $"{Tpa}/xsl/{Tpa}.{Application.ToLower()}.{Type.ToLower()}.xsl";
    }
}
