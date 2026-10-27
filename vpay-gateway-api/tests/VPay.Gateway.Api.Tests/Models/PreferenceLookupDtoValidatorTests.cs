using Bogus;
using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using VPay.Gateway.Application;
using VPay.Gateway.Common;
using Xunit;

#nullable enable

namespace VPay.Gateway.Api.Tests.Models;
public class PreferenceLookupDtoValidatorTests
{
    private const string PREFIX = PreferenceLookupDtoList.ValidationPropertyName;
    private IValidator<PreferenceLookupDtoList> batchValidator;
    private static readonly Faker _faker = new();
    private static PreferenceLookupDtoList Fake(params PreferenceLookupDto[] given)
    {
        var list = new PreferenceLookupDtoList();
        list.AddRange(given);
        return list;
    }
    private static PreferenceLookupDtoList Fake(params IEnumerable<PreferenceLookupDto>[] given)
    {
        var list = new PreferenceLookupDtoList();
        foreach (var givenEnumerable in given)
        {
            list.AddRange(givenEnumerable);
        }
        return list;
    }
    private static PreferenceLookupDtoList Fake(int count,
        Func<string>? correlationId = null,
        Func<string>? payerId = null,
        Func<string>? subPayerId = null,
        Func<string>? taxId = null,
        Func<decimal?>? amount = null,
        Func<string?>? address = null,
        Func<string?>? name = null,
        Func<string?>? postal = null)
    {
        correlationId ??= () => _faker.Random.Guid().ToString();
        payerId ??= () => _faker.Random.AlphaNumeric(10);
        subPayerId ??= () => _faker.Random.AlphaNumeric(10);
        taxId ??= () => _faker.Random.AlphaNumeric(10);
        amount ??= () => _faker.Random.Decimal(-10000.01M, 10000.01M);
        address ??= () => _faker.Address.StreetAddress().Truncate(100);
        name ??= () => _faker.Name.FirstName();
        postal ??= () => _faker.Address.ZipCode();
        var list = new PreferenceLookupDtoList();
        for (var i = 0; i < count; i++)
        {
            list.Add(new PreferenceLookupDto
            {
                CorrelationID = correlationId(),
                PayerID = payerId(),
                SubPayerID = subPayerId(),
                TaxID = taxId(),
                Amount = amount(),
                ProviderAddress1 = address(),
                ProviderName = name(),
                ZipCode = postal(),
            });
        }
        return list;
    }

    public PreferenceLookupDtoValidatorTests()
    {
        batchValidator = new ServiceCollection()
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssemblyContaining<Startup>()
            .BuildServiceProvider()
            .GetRequiredService<IValidator<PreferenceLookupDtoList>>();
    }

    public readonly static IEnumerable<object?[]> WhenLookupHappyPathExpectations = new List<object?[]>
    {
        new object?[] { Fake(1000) },
        new object?[] { Fake(new PreferenceLookupDto
            {
                CorrelationID = "1",
                PayerID = "1",
                SubPayerID = "1",
                TaxID = "1",
                ZipCode = "12345",
                Amount = 0,
            }) },
    };
    public readonly static IEnumerable<object?[]> WhenLookupErrorPathExpectations = new List<object?[]>
    {
        new object?[] { Fake(1001), "Count" },
        new object?[] { Fake(Fake(10), Fake(10, correlationId: () => "DUPLICATE")), Enumerable.Repeat(10,10).Select((initial,increment) => $"{PREFIX}[{initial+increment}]").ToArray() },
        new object?[] { Fake(1, correlationId: () => string.Empty), $"{PREFIX}[0].CorrelationID" },
        new object?[] { Fake(1, payerId: () => string.Empty), $"{PREFIX}[0].PayerID" },
        new object?[] { Fake(1, payerId: () => "12345678901"), $"{PREFIX}[0].PayerID" },
        new object?[] { Fake(1, subPayerId: () => string.Empty), $"{PREFIX}[0].SubPayerID" },
        new object?[] { Fake(1, subPayerId: () => "12345678901"), $"{PREFIX}[0].SubPayerID" },
        new object?[] { Fake(1, taxId: () => string.Empty), $"{PREFIX}[0].TaxID" },
        new object?[] { Fake(1, taxId: () => "12345678901"), $"{PREFIX}[0].TaxID" },
        new object?[] { Fake(1, amount: () => null), $"{PREFIX}[0].Amount" },
        new object?[] { Fake(1, postal: () => null), $"{PREFIX}[0].ZipCode" },
        new object?[] { Fake(1, postal: () => "1234"), $"{PREFIX}[0].ZipCode" },
        new object?[] { Fake(1, address: () => "1".PadRight(101)), $"{PREFIX}[0].ProviderAddress1" },
    };
    [Theory]
    [MemberData(nameof(WhenLookupHappyPathExpectations))]
    public async Task WhenLookupHappyPathShouldPass(PreferenceLookupDtoList when)
    {
        var actual = await batchValidator.TestValidateAsync(when);
        actual.ShouldNotHaveAnyValidationErrors();
    }
    [Theory]
    [MemberData(nameof(WhenLookupErrorPathExpectations))]
    public async Task WhenLookupErrorPathShouldError(PreferenceLookupDtoList when, params string[] expectedPropertyNames)
    {
        var actual = await batchValidator.TestValidateAsync(when);
        foreach (var expectedPropertyName in expectedPropertyNames)
        {
            actual.ShouldHaveValidationErrorFor(expectedPropertyName);
        }
        actual.ShouldHaveAnyValidationError();
    }
}
