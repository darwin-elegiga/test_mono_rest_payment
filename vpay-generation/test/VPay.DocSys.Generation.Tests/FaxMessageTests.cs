using System.Collections.Generic;

using VPay.DocSys.Generation.Models;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class FaxMessageTests
    {
        [Fact]
        public void MessageTerminatorTest()
        {
            FaxMessage msg = new FaxMessage()
            {
                TransactionIds = new List<string>() { "368790718", "368790777" },
                FaxNumber = "1-469-543-6599",
                PDFLocation = "/Fax/queued/blah.pdf",
                PageCount = 0
            };
            Assert.EndsWith(",|", msg.ToString());
        }
    }
}
