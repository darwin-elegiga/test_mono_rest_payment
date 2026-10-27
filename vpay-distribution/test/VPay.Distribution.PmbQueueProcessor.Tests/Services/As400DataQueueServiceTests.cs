using Moq;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;
using VPay.Distribution.PmbQueueProcessor.Services;
using VPay.Distribution.PmbQueueProcessor.Settings;

namespace VPay.Distribution.PmbQueueProcessor.Tests.Services
{
    public class As400DataQueueServiceTests
    {
        private readonly Mock<IAs400ConnectionPool> _connectionPoolMock;
        private readonly Mock<IAs400ObjectFactory> _objectFactoryMock;
        private readonly Mock<IAs400> _as400Mock;
        private readonly Mock<IDataQueue> _dataQueueMock;
        private readonly Mock<IAs400Text> _as400TextMock;
        private readonly As400Settings _settings;

        public As400DataQueueServiceTests()
        {
            _connectionPoolMock = new Mock<IAs400ConnectionPool>();
            _objectFactoryMock = new Mock<IAs400ObjectFactory>();
            _as400Mock = new Mock<IAs400>();
            _dataQueueMock = new Mock<IDataQueue>();
            _as400TextMock = new Mock<IAs400Text>();
            _settings = new As400Settings
            {
                SystemName = "SYS",
                UserId = "USER",
                Password = "PWD",
                DataQueueName = "QUEUE",
                LibraryName = "LIB"
            };

            _objectFactoryMock
                .Setup(f => f.CreateAs400(_settings.SystemName, _settings.UserId, _settings.Password))
                .Returns(_as400Mock.Object);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenConnectionPoolIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new As400DataQueueService(null!, _objectFactoryMock.Object, _settings));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenObjectFactoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new As400DataQueueService(_connectionPoolMock.Object, null!, _settings));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenSettingsIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new As400DataQueueService(_connectionPoolMock.Object, _objectFactoryMock.Object, null!));
        }

        [Fact]
        public void Constructor_SetsDataQueuePathCorrectly()
        {
            var service = new As400DataQueueService(_connectionPoolMock.Object, _objectFactoryMock.Object, _settings);
            // Use reflection to get private field value
            var field = typeof(As400DataQueueService).GetField("_dataQueuePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var value = field?.GetValue(service);
            Assert.Equal("/QSYS.LIB/LIB.LIB/QUEUE.DTAQ", value);
        }

        [Fact]
        public void WriteToQueue_ThrowsArgumentException_WhenMessageIsNull()
        {
            var service = new As400DataQueueService(_connectionPoolMock.Object, _objectFactoryMock.Object, _settings);
            Assert.Throws<ArgumentException>(() => service.WriteToQueue(null!));
        }

        [Fact]
        public void WriteToQueue_ThrowsArgumentException_WhenMessageIsEmpty()
        {
            var service = new As400DataQueueService(_connectionPoolMock.Object, _objectFactoryMock.Object, _settings);
            Assert.Throws<ArgumentException>(() => service.WriteToQueue(""));
        }

        [Fact]
        public void WriteToQueue_CallsDependenciesCorrectly()
        {
            var service = new As400DataQueueService(_connectionPoolMock.Object, _objectFactoryMock.Object, _settings);

            var testMessage = "TestMessage";
            var testBytes = new byte[] { 1, 2, 3 };

            _connectionPoolMock
                .Setup(p => p.GetDatqQueueConnection(_as400Mock.Object))
                .Returns(_as400Mock.Object);

            _objectFactoryMock
                .Setup(f => f.CreateDataQueue(_as400Mock.Object, "/QSYS.LIB/LIB.LIB/QUEUE.DTAQ"))
                .Returns(_dataQueueMock.Object);

            _objectFactoryMock
                .Setup(f => f.CreateAs400Text(testMessage.Length, _as400Mock.Object))
                .Returns(_as400TextMock.Object);

            _as400TextMock
                .Setup(t => t.ToBytes(testMessage))
                .Returns(testBytes);

            service.WriteToQueue(testMessage);

            _connectionPoolMock.Verify(p => p.GetDatqQueueConnection(_as400Mock.Object), Times.Once);
            _objectFactoryMock.Verify(f => f.CreateDataQueue(_as400Mock.Object, "/QSYS.LIB/LIB.LIB/QUEUE.DTAQ"), Times.Once);
            _objectFactoryMock.Verify(f => f.CreateAs400Text(testMessage.Length, _as400Mock.Object), Times.Once);
            _as400TextMock.Verify(t => t.ToBytes(testMessage), Times.Once);
            _connectionPoolMock.Verify(p => p.ReturnToConnectionPool(_as400Mock.Object), Times.Once);
        }
    }
}
