using System;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.Queuing;

using Xunit;

namespace VPay.Distribution.Tests
{
    public class FaxMessagePublisherTests
    {
        private readonly TiffQueueConfigurationSettings _tiffQueueConfigurationSettings;
        private readonly Mock<IMessagePublisherService> _messagePublisherService = new Mock<IMessagePublisherService>();

        private readonly IFaxMessagePublisher SUT;

        private readonly string _origin = "TEST ORIGIN";
        private readonly string _be = "TEST BE";
        private readonly string _tpa = "TEST TPA";

        public FaxMessagePublisherTests()
        {
            _tiffQueueConfigurationSettings = new TiffQueueConfigurationSettings
            {
                MaxPriority = 10,
                QueueName = "TEST QUEUE"
            };

            _messagePublisherService
               .Setup(_ => _.PublishMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), It.IsAny<byte>()))
               .Verifiable();

            SUT = new FaxMessagePublisher(
                _tiffQueueConfigurationSettings,
                _messagePublisherService.Object,
                new NullLogger<FaxMessagePublisher>());
        }

        [Fact]
        public void ShouldDoNothingIfNoFaxesToPublish()
        {
            SUT.PublishToTiffQueue("TEST ORIGIN", "TEST TPA", "TEST BE", DateTime.Now);

            _messagePublisherService.Verify(_ => _.PublishMessage(It.IsAny<TiffQueueConfigurationSettings>(), It.IsAny<string>(), It.IsAny<byte>()), Times.Never);
        }

        [Fact]
        public void ShouldThrowExceptionsForNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => SUT.PublishToTiffQueue(null, _tpa, _be, DateTime.Now));
            Assert.Throws<ArgumentNullException>(() => SUT.PublishToTiffQueue(_origin, null, _be,
               DateTime.Now));
            Assert.Throws<ArgumentNullException>(() => SUT.PublishToTiffQueue(_origin, _tpa, null,
               DateTime.Now));
            Assert.Throws<ArgumentNullException>(() => SUT.PublishToTiffQueue(_origin, _tpa, _be,
               DateTime.Now, null));
        }

        [Fact]
        public void ShouldPublishMessageForEachDistTxnData()
        {
            var data = GetData();

            SUT.PublishToTiffQueue(_origin, _tpa, _be, DateTime.Now, data);

            _messagePublisherService.Verify(_ => _.PublishMessage(It.IsAny<TiffQueueConfigurationSettings>(), It.IsAny<string>(), It.IsAny<byte>()), Times.Exactly(data.Length));
        }

        [Fact]
        public void ShouldSetPriorityProperlyForEachMessagePublished()
        {
            var data = GetData();

            foreach (var d in data)
            {
                SUT.PublishToTiffQueue(_origin, _tpa, _be, DateTime.Now, d);

                var priority = d.Priority.ConvertAndScalePriority(100, _tiffQueueConfigurationSettings.MaxPriority);

                _messagePublisherService.Verify(_ => _.PublishMessage(It.IsAny<TiffQueueConfigurationSettings>(), It.IsAny<string>(), It.Is<byte>(__ => __ == priority)), Times.Once);
            }
        }

        [Fact]
        public void ShouldThrowExceptionIfMissingCorrespondenceIdInAnyDistTxnData()
        {
            var data = GetData();
            data[0].CorrespondenceId = null;

            Assert.Throws<ArgumentException>(() => SUT.PublishToTiffQueue(_origin, _tpa, _be, DateTime.Now, data));
        }

        private DistTxnData[] GetData() => new[]
        {
            new DistTxnData
            {
                CorrespondenceId = 10000,
                TxnId = 10000,
                Priority = 1,
                FaxNumber = "TEST FAXNUMBER",
                PayType = "TEST PAYTYPE",
                PrintVendor = "TEST PRINTVENDOR",
                DistType = 'T'
            },
            new DistTxnData
            {
                CorrespondenceId = 20000,
                TxnId = 20000,
                Priority = 40,
                FaxNumber = "TEST FAXNUMBER2",
                PayType = "TEST PAYTYPE2",
                PrintVendor = "TEST PRINTVENDOR2",
                DistType = 'T'
            }
        };
    }
}
