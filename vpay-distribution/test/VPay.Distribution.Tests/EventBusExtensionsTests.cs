using Bogus;

using System;
using System.Collections.Generic;

using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Storage;

using Xunit;

namespace VPay.Distribution.Tests
{
    public class EventBusExtensionsTests
    {
        Faker Faker = new Faker();

        [Fact]
        public void MapToFileInformationShouldBeNullForNull()
        {
            var result = (null as IArchiveDetail).MapToFileInformation();

            Assert.Null(result);
        }

        [Fact]
        public void MapToFileInformationShouldMappedCorrectlyFromArchiveDetail()
        {
            var actual = new ArchiveDetail
            {
                ArchiveDetailId = Faker.Random.Long(1000),
                Description = Faker.Random.String2(5),
                FileDetailType = FileDetailType.BundledFax,
                FileName = Faker.Random.String2(5),
            };

            var result = actual.MapToFileInformation();

            Assert.NotNull(result);
            Assert.Equal(result.ArchiveDetailId, actual.ArchiveDetailId);
            Assert.Equal(result.Description, actual.Description);
            Assert.Equal(result.FileName, actual.FileName);
        }

        [Theory]
        [MemberData(nameof(DocSysFileDetailTypes))]
        public void ShouldMapFileDetailTypeProperly(FileDetailType dfdt)
        {
            var actual = EventBusExtensions.MapFileDetailType(dfdt);

            Assert.Equal(dfdt.ToString(), actual.ToString());
        }

        public static IEnumerable<object[]> DocSysFileDetailTypes
        {
            get
            {
                foreach (FileDetailType item in Enum.GetValues(typeof(FileDetailType)))
                {
                    yield return new object[] { item };
                }
            }
        }
    }
}
