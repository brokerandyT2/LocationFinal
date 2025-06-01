// Location.Photography.ViewModels.Premium/SunCalculatorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class SunCalculatorViewModel : ViewModelBase, Interfaces.INavigationAware
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;

        // PERFORMANCE: Threading and caching
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private readonly Dictionary<string, CachedSunCalculation> _calculationCache = new();
        private CancellationTokenSource _cancellationTokenSource = new();
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private const int CALCULATION_THROTTLE_MS = 250;

        // Core properties
        private ObservableCollection<LocationListItemViewModel> _locations = new();
        private LocationListItemViewModel _selectedLocation;
        private DateTime _dates = DateTime.Today;
        private string _locationPhoto = string.Empty;
        private string _dateFormat = "MM/dd/yyyy";
        private string _timeFormat = "hh:mm tt";

        // Sun Times properties
        private DateTime _sunrise = DateTime.Now;
        private DateTime _sunset = DateTime.Now;
        private DateTime _solarNoon = DateTime.Now;
        private DateTime _astronomicalDawn = DateTime.Now;
        private DateTime _nauticalDawn = DateTime.Now;
        private DateTime _nauticalDusk = DateTime.Now;
        private DateTime _astronomicalDusk = DateTime.Now;
        private DateTime _civilDawn = DateTime.Now;
        private DateTime _civilDusk = DateTime.Now;
        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<LocationListItemViewModel> _locationsProp = new();

        [ObservableProperty]
        private LocationListItemViewModel _selectedLocationProp;

        [ObservableProperty]
        private DateTime _datesProp = DateTime.Today;

        [ObservableProperty]
        private string _locationPhotoProp = string.Empty;

        [ObservableProperty]
        private string _dateFormatProp = "MM/dd/yyyy";

        [ObservableProperty]
        private string _timeFormatProp = "hh:mm tt";

        // Sun Times properties
        [ObservableProperty]
        private DateTime _sunriseProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _sunsetProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _solarNoonProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _astronomicalDawnProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _nauticalDawnProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _nauticalDuskProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _astronomicalDuskProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _civilDawnProp = DateTime.Now;

        [ObservableProperty]
        private DateTime _civilDuskProp = DateTime.Now;

        // Legacy property mappings for compatibility
        public ObservableCollection<LocationListItemViewModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }

        public LocationListItemViewModel SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (SetProperty(ref _selectedLocation, value))
                {
                    OnSelectedLocationChangedOptimized(value);
                }
            }
        }

        public DateTime Dates
        {
            get => _dates;
            set
            {
                if (SetProperty(ref _dates, value))
                {
                    OnDateChangedOptimized(value);
                }
            }
        }

        public string LocationPhoto
        {
            get => _locationPhoto;
            set => SetProperty(ref _locationPhoto, value);
        }

        public string DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        public string TimeFormat
        {
            get => _timeFormat;
            set
            {
                if (SetProperty(ref _timeFormat, value))
                {
                    OnTimeFormatChangedOptimized(value);
                }
            }
        }

        public DateTime Sunrise
        {
            get => _sunrise;
            set => SetProperty(ref _sunrise, value);
        }

        public DateTime Sunset
        {
            get => _sunset;
            set => SetProperty(ref _sunset, value);
        }

        public DateTime SolarNoon
        {
            get => _solarNoon;
            set => SetProperty(ref _solarNoon, value);
        }

        public DateTime AstronomicalDawn
        {
            get => _astronomicalDawn;
            set => SetProperty(ref _astronomicalDawn, value);
        }

        public DateTime NauticalDawn
        {
            get => _nauticalDawn;
            set => SetProperty(ref _nauticalDawn, value);
        }

        public DateTime NauticalDusk
        {
            get => _nauticalDusk;
            set => SetProperty(ref _nauticalDusk, value);
        }

        public DateTime AstronomicalDusk
        {
            get => _astronomicalDusk;
            set => SetProperty(ref _astronomicalDusk, value);
        }

        public DateTime CivilDawn
        {
            get => _civilDawn;
            set => SetProperty(ref _civilDawn, value);
        }

        public DateTime CivilDusk
        {
            get => _civilDusk;
            set => SetProperty(ref _civilDusk, value);
        }

        // Formatted string properties for display
        public string SunRiseFormatted => Sunrise.ToString(TimeFormat);
        public string SunSetFormatted => Sunset.ToString(TimeFormat);
        public string SolarNoonFormatted => SolarNoon.ToString(TimeFormat);
        public string GoldenHourMorningFormatted => Sunrise.AddHours(1).ToString(TimeFormat);
        public string GoldenHourEveningFormatted => Sunset.AddHours(-1).ToString(TimeFormat);
        public string AstronomicalDawnFormatted => AstronomicalDawn.ToString(TimeFormat);
        public string AstronomicalDuskFormatted => AstronomicalDusk.ToString(TimeFormat);
        public string NauticalDawnFormatted => NauticalDawn.ToString(TimeFormat);
        public string NauticalDuskFormatted => NauticalDusk.ToString(TimeFormat);
        public string CivilDawnFormatted => CivilDawn.ToString(TimeFormat);
        public string CivilDuskFormatted => CivilDusk.ToString(TimeFormat);
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        #endregion

        #region Constructor
        public SunCalculatorViewModel(IMediator mediator, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
        }
        #endregion

        #region PERFORMANCE OPTIMIZED COMMANDS

        [RelayCommand]
        public async Task LoadLocationsAsync()
        {
            if (!await _operationLock.WaitAsync(100))
            {
                return; // Skip if another operation is in progress
            }

            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();

                    ClearErrors();

                    // Create query to get locations
                    var query = new GetLocationsQuery
                    {
                        PageNumber = 1,
                        PageSize = 100, // Get all locations
                        IncludeDeleted = false
                    };

                    // Send the query through MediatR
                    var result = await _mediator.Send(query, _cancellationTokenSource.Token);

                    if (result.IsSuccess && result.Data != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            BeginPropertyChangeBatch();

                            // Clear current collection
                            Locations.Clear();

                            // Add locations to collection
                            foreach (var locationDto in result.Data.Items)
                            {
                                Locations.Add(new LocationListItemViewModel
                                {
                                    Id = locationDto.Id,
                                    Title = locationDto.Title,
                                    Latitude = locationDto.Latitude,
                                    Longitude = locationDto.Longitude,
                                    Photo = locationDto.PhotoPath,
                                    IsDeleted = locationDto.IsDeleted
                                });
                            }

                            // Select the first location by default
                            if (Locations.Count > 0)
                            {
                                SelectedLocation = Locations[0];
                            }

                            _ = EndPropertyChangeBatchAsync();
                        });
                    }
                    else
                    {
                        OnSystemError(result.ErrorMessage ?? "Failed to load locations");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation occurs
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error loading locations: {ex.Message}");
                }
            });

            try
            {
                await ExecuteAndTrackAsync(command);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        [RelayCommand]
        public async Task CalculateSunAsync()
        {
            if (!await _operationLock.WaitAsync(50))
            {
                return; // Skip if another operation is in progress
            }

            var command = new AsyncRelayCommand(async () =>
            {
                await CalculateSunOptimizedAsync();
            });

            try
            {
                await ExecuteAndTrackAsync(command);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZED METHODS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Throttled and cached sun calculations
        /// </summary>
        private async Task CalculateSunOptimizedAsync()
        {
            // Throttle rapid updates
            var now = DateTime.Now;
            if ((now - _lastCalculationTime).TotalMilliseconds < CALCULATION_THROTTLE_MS)
            {
                return;
            }
            _lastCalculationTime = now;

            if (SelectedLocation == null)
                return;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = true;
                    ClearErrors();
                });

                // Generate cache key
                var cacheKey = GenerateCacheKey();

                // Check cache first
                if (_calculationCache.TryGetValue(cacheKey, out var cachedResult))
                {
                    var cacheAge = DateTime.Now - cachedResult.Timestamp;
                    if (cacheAge.TotalMinutes < 30) // Cache valid for 30 minutes
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            ApplyCachedSunTimesOptimized(cachedResult.SunTimes);
                            IsBusy = false;
                        });
                        return;
                    }
                    else
                    {
                        // Remove expired cache entry
                        _calculationCache.Remove(cacheKey);
                    }
                }

                // Perform calculation on background thread
                var sunTimesResult = await Task.Run(async () =>
                {
                    try
                    {
                        // Use MediatR to get sun times
                        var query = new GetSunTimesQuery
                        {
                            Latitude = SelectedLocation.Latitude,
                            Longitude = SelectedLocation.Longitude,
                            Date = Dates
                        };

                        var result = await _mediator.Send(query, _cancellationTokenSource.Token);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Sun times calculation failed: {ex.Message}", ex);
                    }
                }, _cancellationTokenSource.Token);

                if (sunTimesResult.IsSuccess && sunTimesResult.Data != null)
                {
                    var sunTimes = sunTimesResult.Data;

                    // Cache the results
                    _calculationCache[cacheKey] = new CachedSunCalculation
                    {
                        SunTimes = sunTimes,
                        Timestamp = DateTime.Now
                    };

                    // Cleanup old cache entries (keep only last 10)
                    if (_calculationCache.Count > 10)
                    {
                        var oldestKey = _calculationCache.Keys.First();
                        _calculationCache.Remove(oldestKey);
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ApplyCachedSunTimesOptimized(sunTimes);
                        IsBusy = false;
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OnSystemError(sunTimesResult.ErrorMessage ?? "Failed to calculate sun times");
                        IsBusy = false;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = false;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error calculating sun times: {ex.Message}");
                    IsBusy = false;
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Apply cached sun times with batch updates
        /// </summary>
        private void ApplyCachedSunTimesOptimized(SunTimesDto sunTimes)
        {
            BeginPropertyChangeBatch();

            // Update all the time properties
            Sunrise = sunTimes.Sunrise;
            Sunset = sunTimes.Sunset;
            SolarNoon = sunTimes.SolarNoon;
            AstronomicalDawn = sunTimes.AstronomicalDawn;
            AstronomicalDusk = sunTimes.AstronomicalDusk;
            NauticalDawn = sunTimes.NauticalDawn;
            NauticalDusk = sunTimes.NauticalDusk;
            CivilDawn = sunTimes.CivilDawn;
            CivilDusk = sunTimes.CivilDusk;

            _ = EndPropertyChangeBatchAsync();

            // Update the formatted string properties
            UpdateFormattedPropertiesOptimized();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Batch update formatted properties
        /// </summary>
        private void UpdateFormattedPropertiesOptimized()
        {
            BeginPropertyChangeBatch();

            OnPropertyChanged(nameof(SunRiseFormatted));
            OnPropertyChanged(nameof(SunSetFormatted));
            OnPropertyChanged(nameof(SolarNoonFormatted));
            OnPropertyChanged(nameof(GoldenHourMorningFormatted));
            OnPropertyChanged(nameof(GoldenHourEveningFormatted));
            OnPropertyChanged(nameof(AstronomicalDawnFormatted));
            OnPropertyChanged(nameof(AstronomicalDuskFormatted));
            OnPropertyChanged(nameof(NauticalDawnFormatted));
            OnPropertyChanged(nameof(NauticalDuskFormatted));
            OnPropertyChanged(nameof(CivilDawnFormatted));
            OnPropertyChanged(nameof(CivilDuskFormatted));

            _ = EndPropertyChangeBatchAsync();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized location change handler
        /// </summary>
        private void OnSelectedLocationChangedOptimized(LocationListItemViewModel value)
        {
            if (value != null)
            {
                LocationPhoto = value.Photo;
                _ = CalculateSunOptimizedAsync();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized date change handler
        /// </summary>
        private void OnDateChangedOptimized(DateTime value)
        {
            _ = CalculateSunOptimizedAsync();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized time format change handler
        /// </summary>
        private void OnTimeFormatChangedOptimized(string value)
        {
            UpdateFormattedPropertiesOptimized();
        }

        #endregion

        #region Helper Methods

        public void CalculateSun()
        {
            _ = CalculateSunOptimizedAsync();
        }

        public void OnDateChanged(DateTime value)
        {
            OnDateChangedOptimized(value);
        }

        private string GenerateCacheKey()
        {
            return $"{SelectedLocation?.Latitude:F6}_{SelectedLocation?.Longitude:F6}_{Dates:yyyyMMdd}";
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            _ = LoadLocationsAsync();
        }

        public void OnNavigatedFromAsync()
        {
            _cancellationTokenSource?.Cancel();
        }

        public override void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _operationLock?.Dispose();
            _calculationCache.Clear();
            base.Dispose();
        }

        #endregion

        #region Helper Classes

        private class CachedSunCalculation
        {
            public SunTimesDto SunTimes { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}