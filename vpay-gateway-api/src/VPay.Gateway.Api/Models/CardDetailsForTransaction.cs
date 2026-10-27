using static VPay.Gateway.Common.Loaf.GetCardDetails;

namespace VPay.Gateway.Api.Models;

public record CardDetailsForTransaction(
    string PaymentId,
    string TransactionId,
    string CreditCardNumber,
    DateTime ExpirationDate,
    string CVCCode,
    string AddressLine1,
    string AddressLine2,
    string ZipCode,
    decimal CardBalance,
    string TraceNumber) : Response(
        PaymentId,
        TransactionId,
        CreditCardNumber,
        ExpirationDate,
        CVCCode,
        AddressLine1,
        AddressLine2,
        ZipCode,
        CardBalance);
