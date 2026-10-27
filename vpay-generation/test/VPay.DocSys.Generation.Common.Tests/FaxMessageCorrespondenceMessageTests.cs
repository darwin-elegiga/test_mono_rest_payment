using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class FaxMessageCorrespondenceMessageTests
    {
        [Fact]
        public void ShouldCreateValidCorrespondenceHeader()
        {
            var result = new FaxMessageCorrespondenceMessage(new FaxMessageCorrespondencePayload()).ToString()
                .Substring(0, 275);

            var part1 = "PMB00.00.0001FAXNET              FAXISERIES          FAXDIST             0000000000FAXI000000050001    ";
            var part2 = "                    ";
            var part3 = "0                                                                                             ";

            Assert.Equal(part1, result.Substring(0, 103));
            Assert.Equal(part2, result.Substring(135, 20));
            Assert.Equal(part3, result.Substring(181));
        }
    }
}
