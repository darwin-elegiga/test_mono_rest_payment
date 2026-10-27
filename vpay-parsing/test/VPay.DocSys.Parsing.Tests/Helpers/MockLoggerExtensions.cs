using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;

namespace VPay.DocSys.Parsing.Tests.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class MockLoggerExtensions
    {
        public static Mock<ILogger<T>> VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, Times? times = null)
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ), times ?? Times.AtLeastOnce()
            );

            return logger;
        }
        public static Mock<ILogger<T>> VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, string expectedMessage, Times? times = null)
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ), times ?? Times.AtLeastOnce()
            );

            return logger;
        }
        public static Mock<ILogger<T>> VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, Func<string, bool> expectedMessage, Times? times = null)
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => expectedMessage(v.ToString())),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ), times ?? Times.AtLeastOnce()
            );

            return logger;
        }
        public static Mock<ILogger<T>> VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, Func<Exception, bool> expectedException, string expectedMessage, Times? times = null)
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                    It.Is<Exception>(e => expectedException(e)),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ), times ?? Times.AtLeastOnce()
            );

            return logger;
        }
        public static Mock<ILogger<T>> VerifyLog<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, Func<Exception, bool> expectedException, Func<string, bool> expectedMessage, Times? times = null)
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => expectedMessage(v.ToString())),
                    It.Is<Exception>(e => expectedException(e)),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ), times ?? Times.AtLeastOnce()
            );

            return logger;
        }
        public static Mock<ILogger<T>> VerifyLog<T, TException>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, Func<TException, bool> expectedException, string expectedMessage, Times? times = null)
            where TException : Exception
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                    It.Is<TException>(e => expectedException(e)),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ), times ?? Times.AtLeastOnce()
            );

            return logger;
        }
        public static Mock<ILogger<T>> VerifyLog<T, TException>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, Func<TException, bool> expectedException, Func<string, bool> expectedMessage, Times? times = null)
            where TException : Exception
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => expectedMessage(v.ToString())),
                    It.Is<TException>(e => expectedException(e)),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ), times ?? Times.AtLeastOnce()
            );

            return logger;
        }
    }
}
