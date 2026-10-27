using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VPay.Gateway.Common.PlatformConfig;
using Xunit;

#nullable enable
namespace VPay.Gateway.Application.Tests;
public class BatchPrsMappingTests
{
    #region use cases
    private static readonly Faker _faker = new Faker();
    private const string UNAUTHORIZED = "UNAUTHORIZED";
    private static PreferenceLookupDto FakeLookup() => new PreferenceLookupDto
    {
        CorrelationID = _faker.Random.Guid().ToString(),
        PayerID = _faker.Random.AlphaNumeric(10),
        SubPayerID = _faker.Random.AlphaNumeric(10),
        TaxID = _faker.Random.AlphaNumeric(10),
        ZipCode = _faker.Address.ZipCode(),
        Amount = _faker.Random.Decimal(-1000M, 1000M),
    };
    private static List<PreferenceLookupDto> DefaultLookups() => new List<PreferenceLookupDto> { FakeLookup() };
    private static OptumSubPayerDto FakeMapping(PreferenceLookupDto dto) => new OptumSubPayerDto
    {
        OptumPayorId = dto.PayerID,
        OptumSubPayorId = dto.SubPayerID,
        ClientCode = _faker.Random.AlphaNumeric(4),
        BillingEntityCode = _faker.Random.AlphaNumeric(10),
    };
    private static List<OptumSubPayerDto> DefaultMappings(IEnumerable<PreferenceLookupDto> dto) => dto.Select(FakeMapping).ToList();
    private static object?[] DefaultArgs()
    {
        var lookups = DefaultLookups();
        var mappings = DefaultMappings(lookups);
        return new object?[] { new BatchPrsMapping.Query(lookups), mappings, mappings };
    }
    private static object?[] MultipleMatchArgs()
    {
        var lookup1 = FakeLookup();
        lookup1.PayerID = "PENG1";
        lookup1.SubPayerID = "3PREFCOS01";
        var lookup2 = FakeLookup();
        lookup2.PayerID = "PENG1";
        lookup2.SubPayerID = "3PREFCOS01";
        var lookup3 = FakeLookup();
        lookup3.PayerID = "PENG1";
        lookup3.SubPayerID = "1POSTCA";
        var lookups = new List<PreferenceLookupDto> { lookup1, lookup2, lookup3 };
        var mappings = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OptumSubPayerDto>>(
            @"[
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
            ]");
        return new object?[] { new BatchPrsMapping.Query(lookups), mappings, mappings.Where(m => m.OptumSubPayorId is "3PREFCOS01" or "1POSTCA").ToList() };
    }
    public readonly static IEnumerable<object?[]> WhenHandleHappyPathExpectations = new List<object?[]>
    {
        DefaultArgs(),
        MultipleMatchArgs(),
    };
    private static object?[] PayerMismatch()
    {
        return new object?[] { new BatchPrsMapping.Query(DefaultLookups()), null, typeof(FluentValidation.ValidationException), "*" };
    }
    private static object?[] PayerUnauthorized()
    {
        var lookups = DefaultLookups();
        var mappings = DefaultMappings(lookups);
        mappings[0].OptumPayorId = UNAUTHORIZED;
        return new object?[] { new BatchPrsMapping.Query(DefaultLookups()), null, typeof(FluentValidation.ValidationException), "*" };
    }
    private static object?[] SubPayerUnauthorized()
    {
        var lookups = DefaultLookups();
        var mappings = DefaultMappings(lookups);
        mappings[0].OptumSubPayorId = UNAUTHORIZED;
        return new object?[] { new BatchPrsMapping.Query(lookups), mappings, typeof(FluentValidation.ValidationException), "*" };
    }
    private static object?[] SubPayerMismatch()
    {
        var lookups = DefaultLookups();
        var mappings = DefaultMappings(lookups);
        mappings[0].OptumSubPayorId = _faker.Random.AlphaNumeric(9);
        return new object?[] { new BatchPrsMapping.Query(lookups), mappings, typeof(FluentValidation.ValidationException), "*" };
    }
    public readonly static IEnumerable<object?[]> WhenHandleErrorPathExpectations = new List<object?[]>
    {
        PayerMismatch(),
        PayerUnauthorized(),
        SubPayerMismatch(),
        SubPayerUnauthorized(),
    };
    #endregion

    private readonly ILogger<BatchPrsMapping.Handler> _logger;
    private readonly MediatR.IMediator _mockMediator;
    private readonly IUserInfo _userInfo;
    private readonly BatchPrsMapping.Handler _sut;
    public BatchPrsMappingTests()
    {
        _logger = NullLogger<BatchPrsMapping.Handler>.Instance;
        _mockMediator = Substitute.For<MediatR.IMediator>();
        _userInfo = Substitute.For<IUserInfo>();
        _sut = new BatchPrsMapping.Handler(_logger, _mockMediator, _userInfo);
    }

    [Theory]
    [MemberData(nameof(WhenHandleHappyPathExpectations))]
    public async Task WhenHandleHappyPathShouldReturn(BatchPrsMapping.Query when, List<OptumSubPayerDto> givenMapping, List<OptumSubPayerDto> expected)
    {
        _mockMediator
            .Send(Arg.Any<OptumPayerQuery>(), default)
            .Returns(Task.FromResult(givenMapping));
        _userInfo
            .CanAccess(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        var actual = await _sut.Handle(when, default);
        actual.Should().BeEquivalentTo(expected);
    }
    [Theory]
    [MemberData(nameof(WhenHandleErrorPathExpectations))]
    public async Task WhenHandleBadRequestShouldThrow(BatchPrsMapping.Query when, List<OptumSubPayerDto> givenMapping, Type expectedErrorType, string expectedMessage)
    {
        _mockMediator
            .Send(Arg.Any<OptumPayerQuery>(), default)
            .Returns(Task.FromResult(givenMapping));
        _userInfo
            .CanAccess(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        _userInfo
            .CanAccess(Arg.Is(UNAUTHORIZED), Arg.Any<string>())
            .Returns(false);
        _userInfo
            .CanAccess(Arg.Any<string>(), Arg.Is(UNAUTHORIZED))
            .Returns(false);
        var act = async () => await _sut.Handle(when, default);
        (await act.Should().ThrowAsync<Exception>()).WithMessage(expectedMessage).And.Should().BeOfType(expectedErrorType);
    }
}
