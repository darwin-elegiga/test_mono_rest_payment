using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.Preferencing.Models;
using Xunit;

#nullable enable

namespace VPay.Gateway.Api.Tests.Controllers;
public partial class PreferenceControllerTests
{
    private static readonly Faker _faker = new();

    private static
    (
        PreferenceLookupDtoList Lookup,
        List<OptumSubPayerDto> Mapping,
        List<BatchSolveRequest> Requests,
        List<BatchSolveResponse> Responses,
        List<PrsPaymentTypeCodeResult> Results
    ) Default()
    {
        var lookup = new PreferenceLookupDtoList { new PreferenceLookupDto
        {
            CorrelationID = _faker.Random.Guid().ToString(),
            PayerID = _faker.Random.AlphaNumeric(10),
            SubPayerID = _faker.Random.AlphaNumeric(10),
            TaxID = _faker.Random.AlphaNumeric(10),
            ZipCode = _faker.Address.ZipCode(),
            Amount = _faker.Random.Decimal(-1000M,1000M),
        } };
        var mapping = new List<OptumSubPayerDto> { new() {
            OptumPayorId = lookup[0].PayerID,
            OptumSubPayorId = lookup[0].SubPayerID,
            ClientCode = _faker.Random.AlphaNumeric(4),
            BillingEntityCode = _faker.Random.AlphaNumeric(10),
        } };
        var requests = new List<BatchSolveRequest> { new() {
            CorrelationId = lookup[0].CorrelationID,
            OriginCode = _faker.Random.AlphaNumeric(2),
            ClientCode = mapping[0].ClientCode,
            BillingEntityCode = mapping[0].BillingEntityCode,
            TaxId = lookup[0].TaxID,
            ProviderName = lookup[0].ProviderName,
            ProviderAddress1 = lookup[0].ProviderAddress1,
            ProviderType = _faker.Name.FirstName().ToUpperInvariant(),
            ZipCode = lookup[0].ZipCode,
            Amount = lookup[0].Amount,
        } };
        var responses = new List<BatchSolveResponse> { new() {
            CorrelationId = lookup[0].CorrelationID,
            PaymentTypeCode = _faker.Random.AlphaNumeric(5),
        } };
        var results = new List<PrsPaymentTypeCodeResult> { new() {
            CorrelationID = responses[0].CorrelationId,
            PaymentTypeCode = responses[0].PaymentTypeCode
        } };
        return (lookup, mapping, requests, responses, results);
    }

    private static
    (
        PreferenceLookupDtoList Lookup,
        List<OptumSubPayerDto> Mapping,
        List<BatchSolveRequest> Requests,
        List<BatchSolveResponse> Responses,
        List<PrsPaymentTypeCodeResult> Results
    ) DistMethod()
    {
        var lookup = new PreferenceLookupDtoList { new PreferenceLookupDto
        {
            CorrelationID = _faker.Random.Guid().ToString(),
            PayerID = _faker.Random.AlphaNumeric(10),
            SubPayerID = _faker.Random.AlphaNumeric(10),
            TaxID = _faker.Random.AlphaNumeric(10),
            ZipCode = _faker.Address.ZipCode(),
            Amount = _faker.Random.Decimal(-1000M,1000M),
        } };
        var mapping = new List<OptumSubPayerDto> { new() {
            OptumPayorId = lookup[0].PayerID,
            OptumSubPayorId = lookup[0].SubPayerID,
            ClientCode = _faker.Random.AlphaNumeric(4),
            BillingEntityCode = _faker.Random.AlphaNumeric(10),
        } };
        var requests = new List<BatchSolveRequest> { new() {
            CorrelationId = lookup[0].CorrelationID,
            OriginCode = _faker.Random.AlphaNumeric(2),
            ClientCode = mapping[0].ClientCode,
            BillingEntityCode = mapping[0].BillingEntityCode,
            TaxId = lookup[0].TaxID,
            ProviderName = lookup[0].ProviderName,
            ProviderAddress1 = lookup[0].ProviderAddress1,
            ProviderType = _faker.Name.FirstName().ToUpperInvariant(),
            ZipCode = lookup[0].ZipCode,
            Amount = lookup[0].Amount,
        } };
        var responses = new List<BatchSolveResponse> { new() {
            CorrelationId = lookup[0].CorrelationID,
            PaymentTypeCode = _faker.Random.AlphaNumeric(5),
            DistributionDescription = _faker.Random.AlphaNumeric(5),
        } };
        var results = new List<PrsPaymentTypeCodeResult> { new() {
            CorrelationID = responses[0].CorrelationId,
            PaymentTypeCode = responses[0].PaymentTypeCode,
            DistributionDescription = responses[0].DistributionDescription,
        } };
        return (lookup, mapping, requests, responses, results);
    }

