using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using Xunit;

namespace VPay.DocSys.Parsing.Http.Tests.PlatformConfiguration
{
    public partial class PlatformConfigApiTests
    {
        private readonly static List<ClientBoardingConfigDto> DefaultGetClientBoardingConfigsResponse = new List<ClientBoardingConfigDto>
        {
            new ClientBoardingConfigDto
            {
                Status = "INACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(3000,2,2),
                OriginCode = "DEFAULT",
                ClientCode = "",
                BillingEntityCode = "",
                PaymentTypeCode = "",
                Data1 = "0",
                Data2 = "0",
                Data3 = "0",
                Data4 = "0",
                Data5 = "0",
                Data6 = "0",
            },
            new ClientBoardingConfigDto
            {
                Status = "ACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(2002,2,2),
                OriginCode = "DEFAULT",
                ClientCode = "",
                BillingEntityCode = "",
                PaymentTypeCode = "",
                Data1 = "1",
                Data2 = "1",
                Data3 = "1",
                Data4 = "1",
                Data5 = "1",
                Data6 = "1",
            },
            new ClientBoardingConfigDto
            {
                Status = "ACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(3000,2,2),
                OriginCode = "DEFAULT",
                ClientCode = "",
                BillingEntityCode = "",
                PaymentTypeCode = "",
                Data1 = "2",
                Data2 = "2",
                Data3 = "2",
                Data4 = "2",
                Data5 = "2",
                Data6 = "2",
            },
            new ClientBoardingConfigDto
            {
                Status = "ACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(3000,2,2),
                OriginCode = "T",
                ClientCode = "TEST",
                BillingEntityCode = "",
                PaymentTypeCode = "",
                Data1 = "3",
                Data2 = "3",
                Data3 = "3",
                Data4 = "3",
                Data5 = "3",
                Data6 = "3",
            },
            new ClientBoardingConfigDto
            {
                Status = "ACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(3000,2,2),
                OriginCode = "T",
                ClientCode = "TEST",
                BillingEntityCode = "TEST12345",
                PaymentTypeCode = "",
                Data1 = "4",
                Data2 = "4",
                Data3 = "4",
                Data4 = "4",
                Data5 = "4",
                Data6 = "4",
            },
            new ClientBoardingConfigDto
            {
                Status = "ACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(3000,2,2),
                OriginCode = "T",
                ClientCode = "TEST",
                BillingEntityCode = "TEST12345",
                PaymentTypeCode = "ACH",
                Data1 = "5",
                Data2 = "5",
                Data3 = "5",
                Data4 = "5",
                Data5 = "5",
                Data6 = "5",
            },
            new ClientBoardingConfigDto
            {
                Status = "ACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(3000,2,2),
                OriginCode = "T",
                ClientCode = "TEST",
                BillingEntityCode = "TEST12345",
                PaymentTypeCode = "ACH",
                Data1 = "6",
                Data2 = "6",
                Data3 = "6",
                Data4 = "6",
                Data5 = "6",
                Data6 = "6",
            },
            new ClientBoardingConfigDto
            {
                Status = "ACTIVE",
                BeginDate = new DateTime(2000,2,2),
                EndDate = new DateTime(3000,2,2),
                OriginCode = "T",
                ClientCode = "TEST",
                BillingEntityCode = "TEST12345",
                PaymentTypeCode = "CHK",
                Data1 = "7",
                Data2 = "7",
                Data3 = "7",
                Data4 = "7",
                Data5 = "7",
                Data6 = "7",
            }
        };

        public readonly static IEnumerable<object?[]> WhenHappyPathGetClientBoardingConfigsExpectations = new List<object?[]>
        {
            new object?[] { null, null, null, null, null, DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(2).Take(1) },
            new object?[] { DateTime.Now, null, null, null, null, DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(2).Take(1) },
            new object?[] { DateTime.Now, "NONE", null, null, null, DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(2).Take(1) },
            new object?[] { DateTime.Now, null, "NONE", null, null, DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(2).Take(1) },
            new object?[] { DateTime.Now, null, "TEST", null, null, DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(3).Take(1) },
            new object?[] { DateTime.Now, null, "TEST", "NONE", null, DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(3).Take(1) },
            new object?[] { DateTime.Now, null, "TEST", "TEST12345", null, DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(4).Take(1) },
            new object?[] { DateTime.Now, null, "TEST", "TEST12345", "ACH", DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(5).Take(2) },
            new object?[] { DateTime.Now, null, "TEST", "TEST12345", "CHK", DefaultGetClientBoardingConfigsResponse, DefaultGetClientBoardingConfigsResponse.Skip(7).Take(1) },
        };

        [Theory]
        [MemberData(nameof(WhenHappyPathGetClientBoardingConfigsExpectations))]
        public async Task WhenHappyPathGetClientBoardingConfigsShouldReturn(
            DateTime? time, string origin, string client, string entity, string type,
            IEnumerable<ClientBoardingConfigDto> given,
            IEnumerable<ClientBoardingConfigDto> expected)
        {
            MockSUT.Setup(m => m.GetClientBoardingConfigs<ClientBoardingConfigDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<ClientBoardingConfigDto>(given));
            var response = await MockSUT.Object.GetClientBoardingConfigs<ClientBoardingConfigDto>("test");
            var relevant = response.Items
                .GroupBy(o => o.Relevance(time, origin, client, entity, type))
                .Last()
                .ToList()
                .Should().BeEquivalentTo(expected);
        }
    }
}
