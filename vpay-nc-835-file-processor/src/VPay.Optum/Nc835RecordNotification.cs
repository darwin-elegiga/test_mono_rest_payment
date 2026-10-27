namespace VPay.Optum
{
    public record Nc835RecordNotification(long InboundFileRecordId, string TransactionId, string PaymentType, string FromRouting, string FromAccount, string ToRouting, string ToAccount, string PayerTaxId);
}
