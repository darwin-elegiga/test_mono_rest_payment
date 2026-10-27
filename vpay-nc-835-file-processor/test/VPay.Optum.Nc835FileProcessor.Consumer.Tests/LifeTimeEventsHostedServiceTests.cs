using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Generic;

namespace VPay.Optum.Nc835FileProcessor.Consumer.Tests
{
    public class LifetimeEventsHostedServiceTests
    {
        private readonly MockLogger<LifetimeEventsHostedService> _mockLogger; // Changed type to MockLogger
        private readonly Mock<IHostApplicationLifetime> _mockHostLifetime;
        private readonly LifetimeEventsHostedService _service;
        private readonly TestHostApplicationLifetime _testHostLifetime;

        public LifetimeEventsHostedServiceTests()
        {
            _testHostLifetime = new TestHostApplicationLifetime();
            _mockLogger = new MockLogger<LifetimeEventsHostedService>(); // Updated initialization
            _mockHostLifetime = new Mock<IHostApplicationLifetime>();
            _service = new LifetimeEventsHostedService(_mockLogger, _mockHostLifetime.Object);
        }


        [Fact]
        public async Task StopAsync_DoesNothing()
        {
            // Act
            await _service.StopAsync(CancellationToken.None);

            // Assert
            // No exceptions should be thrown, and no interactions with logger or host lifetime
            Assert.Empty(_mockLogger.LoggedMessages);
            _mockHostLifetime.VerifyNoOtherCalls();
        }

        private class TestHostApplicationLifetime : IHostApplicationLifetime
        {
            private readonly ActionList _applicationStarted = new();
            private readonly ActionList _applicationStopping = new();
            private readonly ActionList _applicationStopped = new();

            public CancellationToken ApplicationStarted => _applicationStarted.Token;
            public CancellationToken ApplicationStopping => _applicationStopping.Token;
            public CancellationToken ApplicationStopped => _applicationStopped.Token;

            public void StopApplication()
            {
                // Implementation for StopApplication can be added here if needed.
                // For now, it can be left empty if no specific behavior is required.
            }

            public void TriggerApplicationStarted() => _applicationStarted.Invoke();
            public void TriggerApplicationStopping() => _applicationStopping.Invoke();
            public void TriggerApplicationStopped() => _applicationStopped.Invoke();

            private class ActionList
            {
                private readonly CancellationTokenSource _cts = new();
                private readonly List<Action> _callbacks = new();

                public CancellationToken Token => _cts.Token;

                public void Invoke()
                {
                    foreach (var callback in _callbacks)
                    {
                        callback();
                    }
                }

                public void Register(Action callback)
                {
                    _callbacks.Add(callback);
                }
            }
        }

        private class MockLogger<T> : ILogger<T>
        {
            public List<string> LoggedMessages { get; } = new();

            // Explicit interface implementation without re-specifying constraints
            IDisposable? ILogger.BeginScope<TState>(TState state)
            {
                return null; // Explicitly returning null to resolve CS8603
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                LoggedMessages.Add(formatter(state, exception));
            }
        }
    }
}
