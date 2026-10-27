using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace VPay.DocSys.Parsing.QueueProcessor.Consolidated
{
    public class PerfTimerLogger : IDisposable
    {
        private readonly string _message;
        private readonly Stopwatch _timer;
        private readonly ILogger _logger;

        public PerfTimerLogger(ILogger logger, string message)
        {
            _logger = logger;
            _message = message;
            _timer = new Stopwatch();
            _timer.Start();
        }
        public void Dispose()
        {
            this._timer.Stop();
            var ms = this._timer.ElapsedMilliseconds;
            _logger.LogInformation($"{_message} in {ms} ms");
        }
    }
}
