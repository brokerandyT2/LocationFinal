// Location.Photography.ViewModels/SunCalculationsViewModel.cs
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using System.Windows.Input;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.ViewModels
{
    public class SunCalculationsViewModel : ViewModelBase, ISunCalculations
    {
        #region Fields
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly IErrorDisplayService _errorDisplayService;

        // PERFORMANCE: Threading and caching
        private readonly SemaphoreSlim _calculationLock = new(1, 1);
        private readonly Dictionary<string, SunCalculationResult> _calculationCache = new();
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private const int CALCULATION_THROTTLE_MS = 300;

        private List<LocationViewModel> _locations = new List<LocationViewModel>();
        private LocationViewModel _selectedLocation = new LocationViewModel();
        private double _latitude;
        private double _longitude;
        private DateTime _date = DateTime.Today;
        private string _dateFormat = "MM/dd/yyyy";
        private string _timeFormat = "hh:mm tt";

        private DateTime _sunrise = DateTime.Now;
        private DateTime _sunset = DateTime.Now;
        private DateTime _solarnoon = DateTime.Now;
        private DateTime _astronomicalDawn = DateTime.Now;
        private DateTime _nauticaldawn = DateTime.Now;
        private DateTime _nauticaldusk = DateTime.Now;
        private DateTime _astronomicalDusk = DateTime.Now;
        private DateTime _civildawn = DateTime.Now;
        private DateTime _civildusk = DateTime.Now;

        private string _locationPhoto = string.Empty;
        #endregion

        #region Properties
        public List<LocationViewModel> LocationsS
        {
            get => _locations;
            set
            {
                _locations = value;
                OnPropertyChanged();
            }
        }

        public LocationViewModel SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (_selectedLocation != value)
                {
                    _selectedLocation = value;
                    if (_selectedLocation != null)
                    {
                        Latitude = _selectedLocation.Lattitude;
                        Longitude = _selectedLocation.Longitude;
                        LocationPhoto = _selectedLocation.Photo;
                        _ = CalculateSunOptimizedAsync();
                    }
                    OnPropertyChanged();
                }
            }
        }

        public double Latitude
        {
            get => _latitude;
            set
            {
                if (SetProperty(ref _latitude, value))
                {
                    _ = CalculateSunOptimizedAsync();
                }
            }
        }

        public double Longitude
        {
            get => _longitude;
            set
            {
                if (SetProperty(ref _longitude, value))
                {
                    _ = CalculateSunOptimizedAsync();
                }
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                if (SetProperty(ref _date, value))
                {
                    _ = CalculateSunOptimizedAsync();
                }
            }
        }

        public string DateFormat
        {
            get => _dateFormat;
            set
            {
                if (SetProperty(ref _dateFormat, value))
                {
                    UpdateFormattedTimePropertiesOptimized();
                }
            }
        }

        public string TimeFormat
        {
            get => _timeFormat;
            set
            {
                if (SetProperty(ref _timeFormat, value))
                {
                    UpdateFormattedTimePropertiesOptimized();
                }
            }
        }

        public DateTime Sunrise
        {
            get => _sunrise;
            set
            {
                if (SetProperty(ref _sunrise, value))
                {
                    UpdateRelatedTimePropertiesOptimized();
                }
            }
        }

        public DateTime Sunset
        {
            get => _sunset;
            set
            {
                if (SetProperty(ref _sunset, value))
                {
                    UpdateRelatedTimePropertiesOptimized();
                }
            }
        }

        public DateTime SolarNoon
        {
            get => _solarnoon;
            set
            {
                if (SetProperty(ref _solarnoon, value))
                {
                    OnPropertyChanged(nameof(SolarNoonFormatted));
                }
            }
        }

        public DateTime AstronomicalDawn
        {
            get => _astronomicalDawn;
            set
            {
                if (SetProperty(ref _astronomicalDawn, value))
                {
                    OnPropertyChanged(nameof(AstronomicalDawnFormatted));
                }
            }
        }

        public DateTime AstronomicalDusk
        {
            get => _astronomicalDusk;
            set
            {
                if (SetProperty(ref _astronomicalDusk, value))
                {
                    OnPropertyChanged(nameof(AstronomicalDuskFormatted));
                }
            }
        }

        public DateTime NauticalDawn
        {
            get => _nauticaldawn;
            set
            {
                if (SetProperty(ref _nauticaldawn, value))
                {
                    OnPropertyChanged(nameof(NauticalDawnFormatted));
                }
            }
        }

        public DateTime NauticalDusk
        {
            get => _nauticaldusk;
            set
            {
                if (SetProperty(ref _nauticaldusk, value))
                {
                    OnPropertyChanged(nameof(NauticalDuskFormatted));
                }
            }
        }

        public DateTime Civildawn
        {
            get => _civildawn;
            set
            {
                if (SetProperty(ref _civildawn, value))
                {
                    OnPropertyChanged(nameof(CivilDawnFormatted));
                }
            }
        }

        public DateTime Civildusk
        {
            get => _civildusk;
            set
            {
                if (SetProperty(ref _civildusk, value))
                {
                    OnPropertyChanged(nameof(CivilDuskFormatted));
                }
            }
        }

        public DateTime GoldenHourMorning => _sunrise.AddHours(1);

        public DateTime GoldenHourEvening => _sunset.AddHours(-1);

        public string SunRiseFormatted => _sunrise.ToString(TimeFormat);

        public string SunSetFormatted => _sunset.ToString(TimeFormat);

        public string SolarNoonFormatted => _solarnoon.ToString(TimeFormat);

        public string GoldenHourMorningFormatted => GoldenHourMorning.ToString(TimeFormat);

        public string GoldenHourEveningFormatted => GoldenHourEvening.ToString(TimeFormat);

        public string AstronomicalDawnFormatted => _astronomicalDawn.ToString(TimeFormat);

        public string AstronomicalDuskFormatted => _astronomicalDusk.ToString(TimeFormat);

        public string NauticalDawnFormatted => _nauticaldawn.ToString(TimeFormat);

        public string NauticalDuskFormatted => _nauticaldusk.ToString(TimeFormat);

        public string CivilDawnFormatted => _civildawn.ToString(TimeFormat);

        public string CivilDuskFormatted => _civildusk.ToString(TimeFormat);

        public string LocationPhoto
        {
            get => _locationPhoto;
            set
            {
                _locationPhoto = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand LoadLocationsCommand { get; internal set; }
        public ICommand CalculateSunTimesCommand { get; internal set; }
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Constructor
        public SunCalculationsViewModel(ISunCalculatorService sunCalculatorService, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            LoadLocationsCommand = new AsyncRelayCommand(LoadLocationsAsync);
            CalculateSunTimesCommand = new RelayCommand(() => _ = CalculateSunOptimizedAsync());
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

            if (!await _calculationLock.WaitAsync(100))
            {
                return; // Skip if another calculation is in progress
            }

            try
            {
                await CalculateSunCoreAsync();
            }
            finally
            {
                _calculationLock.Release();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Core sun calculation logic
        /// </summary>
        private async Task CalculateSunCoreAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = true;
                    ClearErrors();
                });

                if (Latitude == 0 && Longitude == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsBusy = false;
                    });
                    return; // Do not calculate for default coordinates
                }

                // Generate cache key
                var cacheKey = GenerateCacheKey();

                // Check cache first
                if (_calculationCache.TryGetValue(cacheKey, out var cachedResult))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ApplyCachedResultsOptimized(cachedResult);
                        IsBusy = false;
                    });
                    return;
                }

                // Perform calculations on background thread
                var sunTimes = await Task.Run(() => CalculateSunTimesBackground());

                // Cache the results
                _calculationCache[cacheKey] = sunTimes;

                // Cleanup old cache entries (keep only last 10)
                if (_calculationCache.Count > 10)
                {
                    var oldestKey = _calculationCache.Keys.First();
                    _calculationCache.Remove(oldestKey);
                }

                // Update UI on main thread with batch updates
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ApplyCachedResultsOptimized(sunTimes);
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
        /// PERFORMANCE OPTIMIZATION: Background sun times calculation
        /// </summary>
        private SunCalculationResult CalculateSunTimesBackground()
        {
            try
            {
                var timezone = TimeZoneInfo.Local.ToString();

                return new SunCalculationResult
                {
                    Sunrise = _sunCalculatorService.GetSunrise(Date, Latitude, Longitude, timezone),
                    Sunset = _sunCalculatorService.GetSunset(Date, Latitude, Longitude, timezone),
                    SolarNoon = _sunCalculatorService.GetSolarNoon(Date, Latitude, Longitude, timezone),
                    AstronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(Date, Latitude, Longitude, timezone),
                    AstronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(Date, Latitude, Longitude, timezone),
                    NauticalDawn = _sunCalculatorService.GetNauticalDawn(Date, Latitude, Longitude, timezone),
                    NauticalDusk = _sunCalculatorService.GetNauticalDusk(Date, Latitude, Longitude, timezone),
                    CivilDawn = _sunCalculatorService.GetCivilDawn(Date, Latitude, Longitude, timezone),
                    CivilDusk = _sunCalculatorService.GetCivilDusk(Date, Latitude, Longitude, timezone)
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Sun times calculation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Apply cached results with batch updates
        /// </summary>
        private void ApplyCachedResultsOptimized(SunCalculationResult sunTimes)
        {
            BeginPropertyChangeBatch();

            _sunrise = sunTimes.Sunrise;
            _sunset = sunTimes.Sunset;
            _solarnoon = sunTimes.SolarNoon;
            _astronomicalDawn = sunTimes.AstronomicalDawn;
            _astronomicalDusk = sunTimes.AstronomicalDusk;
            _nauticaldawn = sunTimes.NauticalDawn;
            _nauticaldusk = sunTimes.NauticalDusk;
            _civildawn = sunTimes.CivilDawn;
            _civildusk = sunTimes.CivilDusk;

            _ = EndPropertyChangeBatchAsync();

            // Update all formatted properties
            UpdateAllFormattedPropertiesOptimized();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Batch update all formatted time properties
        /// </summary>
        private void UpdateAllFormattedPropertiesOptimized()
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
            OnPropertyChanged(nameof(GoldenHourMorning));
            OnPropertyChanged(nameof(GoldenHourEvening));
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
            OnPropertyChanged(nameof(GoldenHourMorning));
            OnPropertyChanged(nameof(GoldenHourEvening));
            _ = EndPropertyChangeBatchAsync();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update related time properties when sunrise/sunset changes
        /// </summary>
        private void UpdateRelatedTimePropertiesOptimized()
        {
            BeginPropertyChangeBatch();

            OnPropertyChanged(nameof(SunRiseFormatted));
            OnPropertyChanged(nameof(SunSetFormatted));
            OnPropertyChanged(nameof(GoldenHourMorning));
            OnPropertyChanged(nameof(GoldenHourMorningFormatted));
            OnPropertyChanged(nameof(GoldenHourEvening));
            OnPropertyChanged(nameof(GoldenHourEveningFormatted));

            _ = EndPropertyChangeBatchAsync();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update formatted time properties when format changes
        /// </summary>
        private void UpdateFormattedTimePropertiesOptimized()
        {
            UpdateAllFormattedPropertiesOptimized();
        }

        #endregion

        #region Methods

        public void CalculateSun()
        {
            _ = CalculateSunOptimizedAsync();
        }

        public async Task LoadLocationsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Note: In a real implementation, this would call a service to get locations
                // For now, we'll assume this method would be implemented to load data
                await Task.Delay(100); // Placeholder
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading locations: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string GenerateCacheKey()
        {
            return $"{Latitude:F6}_{Longitude:F6}_{Date:yyyyMMdd}";
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            InitializeCommands();
        }

        public void OnNavigatedFromAsync()
        {
            // Cleanup not required for this implementation
        }

        public override void Dispose()
        {
            _calculationLock?.Dispose();
            _calculationCache.Clear();
            base.Dispose();
        }

        #endregion

        #region Helper Classes

        private class SunCalculationResult
        {
            public DateTime Sunrise { get; set; }
            public DateTime Sunset { get; set; }
            public DateTime SolarNoon { get; set; }
            public DateTime AstronomicalDawn { get; set; }
            public DateTime AstronomicalDusk { get; set; }
            public DateTime NauticalDawn { get; set; }
            public DateTime NauticalDusk { get; set; }
            public DateTime CivilDawn { get; set; }
            public DateTime CivilDusk { get; set; }
        }

        #endregion
    }

    // Helper class needed for the ViewModel
    public class LocationViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Lattitude { get; set; }
        public double Longitude { get; set; }
        public string Photo { get; set; } = string.Empty;
        public int Id { get; set; } = 0;
    }
}