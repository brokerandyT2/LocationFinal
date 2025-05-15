using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Services
{
    public interface ILoggingService
    {
        Task LogToDatabaseAsync(LogLevel level, string message, Exception? exception = null);
        Task<List<Infrastructure.Data.Entities.Log>> GetLogsAsync(int count = 100);
        Task ClearLogsAsync();
    }

    public class LoggingService : ILoggingService
    {
        private readonly Data.IDatabaseContext _context;
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(Data.IDatabaseContext context, ILogger<LoggingService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LogToDatabaseAsync(LogLevel level, string message, Exception? exception = null)
        {
            try
            {
                var log = new Data.Entities.Log
                {
                    Timestamp = DateTime.UtcNow,
                    Level = level.ToString(),
                    Message = message,
                    Exception = exception?.ToString() ?? string.Empty
                };

                await _context.InsertAsync(log);
            }
            catch (Exception ex)
            {
                // If we can't log to database, log to the standard logger
                _logger.LogError(ex, "Failed to write log to database");
                // Do NOT rethrow - this method should be fault-tolerant
            }
        }

        public async Task<List<Data.Entities.Log>> GetLogsAsync(int count = 100)
        {
            try
            {
                return await _context.Table<Data.Entities.Log>()
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve logs from database");
                return new List<Data.Entities.Log>();
            }
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                var query = "DELETE FROM Log";
                await _context.ExecuteAsync(query);
                _logger.LogInformation("Cleared all logs from database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear logs from database");
                throw;
            }
        }
    }
}