using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class ReminderFaxMessageQueuerTests
    {
        private readonly QueueConfigurations _queueConfigurations = new QueueConfigurations();
        private readonly Mock<IFax> _faxRepository = new Mock<IFax>();
        private readonly Mock<IQueueService> _queueService = new Mock<IQueueService>();

        private readonly IReminderFaxMessageQueuer SUT;

        public ReminderFaxMessageQueuerTests()
        {
            _queueConfigurations.ReminderFaxQueue = new QueueConfigurationSettings
            {
                QueueName = "RFAX-TEST",

            };

            _queueService.Setup(_ => _.PublishQueueMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), 0));

            SUT = new ReminderFaxMessageQueuer(
                _queueConfigurations,
                _faxRepository.Object,
                _queueService.Object,
                new NullLogger<IMessagePublisher>(),
                new NullLogger<ReminderFaxMessageQueuer>());
        }

        [Theory]
        [MemberData(nameof(CtorValues))]
        public void ShouldThrowExceptionForInvalidCtorParameters(
            Type exceptionType,
            QueueConfigurations queueConfigurations,
            IFax faxRepository,
            IQueueService queueService,
            ILogger<IMessagePublisher> messageLogger,
            ILogger<ReminderFaxMessageQueuer> logger)
        {
            Assert.Throws(exceptionType, () => new ReminderFaxMessageQueuer(queueConfigurations, faxRepository, queueService, messageLogger, logger));
        }

        [Theory]
        [MemberData(nameof(TestReminderFaxes))]
        public async Task ReminderFaxQueueMessagesShouldSplitOnTpa(IEnumerable<ReminderFax> faxes, int tpaCount)
        {
            _faxRepository
                .Setup(_ => _.GetReminderFaxes(It.IsAny<ClientType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(faxes)
                .Verifiable();

            await SUT.QueueReminderFaxes(ClientType.BOTH, DateTime.Now, DateTime.Now);

            _faxRepository.VerifyAll();

            _queueService.Verify(_ => _.PublishQueueMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), 0), Times.Exactly(tpaCount));
        }

        public static IEnumerable<object[]> CtorValues
        {
            get
            {
                var qc = new QueueConfigurations { ReminderFaxQueue = new QueueConfigurationSettings() };
                var fr = new Mock<IFax>().Object;
                var qs = new Mock<IQueueService>().Object;
                var mpnl = new NullLogger<IMessagePublisher>();

                yield return new object[] { typeof(NullReferenceException), null, null, null, null, null };
                yield return new object[] { typeof(ArgumentException), new QueueConfigurations(), null, null, null, null };
                yield return new object[] { typeof(ArgumentNullException), qc, null, null, null, null };
                yield return new object[] { typeof(ArgumentNullException), qc, fr, null, null, null };
                yield return new object[] { typeof(ArgumentNullException), qc, fr, qs, null, null };
                yield return new object[] { typeof(ArgumentNullException), qc, fr, qs, mpnl, null };
            }
        }

        public static IEnumerable<object[]> TestReminderFaxes
        {
            get
            {
                yield return new object[] { Enumerable.Empty<ReminderFax>(), 0 };

                yield return new object[] { new List<ReminderFax> { new ReminderFax() }, 1 };
                yield return new object[] { new List<ReminderFax> { new ReminderFax(), new ReminderFax(), }, 1 };
                yield return new object[] { new List<ReminderFax> { new ReminderFax(), new ReminderFax(), new ReminderFax(), }, 1 };

                yield return new object[] { new List<ReminderFax> { new ReminderFax { TPACode = "TPA1" } }, 1 };
                yield return new object[] { new List<ReminderFax> { new ReminderFax { TPACode = "TPA1" }, new ReminderFax { TPACode = "TPA2" }, }, 2 };
                yield return new object[] { new List<ReminderFax> { new ReminderFax { TPACode = "TPA1" }, new ReminderFax { TPACode = "TPA2" }, new ReminderFax { TPACode = "TPA3" }, }, 3 };
            }
        }

    }
}
