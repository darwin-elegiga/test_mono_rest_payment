using System.Text.RegularExpressions;
using VPay.DocSys.Generation.Common.Helpers;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class RegexHelperTests
    {
        [Theory]
        [InlineData("AAA", "aaa")]
        [InlineData("(AA)A", "ABABA")]
        [InlineData("(A)", "")]
        public void ShouldReturnNullForNoSuccessfulMatch(string pattern, string input)
        {
            var actual = new Regex(pattern).Match(input).GetLastSuccess();

            Assert.Null(actual);
        }

        [Theory]
        [InlineData("(A)AA", "AAA", "A")]
        [InlineData("(A)", "ABABA", "A")]
        [InlineData(@"(\SA3)", "1A32A34A3", "4A3")]
        public void ShouldReturnLastSuccessfulMatchOnly(string pattern, string input, string expected)
        {
            var actual = new Regex(pattern).Matches(input).GetLastSuccess();

            Assert.Equal(expected, actual);
        }
    }
}
