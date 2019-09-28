using System;
using Microsoft.Extensions.Logging;

namespace EncyclopediaBot.Web
{
    public class WebLogger : Logic.ILogger
    {
        private ILogger _logger;

        public WebLogger(ILogger logger)
        {
            _logger = logger;
        }


        public void Debug(string message, Guid? requestId = null)
        {
            if (requestId != null)
            {

                _logger.LogDebug(string.Format("{0}, {1}", requestId.Value.ToString(), message));
            }
            else
            {
                _logger.LogDebug(message);
            }
        }

        public void Fatal(string message, Guid? requestId = null, Exception exception = null)
        {
            if (requestId != null)
            {

                _logger.LogCritical(string.Format("{0}, {1}", requestId.Value.ToString(), message));
            }
            else
            {
                _logger.LogCritical(message);
            }
        }

        public void Error(string message, Guid? requestId = null, Exception exception = null)
        {
            if (requestId != null)
            {

                _logger.LogError(string.Format("{0}, {1}", requestId.Value.ToString(), message));
            }
            else
            {
                _logger.LogError(message);
            }
        }

        public void Info(string message, Guid? requestId = null)
        {
            if (requestId != null)
            {

                _logger.LogInformation(string.Format("{0}, {1}", requestId.Value.ToString(), message));
            }
            else
            {
                _logger.LogInformation(message);
            }
        }

        public void Warning(string message, Guid? requestId = null, Exception exception = null)
        {
            if (requestId != null)
            {

                _logger.LogWarning(string.Format("{0}, {1}", requestId.Value.ToString(), message));
            }
            else
            {
                _logger.LogWarning(message);
            }
        }

        public bool IsEnabled(Logic.LogLevel logLevel)
        {
            var internalLevel = MapToLevel(logLevel);
            return _logger.IsEnabled(internalLevel);
            
        }

        private Microsoft.Extensions.Logging.LogLevel MapToLevel(Logic.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case Logic.LogLevel.Fatal:
                    return  LogLevel.Critical;
                case Logic.LogLevel.Error:
                    return  LogLevel.Error;
                case Logic.LogLevel.Warning:
                    return  LogLevel.Warning;
                case Logic.LogLevel.Info:
                    return  LogLevel.Information;
                case Logic.LogLevel.Debug:
                    return LogLevel.Debug;
                default:
                    return LogLevel.None;
            }
        }
    }
}