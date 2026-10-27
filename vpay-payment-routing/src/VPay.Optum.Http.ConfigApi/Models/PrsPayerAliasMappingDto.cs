namespace VPay.Optum.Http.ConfigApi.Models;
public record PrsPayerAliasMappingDto
    (
        string PayerAlias,
        string OptumPayerId,
        string OriginCode,
        string ClientCode,
        string Client835Prefix
    );
