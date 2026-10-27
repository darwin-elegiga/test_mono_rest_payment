using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace VPay.Optum.PaymentRouting.TestsCommon.Extensions
{
    // Helper extension for verifying logs
    public static class LoggerMockExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, Times times)
        {
            loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                times
            );
        }
    }
}
