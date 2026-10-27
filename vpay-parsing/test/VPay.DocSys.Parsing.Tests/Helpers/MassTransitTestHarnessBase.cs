using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace VPay.DocSys.Parsing.Tests.Helpers
{
    [ExcludeFromCodeCoverage]
    public abstract class MassTransitTestHarnessBase : IDisposable, IAsyncDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ILoggerFactory _loggerFactory;

        protected bool IsDisposed { get; private set; }
        protected bool IsDisposedAsync { get; private set; }

        protected ServiceProvider Provider { get; }

        protected MassTransitTestHarnessBase(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder
                        .ClearProviders()
                        .AddXUnit(testOutputHelper)
                        .AddFilter(level => level >= LogLevel.Debug);
                })
                .AddMassTransitInMemoryTestHarness(ConfigureBus);

            // ReSharper disable once VirtualMemberCallInConstructor
            ConfigureServices(services);

            Provider = services.BuildServiceProvider(true);
            _loggerFactory = Provider.GetRequiredService<ILoggerFactory>();
        }

        #region Initialize/Dispose

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed || IsDisposedAsync)
            {
                return;
            }

            await DisposeAsyncCore().ConfigureAwait(false);

            IsDisposedAsync = true;

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                if (!IsDisposedAsync)
                {
                    DisposeAsync()
                        .AsTask()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
            }

            IsDisposed = true;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (IsDisposed || IsDisposedAsync)
            {
                return;
            }

            await Provider.DisposeAsync().ConfigureAwait(false);
        }

        #endregion Initialize/Dispose

        protected virtual void ConfigureServices(IServiceCollection services)
        {
        }

        protected virtual void ConfigureBus(IServiceCollectionBusConfigurator cfg)
        {
        }

        /// <summary>
        /// Helper to create a mock ILogger that gets hooked up to a real logger implementation as well.
        /// </summary>
        /// <typeparam name="T">The type of the logger.</typeparam>
        /// <returns>The mock logger.</returns>
        protected Mock<ILogger<T>> CreateMockLogger<T>()
        {
            /* NOTE: This is kind of hacky, but I want to be able to use the "Verify" method on the logger
             * while also having the message output as part of the test output.  A simple way to do that
             * is just to handle the callback of the "Log" method on the mocked logger and output that
             * using a non-mocked logger.
             */
            var loggerMock = new Mock<ILogger<T>>();
            var logger = _loggerFactory.CreateLogger(nameof(T));
            loggerMock.SetupCallback(logger.Log);
            return loggerMock;
        }

        protected TestHarness CreateTestHarness() =>
            new TestHarness(this);

        protected class TestHarness : IDisposable, IAsyncDisposable
        {
            private readonly TextWriter _textWriter;
            private bool _disposed;
            private bool _disposedAsync;

            public TestHarness(MassTransitTestHarnessBase app)
            {
                _textWriter = new TestOutputHelperTextWriterAdapter(app._testOutputHelper);
                Harness = app.Provider.GetRequiredService<InMemoryTestHarness>();
            }

            /// <summary>
            /// Gets the test harness associated with current test.
            /// </summary>
            public InMemoryTestHarness Harness { get; }

            public Task Start(CancellationToken cancellationToken = default) =>
                Harness.Start(cancellationToken);

            #region Initialize/Dispose

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            public async ValueTask DisposeAsync()
            {
                if (_disposed || _disposedAsync)
                {
                    return;
                }

                await DisposeAsyncCore().ConfigureAwait(false);

                _disposedAsync = true;

                Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
                GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing && !_disposedAsync)
                {
                    DisposeAsync()
                        .AsTask()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }

                _disposed = true;
            }

            protected virtual async ValueTask DisposeAsyncCore()
            {
                if (_disposed || _disposedAsync)
                {
                    return;
                }

                await Harness.Stop().ConfigureAwait(false);

                await _textWriter.DisposeAsync().ConfigureAwait(false);
                Harness.Dispose();
            }

            #endregion Initialize/Dispose

            #region Verification Helpers

            public async Task VerifyMessageWasPublished<TMessage>()
                where TMessage : class
            {
                // verify the message was published
                (await Harness.Published.Any<TMessage>().ConfigureAwait(false)).Should().BeTrue();
            }

            public async Task VerifyMessageWasPublishedAndConsumed<TMessage>()
                where TMessage : class
            {
                await VerifyMessageWasPublished<TMessage>().ConfigureAwait(false);

                // verify the endpoint consumed the message
                (await Harness.Consumed.Any<TMessage>().ConfigureAwait(false)).Should().BeTrue();
            }

            public async Task VerifyConsumerConsumedMessage<TConsumer, TMessage>(IConsumerTestHarness<TConsumer> consumerTestHarness)
                where TConsumer : class, IConsumer
                where TMessage : class
            {
                // verify the actual consumer consumed the message
                (await consumerTestHarness.Consumed.Any<TMessage>().ConfigureAwait(false)).Should().BeTrue();
            }

            public async Task VerifyMessageWasNotFaulted<TMessage>()
                where TMessage : class
            {
                // verify no faults were published
                (await Harness.Published.Any<Fault<TMessage>>().ConfigureAwait(false)).Should().BeFalse();
            }

            public async Task VerifyMessageWasFaulted<TMessage>()
                where TMessage : class
            {
                // verify no faults were published
                (await Harness.Published.Any<Fault<TMessage>>().ConfigureAwait(false)).Should().BeTrue();
            }

            public bool HasException<TException>(Fault fault, string errorMessage)
            {
                return fault.Exceptions.Any(exceptionInfo =>
                    exceptionInfo.ExceptionType == typeof(TException).FullName
                    && exceptionInfo.Message == errorMessage);
            }

            #endregion Verification Helpers
        }
    }
}
