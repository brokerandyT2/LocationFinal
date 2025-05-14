using Location.Core.Application.Common.Interfaces;
using Location.Core.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Location.Core.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;
        private bool _disposed = false;
        private bool _inTransaction = false;

        public UnitOfWork(
            IDatabaseContext context,
            ILogger<UnitOfWork> logger,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        // The repositories are already registered in DI as application interfaces
        // We just need to resolve them from the service provider
        public Location.Core.Application.Common.Interfaces.ILocationRepository Locations =>
            _serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.ILocationRepository>();

        public Location.Core.Application.Common.Interfaces.IWeatherRepository Weather =>
            _serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.IWeatherRepository>();

        public Location.Core.Application.Common.Interfaces.ITipRepository Tips =>
            _serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.ITipRepository>();

        public Location.Core.Application.Common.Interfaces.ITipTypeRepository TipTypes =>
            _serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.ITipTypeRepository>();

        public Location.Core.Application.Common.Interfaces.ISettingRepository Settings =>
            _serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.ISettingRepository>();

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // In SQLite, changes are automatically persisted
            // This method is here for compatibility with the pattern
            _logger.LogDebug("SaveChangesAsync called");

            // Could perform any pending operations here
            return await Task.FromResult(1); // Return 1 to indicate success
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_inTransaction)
            {
                throw new InvalidOperationException("Transaction already in progress");
            }

            try
            {
                await _context.BeginTransactionAsync();
                _inTransaction = true;
                _logger.LogDebug("Transaction started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin transaction");
                throw;
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("CommitAsync called");

            if (_inTransaction)
            {
                await _context.CommitTransactionAsync();
                _inTransaction = false;
                _logger.LogDebug("Transaction committed");
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (!_inTransaction)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            try
            {
                await _context.RollbackTransactionAsync();
                _inTransaction = false;
                _logger.LogDebug("Transaction rolled back");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback transaction");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_inTransaction)
                {
                    try
                    {
                        RollbackAsync().Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error rolling back transaction during disposal");
                    }
                }

                // The context is managed by DI container, so we don't dispose it here
            }

            _disposed = true;
        }
    }
}