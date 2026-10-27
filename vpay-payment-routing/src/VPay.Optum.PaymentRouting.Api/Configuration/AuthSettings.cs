namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    public class AuthSettings
    {
        public string Issuer { get; set; } = "VpayIssuer";

        public string SecretKey { get; set; } = "RestApiSecretKey";
    }
}
