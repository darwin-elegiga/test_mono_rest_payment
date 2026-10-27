using Refit;

namespace VPay.Distribution.Http.Abstractions
{
    public interface IRemitPartnerNetworkApi
    {
        // Refit OAuth Example.
        // Note: Static headers and dynamic headers can be combined.
        //[Headers("Accept: application/json", "Content-Type: application/json")]
        //[Post("api/my-endpoint")]
        //Task<bool> GetUser([Body] int id, [Header("Authorization")] string bearerToken);

        [Post("/api/v1/b2b-validate")]
        Task<(bool documentAccepted, string responseBody)> ValidateB2BRemittanceDocument(string remittanceData, [Header("Authorization")] string BearerToken, CancellationToken cancellationToken = default);

        [Post("/api/v1/c2b-validate")]
        Task<(bool documentAccepted, string responseBody)> ValidateC2BRemittanceDocument(string remittanceData, [Header("Authorization")] string BearerToken, CancellationToken cancellationToken = default);

        [Post("/api/v1/b2b-payments")]
        Task<(bool documentAccepted, string responseBody)> PostB2BRemittanceDocument(string remittanceData, [Header("Authorization")] string BearerToken, CancellationToken cancellationToken = default);

        [Post("/api/v1/c2b-payments")]
        Task<(bool documentAccepted, string responseBody)> PostC2BRemittanceDocument(string remittanceData, [Header("Authorization")] string BearerToken, CancellationToken cancellationToken = default);
    }
}
