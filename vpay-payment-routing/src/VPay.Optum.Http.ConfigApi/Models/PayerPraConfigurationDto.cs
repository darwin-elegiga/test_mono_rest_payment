using System;

namespace VPay.Optum.Http.ConfigApi.Models;

public record PayerPraConfigurationDto
    (
        string OriginCode,
        string ClientCode,
        bool IsPayerPraEnabled,
        string RemitMethod,
        DateTime BeginDate,
        DateTime EndDate
    );
