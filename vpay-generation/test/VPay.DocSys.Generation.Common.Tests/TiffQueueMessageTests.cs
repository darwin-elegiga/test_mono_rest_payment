using Newtonsoft.Json;

using VPay.DocSys.Common;

using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class TiffQueueMessageTests
    {
        [Theory]
        [InlineData("WARRANTY", FaxReserveName.WARRANTY)]
        [InlineData("wARRANTY", FaxReserveName.WARRANTY)]
        [InlineData("Warranty", FaxReserveName.WARRANTY)]
        [InlineData("warranty", FaxReserveName.WARRANTY)]

        [InlineData("MEDICAL", FaxReserveName.MEDICAL)]
        [InlineData("mEDICAL", FaxReserveName.MEDICAL)]
        [InlineData("Medical", FaxReserveName.MEDICAL)]
        [InlineData("medical", FaxReserveName.MEDICAL)]

        [InlineData("CSS", FaxReserveName.CSS)]
        [InlineData("cSS", FaxReserveName.CSS)]
        [InlineData("Css", FaxReserveName.CSS)]
        [InlineData("css", FaxReserveName.CSS)]
        public void ShouldSetReserveNameCorrectly(string actual, FaxReserveName expected)
        {
            var obj = actual == null ? new object() : new { ReserveName = actual };
            var json = JsonConvert.DeserializeObject<TiffQueueMessage>(JsonConvert.SerializeObject(obj));

            Assert.Equal(expected, json.ReserveName);
        }
    }
}
