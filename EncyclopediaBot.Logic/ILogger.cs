using System;

namespace EncyclopediaBot.Logic
{
    public interface ILogger
    {
        void Fatal(string message, Guid? requestId = null, Exception exception = null);
        void Error(string message, Guid? requestId = null, Exception exception = null);
        void Info(string message, Guid? requestId = null);
        void Warning(string message, Guid? requestId = null, Exception exception = null);
        void Debug(string message, Guid? requestId = null);
        bool IsEnabled(LogLevel logLevel);
    }
}