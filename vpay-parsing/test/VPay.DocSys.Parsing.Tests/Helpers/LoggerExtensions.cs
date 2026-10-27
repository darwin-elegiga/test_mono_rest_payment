using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;

namespace VPay.DocSys.Parsing.Tests.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class LoggerExtensions
    {
        /// <summary>
        ///     This makes it easier to handle the callback when using the <see cref="ILogger" /> Log method.
        /// </summary>
        /// <param name="mockedLogger">The mocked logger.</param>
        /// <param name="callback">The callback to perform when the Log method is invoked.</param>
        /// <typeparam name="TLogger">The type of the logger.</typeparam>
        /// <returns>The setup information for the mock.</returns>
        /// <remarks>
        ///     Note that this will perform the Moq setup for the Log method and handle the callback.
        /// </remarks>
        public static ISetup<ILogger<TLogger>> SetupCallback<TLogger>(
            this Mock<ILogger<TLogger>> mockedLogger,
            Action<LogLevel, EventId, IReadOnlyList<KeyValuePair<string, object>>, Exception, Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string>> callback = null)
        {
            ISetup<ILogger<TLogger>> setup = mockedLogger
                .Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            setup.Callback((IInvocation invocation) =>
            {
                var logLevel = (LogLevel)invocation.Arguments[0];
                var eventId = (EventId)invocation.Arguments[1];
                var state = (IReadOnlyList<KeyValuePair<string, object>>)invocation.Arguments[2];
                var exception = (Exception)invocation.Arguments[3];

                callback?.Invoke(logLevel, eventId, state, exception, Formatter);

                string Formatter(IReadOnlyList<KeyValuePair<string, object>> pairs, Exception error)
                {
                    return (invocation.Arguments[4] as Delegate)?.DynamicInvoke(pairs, error) as string;
                }
            });

            return setup;
        }
    }
}
