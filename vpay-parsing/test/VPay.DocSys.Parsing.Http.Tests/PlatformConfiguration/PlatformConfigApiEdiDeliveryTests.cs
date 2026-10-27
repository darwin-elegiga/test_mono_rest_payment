using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using Xunit;

namespace VPay.DocSys.Parsing.Http.Tests.PlatformConfiguration
{
    public partial class PlatformConfigApiTests
    {
        private static Pagination<EdiDeliveryClientBoardingConfigDto> CreateEdiDeliveryClientBoardingConfigDto(params (string ClientCode, string EntityCode, EdiDeliveryDestination Destination, EdiDeliveryFileExtension Extension)[] codes)
        {
            var pagination = new Pagination<EdiDeliveryClientBoardingConfigDto>();
            foreach (var code in codes)
            {
                pagination.Items.Add(new EdiDeliveryClientBoardingConfigDto
                {
                    ClientCode = code.ClientCode,
                    Destination = code.Destination,
                    FileExtension = code.Extension
                });
            }
            return pagination;
        }

        public readonly static IEnumerable<object[]> WhenHappyPathGetEdiDeliveryByClientExpectations = new List<object[]>
        {
            new object[] { "client", "entity", CreateEdiDeliveryClientBoardingConfigDto(("client", "entity", EdiDeliveryDestination.Availity, EdiDeliveryFileExtension.Edi835)) },
        };
        public const string WhenEdiDeliveryClientBoardingConfigJson = @"{
  ""currentPage"": 1,
  ""pageSize"": 50,
  ""totalRows"": 23,
  ""totalPages"": 1,
  ""items"": [
    {
      ""id"": 1902,
      ""originCode"": ""DEFAULT"",
      ""configurationName"": ""EDI_DELIVERY"",
      ""clientCode"": """",
      ""billingEntityCode"": """",
      ""paymentTypeCode"": """",
      ""status"": ""ACTIVE"",
      ""data1"": """",
      ""data2"": """",
      ""data3"": """",
      ""data4"": """",
      ""data5"": """",
      ""data6"": """",
      ""beginDate"": ""2001-01-01"",
      ""endDate"": ""9999-12-31"",
      ""createdDateTime"": ""2023-06-19T14:26:44.24"",
      ""createdByProgram"": ""CP-9899"",
      ""createdBy"": ""JBURKE"",
      ""lastModifiedDateTime"": ""2023-06-19T14:26:44.24"",
      ""lastModifiedByProgram"": ""CP-9899"",
      ""lastModifiedBy"": ""JBURKE""
    },
    {
      ""id"": 1903,
      ""originCode"": ""RC"",
      ""configurationName"": ""EDI_DELIVERY"",
      ""clientCode"": ""TLC"",
      ""billingEntityCode"": """",
      ""paymentTypeCode"": """",
      ""status"": ""ACTIVE"",
      ""data1"": """",
      ""data2"": """",
      ""data3"": """",
      ""data4"": """",
      ""data5"": """",
      ""data6"": """",
      ""beginDate"": ""2001-01-01"",
      ""endDate"": ""9999-12-31"",
      ""createdDateTime"": ""2023-06-19T14:26:44.24"",
      ""createdByProgram"": ""CP-9899"",
      ""createdBy"": ""JBURKE"",
      ""lastModifiedDateTime"": ""2023-06-19T14:26:44.24"",
      ""lastModifiedByProgram"": ""CP-9899"",
      ""lastModifiedBy"": ""JBURKE""
    },
    {
      ""id"": 1904,
      ""originCode"": ""RC"",
      ""configurationName"": ""EDI_DELIVERY"",
      ""clientCode"": ""TLC"",
      ""billingEntityCode"": ""TLC0001270"",
      ""paymentTypeCode"": """",
      ""status"": ""ACTIVE"",
      ""data1"": ""Availity"",
      ""data2"": "".835"",
      ""data3"": """",
      ""data4"": """",
      ""data5"": """",
      ""data6"": """",
      ""beginDate"": ""2001-01-01"",
      ""endDate"": ""9999-12-31"",
      ""createdDateTime"": ""2023-06-19T14:26:44.24"",
      ""createdByProgram"": ""CP-9899"",
      ""createdBy"": ""JBURKE"",
      ""lastModifiedDateTime"": ""2023-06-19T14:26:44.24"",
      ""lastModifiedByProgram"": ""CP-9899"",
      ""lastModifiedBy"": ""JBURKE""
    },
    {
      ""id"": 1924,
      ""originCode"": ""RC"",
      ""configurationName"": ""EDI_DELIVERY"",
      ""clientCode"": ""WEB"",
      ""billingEntityCode"": """",
      ""paymentTypeCode"": """",
      ""status"": ""ACTIVE"",
      ""data1"": ""Availity"",
      ""data2"": "".brn"",
      ""data3"": """",
      ""data4"": """",
      ""data5"": """",
      ""data6"": """",
      ""beginDate"": ""2001-01-01"",
      ""endDate"": ""9999-12-31"",
      ""createdDateTime"": ""2023-06-19T14:26:44.24"",
      ""createdByProgram"": ""CP-9899"",
      ""createdBy"": ""JBURKE"",
      ""lastModifiedDateTime"": ""2023-06-19T14:26:44.24"",
      ""lastModifiedByProgram"": ""CP-9899"",
      ""lastModifiedBy"": ""JBURKE""
    }
  ]
}";

        [Theory]
        [InlineData(WhenEdiDeliveryClientBoardingConfigJson)]
        public void WhenHappyPathEdiDeliveryShouldDeserialize(string json)
        {
            var response = Newtonsoft.Json.JsonConvert
                .DeserializeObject<Pagination<EdiDeliveryClientBoardingConfigDto>>(json);
            var serializedResponse = Newtonsoft.Json.JsonConvert
                .SerializeObject(response);
            var redeserializedResponse = Newtonsoft.Json.JsonConvert
                .DeserializeObject<Pagination<EdiDeliveryClientBoardingConfigDto>>(serializedResponse);

            response.Should().NotBeNull();
            redeserializedResponse.Should().NotBeNull();
            response.Should().BeEquivalentTo(redeserializedResponse);
            redeserializedResponse.Items[0].Destination.Should().Be(EdiDeliveryDestination.None);
            redeserializedResponse.Items[0].FileExtension.Should().Be(EdiDeliveryFileExtension.None);
            redeserializedResponse.Items[1].Destination.Should().Be(EdiDeliveryDestination.None);
            redeserializedResponse.Items[1].FileExtension.Should().Be(EdiDeliveryFileExtension.None);
            redeserializedResponse.Items[2].Destination.Should().Be(EdiDeliveryDestination.Availity);
            redeserializedResponse.Items[2].FileExtension.Should().Be(EdiDeliveryFileExtension.Edi835);
            redeserializedResponse.Items[3].Destination.Should().Be(EdiDeliveryDestination.Availity);
            redeserializedResponse.Items[3].FileExtension.Should().Be(EdiDeliveryFileExtension.EdiBRN);
        }

        [Theory]
        [MemberData(nameof(WhenHappyPathGetEdiDeliveryByClientExpectations))]
        public async Task WhenHappyPathGetEdiDeliveryByClientShouldReturn(string whenClient, string whenEntity, Pagination<EdiDeliveryClientBoardingConfigDto> expected)
        {
            var sut = GetSUT(CreateServiceProvider(
                DEV_API_NAME,
                DEV_API_PROTOCOL,
                DEV_API_HOST,
                DEV_API_PORT,
                expected));
            var actual = await sut.GetEdiDeliveryByClient(whenClient, whenEntity);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
