using Location.Core.Application.Common.Interfaces;
using Location.Core.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Data.IDatabaseContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ILoggerFactory _loggerFactory;

        // Lazy initialization for repositories (using Application interfaces)
        private readonly Lazy<Location.Core.Application.Common.Interfaces.ILocationRepository> _locationRepository;
        private readonly Lazy<Location.Core.Application.Common.Interfaces.IWeatherRepository> _weatherRepository;
        private readonly Lazy<Location.Core.Application.Common.Interfaces.ITipRepository> _tipRepository;
        private readonly Lazy<Location.Core.Application.Common.Interfaces.ITipTypeRepository> _tipTypeRepository;
        private readonly Lazy<Location.Core.Application.Common.Interfaces.ISettingRepository> _settingRepository;

        private bool _disposed = false;
        private bool _inTransaction = false;

        public UnitOfWork(
            Data.IDatabaseContext context,
            ILogger<UnitOfWork> logger,
            ILoggerFactory loggerFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            // Initialize lazy repositories
            _locationRepository = new Lazy<Location.Core.Application.Common.Interfaces.ILocationRepository>(() =>
                CreateLocationRepository());
            _weatherRepository = new Lazy<Location.Core.Application.Common.Interfaces.IWeatherRepository>(() =>
                CreateWeatherRepository());
            _tipRepository = new Lazy<Location.Core.Application.Common.Interfaces.ITipRepository>(() =>
                CreateTipRepository());
            _tipTypeRepository = new Lazy<Location.Core.Application.Common.Interfaces.ITipTypeRepository>(() =>
                CreateTipTypeRepository());
            _settingRepository = new Lazy<Location.Core.Application.Common.Interfaces.ISettingRepository>(() =>
                CreateSettingRepository());
        }

        public Location.Core.Application.Common.Interfaces.ILocationRepository Locations => _locationRepository.Value;
        public Location.Core.Application.Common.Interfaces.IWeatherRepository Weather => _weatherRepository.Value;
        public Location.Core.Application.Common.Interfaces.ITipRepository Tips => _tipRepository.Value;
        public Location.Core.Application.Common.Interfaces.ITipTypeRepository TipTypes => _tipTypeRepository.Value;
        public Location.Core.Application.Common.Interfaces.ISettingRepository Settings => _settingRepository.Value;

        private Location.Core.Application.Common.Interfaces.ILocationRepository CreateLocationRepository()
        {
            var persistenceRepo = new Data.Repositories.LocationRepository(_context,
                _loggerFactory.CreateLogger<Data.Repositories.LocationRepository>());
            return new Data.Repositories.LocationRepositoryAdapter(persistenceRepo);
        }

        private Location.Core.Application.Common.Interfaces.IWeatherRepository CreateWeatherRepository()
        {
            var persistenceRepo = new Data.Repositories.WeatherRepository(_context,
                _loggerFactory.CreateLogger<Data.Repositories.WeatherRepository>());
            return new Data.Repositories.WeatherRepositoryAdapter(persistenceRepo);
        }

        private Location.Core.Application.Common.Interfaces.ITipRepository CreateTipRepository()
        {
            var persistenceRepo = new Data.Repositories.TipRepository(_context,
                _loggerFactory.CreateLogger<Data.Repositories.TipRepository>());
            return new Data.Repositories.TipRepositoryAdapter(persistenceRepo);
        }

        private Location.Core.Application.Common.Interfaces.ITipTypeRepository CreateTipTypeRepository()
        {
            var persistenceRepo = new Data.Repositories.TipTypeRepository(_context,
                _loggerFactory.CreateLogger<Data.Repositories.TipTypeRepository>());
            return new Data.Repositories.TipTypeRepositoryAdapter(persistenceRepo);
        }

        private Location.Core.Application.Common.Interfaces.ISettingRepository CreateSettingRepository()
        {
            var persistenceRepo = new Data.Repositories.SettingRepository(_context,
                _loggerFactory.CreateLogger<Data.Repositories.SettingRepository>());
            return new Data.Repositories.SettingRepositoryAdapter(persistenceRepo);
        }

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