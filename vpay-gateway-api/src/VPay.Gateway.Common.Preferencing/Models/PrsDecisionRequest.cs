using System.ComponentModel.DataAnnotations;

namespace VPay.Gateway.Common.Preferencing.Models;

public class PrsDecisionRequest
{
    public string OriginCode { get; set; }

    public string ClientCode { get; set; }

    public string BillingEntityCode { get; set; }

    public string TaxId { get; set; }

    public string ZipCode { get; set; }

    public decimal? Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }
}
