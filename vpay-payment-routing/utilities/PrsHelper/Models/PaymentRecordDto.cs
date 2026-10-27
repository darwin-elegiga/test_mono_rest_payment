// See https://aka.ms/new-console-template for more information
using System.Text.Json.Serialization;

public class PaymentRecordDto
{
    public string RemitMethod { get; set; } = default!;
    public string BatchID { get; set; } = default!;
    public string FileProcID { get; set; } = default!;
    public string PayerAlias { get; set; } = default!;
    public string TransactionType { get; set; } = default!;
    public string ProvTIN { get; set; } = default!;
    public string Payer835ID { get; set; } = default!;
    public string PayerSubpayerID { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    public string UnconsolPaymentNbr { get; set; } = default!;
    public string UconslPaymentAmount { get; set; } = default!;
    [JsonIgnore]
    public decimal PaymentAmount
    {
        get
        {
            decimal val = 0m;
            decimal.TryParse(UconslPaymentAmount, out val);
            return val;
        }
    }
    public string BlobStorageURI { get; set; } = default!;
    public string PaymentClob { get; set; } = default!;
}
/// <summary>
/// Used to "force" JSON to serialize the object correctly for endpoint
/// </summary>
public class PaymentRecordWrapper
{
    public PaymentRecordWrapper(PaymentRecordDto paymentRecord)
    {
        PaymentRecord = paymentRecord;
    }
    public PaymentRecordDto PaymentRecord { get; set; }
}
