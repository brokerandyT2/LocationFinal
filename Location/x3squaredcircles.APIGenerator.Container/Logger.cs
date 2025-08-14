using System;
using System.IO;
using System.Threading;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Logger implementation that handles console and file logging with proper formatting
    /// </summary>
    public class Logger
    {
        private readonly LogLevel _logLevel;
        private readonly bool _verbose;
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        private readonly string _toolName;
        private readonly string _version;

        public enum LogLevel
        {
            DEBUG = 0,
            INFO = 1,
            WARN = 2,
            ERROR = 3
        }

        public Logger(Configuration config, string toolName = "api-generator", string version = "1.0.0")
        {
            _logLevel = ParseLogLevel(config.LogLevel);
            _verbose = config.Verbose;
            _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "pipeline-tools.log");
            _toolName = toolName;
            _version = version;

            // Write tool startup entry to pipeline-tools.log
            WritePipelineEntry();
        }

        public void Debug(string message)
        {
            Log(LogLevel.DEBUG, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.INFO, message);
        }

        public void Warn(string message)
        {
            Log(LogLevel.WARN, message);
        }

        public void Error(string message)
        {
            Log(LogLevel.ERROR, message);
        }

        public void Error(string message, Exception exception)
        {
            Log(LogLevel.ERROR, $"{message}\nException: {exception}");
        }

        public void LogConfiguration(Configuration config)
        {
            if (_verbose)
            {
                Info("=== API Generator Configuration ===");
                Info(config.ToMaskedString());
                Info("=====================================");
            }
            else
            {
                Info($"Starting API Generator - Language: {config.SelectedLanguage}, Cloud: {config.SelectedCloud}");
            }
        }

        public void LogStartPhase(string phase)
        {
            Info($"=== Starting {phase} ===");
        }

        public void LogEndPhase(string phase, bool success = true)
        {
            var status = success ? "SUCCESS" : "FAILED";
            Info($"=== {phase} {status} ===");
        }

        public void LogTemplateValidation(string templatePath, bool isValid, string details = "")
        {
            if (_verbose)
            {
                var status = isValid ? "VALID" : "INVALID";
                Info($"Template validation [{templatePath}]: {status}");
                if (!string.IsNullOrWhiteSpace(details))
                {
                    Debug($"Template details: {details}");
                }
            }
        }

        public void LogEntityDiscovery(int entityCount, string attribute)
        {
            if (entityCount == 0)
            {
                Warn($"No entities found with attribute: {attribute}");
            }
            else
            {
                Info($"Discovered {entityCount} entities with attribute: {attribute}");
            }
        }

        public void LogCloudOperation(string operation, string details = "")
        {
            Info($"Cloud Operation: {operation}");
            if (_verbose && !string.IsNullOrWhiteSpace(details))
            {
                Debug($"Operation details: {details}");
            }
        }

        public void LogLicenseStatus(string status, string details = "")
        {
            Info($"License Status: {status}");
            if (!string.IsNullOrWhiteSpace(details))
            {
                if (status.Contains("FAILED") || status.Contains("EXPIRED"))
                {
                    Warn($"License details: {details}");
                }
                else if (_verbose)
                {
                    Debug($"License details: {details}");
                }
            }
        }

        public void LogExecutionTiming(string operation, TimeSpan elapsed)
        {
            if (_verbose)
            {
                Debug($"Timing [{operation}]: {elapsed.TotalSeconds:F2} seconds");
            }
        }

        public void LogFileGeneration(string filePath, long sizeBytes = 0)
        {
            var sizeInfo = sizeBytes > 0 ? $" ({FormatFileSize(sizeBytes)})" : "";
            Info($"Generated: {filePath}{sizeInfo}");
        }

        public void LogDeploymentInfo(string serviceName, string url, string status)
        {
            Info($"Deployment [{serviceName}]: {status}");
            if (!string.IsNullOrWhiteSpace(url))
            {
                Info($"Service URL: {url}");
            }
        }

        private void Log(LogLevel level, string message)
        {
            if (level < _logLevel)
                return;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var levelStr = level.ToString().PadRight(5);
            var threadId = Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(3);
            var formattedMessage = $"[{timestamp}] [{levelStr}] [T{threadId}] {message}";

            lock (_lockObject)
            {
                // Always write to console
                if (level >= LogLevel.ERROR)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(formattedMessage);
                    Console.ResetColor();
                }
                else if (level >= LogLevel.WARN)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(formattedMessage);
                    Console.ResetColor();
                }
                else if (level >= LogLevel.INFO)
                {
                    Console.WriteLine(formattedMessage);
                }
                else if (_verbose && level >= LogLevel.DEBUG)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(formattedMessage);
                    Console.ResetColor();
                }

                // Write all levels to file if verbose, otherwise INFO and above
                if (_verbose || level >= LogLevel.INFO)
                {
                    try
                    {
                        File.AppendAllText(_logFilePath, formattedMessage + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        // Fallback - write to console if file logging fails
                        Console.Error.WriteLine($"[LOG ERROR] Failed to write to log file: {ex.Message}");
                    }
                }
            }
        }

        private void WritePipelineEntry()
        {
            try
            {
                var entry = $"{_toolName}={_version}";
                lock (_lockObject)
                {
                    File.AppendAllText(_logFilePath, entry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // Don't fail startup if we can't write pipeline entry
                Console.Error.WriteLine($"[LOG WARNING] Failed to write pipeline entry: {ex.Message}");
            }
        }

        private static LogLevel ParseLogLevel(string logLevel)
        {
            if (Enum.TryParse<LogLevel>(logLevel, true, out var level))
            {
                return level;
            }

            // Default to INFO if invalid level specified
            return LogLevel.INFO;
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        /// <summary>
        /// Create a scoped logger for timing operations
        /// </summary>
        public IDisposable TimeOperation(string operationName)
        {
            return new TimedOperation(this, operationName);
        }

        private class TimedOperation : IDisposable
        {
            private readonly Logger _logger;
            private readonly string _operationName;
            private readonly DateTime _startTime;

            public TimedOperation(Logger logger, string operationName)
            {
                _logger = logger;
                _operationName = operationName;
                _startTime = DateTime.UtcNow;

                if (_logger._verbose)
                {
                    _logger.Debug($"Starting operation: {operationName}");
                }
            }

            public void Dispose()
            {
                var elapsed = DateTime.UtcNow - _startTime;
                _logger.LogExecutionTiming(_operationName, elapsed);
            }
        }

        /// <summary>
        /// Log a structured message for parsing by downstream tools
        /// </summary>
        public void LogStructured(string component, string action, string status, object data = null)
        {
            var message = $"[{component}] {action}: {status}";
            if (data != null && _verbose)
            {
                message += $" | Data: {System.Text.Json.JsonSerializer.Serialize(data)}";
            }

            Info(message);
        }

        /// <summary>
        /// Log progress for long-running operations
        /// </summary>
        public void LogProgress(string operation, int current, int total, string details = "")
        {
            if (_verbose)
            {
                var percentage = total > 0 ? (current * 100 / total) : 0;
                var message = $"Progress [{operation}]: {current}/{total} ({percentage}%)";
                if (!string.IsNullOrWhiteSpace(details))
                {
                    message += $" - {details}";
                }
                Debug(message);
            }
        }
    }
}