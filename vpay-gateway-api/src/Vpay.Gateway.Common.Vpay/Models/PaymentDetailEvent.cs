using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vpay.Gateway.Common.Vpay.Models;
public class PaymentDetailEvent
{
    public long TransactionEventId { get; set; }
    public long TransactionId { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonDescription { get; set; }
    public string? Merchant { get; set; }
    public string? RequesterName { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime ProcessedDate { get; set; }
    public string? BatchNumber { get; set; }
}   
