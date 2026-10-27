using System.ComponentModel.DataAnnotations;

namespace VPay.Gateway.Api.Models;

public class GetPocDto
{
    public string BillingEntityId { get; set; }

    public string TaxId { get; set; }

    public string ZipCode { get; set; }

    public decimal? Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }
}
