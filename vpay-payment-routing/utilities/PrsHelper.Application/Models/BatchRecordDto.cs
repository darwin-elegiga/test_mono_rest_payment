// See https://aka.ms/new-console-template for more information
internal class BatchRecordDto
{
    public string BatchId { get; set; }
    public string Payer835Id { get; set; }
    public int TotalPaymentCount { get; set; }
    public decimal TotalPaymentAmount { get; set; }
}
