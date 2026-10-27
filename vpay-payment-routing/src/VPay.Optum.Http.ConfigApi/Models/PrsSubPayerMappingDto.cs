namespace VPay.Optum.Http.ConfigApi.Models
{
    public record PrsSubPayerMappingDto
    (
        string ClientCode,
        string BillingEntityCode,
        string OptumPayorId,
        string OptumSubPayorId
    );
}
