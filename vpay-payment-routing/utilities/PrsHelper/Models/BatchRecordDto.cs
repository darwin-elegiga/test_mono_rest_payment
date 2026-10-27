// See https://aka.ms/new-console-template for more information
public class BatchRecordDto
{
    public string BatchId { get; set; } = default!;
    public string Payer835Id { get; set; } = default!;
    public int TotalPaymentCount { get; set; }
    public decimal TotalPaymentAmount { get; set; }
}
