using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public static class TestLogger
    {
        public static ILogger<T> Create<T>()
        {
            var logger = new Logger<T>();
            return logger;
        }

        public static ILogger<T> Create<T>(ITestOutputHelper outputHelper)
            => new Logger<T>(outputHelper.WriteLine);

        private class Logger<T> : ILogger<T>, IDisposable
        {
            public Logger()
                => _output = Console.WriteLine;
            public Logger(Action<string> output)
                => _output = output;

            private readonly Action<string> _output;

            public void Dispose()
            {
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter) => _output(formatter(state, exception));

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => this;
        }
    }
}
