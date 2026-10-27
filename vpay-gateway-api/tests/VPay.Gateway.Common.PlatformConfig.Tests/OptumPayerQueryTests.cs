using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;
using Xunit;

namespace VPay.Gateway.Common.PlatformConfig.Tests;

public class SubsituteDelegatingHandler : DelegatingHandler
{
    private Dictionary<int, HttpResponseMessage> _responseMessages;
    public SubsituteDelegatingHandler(params (HttpStatusCode code, string content)[] responses)
    {
        _responseMessages = new Dictionary<int, HttpResponseMessage>();
        for (int i = 0; i < responses.Length; i++)
        {
            var (code, content) = responses[i];
            _responseMessages.Add(i + 1, new HttpResponseMessage
            {
                StatusCode = code,
                Content = new StringContent(content)
            });
        }
    }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var pattern = new Regex("page=(\\d+)");
        var query = request.RequestUri!.Query!;
        var match = pattern.Match(query);
        var keyString = match.Success ? match.Groups[1].Value : "-1";
        var key = int.Parse(keyString, System.Globalization.CultureInfo.InvariantCulture);

        if (_responseMessages.ContainsKey(key))
        {
            return Task.FromResult(_responseMessages[key]);
        }
        else
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
            });
        }
    }
}
public partial class OptumPayerQueryTests
{
    private static ServiceProvider CreateServices(SubsituteDelegatingHandler handler)
    {
        var settings = new Newtonsoft.Json.JsonSerializerSettings
            { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() };
        settings.Converters.Add(new PaginationOrArrayNewtonsoftJsonConverter<OptumSubPayerDto>());

        return new ServiceCollection()
            .AddLogging(l => l.ClearProviders().AddProvider(NullLoggerProvider.Instance))
            .AddPolicies()
            .AddRefitClient<IPlatformConfigApi>(new RefitSettings(
                new NewtonsoftJsonContentSerializer(settings)))
            .ConfigureHttpClient((_, client) =>
            {
                client.BaseAddress = new Uri("https://localhost:8080");
                client.Timeout = TimeSpan.FromMilliseconds(1);
            })
            .AddHttpMessageHandler(_ => handler)
            .AddPolicyHandlerFromRegistry(PolicyNames.HttpRetry)
            .Services
            .AddVPayMemoryCache()
            .AddTransient<OptumPayerQueryHandler>()
            .BuildServiceProvider();
    }
    public static IEnumerable<object?[]> WhenHandle_Expected => new List<object?[]>
    {
        new object?[]{
            new List<OptumSubPayerDto>{
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1COS01", OptumPayorId = "PENG1", OptumSubPayorId = "3PREFCOS01"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP01", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTCA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP02", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTNY"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP03", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTWA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP04", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTMI"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP05", OptumPayorId = "PENG1", OptumSubPayorId = "1PREFUHIC"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP06", OptumPayorId = "PENG1", OptumSubPayorId = "1PREFIFP"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP01", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP02", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWB"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP03", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWC"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP04", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWD"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP05", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFOFIN2"}
            },
            (HttpStatusCode.OK, @"[
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1COS01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""3PREFCOS01""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTCA""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP02"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTNY""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP03"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTWA""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP04"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTMI""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP05"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1PREFUHIC""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP06"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1PREFIFP""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWA""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP02"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWB""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP03"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWC""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP04"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWD""},
                    {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP05"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFOFIN2""}
                ]"),
        },
        new object?[]{
            new List<OptumSubPayerDto>{
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1COS01", OptumPayorId = "PENG1", OptumSubPayorId = "3PREFCOS01"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP01", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTCA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP02", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTNY"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP03", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTWA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP04", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTMI"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP05", OptumPayorId = "PENG1", OptumSubPayorId = "1PREFUHIC"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP06", OptumPayorId = "PENG1", OptumSubPayorId = "1PREFIFP"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP01", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP02", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWB"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP03", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWC"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP04", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWD"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP05", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFOFIN2"}
            },
            (HttpStatusCode.OK, @"{
                    ""currentPage"": 1,
                    ""pageSize"": 12,
                    ""totalRows"": 12,
                    ""totalPages"": 1,
                    ""items"": [
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1COS01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""3PREFCOS01""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTCA""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP02"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTNY""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP03"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTWA""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP04"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTMI""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP05"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1PREFUHIC""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP06"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1PREFIFP""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWA""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP02"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWB""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP03"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWC""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP04"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWD""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP05"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFOFIN2""}
                    ]
                }"),
        },
        new object?[]{
            new List<OptumSubPayerDto>{
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1COS01", OptumPayorId = "PENG1", OptumSubPayorId = "3PREFCOS01"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP01", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTCA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP02", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTNY"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP03", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTWA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP04", OptumPayorId = "PENG1", OptumSubPayorId = "1POSTMI"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP05", OptumPayorId = "PENG1", OptumSubPayorId = "1PREFUHIC"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1CSP06", OptumPayorId = "PENG1", OptumSubPayorId = "1PREFIFP"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP01", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWA"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP02", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWB"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP03", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWC"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP04", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFWD"},
                new() { ClientCode = "PENG1", BillingEntityCode = "PENG1USP05", OptumPayorId = "PENG1", OptumSubPayorId = "2PREFOFIN2"}
            },
            (HttpStatusCode.OK, @"{
                    ""currentPage"": 1,
                    ""pageSize"": 6,
                    ""totalRows"": 12,
                    ""totalPages"": 2,
                    ""items"": [
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1COS01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""3PREFCOS01""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTCA""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP02"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTNY""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP03"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTWA""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP04"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1POSTMI""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP05"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1PREFUHIC""},
                    ]
                }"),
            (HttpStatusCode.OK, @"{
                    ""currentPage"": 2,
                    ""pageSize"": 6,
                    ""totalRows"": 12,
                    ""totalPages"": 2,
                    ""items"": [
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1CSP06"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""1PREFIFP""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP01"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWA""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP02"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWB""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP03"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWC""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP04"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFWD""},
                        {""clientCode"":""PENG1"",""billingEntityCode"":""PENG1USP05"",""optumPayorId"":""PENG1"",""optumSubPayorId"":""2PREFOFIN2""}
                    ]
                }"),
        },
    };

    [Theory]
    [MemberData(nameof(WhenHandle_Expected))]
    public async Task WhenHandle_ExpectResponse(List<OptumSubPayerDto> expected, params (HttpStatusCode code, string stringContent)[] responses)
    {
        var handler = new SubsituteDelegatingHandler(responses);

        var services = CreateServices(handler);

        var actual = await services.GetRequiredService<OptumPayerQueryHandler>().Handle(new OptumPayerQuery("PENG1"), default);

        actual.Should().BeEquivalentTo(expected);
    }
}
