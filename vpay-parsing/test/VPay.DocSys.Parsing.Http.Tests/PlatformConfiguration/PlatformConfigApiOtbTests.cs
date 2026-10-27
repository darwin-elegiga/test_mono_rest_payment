using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using Xunit;
using VPay.DocSys.Parsing.Http.Extensions;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VPay.DocSys.Parsing.Http.Tests.PlatformConfiguration
{
    public partial class PlatformConfigApiTests
    {
        private static OtbBillingEntityClientDto CreateOtbBillingEntityClientDto(string tpa, string[] bes)
        {
            return new OtbBillingEntityClientDto
            {
                ClientCode = tpa,
                BillingEntities = bes.Select(be => new OtbBillingEntityDetailDto
                {
                    BillingEntityCode = be,
                    Fields = new List<OtbBillingEntityFieldDto>
                    {
                        new OtbBillingEntityFieldDto
                        {
                            FieldName = "default",
                            FieldValue = "default",
                        }
                    }
                })
            };
        }

        public readonly static IEnumerable<object[]> WhenHappyPathGetOtbDetailsExpectations = new List<object[]>
        {
            new object[] { "client", new string[] {}, true, CreateOtbBillingEntityClientDto("client", new[] {"entity1", "entity2"}) },
            new object[] { "client", new string[] { "entity1"}, true, CreateOtbBillingEntityClientDto("client", new[] {"entity1", "entity2"}) },
            new object[] { "client", new string[] { "entity2"}, true, CreateOtbBillingEntityClientDto("client", new[] {"entity1", "entity2"}) },
            new object[] { "client", new string[] { "entity1" }, false, CreateOtbBillingEntityClientDto("client", new[] {"entity1", "entity2"}) },
        };

        [Theory]
        [MemberData(nameof(WhenHappyPathGetOtbDetailsExpectations))]
        public async Task WhenHappyPathGetOtbDetailsShouldReturn(string whenClient, string[] whenBillingEntity, bool useBillingEntity, OtbBillingEntityClientDto expected)
        {
            var sut = GetSUT(CreateServiceProvider(
                DEV_API_NAME,
                DEV_API_PROTOCOL,
                DEV_API_HOST,
                DEV_API_PORT,
                expected));
            var actual = useBillingEntity
                ? await sut.GetOtbDetails(whenClient, whenBillingEntity)
                : await sut.GetOtbDetails(whenClient);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
