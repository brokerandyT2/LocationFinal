using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ILoggerFactory _loggerFactory;

        // Store the persistence repositories
        private Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository? _locationRepository;
        private Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository? _weatherRepository;
        private Location.Core.Application.Common.Interfaces.Persistence.ITipRepository? _tipRepository;
        private Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository? _tipTypeRepository;
        private Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository? _settingRepository;

        // Store the adapters
        private Location.Core.Application.Common.Interfaces.ILocationRepository? _locationAdapter;
        private Location.Core.Application.Common.Interfaces.IWeatherRepository? _weatherAdapter;
        private Location.Core.Application.Common.Interfaces.ITipRepository? _tipAdapter;
        private Location.Core.Application.Common.Interfaces.ITipTypeRepository? _tipTypeAdapter;
        private Location.Core.Application.Common.Interfaces.ISettingRepository? _settingAdapter;

        private bool _disposed = false;
        private bool _inTransaction = false;

        public UnitOfWork(
            IDatabaseContext context,
            ILogger<UnitOfWork> logger,
            ILoggerFactory loggerFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        // Properties that match the IUnitOfWork interface - return the adapted interfaces
        public Location.Core.Application.Common.Interfaces.ILocationRepository Locations
        {
            get
            {
                if (_locationAdapter == null)
                {
                    _locationRepository ??= new LocationRepository(_context,
                        _loggerFactory.CreateLogger<LocationRepository>());
                    _locationAdapter = new LocationRepositoryAdapter(_locationRepository);
                }
                return _locationAdapter;
            }
        }

        public Location.Core.Application.Common.Interfaces.IWeatherRepository Weather
        {
            get
            {
                if (_weatherAdapter == null)
                {
                    _weatherRepository ??= new WeatherRepository(_context,
                        _loggerFactory.CreateLogger<WeatherRepository>());
                    _weatherAdapter = new WeatherRepositoryAdapter(_weatherRepository);
                }
                return _weatherAdapter;
            }
        }

        public Location.Core.Application.Common.Interfaces.ITipRepository Tips
        {
            get
            {
                if (_tipAdapter == null)
                {
                    _tipRepository ??= new TipRepository(_context,
                        _loggerFactory.CreateLogger<TipRepository>());
                    _tipAdapter = new TipRepositoryAdapter(_tipRepository);
                }
                return _tipAdapter;
            }
        }

        public Location.Core.Application.Common.Interfaces.ITipTypeRepository TipTypes
        {
            get
            {
                if (_tipTypeAdapter == null)
                {
                    _tipTypeRepository ??= new TipTypeRepository(_context,
                        _loggerFactory.CreateLogger<TipTypeRepositoryAdapter>());
                    _tipTypeAdapter = new TipTypeRepositoryAdapter(_tipTypeRepository);
                }
                return _tipTypeAdapter;
            }
        }

        public Location.Core.Application.Common.Interfaces.ISettingRepository Settings
        {
            get
            {
                if (_settingAdapter == null)
                {
                    _settingRepository ??= new SettingRepository(_context,
                        _loggerFactory.CreateLogger<SettingRepository>());
                    _settingAdapter = new SettingRepositoryAdapter(_settingRepository);
                }
                return _settingAdapter;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // In SQLite, changes are automatically persisted
            // This method is here for compatibility with the pattern
            _logger.LogDebug("SaveChangesAsync called");

            // Could perform any pending operations here
            return await Task.FromResult(0);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            // In SQLite, changes are automatically persisted
            // This method is here for compatibility with the pattern
            _logger.LogDebug("CommitAsync called");

            if (_inTransaction)
            {
                await _context.CommitTransactionAsync();
                _inTransaction = false;
            }
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