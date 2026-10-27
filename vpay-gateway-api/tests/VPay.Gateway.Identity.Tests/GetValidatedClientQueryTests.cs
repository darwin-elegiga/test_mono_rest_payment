using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Refit;
using VPay.Extensions.Caching.Memory;
using VPay.Gateway.Common.UserManagement;
using VPay.Gateway.Common.UserManagement.Models;
using Xunit;

namespace VPay.Gateway.Identity.Tests;

public class GetValidatedClientQueryTests
{
    private readonly Mock<IUserManagementApi> _api;
    private readonly Mock<IMemoryCache<ValidateServiceResult>> _cache;
    private readonly ILogger<GetValidatedClientQuery> _logger;
    private readonly Mock<IConfiguration> _configuration;

    public GetValidatedClientQueryTests()
    {
        _api = new Mock<IUserManagementApi>();
        _cache = new Mock<IMemoryCache<ValidateServiceResult>>();
        _logger = new NullLogger<GetValidatedClientQuery>();
        _configuration = new Mock<IConfiguration>();
        _configuration.Setup(x => x["ValidateUserCacheTime"]).Returns("1");
    }

    [Fact]
    public async Task GetUser_WorksAsExpected()
    {
        var clientId = "12345";
        var clientSecret = "568993";
        var response = new ValidateServiceResult(1, "test",
            new List<Permission> { new(1, "test", "testing") },
            new AccessGroup(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));

        _cache
            .Setup(mock =>
                mock.GetOrCreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<ValidateServiceResult>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()))
            .ReturnsAsync(response)
            .Verifiable();

        var query = new GetValidatedClientQuery(_api.Object, _cache.Object, _configuration.Object, _logger);
        var getUserResponse = await query.GetUser(clientId, clientSecret);

        Assert.Equal(response, getUserResponse);
        _cache
            .Verify(mock =>
                mock.GetOrCreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<ValidateServiceResult>>>(), It.IsAny<MemoryCacheEntryOptions>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_Cache_WorksAsExpected()
    {
        var clientId = "12345";
        var clientSecret = "568993";
        var response = new ValidateServiceResult(1, "test",
            new List<Permission> { new(1, "test", "testing") },
            new AccessGroup(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        var provider = new ServiceCollection()
            .AddVPayMemoryCache()
            .BuildServiceProvider();

        var cache = provider.GetRequiredService<IMemoryCache<ValidateServiceResult>>();

        _api.Setup(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<ValidateServiceResult>(new HttpResponseMessage(HttpStatusCode.OK), response,
                new RefitSettings()));

        var query = new GetValidatedClientQuery(_api.Object, cache, _configuration.Object, _logger);

        foreach (int unused in Enumerable.Range(1, 100))
        {
            var getUserResponse = await query.GetUser(clientId, clientSecret);

            Assert.Equal(response, getUserResponse);
        }

        // Should only call the API 1 time the rest should be cached
        _api.Verify(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUser_Cache2Clients_WorksAsExpected()
    {
        var clientId1 = "12345";
        var clientId2 = "4120485";
        var clientSecret1 = "568993";
        var clientSecret2 = "012385039824";
        var response = new ValidateServiceResult(1, "test",
            new List<Permission> { new(1, "test", "testing") },
            new AccessGroup(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        var provider = new ServiceCollection()
            .AddVPayMemoryCache()
            .BuildServiceProvider();

        var cache = provider.GetRequiredService<IMemoryCache<ValidateServiceResult>>();

        _api.Setup(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<ValidateServiceResult>(new HttpResponseMessage(HttpStatusCode.OK), response,
                new RefitSettings()));

        var query = new GetValidatedClientQuery(_api.Object, cache, _configuration.Object, _logger);

        foreach (int unused in Enumerable.Range(1, 100))
        {
            var getUserResponse1 = await query.GetUser(clientId1, clientSecret1);
            var getUserResponse2 = await query.GetUser(clientId2, clientSecret2);

            Assert.Equal(response, getUserResponse1);
            Assert.Equal(response, getUserResponse2);
        }

        // Should only call the API 1 time the rest should be cached
        _api.Verify(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(550)]
    [InlineData(599)]
    public async Task GetUser_Status_5XX_ThrowsException(int statusCode)
    {
        var clientId = "12345";
        var clientSecret = "568993";
        var response = new ValidateServiceResult(1, "test",
            new List<Permission> { new(1, "test", "testing") },
            new AccessGroup(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        var provider = new ServiceCollection()
            .AddVPayMemoryCache()
            .BuildServiceProvider();

        var cache = provider.GetRequiredService<IMemoryCache<ValidateServiceResult>>();

        _api.Setup(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<ValidateServiceResult>(new HttpResponseMessage((HttpStatusCode)statusCode) { RequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.g.com")) }, response,
                new RefitSettings()));

        var query = new GetValidatedClientQuery(_api.Object, cache, _configuration.Object, _logger);

        var func = async () => await query.GetUser(clientId, clientSecret);
        await func.Should().ThrowExactlyAsync<ApiException>();

        // Should only call the API 1 time the rest should be cached
        _api.Verify(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUser_FailedRequest_Returns_Null()
    {
        var clientId = "12345";
        var clientSecret = "568993";
        var response = new ValidateServiceResult(1, "test",
            new List<Permission> { new(1, "test", "testing") },
            new AccessGroup(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        var provider = new ServiceCollection()
            .AddVPayMemoryCache()
            .BuildServiceProvider();

        var cache = provider.GetRequiredService<IMemoryCache<ValidateServiceResult>>();

        _api.Setup(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<ValidateServiceResult>(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.g.com")) }, response,
                new RefitSettings()));

        var query = new GetValidatedClientQuery(_api.Object, cache, _configuration.Object, _logger);

        var getUserResponse = await query.GetUser(clientId, clientSecret);
        Assert.Null(getUserResponse);

        // Should only call the API 1 time the rest should be cached
        _api.Verify(x => x.ValidateUser(It.IsAny<ValidateServiceRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
