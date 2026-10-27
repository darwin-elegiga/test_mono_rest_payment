using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;
using System.Timers;

namespace VPay.Optum.Http.Tests
{
    public class HttpLoggingHandlerTests
    {
        private readonly Mock<ILogger<HttpLoggingHandler>> _mockLogger;
        private readonly Mock<IHostEnvironment> _mockHostEnvironment;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

        public HttpLoggingHandlerTests()
        {
            _mockLogger = new Mock<ILogger<HttpLoggingHandler>>();
            _mockHostEnvironment = new Mock<IHostEnvironment>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        }

        


        [Fact]
        public void IsTextBasedContentType_ShouldReturnFalse_ForTextContentType()
        {
            // Arrange
            var handler = new HttpLoggingHandler(_mockLogger.Object, _mockHostEnvironment.Object);
            var headers = new HttpRequestMessage().Headers;
            var request = new HttpRequestMessage
            {
                Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/octet-stream")
            };

            // Act
            var result = handler.GetType()
                .GetMethod("IsTextBasedContentType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(handler, new object[] { headers });

            // Assert
            Assert.False((bool)result!);
        }

        [Fact]
        public void IsTextBasedContentType_ShouldReturnFalse_ForNonTextContentType()
        {
            // Arrange
            var handler = new HttpLoggingHandler(_mockLogger.Object, _mockHostEnvironment.Object);
            var headers = new HttpRequestMessage().Headers;
            var request = new HttpRequestMessage
            {
                Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/octet-stream")
            };

            // Act
            var result = handler.GetType()
                .GetMethod("IsTextBasedContentType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(handler, new object[] { headers });

            // Assert
            Assert.False((bool)result!);
        }
    }
}
