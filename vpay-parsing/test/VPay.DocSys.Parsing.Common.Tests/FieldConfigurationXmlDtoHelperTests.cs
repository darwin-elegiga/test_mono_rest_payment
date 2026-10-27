using System.Collections.Generic;
using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class FieldConfigurationXmlDtoHelperTests
    {
        List<FieldConfigurationXmlDto> _testData;

        public FieldConfigurationXmlDtoHelperTests()
        {
            _testData = new List<FieldConfigurationXmlDto>
            {
                new FieldConfigurationXmlDto
                {
                    Name="DUPE NAME",
                    Value="VALUE 1"
                },
                new FieldConfigurationXmlDto
                {
                    Name="DUPE NAME",
                    Value="VALUE 2"
                },
                new FieldConfigurationXmlDto
                {
                    Name="UNIQUE NAME",
                    Value="UNIQUE VALUE"
                },
            };
        }

        [Fact]
        public void ShouldGetLastWhenMoreThanOneFieldWithTheSameName()
        {
            var actual = _testData.ConsolidateFields();

            Assert.Contains(actual, _ => _.Value == "VALUE 2");
        }

        [Fact]
        public void ShouldGetSingleIfOnlyOneEntryForFieldWithName()
        {
            var actual = _testData.ConsolidateFields();

            Assert.Contains(actual, _ => _.Value == "UNIQUE VALUE");

        }
    }
}
