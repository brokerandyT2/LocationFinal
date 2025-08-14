using System;
using System.IO;
using x3squaredcircles.MobileAdapter.Generator.Configuration;

namespace x3squaredcircles.MobileAdapter.Generator.Logging
{
    public class FileLogger : ILogger
    {
        private const string LogFileName = "pipeline-tools.log";
        private const string ToolName = "mobile-adapter-generator";
        private const string Version = "1.0.0";

        private LogLevel _logLevel = LogLevel.Info;
        private bool _verbose = false;

        public FileLogger()
        {
            // Log tool execution to pipeline-tools.log (append-only)
            LogToolExecution();
        }

        public void LogDebug(string message)
        {
            if (_logLevel <= LogLevel.Debug || _verbose)
            {
                WriteToStdOut("DEBUG", message);
            }
        }

        public void LogInfo(string message)
        {
            if (_logLevel <= LogLevel.Info)
            {
                WriteToStdOut("INFO", message);
            }
        }

        public void LogWarning(string message)
        {
            if (_logLevel <= LogLevel.Warning)
            {
                WriteToStdOut("WARN", message);
            }
        }

        public void LogError(string message)
        {
            WriteToStdOut("ERROR", message);
        }

        public void LogError(string message, Exception exception)
        {
            WriteToStdOut("ERROR", $"{message}: {exception}");
        }

        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        public void SetVerbose(bool verbose)
        {
            _verbose = verbose;
        }

        private void WriteToStdOut(string level, string message)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            Console.WriteLine(logEntry);
        }

        private void LogToolExecution()
        {
            try
            {
                var executionEntry = $"{ToolName}={Version}";
                File.AppendAllText(LogFileName, executionEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to write tool execution to pipeline log: {ex.Message}");
            }
        }
    }
}