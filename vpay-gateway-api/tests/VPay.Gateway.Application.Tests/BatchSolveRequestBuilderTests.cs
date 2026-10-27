using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.Preferencing.Models;
using Xunit;

#nullable enable

namespace VPay.Gateway.Application.Tests;
public class BatchSolveRequestBuilderTests
{
    #region test cases
    private static readonly Faker _faker = new Faker();
    private static (List<PreferenceLookupDto> Lookup, List<OptumSubPayerDto> Mapping, List<BillingEntityDto> Entities, OriginClientDto Otb, List<BatchSolveRequest> Solve) Default()
    {
        var lookup = new List<PreferenceLookupDto> { new PreferenceLookupDto
        {
            CorrelationID = _faker.Random.Guid().ToString(),
            PayerID = _faker.Random.AlphaNumeric(10),
            SubPayerID = _faker.Random.AlphaNumeric(10),
            TaxID = _faker.Random.AlphaNumeric(10),
            ZipCode = _faker.Address.ZipCode(),
            Amount = _faker.Random.Decimal(-1000M,1000M),
        } };
        var mapping = new List<OptumSubPayerDto>{ new OptumSubPayerDto
        {
            OptumPayorId = lookup[0].PayerID,
            OptumSubPayorId = lookup[0].SubPayerID,
            ClientCode = _faker.Random.AlphaNumeric(4),
            BillingEntityCode = _faker.Random.AlphaNumeric(10),
        } };
        var entities = new List<BillingEntityDto> { new BillingEntityDto
        {
            BillingEntityCode = mapping[0].BillingEntityCode,
            ClientCode = mapping[0].ClientCode,
            Type = new BillingEntityTypeDto
            {
                Name = _faker.Name.FirstName().ToUpperInvariant()
            }
        } };
        var otb = new OriginClientDto
        {
            BillingEntityCode = mapping[0].BillingEntityCode,
            ClientCode = mapping[0].ClientCode,
            Origin = new OriginInfoDto
            {
                OriginCode = _faker.Random.AlphaNumeric(2),
            }
        };
        var solve = new List<BatchSolveRequest> { new BatchSolveRequest
        {
            CorrelationId = lookup[0].CorrelationID,
            OriginCode = otb.Origin.OriginCode,
            ClientCode = mapping[0].ClientCode,
            BillingEntityCode = mapping[0].BillingEntityCode,
            TaxId = lookup[0].TaxID,
            ProviderName = lookup[0].ProviderName,
            ProviderAddress1 = lookup[0].ProviderAddress1,
            ProviderType = entities[0].Type.Name,
            ZipCode = lookup[0].ZipCode,
            Amount = lookup[0].Amount,
        } };
        return (lookup, mapping, entities, otb, solve);
    }
    private static object?[] DefaultArgs()
    {
        var (lookup, mapping, entities, otb, expected) = Default();
        return new object?[] { new BatchSolveRequestBuilder.Query(lookup, mapping), entities, otb, expected };
    }
    public readonly static IEnumerable<object?[]> WhenHandleHappyPathExpectations = new List<object?[]>
    {
        DefaultArgs(),
    };
    private static object?[] ClientCodeMismatch()
    {
        var (lookup, mapping, entities, otb, _) = Default();
        entities = new List<BillingEntityDto>();
        return new object?[] { new BatchSolveRequestBuilder.Query(lookup, mapping), entities, otb, typeof(NoPrsConfigurationValidationException), "*" };
    }
    private static object?[] BillingEntityCodeMismatch()
    {
        var (lookup, mapping, entities, otb, _) = Default();
        entities[0].BillingEntityCode = _faker.Random.AlphaNumeric(9);
        return new object?[] { new BatchSolveRequestBuilder.Query(lookup, mapping), entities, otb, typeof(NoPrsConfigurationValidationException), "*" };
    }
    private static object?[] OriginCodeMismatch()
    {
        var (lookup, mapping, entities, __, _) = Default();
        return new object?[] { new BatchSolveRequestBuilder.Query(lookup, mapping), entities, null, typeof(NoPrsConfigurationValidationException), "*" };
    }
    public readonly static IEnumerable<object?[]> WhenHandleErrorPathExpectations = new List<object?[]>
    {
        ClientCodeMismatch(),
        BillingEntityCodeMismatch(),
        OriginCodeMismatch(),
    };
    #endregion

    private readonly ILogger<BatchSolveRequestBuilder.Handler> _logger;
    private readonly MediatR.IMediator _mockMediator;
    private readonly BatchSolveRequestBuilder.Handler _sut;
    public BatchSolveRequestBuilderTests()
    {
        _logger = NullLogger<BatchSolveRequestBuilder.Handler>.Instance;
        _mockMediator = Substitute.For<MediatR.IMediator>();
        _sut = new BatchSolveRequestBuilder.Handler(_logger, _mockMediator);
    }

    [Theory]
    [MemberData(nameof(WhenHandleHappyPathExpectations))]
    public async Task WhenHappyPathShouldReturn(
        BatchSolveRequestBuilder.Query when,
        List<BillingEntityDto> givenEntities,
        OriginClientDto? givenOrigin,
        List<BatchSolveRequest> expected
        )
    {
        _mockMediator
            .Send(Arg.Any<BillingEntitiesQuery>(), default)
            .Returns(Task.FromResult(givenEntities));
        _mockMediator
            .Send(Arg.Any<OriginClientQuery>(), default)
            .Returns(Task.FromResult(givenOrigin));

        var actual = await _sut.Handle(when, default);
        actual.Should().BeEquivalentTo(expected);
    }
    [Theory]
    [MemberData(nameof(WhenHandleErrorPathExpectations))]
    public async Task WhenBadRequestShouldThrow(
        BatchSolveRequestBuilder.Query when,
        List<BillingEntityDto> givenEntities,
        OriginClientDto? givenOrigin,
        Type expectedErrorType,
        string expectedMessage)
    {
        _mockMediator
            .Send(Arg.Any<BillingEntitiesQuery>(), default)
            .Returns(Task.FromResult(givenEntities));
        _mockMediator
            .Send(Arg.Any<OriginClientQuery>(), default)
            .Returns(Task.FromResult(givenOrigin));

        var act = async () => await _sut.Handle(when, default);
        (await act.Should().ThrowAsync<Exception>()).WithMessage(expectedMessage).And.Should().BeOfType(expectedErrorType);
    }
}
