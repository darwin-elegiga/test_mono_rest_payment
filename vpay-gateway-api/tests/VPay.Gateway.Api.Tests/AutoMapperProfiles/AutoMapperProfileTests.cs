using AutoMapper;
using Microsoft.Extensions.Logging;
using Xunit;

namespace VPay.Gateway.Api.Tests.AutoMapperProfiles;

/// <summary>
/// This tests the AutoMapper profiles in the Gateway API.
/// </summary>
/// <remarks>  When a new profile is added, it will be automatically picked up for testing.</remarks>
public class AutoMapperProfileTests
{
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory();

    [Theory]
    [MemberData(nameof(Profiles))]
    public void ProfileConfigurationShouldBeValid(params Type[] profileTypes)
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            foreach (Type profileType in profileTypes)
            {
                cfg.AddProfile(profileType);
            }
        },_loggerFactory);

        configuration.AssertConfigurationIsValid();
    }

    public static IEnumerable<object[]> Profiles =>
        from type in typeof(Startup).Assembly.GetTypes()
        where typeof(Profile).IsAssignableFrom(type)
        select new object[] { type };
}
