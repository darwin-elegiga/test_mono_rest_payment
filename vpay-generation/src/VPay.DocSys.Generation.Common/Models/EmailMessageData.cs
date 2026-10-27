namespace VPay.DocSys.Generation.Common.Models;

public class EmailMessageData : FaxMessageData
{
    public EmailMessageData(NotificationRequestXmlDto notificationRequest) : base(notificationRequest)
    { }

    // email uses the same fax xsl to create vpayges
    public new string XslUrlPath => $"{Tpa}/xsl/{Tpa}.{Application.ToLower()}.fax.xsl";
}
