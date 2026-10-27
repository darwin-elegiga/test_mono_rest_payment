using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Parsing.Common.Extensions;
using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class LoggingHelperTests
    {
        // Tests for Sanitize(int)
        [Theory]
        [InlineData(-1, -1)]
        [InlineData(0, 0)]
        [InlineData(123, 123)]
        [InlineData(1_000_000_000, 1_000_000_000)]
        [InlineData(1_000_000_000 + 1L, 1_000_000_000)] // Overflow case
        [InlineData(-1_000_000_000 - 1L, -1_000_000_000)] // Overflow case
        public void Sanitize_Int_ReturnsExpected(int input, int expected)
        {
            var result = input.Sanitize();
            Assert.Equal(expected, result);
        }

        // Tests for Sanitize(IEnumerable<int>)
        [Fact]
        public void Sanitize_IEnumerableInt_ReturnsSanitizedValues()
        {
            var input = new List<int> { -5, 0, 100, 1_000_000_000, 1_000_000_000 + 1 };
            var expected = new List<int> { -5, 0, 100, 1_000_000_000, 1_000_000_000 };

            var result = input.Sanitize().ToList();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sanitize_IEnumerableInt_Empty_ReturnsEmpty()
        {
            var input = new List<int>();
            var result = input.Sanitize().ToList();

            Assert.Empty(result);
        }

        // Tests for Sanitize(long)
        [Theory]
        [InlineData(-10L, -10L)]
        [InlineData(0L, 0L)]
        [InlineData(500L, 500L)]
        [InlineData(1_000_000_000L, 1_000_000_000L)]
        [InlineData(1_000_000_000 + 1L, 1_000_000_000)] // Overflow case
        [InlineData(-1_000_000_000 - 1L, -1_000_000_000)] // Overflow case
        public void Sanitize_Long_ReturnsExpected(long input, long expected)
        {
            var result = input.Sanitize();
            Assert.Equal(expected, result);
        }

        // Tests for Sanitize(IEnumerable<long>)
        [Fact]
        public void Sanitize_IEnumerableLong_ReturnsSanitizedValues()
        {
            var input = new List<long> { -5L, 0L, 100L, 1_000_000_001L };
            var expected = new List<long> { -5L, 0L, 100L, 1_000_000_000L };

            var result = input.Sanitize().ToList();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Sanitize_IEnumerableLong_Empty_ReturnsEmpty()
        {
            var input = new List<long>();
            var result = input.Sanitize().ToList();

            Assert.Empty(result);
        }
    }
}
