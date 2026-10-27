using System.Collections.Generic;
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

        private static Pagination<CustomSendBackClientBoardingConfigDto> CreateCustomSendBackClientBoardingConfigDto(params (string Code, bool? Enabled)[] codes)
        {
            var pagination = new Pagination<CustomSendBackClientBoardingConfigDto>();
            pagination.Items.Add(new CustomSendBackClientBoardingConfigDto
            {
                Status = ClientBoardingConfigDtoBase.ACTIVE,
                OriginCode = ClientBoardingConfigDtoBase.DEFAULT,
                ClientCode = "",
                IsCustomSendBack = false,
            });
            foreach (var code in codes)
            {
                pagination.Items.Add(new CustomSendBackClientBoardingConfigDto
                {
                    Status = ClientBoardingConfigDtoBase.ACTIVE,
                    OriginCode = "OC",
                    ClientCode = code.Code,
                    IsCustomSendBack = code.Enabled,
                });
            }
            return pagination;
        }


        public readonly static IEnumerable<object[]> WhenHappyPathGetCustomSendBackByClientExpectations = new List<object[]>
        {
            new object[] { "", CreateCustomSendBackClientBoardingConfigDto(("client1", true), ("client2", false)) },
            new object[] { "client", CreateCustomSendBackClientBoardingConfigDto(("client1", true), ("client2", false)) },
        };
        public readonly static IEnumerable<object[]> WhenHappyPathGetCustomSendBackEnabledClientCodesExpectations = new List<object[]>
        {
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", true)), new HashSet<string> { "client1"} },
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", true), ("client2", true)), new HashSet<string> { "client1", "client2"} },
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", true), ("client2", false)), new HashSet<string> { "client1"} },
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", true), ("client2", null)), new HashSet<string> { "client1"} },
        };
        public readonly static IEnumerable<object[]> WhenHappyPathIsCustomSendBackEnabledForClientExpectations = new List<object[]>
        {
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", true)), "client1", true, },
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", true), ("client2", true)), "client1", true, },
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", false), ("client2", false)), "client1", false, },
            new object[] { CreateCustomSendBackClientBoardingConfigDto(("client1", null), ("client2", null)), "client1", false, },
        };
        public const string WhenCustomSendBackClientBoardingConfigJson = @"{
    'currentPage': 1,
    'pageSize': 50,
    'totalRows': 6,
    'totalPages': 1,
    'items': [
        {
            'id': 1672,
            'originCode': 'DEFAULT',
            'configurationName': 'CUSTOM_SEND_BACK',
            'clientCode': '',
            'billingEntityCode': '',
            'paymentTypeCode': '',
            'status': 'ACTIVE',
            'data1': 'N',
            'data2': '',
            'data3': '',
            'data4': '',
            'data5': '',
            'data6': '',
            'beginDate': '2000-01-01',
            'endDate': '2999-12-31',
            'createdDateTime': '2022-03-01T14:16:17.154',
            'createdByProgram': 'VPADMR095',
            'createdBy': 'JBURKE',
            'lastModifiedDateTime': '0001-01-01T00:00:00',
            'lastModifiedByProgram': '',
            'lastModifiedBy': ''
        },
        {
            'id': 1673,
            'originCode': 'IC',
            'configurationName': 'CUSTOM_SEND_BACK',
            'clientCode': 'EBMC',
            'billingEntityCode': '',
            'paymentTypeCode': '',
            'status': 'ACTIVE',
            'data1': 'Y',
            'data2': '',
            'data3': '',
            'data4': '',
            'data5': '',
            'data6': '',
            'beginDate': '2000-01-01',
            'endDate': '2999-12-31',
            'createdDateTime': '2022-03-01T14:16:17.154',
            'createdByProgram': 'VPADMR095',
            'createdBy': 'JBURKE',
            'lastModifiedDateTime': '0001-01-01T00:00:00',
            'lastModifiedByProgram': '',
            'lastModifiedBy': ''
        },
        {
    'id': 1674,
            'originCode': 'IC',
            'configurationName': 'CUSTOM_SEND_BACK',
            'clientCode': 'FHI',
            'billingEntityCode': '',
            'paymentTypeCode': '',
            'status': 'ACTIVE',
            'data1': 'Y',
            'data2': '',
            'data3': '',
            'data4': '',
            'data5': '',
            'data6': '',
            'beginDate': '2000-01-01',
            'endDate': '2999-12-31',
            'createdDateTime': '2022-03-01T14:16:17.154',
            'createdByProgram': 'VPADMR095',
            'createdBy': 'JBURKE',
            'lastModifiedDateTime': '0001-01-01T00:00:00',
            'lastModifiedByProgram': '',
            'lastModifiedBy': ''
        },
        {
    'id': 1675,
            'originCode': 'IC',
            'configurationName': 'CUSTOM_SEND_BACK',
            'clientCode': 'HGI',
            'billingEntityCode': '',
            'paymentTypeCode': '',
            'status': 'ACTIVE',
            'data1': 'Y',
            'data2': '',
            'data3': '',
            'data4': '',
            'data5': '',
            'data6': '',
            'beginDate': '2000-01-01',
            'endDate': '2999-12-31',
            'createdDateTime': '2022-03-01T14:16:17.154',
            'createdByProgram': 'VPADMR095',
            'createdBy': 'JBURKE',
            'lastModifiedDateTime': '0001-01-01T00:00:00',
            'lastModifiedByProgram': '',
            'lastModifiedBy': ''
        },
        {
    'id': 1676,
            'originCode': 'IC',
            'configurationName': 'CUSTOM_SEND_BACK',
            'clientCode': 'HSB',
            'billingEntityCode': '',
            'paymentTypeCode': '',
            'status': 'ACTIVE',
            'data1': 'Y',
            'data2': '',
            'data3': '',
            'data4': '',
            'data5': '',
            'data6': '',
            'beginDate': '2000-01-01',
            'endDate': '2999-12-31',
            'createdDateTime': '2022-03-01T14:16:17.154',
            'createdByProgram': 'VPADMR095',
            'createdBy': 'JBURKE',
            'lastModifiedDateTime': '0001-01-01T00:00:00',
            'lastModifiedByProgram': '',
            'lastModifiedBy': ''
        },
        {
    'id': 1677,
            'originCode': 'IC',
            'configurationName': 'CUSTOM_SEND_BACK',
            'clientCode': 'SBI',
            'billingEntityCode': '',
            'paymentTypeCode': '',
            'status': 'ACTIVE',
            'data1': 'Y',
            'data2': '',
            'data3': '',
            'data4': '',
            'data5': '',
            'data6': '',
            'beginDate': '2000-01-01',
            'endDate': '2999-12-31',
            'createdDateTime': '2022-03-01T14:16:17.154',
            'createdByProgram': 'VPADMR095',
            'createdBy': 'JBURKE',
            'lastModifiedDateTime': '0001-01-01T00:00:00',
            'lastModifiedByProgram': '',
            'lastModifiedBy': ''
        }
    ]
}";

        [Theory]
        [InlineData(WhenCustomSendBackClientBoardingConfigJson)]
        public void WhenHappyPathShouldDeserialize(string json)
        {
            var response = Newtonsoft.Json.JsonConvert
                .DeserializeObject<Pagination<CustomSendBackClientBoardingConfigDto>>(json);
            var serializedResponse = Newtonsoft.Json.JsonConvert
                .SerializeObject(response);
            var redeserializedResponse = Newtonsoft.Json.JsonConvert
                .DeserializeObject<Pagination<CustomSendBackClientBoardingConfigDto>>(serializedResponse);

            response.Should().NotBeNull();
            redeserializedResponse.Should().NotBeNull();
            response.Items.Should().NotBeNull();
            response.Items.Count.Should().Be(6);
            response.Items[0].IsCustomSendBack.Should().BeFalse();
            response.Items[1].IsCustomSendBack.Should().BeTrue();
            redeserializedResponse.Items[1].IsCustomSendBack.Should().Be(response.Items[1].IsCustomSendBack);
        }

        [Theory]
        [MemberData(nameof(WhenHappyPathGetCustomSendBackByClientExpectations))]
        public async Task WhenHappyPathGetCustomSendBackByClientShouldReturn(string whenClient, Pagination<CustomSendBackClientBoardingConfigDto> expected)
        {
            var sut = GetSUT(CreateServiceProvider(
                DEV_API_NAME,
                DEV_API_PROTOCOL,
                DEV_API_HOST,
                DEV_API_PORT,
                expected));
            var actual = await sut.GetCustomSendBackByClient(whenClient);
            actual.Should().BeEquivalentTo(expected);
        }
        [Theory]
        [MemberData(nameof(WhenHappyPathGetCustomSendBackEnabledClientCodesExpectations))]
        public async Task WhenHappyPathGetCustomSendBackEnabledClientCodesShouldReturn(Pagination<CustomSendBackClientBoardingConfigDto> given, HashSet<string> expected)
        {
            MockSUT.Setup(m => m.GetCustomSendBackByClient("", default))
                .ReturnsAsync(given);
            var actual = await MockSUT.Object.GetCustomSendBackEnabledClientCodes();
            actual.Should().BeEquivalentTo(expected);
        }
        [Theory]
        [MemberData(nameof(WhenHappyPathIsCustomSendBackEnabledForClientExpectations))]
        public async Task WhenHappyPathIsCustomSendBackEnabledForClientShouldReturn(Pagination<CustomSendBackClientBoardingConfigDto> given, string when, bool expected)
        {
            MockSUT.Setup(m => m.GetCustomSendBackByClient("", default))
                .ReturnsAsync(given);
            var actual = await MockSUT.Object.IsCustomSendBackEnabledForClient(when);
            actual.Should().Be(expected);
        }

    }
}
