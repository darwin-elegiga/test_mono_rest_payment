using System;
using System.Collections.Generic;

using VPay.DocSys.Common;

using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class FaxJobQueueMessageTest
    {
        [Fact]
        public void ShouldCreateValidMessageFromFaxJobRequest()
        {
            var tqm = new TiffQueueMessage
            {
                CorrespondenceId = 123L,
                TransactionIds = new List<long> { 456L },
                Priority = 99
            };

            var actual = new FaxJobQueueMessage(new FaxJobRequest(tqm, 1, 1, new FaxJobSettings { Scale = 1, MaxHoursCap = 1 }));

            Assert.Equal(tqm.CorrespondenceId, actual.CorrespondenceId);
            Assert.Equal(string.Join(",", tqm.TransactionIds.ToArray()), actual.AdditionalInfo);
            Assert.Equal(tqm.Priority, actual.Priority);
            Assert.Equal("FaxJobInsertRequest", actual.PayloadType);
            Assert.NotNull(actual.Payload);
        }

        [Fact]
        public void ShouldCreateValidMessageFromKillFaxNumberRequest()
        {
            var priority = 77;
            var kfn = new KillFaxNumberRequest("FaxNumber");
            var actual = new FaxJobQueueMessage(kfn, priority);

            Assert.Equal(0, actual.CorrespondenceId);
            Assert.Equal("", actual.AdditionalInfo);
            Assert.Equal(priority, actual.Priority);
            Assert.Equal("FaxNumberKillRequest", actual.PayloadType);
            Assert.True(actual.Payload.Contains(kfn.PhoneNumber, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public void ShouldCreateValidMessageFromFaxNumberUpdateRequest()
        {
            var priority = 4;
            var fnu = new FaxNumberUpdateRequest
            {
                CorrespondenceId = 907893405L,
                PhoneNumber = "TESTPH"
            };

            var actual = new FaxJobQueueMessage(fnu, priority);

            Assert.Equal(fnu.CorrespondenceId, actual.CorrespondenceId);
            Assert.Equal("", actual.AdditionalInfo);
            Assert.Equal(priority, actual.Priority);
            Assert.Equal("FaxNumberChangeRequest", actual.PayloadType);
            Assert.True(actual.Payload.Contains(fnu.PhoneNumber, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public void ShouldCreateValidMessageFromFaxJobUpdateRequest()
        {
            var priority = 4;
            var fju = new FaxJobUpdateRequest
            {
                CorrespondenceId = 907893405L,
                Status = "TESTStatus"
            };

            var actual = new FaxJobQueueMessage(fju, priority);

            Assert.Equal(fju.CorrespondenceId, actual.CorrespondenceId);
            Assert.Equal("", actual.AdditionalInfo);
            Assert.Equal(priority, actual.Priority);
            Assert.Equal("FaxJobStatusUpdateRequest", actual.PayloadType);
            Assert.True(actual.Payload.Contains(fju.Status, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
