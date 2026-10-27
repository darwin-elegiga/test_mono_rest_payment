using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.Extensions.Caching.Memory;
using VPay.Gateway.Common.UserManagement;
using VPay.Gateway.Common.UserManagement.Models;

namespace VPay.Gateway.Identity;

public class GetValidatedClientQuery : IGetValidatedClientQuery
{
    private readonly IUserManagementApi _api;
    private readonly ILogger<GetValidatedClientQuery> _logger;
    private readonly IMemoryCache<ValidateServiceResult?> _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public GetValidatedClientQuery(IUserManagementApi api, IMemoryCache<ValidateServiceResult?> cache, IConfiguration configuration, ILogger<GetValidatedClientQuery> logger)
    {
        _api = api;
        _cache = cache;
        _logger = logger;

        var cacheTime = configuration["ValidateUserCacheTime"];
        if (!int.TryParse(cacheTime, out var cacheTimeVal))
        {
            cacheTimeVal = 1;
        }

        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheTimeVal));
    }

    public async Task<ValidateServiceResult?> GetUser(string clientId, string clientSecret)
    {
        var cacheKey = HashValue($"{clientId}:{clientSecret}");

        return await _cache.GetOrCreateAsync(cacheKey,
            async () => await InternalGetUser(clientId, clientSecret),
            _cacheOptions,
            false);
    }

    private static string HashValue(string value)
    {
        using var sha256 = HashAlgorithm.Create(nameof(SHA256));
        var bytes = sha256!.ComputeHash(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes);
    }

    private async Task<ValidateServiceResult?> InternalGetUser(string clientId, string clientSecret)
    {
        try
        {
            // Uncomment to run in Local Development environment without User Management API running in DEV.
            // This will be used when running in local development environment and the API throws error
            // "Socket Exception: No Route to Host". (Also uncomment the section under Helpers)
            //if (IsLocalEnvironment())
            //{
            //    return GetLocalUser();
            //}

            var result = await _api.ValidateUser(new ValidateServiceRequest(clientId, clientSecret), default)
                .ConfigureAwait(false);

            if (result.IsSuccessStatusCode)
            {
                return result.Content;
            }

            if ((int)result.StatusCode >= 500 && (int)result.StatusCode < 600)
            {
                // use this to throw error
                await result.EnsureSuccessStatusCodeAsync().ConfigureAwait(false);
            }

            using (_logger.BeginScope(new Dictionary<string, object> { ["ClientId"] = clientId }))
            {
                _logger.LogWarning("Login Failed with a status code of {StatusCode}", result.StatusCode);
            }

            return null;
        }
        catch (Exception ex)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["ClientId"] = clientId }))
            {
                // When running in local development environment and the API throws error
                // "Socket Exception: No Route to Host"
                // a) check if User Management API is running in DEV, or
                // b) shutdown the docker containers and restart VS or restart the computer.
                _logger.LogError(ex, "Unable to verify user credentials");
            }
            throw;
        }
    }

    #region Helpers
    /*
    private static bool IsLocalEnvironment()
    {
        // Check if running in local enviroment
        var localEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return string.Equals(localEnv, "LocalDevelopment", StringComparison.OrdinalIgnoreCase);
    }

    private static ValidateServiceResult? GetLocalUser()
    {
        // Simulate user in LocalDevelopment environment
        const string ValidatedUser =
            """
            {
              "userId": 2,
              "displayName": "VPA1",
              "permissions": [
                {
                  "id": 2000,
                  "name": "ViewAllDocuments",
                  "displayName": "View All Documents"
                },
                {
                  "id": 2001,
                  "name": "GuidewireEnrollment",
                  "displayName": "Guidewire Enrollment"
                },
                {
                  "id": 2006,
                  "name": "Void Payment",
                  "displayName": "Void Payment"
                },
                {
                  "id": 2007,
                  "name": "Card Details",
                  "displayName": "Card Details"
                },
                {
                  "id": 2009,
                  "name": "PartnerEnrollment",
                  "displayName": "Partner Enrollment"
                },
                {
                  "id": 2010,
                  "name": "Reject Payment",
                  "displayName": "Reject Payment"
                },
                {
                  "id": 2011,
                  "name": "Monetization",
                  "displayName": "Monetization"
                },
                {
                  "id": 2013,
                  "name": "B2C Expire Bank Account",
                  "displayName": "B2C Expire Bank Account"
                },
                {
                  "id": 2014,
                  "name": "B2C Add Bank Account",
                  "displayName": "B2C Add Bank Account"
                },
                {
                  "id": 2015,
                  "name": "B2C Get Documents",
                  "displayName": "B2C Get Documents"
                },
                {
                  "id": 2016,
                  "name": "B2C Get Payment Data",
                  "displayName": "B2C Get Payment Data"
                },
                {
                  "id": 2017,
                  "name": "B2C Update Payment",
                  "displayName": "B2C Update Payment"
                },
                {
                  "id": 2018,
                  "name": "B2C Post Payment Decision",
                  "displayName": "B2C Post Payment Decision"
                },
                {
                  "id": 2019,
                  "name": "B2C Update Bank Account",
                  "displayName": "B2C Update Bank Account"
                }
              ],
              "accessGroup": {
                "clientFilter": [
                  "VPA1"
                ],
                "billingEntityFilter": [],
                "billingEntityExclusions": []
              }
            }
            """;

        // Use this to return a local user for testing purposes.  This will be used when running in local development environment and the API throws error "Socket Exception: No Route to Host"
        var user = JsonConvert.DeserializeObject<ValidateServiceResult>(ValidatedUser);
        return user;
    }
    */
    #endregion
}
