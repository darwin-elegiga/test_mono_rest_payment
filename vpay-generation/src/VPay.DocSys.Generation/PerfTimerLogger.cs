using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

//TODO: move to DocSys.Common
namespace VPay.DocSys.Generation
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
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

            _logger.LogInformation($"{_message} in {ms} ms ({_timer.Elapsed:c})");
        }
    }
#pragma warning restore CA1063 // Implement IDisposable Correctly
}
