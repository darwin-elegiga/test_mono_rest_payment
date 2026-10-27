using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.Queuing;
using VPay.DocSys.Parsing.Common.Rabbitmq;

namespace VPay.DocSys.Parsing.Inbound835FileQueueProcessor
{
    public class MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        private IMessageReceiver _queueReceiver;

        public MessageProcessor(
            IServiceProvider serviceProvider,
            IFileSystem fileSystem,
            ILogger<MessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _logger = logger;
            _fileSystem = fileSystem;
            _serviceProvider = serviceProvider;
        }


        public void Start()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var queueSettings =
                    scope.ServiceProvider
                        .GetRequiredService<Inbound835FileQueueSettings>();

                var beginLogScope = new Dictionary<string, object>();
                foreach (var kvp in queueSettings.ToDictionaryExcept(nameof(Inbound835FileQueueSettings.Password)))
                {
                    beginLogScope.Add(kvp.Key.Replace(Environment.NewLine, ""), kvp.Value?.ToString().Replace(Environment.NewLine, ""));
                }

                using (_logger.BeginScope(beginLogScope))
                {
                    _logger.LogInformation($"{this.GetType().FullName} starting.");
                }

                _queueReceiver = QuorumReceiverFactory.GetReceiver(queueSettings, async (string s) => await this.ProcessMessage(s), _logger);

                _queueReceiver.Consume();
            }
        }

        public void Stop()
        {
            _queueReceiver.Stop();
        }


        private Task<bool> ProcessMessage(string message)
        {
            return this.ProcessMessage(JsonConvert.DeserializeObject<Inbound835FileMessage>(message));
        }

        private async Task<bool> ProcessMessage(Inbound835FileMessage message)
        {
            using(BeginLogScope(message))
            using(new PerfTimerLogger(_logger, $"{nameof(Inbound835FileMessage)} Processed"))
            {
                try
                {
                    _logger.LogInformation($"Processing {nameof(Inbound835FileMessage)}: {message.FilePath}");

                    using(var scope = _serviceProvider.CreateScope())
                    {
                        await scope
                            .ServiceProvider
                            .GetRequiredService<IInbound835FileService>()
                            .ProcessFile(message);
                    }

                    _logger.LogInformation($"Processing {nameof(Inbound835FileMessage)} complete: {message.FilePath}");

                    return true;
                }
                catch(Exception exception)
                {
                    while(exception is AggregateException)
                        exception = exception.InnerException;

                    _logger.LogError(exception, $"Processing {nameof(Inbound835FileMessage)} failed: {exception.Message}");

                    throw exception;
                }
            }
        }

        private IDisposable BeginLogScope(Inbound835FileMessage message)
        {
            return _logger.BeginScope(new Dictionary<string, object>
            {
                { "RequestId", Guid.NewGuid() },
                { "FilePath",  message.FilePath },
                { "FileName", _fileSystem.Path.GetFileName(message.FilePath) },
                { "BaseFileName", _fileSystem.Path.GetFileNameWithoutExtension(message.FilePath) },
                { "MessageType", message.GetType().Name },
            });
        }
    }
}
