using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests.Utilities
{
    public class XUnitLogger : ILogger, IDisposable
    {
        #region constants, constructors, privates

        public XUnitLogger(ITestOutputHelper output, string categoryName)
        {
            _output = output;
            _categoryName = categoryName;
        }

        private readonly ITestOutputHelper _output;
        private readonly string _categoryName;

        #endregion

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _output.WriteLine("[{0}:{1}:{2}] {3}",
                logLevel.ToString(),
                eventId.ToString(),
                _categoryName,
                formatter(state, exception));
        }

        public void Dispose()
        {
            return;
        }
    }

    public class XUnitLoggerProvider : ILoggerProvider
    {
        #region constants, constructors, privates

        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        #endregion

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_output, categoryName);
        }

        public void Dispose()
        {
            return;
        }
    }
}