    private static object?[] DefaultArgs()
    {
        var (lookup, mapping, requests, responses, expected) = Default();
        return new object?[] { lookup, mapping, requests, responses, expected };
    }

    private static object?[] DistMethodArgs()
    {
        var (lookup, mapping, requests, responses, expected) = DistMethod();
        return new object?[] { lookup, mapping, requests, responses, expected };
    }

    public readonly static IEnumerable<object?[]> WhenLookupHappyPathExpectations = new List<object?[]>
    {
        DefaultArgs(),
    };

    public readonly static IEnumerable<object?[]> WhenLookupDistMethodExpectations = new List<object?[]>
    {
        DistMethodArgs(),
    };

    private static object?[] BatchSolveFailure()
    {
        var (lookup, mapping, requests, _, _) = Default();
        return new object?[] { lookup, mapping, requests, null, "*", typeof(ArgumentNullException) };
    }

    public readonly static IEnumerable<object?[]> WhenLookupErrorPathExpectations = new List<object?[]>
    {
        BatchSolveFailure(),
    };

    [Theory]
    [MemberData(nameof(WhenLookupHappyPathExpectations))]
    public async Task WhenLookupHappyPathShouldReturn(
        PreferenceLookupDtoList when,
        List<OptumSubPayerDto> givenMapping,
        List<BatchSolveRequest> givenRequests,
        IEnumerable<BatchSolveResponse> givenResponses,
        IEnumerable<PrsPaymentTypeCodeResult> expected)
    {
        _mockMediator
            .Send(Arg.Any<Application.BatchPrsMapping.Query>(), default)
            .Returns(Task.FromResult(givenMapping));
        _mockMediator
            .Send(Arg.Any<Application.BatchSolveRequestBuilder.Query>(), default)
            .Returns(Task.FromResult(givenRequests));
        _mockMediator
            .Send(Arg.Any<Application.BatchSolve.Query>(), default)
            .Returns(Task.FromResult(givenResponses));

        var actual = await _sut.PreferenceLookup(when);
        var actualOk = (actual.Result as OkObjectResult);
        actualOk!.Value.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(WhenLookupErrorPathExpectations))]
    public async Task WhenLookupErrorPathShouldReturn(
        PreferenceLookupDtoList when,
        List<OptumSubPayerDto> givenMapping,
        List<BatchSolveRequest> givenRequests,
        IEnumerable<BatchSolveResponse> givenResponses,
        string expectedWildCardPattern,
        Type expectedExceptionType)
    {
        _mockMediator
            .Send(Arg.Any<Application.BatchPrsMapping.Query>(), default)
            .Returns(Task.FromResult(givenMapping));
        _mockMediator
            .Send(Arg.Any<Application.BatchSolveRequestBuilder.Query>(), default)
            .Returns(Task.FromResult(givenRequests));
        _mockMediator
            .Send(Arg.Any<Application.BatchSolve.Query>(), default)
            .Returns(Task.FromResult(givenResponses));

        var act = async () => await _sut.PreferenceLookup(when);
        (await act.Should().ThrowAsync<Exception>()).WithMessage(expectedWildCardPattern).And.Should().BeOfType(expectedExceptionType);
    }

    [Theory]
    [MemberData(nameof(WhenLookupDistMethodExpectations))]
    public async Task WhenLookupHasDistMethod_ShouldReturnDistMethodDescription(
        PreferenceLookupDtoList when,
        List<OptumSubPayerDto> givenMapping,
        List<BatchSolveRequest> givenRequests,
        IEnumerable<BatchSolveResponse> givenResponses,
        IEnumerable<PrsPaymentTypeCodeResult> expected)
    {
        _mockMediator
            .Send(Arg.Any<Application.BatchPrsMapping.Query>(), default)
            .Returns(Task.FromResult(givenMapping));
        _mockMediator
            .Send(Arg.Any<Application.BatchSolveRequestBuilder.Query>(), default)
            .Returns(Task.FromResult(givenRequests));
        _mockMediator
            .Send(Arg.Any<Application.BatchSolve.Query>(), default)
            .Returns(Task.FromResult(givenResponses));

        var actual = await _sut.PreferenceLookup(when);
        var actualOk = (actual.Result as OkObjectResult);
        actualOk!.Value.Should().BeEquivalentTo(expected);
    }
}
