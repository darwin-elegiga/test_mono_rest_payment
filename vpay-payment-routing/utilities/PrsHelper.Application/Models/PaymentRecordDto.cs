// See https://aka.ms/new-console-template for more information
internal class PaymentRecordDto
{
    public string RemitMethod { get; internal set; } = default!;
    public string BatchID { get; internal set; } = default!;
    public string FileProcID { get; internal set; } = default!;
    public string PayerAlias { get; internal set; } = default!;
    public string TransactionType { get; internal set; } = default!;
    public string ProvTIN { get; internal set; } = default!;
    public string Payer835ID { get; internal set; } = default!;
    public string PayerSubpayerID { get; internal set; } = default!;
    public string ZipCode { get; internal set; } = default!;
    public string UnconsolPaymentNbr { get; internal set; } = default!;
    public string UconslPaymentAmount { get; internal set; } = default!;
    public string BlobStorageURI { get; internal set; } = default!;
    public string PaymentClob { get; internal set; } = default!;
}
/// <summary>
/// Used to "force" JSON to serialize the object correctly for endpoint
/// </summary>
internal class PaymentRecordWrapper {
    public PaymentRecordWrapper(PaymentRecordDto paymentRecord)
    {
        PaymentRecord = paymentRecord;
    }
    public PaymentRecordDto PaymentRecord { get; set; }
}
