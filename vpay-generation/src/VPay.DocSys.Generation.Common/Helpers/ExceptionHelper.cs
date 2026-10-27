using System;

using Microsoft.Extensions.Logging;

namespace VPay.DocSys.Generation.Common.Helpers
{
    public static class ExceptionHelper
    {
        /// <summary>
        /// Use with try / catch outside of <see cref="ILogger.BeginScope{TState}(TState)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="logger"></param>
        /// <param name="logLevel">defaults to <see cref="LogLevel.Error"/></param>
        /// <param name="message">defaults to null</param>
        /// <returns></returns>
        public static bool LogWithScope(this Exception e, ILogger logger, LogLevel logLevel = LogLevel.Error, string message = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                logger.Log(logLevel, e, message, e.Message);
            }
            else
            {
                logger.Log(logLevel, e, e.Message);
            }

            return true;
        }
    }
}
