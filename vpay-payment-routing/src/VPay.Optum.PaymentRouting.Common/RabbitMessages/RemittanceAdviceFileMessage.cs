namespace VPay.Optum.PaymentRouting.Common.RabbitMessages
{
    public record RemittanceAdviceFileMessage(
        string Payer835Id,
        string InboundZipfile,
        string UserKey,
        string PayerAlias,
        string OriginCode,
        string ClientCode,
        string BillingEntityCode,
        string RemitMethod,
        string PdfExtractPath,
        string CreatedBy);
}
