using System;
using x3squaredcircles.MobileAdapter.Generator.Configuration;

namespace x3squaredcircles.MobileAdapter.Generator.Logging
{
    public interface ILogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception exception);
        void SetLogLevel(LogLevel level);
        void SetVerbose(bool verbose);
    }
}