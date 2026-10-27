using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Http.Abstractions;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using VPay.DocSys.Parsing.Http.Extensions;
using Xunit;

namespace VPay.DocSys.Parsing.Http.Tests.PlatformConfiguration
{
    public partial class PlatformConfigApiTests
    {
        private static PrsMappingResult CreatePRSMappingResult(string clientCode, string billingEntityCode)
        {
            return new PrsMappingResult
            {
                ClientCode = clientCode,
                BillingEntityCode = billingEntityCode,
                OptumPayorId = "payerId",
                OptumSubPayorId = "subPayerId",
            };
        }

        public readonly static IEnumerable<object[]> WhenHappyPathGetMappingByClientCodeAndBillingEntityCodeExpectations = new List<object[]>
        {
            new object[] { "tpa", "be1", CreatePRSMappingResult("tpa", "be1") },
        };

        [Theory]
        [MemberData(nameof(WhenHappyPathGetMappingByClientCodeAndBillingEntityCodeExpectations))]
        public async Task WhenHappyPathGetMappingByClientCodeAndBillingEntityCodeShouldReturn(string whenClient, string whenBillingEntity, PrsMappingResult expected)
        {
            var sut = GetSUT(CreateServiceProvider(
                DEV_API_NAME,
                DEV_API_PROTOCOL,
                DEV_API_HOST,
                DEV_API_PORT,
                expected));
            var actual = await sut.GetMappingByClientCodeAndBillingEntityCode(whenClient, whenBillingEntity);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
