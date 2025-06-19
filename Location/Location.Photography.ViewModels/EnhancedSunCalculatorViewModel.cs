using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Weather.Queries.GetHourlyForecast;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using Location.Core.ViewModels;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using MediatR;
using System.Collections.ObjectModel;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class EnhancedSunCalculatorViewModel : ViewModelBase
    {
        public string HourlyPredictionsHeader => "Hourly Light Predictions";
        [ObservableProperty]
        private bool _isNotBusy = true;

        // Update IsBusy to automatically update IsNotBusy
        public override bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    IsNotBusy = !value;
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }
        #region Fields
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly IPredictiveLightService _predictiveLightService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITimezoneService _timezoneService;
        private readonly IWeatherService _weatherService;
        // PERFORMANCE: Threading and caching
        private CancellationTokenSource _cancellationTokenSource = new();
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private readonly Dictionary<string, WeatherDataResult> _weatherCache = new();
        private readonly Dictionary<string, List<HourlyPredictionDisplayModel>> _predictionCache = new();
        private DateTime _lastWeatherUpdate = DateTime.MinValue;
        private const int WEATHER_CACHE_DURATION_MINUTES = 30;
        private const int PREDICTION_CACHE_DURATION_MINUTES = 60;

        // Core properties
        private ObservableCollection<LocationListItemViewModel> _locations = new();
        private LocationListItemViewModel? _selectedLocation;
        private DateTime _selectedDate = DateTime.Today;
        private string _locationPhoto = string.Empty;
        private string _timeFormat = "HH:mm";
        private string _dateFormat = "MM/dd/yyyy";
        private TimeZoneInfo _deviceTimeZone = TimeZoneInfo.Local;
        private TimeZoneInfo _locationTimeZone = TimeZoneInfo.Local;
        private string _deviceTimeZoneDisplay = string.Empty;
        private string _locationTimeZoneDisplay = string.Empty;
        private string _currentPredictionText = string.Empty;
        private string _nextOptimalWindowText = string.Empty;
        private ObservableCollection<HourlyPredictionDisplayModel> _hourlyPredictions = new();
        private ObservableCollection<SunPathPoint> _sunPathPoints = new();
        private ObservableCollection<OptimalWindowDisplayModel> _optimalWindows = new();
        private string _sunriseDeviceTime = string.Empty;
        private string _sunriseLocationTime = string.Empty;
        private string _sunsetDeviceTime = string.Empty;
        private string _sunsetLocationTime = string.Empty;
        private string _solarNoonDeviceTime = string.Empty;
        private string _solarNoonLocationTime = string.Empty;
        private double _currentAzimuth;
        private double _currentElevation;
        private bool _isSunUp;
        private WeatherImpactAnalysis _weatherImpact = new();
        private bool _isLightMeterCalibrated;
        private DateTime? _lastLightMeterReading;
        private double _calibrationAccuracy;
        private DateTime _sunriseUtc;
        private DateTime _sunsetUtc;
        private DateTime _solarNoonUtc;
        private HourlyWeatherForecastDto? _hourlyWeatherData;
        private string _weatherDataStatus = "Loading...";
        private DateTime _weatherLastUpdate;
        #endregion
        private CancellationTokenSource _sunPathCancellationTokenSource = new();
        private CancellationTokenSource _hourlyForecastsCancellationTokenSource = new();
        private CancellationTokenSource _optimalEventsCancellationTokenSource = new();
        private string _hourlyPredictionsProgressStatus = string.Empty;
        public string HourlyPredictionsProgressStatus
        {
            get => _hourlyPredictionsProgressStatus;
            set => SetProperty(ref _hourlyPredictionsProgressStatus, value);
        }

        private string _optimalWindowsProgressStatus = string.Empty;
        public string OptimalWindowsProgressStatus
        {
            get => _optimalWindowsProgressStatus;
            set => SetProperty(ref _optimalWindowsProgressStatus, value);
        }
        // Method to cancel all ongoing operations
        public void CancelAllOperations()
        {
            _sunPathCancellationTokenSource?.Cancel();
            _hourlyForecastsCancellationTokenSource?.Cancel();
            _optimalEventsCancellationTokenSource?.Cancel();

            // Create new token sources for next operations
            _sunPathCancellationTokenSource = new CancellationTokenSource();
            _hourlyForecastsCancellationTokenSource = new CancellationTokenSource();
            _optimalEventsCancellationTokenSource = new CancellationTokenSource();
        }
        #region Properties
        public ObservableCollection<LocationListItemViewModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }
        [ObservableProperty]
        private bool _isSunPathLoading;

        [ObservableProperty]
        private bool _isHourlyForecastsLoading;

        [ObservableProperty]
        private bool _isOptimalEventsLoading;

        [ObservableProperty]
        private LocationListItemViewModel? _selectedLocationProp;

        [ObservableProperty]
        private DateTime _selectedDateProp = DateTime.Today;

        [ObservableProperty]
        private string _locationPhotoProp = string.Empty;

        [ObservableProperty]
        private string _timeFormatProp = "HH:mm";

        [ObservableProperty]
        private string _dateFormatProp = "MM/dd/yyyy";

        [ObservableProperty]
        private TimeZoneInfo _deviceTimeZoneProp = TimeZoneInfo.Local;

        [ObservableProperty]
        private TimeZoneInfo _locationTimeZoneProp = TimeZoneInfo.Local;

        [ObservableProperty]
        private string _deviceTimeZoneDisplayProp = string.Empty;

        [ObservableProperty]
        private string _locationTimeZoneDisplayProp = string.Empty;

        [ObservableProperty]
        private string _currentPredictionTextProp = string.Empty;

        [ObservableProperty]
        private string _nextOptimalWindowTextProp = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SunPathPoint> _sunPathPointsProp = new();

        [ObservableProperty]
        private ObservableCollection<OptimalWindowDisplayModel> _optimalWindowsProp = new();

        [ObservableProperty]
        private string _sunriseDeviceTimeProp = string.Empty;

        [ObservableProperty]
        private string _sunriseLocationTimeProp = string.Empty;

        [ObservableProperty]
        private string _sunsetDeviceTimeProp = string.Empty;

        [ObservableProperty]
        private string _sunsetLocationTimeProp = string.Empty;

        [ObservableProperty]
        private string _solarNoonDeviceTimeProp = string.Empty;

        [ObservableProperty]
        private string _solarNoonLocationTimeProp = string.Empty;

        [ObservableProperty]
        private double _currentAzimuthProp;

        [ObservableProperty]
        private double _currentElevationProp;

        [ObservableProperty]
        private bool _isSunUpProp;

        [ObservableProperty]
        private WeatherImpactAnalysis _weatherImpactProp = new();

        [ObservableProperty]
        private bool _isLightMeterCalibratedProp;

        [ObservableProperty]
        private DateTime? _lastLightMeterReadingProp;

        [ObservableProperty]
        private double _calibrationAccuracyProp;

        [ObservableProperty]
        private DateTime _sunriseUtcProp;

        [ObservableProperty]
        private DateTime _sunsetUtcProp;

        [ObservableProperty]
        private DateTime _solarNoonUtcProp;

        [ObservableProperty]
        private HourlyWeatherForecastDto? _hourlyWeatherDataProp;

        [ObservableProperty]
        private string _weatherDataStatusProp = "Loading...";

        public DateTime WeatherLastUpdate
        {
            get { return _weatherLastUpdate; }
            set { _weatherLastUpdate = value; OnPropertyChanged(nameof(WeatherLastUpdate)); OnPropertyChanged(nameof(HourlyPredictionsHeader)); }
        }

        public ObservableCollection<HourlyPredictionDisplayModel> HourlyPredictions
        {
            get => _hourlyPredictions;
            set { _hourlyPredictions = value; OnPropertyChanged(nameof(HourlyPredictions)); }
        }

        // Legacy property mappings for compatibility
        public LocationListItemViewModel? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (SetProperty(ref _selectedLocation, value))
                {
                    SelectedLocationProp = value;
                    OnSelectedLocationChanged(value);
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        public string LocationPhoto
        {
            get => _locationPhoto;
            set
            {
                if (SetProperty(ref _locationPhoto, value))
                {
                    LocationPhotoProp = value;
                }
            }
        }

        public string TimeFormat
        {
            get => _timeFormat;
            set => SetProperty(ref _timeFormat, value);
        }

        public string DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        public TimeZoneInfo DeviceTimeZone
        {
            get => _deviceTimeZone;
            set => SetProperty(ref _deviceTimeZone, value);
        }

        public TimeZoneInfo LocationTimeZone
        {
            get => _locationTimeZone;
            set => SetProperty(ref _locationTimeZone, value);
        }

        public string DeviceTimeZoneDisplay
        {
            get => _deviceTimeZoneDisplay;
            set => SetProperty(ref _deviceTimeZoneDisplay, value);
        }

        public string LocationTimeZoneDisplay
        {
            get => _locationTimeZoneDisplay;
            set => SetProperty(ref _locationTimeZoneDisplay, value);
        }

        public string CurrentPredictionText
        {
            get => _currentPredictionText;
            set => SetProperty(ref _currentPredictionText, value);
        }

        public string NextOptimalWindowText
        {
            get => _nextOptimalWindowText;
            set => SetProperty(ref _nextOptimalWindowText, value);
        }

        public ObservableCollection<SunPathPoint> SunPathPoints
        {
            get => _sunPathPoints;
            set => SetProperty(ref _sunPathPoints, value);
        }

        public ObservableCollection<OptimalWindowDisplayModel> OptimalWindows
        {
            get => _optimalWindows;
            set => SetProperty(ref _optimalWindows, value);
        }

        public string SunriseDeviceTime
        {
            get => _sunriseDeviceTime;
            set => SetProperty(ref _sunriseDeviceTime, value);
        }

        public string SunriseLocationTime
        {
            get => _sunriseLocationTime;
            set => SetProperty(ref _sunriseLocationTime, value);
        }

        public string SunsetDeviceTime
        {
            get => _sunsetDeviceTime;
            set => SetProperty(ref _sunsetDeviceTime, value);
        }

        public string SunsetLocationTime
        {
            get => _sunsetLocationTime;
            set => SetProperty(ref _sunsetLocationTime, value);
        }

        public string SolarNoonDeviceTime
        {
            get => _solarNoonDeviceTime;
            set => SetProperty(ref _solarNoonDeviceTime, value);
        }

        public string SolarNoonLocationTime
        {
            get => _solarNoonLocationTime;
            set => SetProperty(ref _solarNoonLocationTime, value);
        }

        public double CurrentAzimuth
        {
            get => _currentAzimuth;
            set => SetProperty(ref _currentAzimuth, value);
        }

        public double CurrentElevation
        {
            get => _currentElevation;
            set => SetProperty(ref _currentElevation, value);
        }

        public bool IsSunUp
        {
            get => _isSunUp;
            set => SetProperty(ref _isSunUp, value);
        }

        public WeatherImpactAnalysis WeatherImpact
        {
            get => _weatherImpact;
            set => SetProperty(ref _weatherImpact, value);
        }

        public bool IsLightMeterCalibrated
        {
            get => _isLightMeterCalibrated;
            set => SetProperty(ref _isLightMeterCalibrated, value);
        }

        public DateTime? LastLightMeterReading
        {
            get => _lastLightMeterReading;
            set => SetProperty(ref _lastLightMeterReading, value);
        }

        public double CalibrationAccuracy
        {
            get => _calibrationAccuracy;
            set => SetProperty(ref _calibrationAccuracy, value);
        }

        public DateTime SunriseUtc
        {
            get => _sunriseUtc;
            set => SetProperty(ref _sunriseUtc, value);
        }

        public DateTime SunsetUtc
        {
            get => _sunsetUtc;
            set => SetProperty(ref _sunsetUtc, value);
        }

        public DateTime SolarNoonUtc
        {
            get => _solarNoonUtc;
            set => SetProperty(ref _solarNoonUtc, value);
        }

        public HourlyWeatherForecastDto? HourlyWeatherData
        {
            get => _hourlyWeatherData;
            set => SetProperty(ref _hourlyWeatherData, value);
        }

        public string WeatherDataStatus
        {
            get => _weatherDataStatus;
            set => SetProperty(ref _weatherDataStatus, value);
        }
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion
        private IExposureCalculatorService _exposureCalculatorService;
        #region Constructor
        public EnhancedSunCalculatorViewModel(
            IMediator mediator,
            IErrorDisplayService errorDisplayService,
            IPredictiveLightService predictiveLightService,
            IUnitOfWork unitOfWork,
            ITimezoneService timezoneService, IWeatherService weatherService, IExposureCalculatorService exposureCalculatorService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
            _exposureCalculatorService = exposureCalculatorService;
            PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(IsBusy))
                    {
                        OnPropertyChanged(nameof(IsNotBusy));

                    }
                };
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

            try
            {
                // Set all loading states
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = true;
                    IsOptimalEventsLoading = IsHourlyForecastsLoading = IsSunPathLoading = true;
                });

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                ClearErrors();
                await LoadUserSettingsOptimizedAsync();

                var query = new GetLocationsQuery
                {
                    PageNumber = 1,
                    PageSize = 100,
                    IncludeDeleted = false
                };

                var result = await _mediator.Send(query, _cancellationTokenSource.Token);

                if (result.IsSuccess && result.Data != null)
                {
                    // Update locations on UI thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Locations.Clear();
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

                        // Auto-select first location if none selected
                        if (Locations.Count > 0 && SelectedLocation == null)
                        {
                            SelectedLocation = Locations[0];
                        }
                    });

                    // Calculate sun data for selected location
                    if (SelectedLocation != null)
                    {
                        await CalculateEnhancedSunDataAsync();
                    }
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
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = false;
                    IsOptimalEventsLoading = IsHourlyForecastsLoading = IsSunPathLoading = false;
                });

                _operationLock.Release();
            }
        }

        partial void OnSelectedDatePropChanged(DateTime value)
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));

                // Trigger recalculation automatically
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (SelectedLocation != null)
                        {
                            CancelAllOperations();
                            await CalculateEnhancedSunDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        OnSystemError($"Error recalculating for date change: {ex.Message}");
                    }
                });
            }
        }
        partial void OnSelectedLocationPropChanged(LocationListItemViewModel? value)
        {
            if (value != null && _selectedLocation?.Id != value.Id)
            {
                _selectedLocation = value;
                LocationPhoto = value.Photo ?? string.Empty;
                OnPropertyChanged(nameof(SelectedLocation));

                // Trigger recalculation automatically
                _ = Task.Run(async () =>
                {
                    try
                    {
                        CancelAllOperations();
                        await CalculateEnhancedSunDataAsync();
                    }
                    catch (Exception ex)
                    {
                        OnSystemError($"Error recalculating for location change: {ex.Message}");
                    }
                });
            }
        }

        [RelayCommand]
        public async Task CalculateEnhancedSunDataAsync()
        {
            if (!await _operationLock.WaitAsync(100))
            {
                return; // Skip if another operation is in progress
            }

            try
            {
                if (SelectedLocation == null)
                {
                    SetValidationError("Please select a location");
                    return;
                }

                // Set loading states
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsHourlyForecastsLoading = IsOptimalEventsLoading = IsSunPathLoading = true;
                    WeatherDataStatus = "Loading enhanced weather data...";
                });

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                ClearErrors();

                // Check cache first
                var cacheKey = GetCacheKey();
                if (IsCacheValid(cacheKey))
                {
                    await LoadFromCacheAsync(cacheKey);
                    return;
                }

                // Perform calculations in background
                await Task.Run(async () =>
                {
                    try
                    {
                        // Load timezone first
                        await LoadLocationTimezoneOptimizedAsync();

                        // Update timezone displays on UI thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            InitializeTimezoneDisplays();
                        });

                        // Calculate sun times
                        await CalculateSunTimesOptimizedAsync();

                        // Generate sun path points
                        await GenerateSunPathPointsOptimizedAsync();
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            IsSunPathLoading = false;
                        });

                        // Synchronize weather data
                        await SynchronizeWeatherDataOptimizedAsync();

                        // Cache the results
                        await CacheResultsAsync(cacheKey);

                    }
                    catch (OperationCanceledException)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            WeatherDataStatus = "Update cancelled";
                        });
                        throw;
                    }
                    catch (Exception)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            WeatherDataStatus = "Weather data unavailable";
                        });
                        throw;
                    }

                }, _cancellationTokenSource.Token);

                // Final UI updates
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateCurrentPredictionDisplayOptimized();
                    WeatherDataStatus = $"Updated {WeatherLastUpdate:HH:mm}";
                });
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Update cancelled";
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Weather data unavailable";
                });
                OnSystemError($"Error calculating enhanced sun data: {ex.Message}");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsHourlyForecastsLoading = IsOptimalEventsLoading = false;
                });

                _operationLock.Release();
            }
        }
        public async Task OnLocationChangedAsync(LocationListItemViewModel newLocation)
        {
            try
            {
                if (newLocation?.Id != SelectedLocation?.Id)
                {
                    SelectedLocation = newLocation;
                    await CalculateEnhancedSunDataAsync();
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error handling location change: {ex.Message}");
            }
        }



        [ObservableProperty]
        private bool _isInitialized = false;

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                IsBusy = true;
                await LoadLocationsAsync();
                IsInitialized = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task OnDateChangedAsync(DateTime newDate)
        {
            try
            {
                if (newDate != SelectedDate)
                {
                    SelectedDate = newDate;
                    if (SelectedLocation != null)
                    {
                        await CalculateEnhancedSunDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error handling date change: {ex.Message}");
            }
        }

        // FIX 7: Add method to check if view model is ready
        public bool IsReadyForCalculations => !IsBusy && SelectedLocation != null;

        [RelayCommand]
        public async Task CalibrateWithLightMeterAsync(double actualEV)
        {
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    if (SelectedLocation == null)
                    {
                        SetValidationError("Please select a location first");
                        return;
                    }

                    var calibrationRequest = new LightMeterCalibrationRequest
                    {
                        LocationId = SelectedLocation.Id,
                        Latitude = SelectedLocation.Latitude,
                        Longitude = SelectedLocation.Longitude,
                        DateTime = DateTime.Now,
                        ActualEV = actualEV,
                        WeatherConditions = WeatherImpact?.CurrentConditions
                    };

                    await _predictiveLightService.CalibrateWithActualReadingAsync(calibrationRequest, _cancellationTokenSource.Token);

                    IsLightMeterCalibrated = true;
                    LastLightMeterReading = DateTime.Now;

                    // Invalidate prediction cache and regenerate
                    _predictionCache.Clear();
                    await GenerateWeatherAwarePredictionsOptimizedAsync();
                    UpdateCurrentPredictionDisplayOptimized();
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error calibrating light meter: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
        }

        [RelayCommand]
        public async Task OpenLightMeterAsync(HourlyPredictionDisplayModel prediction)
        {
            try
            {
                // This will be handled in code-behind with PushModalAsync
                // Pass prediction values to pre-populate light meter
            }
            catch (Exception ex)
            {
                OnSystemError($"Error opening light meter: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task OpenCameraTipsAsync(HourlyPredictionDisplayModel prediction)
        {
            try
            {
                // Check if prediction settings match any tips
                // This will be handled in code-behind with PushModalAsync
            }
            catch (Exception ex)
            {
                OnSystemError($"Error opening camera tips: {ex.Message}");
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZED METHODS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Cache management
        /// </summary>
        private string GetCacheKey()
        {
            return $"{SelectedLocation?.Id}_{SelectedDate:yyyyMMdd}";
        }

        private bool IsCacheValid(string cacheKey)
        {
            if (!_weatherCache.TryGetValue(cacheKey, out var cachedData))
                return false;

            var age = DateTime.Now - cachedData.Timestamp;
            return age.TotalMinutes < WEATHER_CACHE_DURATION_MINUTES;
        }

        private async Task LoadFromCacheAsync(string cacheKey)
        {
            try
            {
                if (_weatherCache.TryGetValue(cacheKey, out var cachedWeather))
                {
                    WeatherDataStatus = "Loading from cache...";

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        BeginPropertyChangeBatch();

                        WeatherImpact = cachedWeather.WeatherImpact;
                        WeatherLastUpdate = TimeZoneInfo.ConvertTime(cachedWeather.Timestamp, DeviceTimeZone);

                        _ = EndPropertyChangeBatchAsync();

                        WeatherDataStatus = $"Cached data from {WeatherLastUpdate:HH:mm}";
                    });
                }

                if (_predictionCache.TryGetValue(cacheKey, out var cachedPredictions))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HourlyPredictions.Clear();
                        foreach (var prediction in cachedPredictions)
                        {
                            HourlyPredictions.Add(prediction);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading cached data: {ex.Message}");
            }
        }

        private async Task CacheResultsAsync(string cacheKey)
        {
            try
            {
                var weatherResult = new WeatherDataResult
                {
                    IsSuccess = true,
                    WeatherImpact = WeatherImpact,
                    Timestamp = DateTime.Now
                };

                _weatherCache[cacheKey] = weatherResult;
                _predictionCache[cacheKey] = new List<HourlyPredictionDisplayModel>(HourlyPredictions);

                // Cleanup old cache entries (keep only last 5)
                if (_weatherCache.Count > 5)
                {
                    var oldestWeatherKey = _weatherCache.Keys.First();
                    _weatherCache.Remove(oldestWeatherKey);
                }

                if (_predictionCache.Count > 5)
                {
                    var oldestPredictionKey = _predictionCache.Keys.First();
                    _predictionCache.Remove(oldestPredictionKey);
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error caching results: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized user settings loading
        /// </summary>
        private async Task LoadUserSettingsOptimizedAsync()
        {
            try
            {
                var tasks = new[]
                {
                    _mediator.Send(new GetSettingByKeyQuery { Key = "TimeFormat" }, _cancellationTokenSource.Token),
                    _mediator.Send(new GetSettingByKeyQuery { Key = "DateFormat" }, _cancellationTokenSource.Token)
                };

                var results = await Task.WhenAll(tasks);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    BeginPropertyChangeBatch();

                    if (results[0].IsSuccess && results[0].Data != null)
                    {
                        TimeFormat = results[0].Data.Value;
                    }

                    if (results[1].IsSuccess && results[1].Data != null)
                    {
                        DateFormat = results[1].Data.Value;
                    }

                    _ = EndPropertyChangeBatchAsync();
                });
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading user settings: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized timezone loading
        /// </summary>
        private async Task LoadLocationTimezoneOptimizedAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                // Get weather data which contains the authoritative timezone from OpenWeather API
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                if (weather != null && !string.IsNullOrEmpty(weather.Timezone))
                {
                    // Use timezone from stored weather data (from OpenWeather API)
                    try
                    {
                        LocationTimeZone = _timezoneService.GetTimeZoneInfo(weather.Timezone);
                        DeviceTimeZone = _timezoneService.GetTimeZoneInfo(TimeZoneInfo.Local.StandardName.ToString());
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            LocationTimeZoneDisplay = $"Location: {LocationTimeZone.DisplayName}";
                            DeviceTimeZoneDisplay = $"Local: {DeviceTimeZone.DisplayName}";
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        // _logger?.LogWarning(ex, "Failed to parse stored timezone {Timezone}, will fetch fresh data", weather.Timezone);
                    }
                }

                // No stored weather data or invalid timezone, fetch fresh data from API
                var weatherResult = await _weatherService.UpdateWeatherForLocationAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                if (weatherResult.IsSuccess && weatherResult.Data != null && !string.IsNullOrEmpty(weatherResult.Data.Timezone))
                {
                    try
                    {
                        LocationTimeZone = _timezoneService.GetTimeZoneInfo(weatherResult.Data.Timezone);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            LocationTimeZoneDisplay = $"Location: {LocationTimeZone.DisplayName}";
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        // _logger?.LogWarning(ex, "Failed to parse API timezone {Timezone}, falling back to coordinate lookup", weatherResult.Data.Timezone);
                    }
                }

                // Final fallback: coordinate-based timezone lookup
                await DetermineTimezoneFromCoordinatesOptimizedAsync();
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error loading location timezone: {ex.Message}");
                    LocationTimeZone = TimeZoneInfo.Local;
                    LocationTimeZoneDisplay = $"Location: {LocationTimeZone.DisplayName} (fallback)";
                });
            }
        }

        private async Task DetermineTimezoneFromCoordinatesOptimizedAsync()
        {
            if (SelectedLocation == null) return;

            var timezoneResult = await _timezoneService.GetTimezoneFromCoordinatesAsync(
                SelectedLocation.Latitude,
                SelectedLocation.Longitude,
                _cancellationTokenSource.Token);

            if (timezoneResult.IsSuccess)
            {
                LocationTimeZone = _timezoneService.GetTimeZoneInfo(timezoneResult.Data);
            }
            else
            {
                LocationTimeZone = TimeZoneInfo.Local;
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized sun time calculations
        /// </summary>
        private async Task CalculateSunTimesOptimizedAsync()
        {
            if (SelectedLocation == null) return;

            var sunTimesQuery = new GetSunTimesQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate
            };

            var result = await _mediator.Send(sunTimesQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                var sunTimes = result.Data;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    BeginPropertyChangeBatch();

                    // Store UTC times for proper timezone conversion
                    SunriseUtc = DateTime.SpecifyKind(sunTimes.Sunrise, DateTimeKind.Utc);
                    SunsetUtc = DateTime.SpecifyKind(sunTimes.Sunset, DateTimeKind.Utc);
                    SolarNoonUtc = DateTime.SpecifyKind(sunTimes.SolarNoon, DateTimeKind.Utc);

                    // Convert to display times for both timezones
                    SunriseDeviceTime = FormatTimeForTimezoneOptimized(SunriseUtc, DeviceTimeZone);
                    SunriseLocationTime = FormatTimeForTimezoneOptimized(SunriseUtc, LocationTimeZone);
                    SunsetDeviceTime = FormatTimeForTimezoneOptimized(SunsetUtc, DeviceTimeZone);
                    SunsetLocationTime = FormatTimeForTimezoneOptimized(SunsetUtc, LocationTimeZone);
                    SolarNoonDeviceTime = FormatTimeForTimezoneOptimized(SolarNoonUtc, DeviceTimeZone);
                    SolarNoonLocationTime = FormatTimeForTimezoneOptimized(SolarNoonUtc, LocationTimeZone);

                    _ = EndPropertyChangeBatchAsync();
                });

                // Get current position
                var currentPositionQuery = new GetSunPositionQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    DateTime = DateTime.Now
                };

                var positionResult = await _mediator.Send(currentPositionQuery, _cancellationTokenSource.Token);

                if (positionResult.IsSuccess && positionResult.Data != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CurrentAzimuth = positionResult.Data.Azimuth;
                        CurrentElevation = positionResult.Data.Elevation;
                        IsSunUp = positionResult.Data.Elevation > 0;
                    });
                }
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized sun path generation
        /// </summary>
        private async Task GenerateSunPathPointsOptimizedAsync()
        {
            if (SelectedLocation == null) return;

            var pathQuery = new GetSunPathDataQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate,
                IntervalMinutes = 15
            };

            var result = await _mediator.Send(pathQuery, _sunPathCancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                var pathPoints = result.Data.PathPoints.Select(point => new SunPathPoint
                {
                    Time = point.Time,
                    Azimuth = point.Azimuth,
                    Elevation = point.Elevation,
                    IsCurrentPosition = Math.Abs((point.Time - DateTime.Now).TotalMinutes) < 15
                }).ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SunPathPoints.Clear();
                    foreach (var point in pathPoints)
                    {
                        SunPathPoints.Add(point);
                    }

                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized weather data synchronization
        /// </summary>
        /// <summary>
        /// FIXED: Weather data synchronization with proper weather impact analysis and time conversion
        /// </summary>
        private async Task SynchronizeWeatherDataOptimizedAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Updating weather data...";
                });

                // Phase 1: Force update weather data (this persists to database)
                var updateCommand = new UpdateWeatherCommand
                {
                    LocationId = SelectedLocation.Id,
                    ForceUpdate = true
                };

                var updateResult = await _mediator.Send(updateCommand, _cancellationTokenSource.Token);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (updateResult.IsSuccess && updateResult.Data != null)
                    {
                        WeatherLastUpdate = TimeZoneInfo.ConvertTime(updateResult.Data.LastUpdate, DeviceTimeZone);
                        WeatherDataStatus = "Loading hourly forecast...";
                    }
                    else
                    {
                        WeatherDataStatus = "Using cached weather data";
                    }
                });

                // Phase 2: Load weather data with fallback chain
                await LoadWeatherDataWithFallbackAsync();

                // Phase 3: Generate weather impact analysis for selected date
                await GenerateWeatherImpactForSelectedDateAsync();

                // Phase 4: Generate predictions based on available data
                await GeneratePredictionsFromWeatherDataAsync();

                // Phase 5: Calculate optimal windows
                await CalculateOptimalWindowsOptimizedAsync();

                // FIXED: Convert WeatherLastUpdate to device time for display
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var deviceUpdateTime = ConvertUtcToDeviceTime(WeatherLastUpdate);
                    WeatherDataStatus = $"Updated {deviceUpdateTime:HH:mm}";
                });
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Weather synchronization cancelled";
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Weather synchronization failed";
                    OnSystemError($"Weather sync error: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// FIXED: Load weather data with proper fallback chain using WeatherService
        /// </summary>
        private async Task LoadWeatherDataWithFallbackAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                // Primary: Update weather data (this fetches fresh data if needed)
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Updating weather data...";
                });

                System.Diagnostics.Debug.WriteLine($"Starting weather update for location {SelectedLocation.Id}");

                var weatherUpdateResult = await _weatherService.UpdateWeatherForLocationAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                System.Diagnostics.Debug.WriteLine($"Weather update result: {weatherUpdateResult.IsSuccess}");
                if (!weatherUpdateResult.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"Weather update error: {weatherUpdateResult.ErrorMessage}");
                }

                if (weatherUpdateResult.IsSuccess && weatherUpdateResult.Data != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        WeatherLastUpdate = ConvertUtcToDeviceTime( weatherUpdateResult.Data.LastUpdate);
                        WeatherDataStatus = "Loading hourly forecasts...";
                    });

                    System.Diagnostics.Debug.WriteLine($"Weather data updated, last update: {weatherUpdateResult.Data.LastUpdate}");

                    // Now get the stored weather data with hourly forecasts
                    var storedWeather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                    System.Diagnostics.Debug.WriteLine($"Stored weather found: {storedWeather != null}");
                    if (storedWeather != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Stored weather has {storedWeather.HourlyForecasts?.Count ?? 0} hourly forecasts");
                    }

                    if (storedWeather?.HourlyForecasts?.Any() == true)
                    {
                        var futureForecasts = storedWeather.HourlyForecasts
                            .Where(h => h.DateTime >= DateTime.UtcNow)
                            .Take(48)
                            .ToList();

                        System.Diagnostics.Debug.WriteLine($"Future forecasts found: {futureForecasts.Count}");

                        // SUCCESS: We have hourly data from the weather update
                        var hourlyForecastDto = new HourlyWeatherForecastDto
                        {
                            WeatherId = storedWeather.Id,
                            LastUpdate = storedWeather.LastUpdate,
                            Timezone = storedWeather.Timezone,
                            TimezoneOffset = storedWeather.TimezoneOffset,
                            HourlyForecasts = futureForecasts.Select(h => new HourlyForecastDto
                            {
                                DateTime = h.DateTime,
                                Temperature = h.Temperature,
                                FeelsLike = h.FeelsLike,
                                Description = h.Description,
                                Icon = h.Icon,
                                WindSpeed = h.Wind.Speed,
                                WindDirection = h.Wind.Direction,
                                WindGust = h.Wind.Gust,
                                Humidity = h.Humidity,
                                Pressure = h.Pressure,
                                Clouds = h.Clouds,
                                UvIndex = h.UvIndex,
                                ProbabilityOfPrecipitation = h.ProbabilityOfPrecipitation,
                                Visibility = h.Visibility,
                                DewPoint = h.DewPoint
                            })
                                .ToList()
                        };

                        System.Diagnostics.Debug.WriteLine($"Created hourly forecast DTO with {hourlyForecastDto.HourlyForecasts.Count} forecasts");

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HourlyWeatherData = hourlyForecastDto;
                            WeatherDataStatus = "Hourly weather data loaded";
                        });
                        return;
                    }
                }

                // Secondary: Fall back to daily data and extrapolate
                System.Diagnostics.Debug.WriteLine("Falling back to daily data extrapolation");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Hourly data unavailable - generating from daily data...";
                });

                var dailyWeather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                System.Diagnostics.Debug.WriteLine($"Daily weather found: {dailyWeather != null}");
                if (dailyWeather != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Daily weather has {dailyWeather.Forecasts?.Count ?? 0} daily forecasts");
                }

                if (dailyWeather?.Forecasts?.Any() == true)
                {
                    // Generate hourly data from daily forecasts
                    var extrapolatedHourlyData = await ExtrapolateHourlyFromDailyAsync(dailyWeather);

                    if (extrapolatedHourlyData != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Generated {extrapolatedHourlyData.HourlyForecasts.Count} extrapolated hourly forecasts");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HourlyWeatherData = extrapolatedHourlyData;
                            WeatherLastUpdate = ConvertUtcToDeviceTime(dailyWeather.LastUpdate);
                            WeatherDataStatus = "Generated hourly data from daily forecasts";
                        });
                        return;
                    }
                }

                // Tertiary: No weather data available
                System.Diagnostics.Debug.WriteLine("No weather data available - showing unavailable message");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyWeatherData = null;
                    WeatherDataStatus = "Weather data unavailable";
                });

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadWeatherDataWithFallbackAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyWeatherData = null;
                    WeatherDataStatus = "Weather data service error";
                    OnSystemError($"Error loading weather data: {ex.Message}");
                });
            }
        }
        private async Task GenerateWeatherImpactForSelectedDateAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Analyzing weather impact...";
                });

                // Get weather data for the location
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                if (weather?.Forecasts?.Any() == true)
                {
                    // Check if selected date is within 7 days (API limit)
                    var daysDifference = (SelectedDate.Date - DateTime.Today).TotalDays;
                    if (daysDifference < 0 || daysDifference > 7)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            WeatherImpact = new WeatherImpactAnalysis
                            {
                                Summary = $"Weather data not available for {SelectedDate:MMM dd} (outside 7-day forecast range)",
                                OverallLightReductionFactor = 0.8, // Default assumption
                                CurrentConditions = null
                            };
                            WeatherDataStatus = "Weather impact: Limited data (outside forecast range)";
                        });
                        return;
                    }

                    // Find forecast for selected date
                    var selectedDateForecast = weather.Forecasts
                        .FirstOrDefault(f => f.Date.Date == SelectedDate.Date);

                    if (selectedDateForecast != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found forecast for selected date {SelectedDate:yyyy-MM-dd}");
                        System.Diagnostics.Debug.WriteLine($"Clouds: {selectedDateForecast.Clouds}%, Precipitation: {selectedDateForecast.Precipitation}");

                        // Create weather forecast DTO for the impact analysis
                        var weatherForecastDto = new WeatherForecastDto
                        {
                            WeatherId = weather.Id,
                            LastUpdate = weather.LastUpdate,
                            Timezone = weather.Timezone,
                            TimezoneOffset = weather.TimezoneOffset,
                            DailyForecasts = new List<DailyForecastDto>
                    {
                        new DailyForecastDto
                        {
                            Date = selectedDateForecast.Date,
                            Sunrise = selectedDateForecast.Sunrise,
                            Sunset = selectedDateForecast.Sunset,
                            Temperature = selectedDateForecast.Temperature,
                            MinTemperature = selectedDateForecast.MinTemperature,
                            MaxTemperature = selectedDateForecast.MaxTemperature,
                            Description = selectedDateForecast.Description,
                            Icon = selectedDateForecast.Icon,
                            WindSpeed = selectedDateForecast.Wind.Speed,
                            WindDirection = selectedDateForecast.Wind.Direction,
                            WindGust = selectedDateForecast.Wind.Gust,
                            Humidity = selectedDateForecast.Humidity,
                            Pressure = selectedDateForecast.Pressure,
                            Clouds = selectedDateForecast.Clouds,
                            UvIndex = selectedDateForecast.UvIndex,
                            Precipitation = selectedDateForecast.Precipitation,
                            MoonRise = selectedDateForecast.MoonRise,
                            MoonSet = selectedDateForecast.MoonSet,
                            MoonPhase = selectedDateForecast.MoonPhase
                        }
                    }
                        };

                        // Create enhanced sun times for the analysis
                        var enhancedSunTimes = new Location.Photography.Domain.Models.EnhancedSunTimes
                        {
                            Sunrise = selectedDateForecast.Sunrise,
                            Sunset = selectedDateForecast.Sunset,
                            SolarNoon = selectedDateForecast.Sunrise.AddHours(
                                (selectedDateForecast.Sunset - selectedDateForecast.Sunrise).TotalHours / 2)
                        };

                        // Create moon phase data
                        var moonData = new MoonPhaseData
                        {
                            Phase = selectedDateForecast.MoonPhase,
                            MoonRise= selectedDateForecast.MoonRise,
                            MoonSet = selectedDateForecast.MoonSet
                        };

                        // Create analysis request
                        var analysisRequest = new WeatherImpactAnalysisRequest
                        {
                            WeatherForecast = weatherForecastDto,
                            SunTimes = enhancedSunTimes,
                            MoonData = moonData
                        };

                        // Generate weather impact analysis
                        var weatherImpact = await _predictiveLightService.AnalyzeWeatherImpactAsync(analysisRequest, _cancellationTokenSource.Token);

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            WeatherImpact = weatherImpact;
                            var dateText = SelectedDate.Date == DateTime.Today ? "today" : SelectedDate.ToString("MMM dd");
                            WeatherDataStatus = $"Weather impact analyzed for {dateText}";
                        });

                        System.Diagnostics.Debug.WriteLine($"Weather impact generated - Reduction Factor: {weatherImpact.OverallLightReductionFactor:P0}");
                    }
                    else
                    {
                        // No forecast data for selected date
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            WeatherImpact = new WeatherImpactAnalysis
                            {
                                Summary = $"No weather forecast available for {SelectedDate:MMM dd}",
                                OverallLightReductionFactor = 0.8, // Default assumption
                                CurrentConditions = null
                            };
                            WeatherDataStatus = "Weather impact: No forecast data for selected date";
                        });

                        System.Diagnostics.Debug.WriteLine($"No forecast found for selected date {SelectedDate:yyyy-MM-dd}");
                    }
                }
                else
                {
                    // No weather data at all
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        WeatherImpact = new WeatherImpactAnalysis
                        {
                            Summary = "No weather data available",
                            OverallLightReductionFactor = 0.8, // Default assumption
                            CurrentConditions = null
                        };
                        WeatherDataStatus = "Weather impact: No weather data available";
                    });

                    System.Diagnostics.Debug.WriteLine("No weather data found for location");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating weather impact: {ex.Message}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherImpact = new WeatherImpactAnalysis
                    {
                        Summary = "Error analyzing weather impact",
                        OverallLightReductionFactor = 0.8,
                        CurrentConditions = null
                    };
                    WeatherDataStatus = "Weather impact analysis failed";
                    OnSystemError($"Error generating weather impact: {ex.Message}");
                });
            }
        }
        /// <summary>
        /// FIXED: Extrapolate hourly forecasts from daily weather data
        /// </summary>
        private async Task<HourlyWeatherForecastDto?> ExtrapolateHourlyFromDailyAsync(Location.Core.Domain.Entities.Weather dailyWeather)
        {
            try
            {
                if (SelectedLocation == null || !dailyWeather.Forecasts.Any())
                    return null;

                var hourlyForecasts = new List<HourlyForecastDto>();
                var startTime = GetNextFullHourUtc();

                // Get the relevant daily forecasts (today and tomorrow)
                var relevantDailyForecasts = dailyWeather.Forecasts
                    .Where(f => f.Date.Date >= DateTime.UtcNow.Date && f.Date.Date <= DateTime.UtcNow.Date.AddDays(1))
                    .OrderBy(f => f.Date)
                    .ToList();

                if (!relevantDailyForecasts.Any())
                    return null;

                // Generate hourly forecasts for next 24 hours
                for (int hour = 0; hour < 24; hour++)
                {
                    var targetTime = startTime.AddHours(hour);
                    var targetDate = targetTime.Date;

                    // Find the daily forecast for this date
                    var dailyForecast = relevantDailyForecasts.FirstOrDefault(f => f.Date.Date == targetDate)
                                      ?? relevantDailyForecasts.First(); // Fallback to first available

                    // Calculate sun position for this hour to help with interpolation
                    var sunPositionQuery = new GetSunPositionQuery
                    {
                        Latitude = SelectedLocation.Latitude,
                        Longitude = SelectedLocation.Longitude,
                        DateTime = targetTime
                    };

                    var sunPosition = await _mediator.Send(sunPositionQuery, _cancellationTokenSource.Token);

                    // Create interpolated hourly forecast
                    var hourlyForecast = new HourlyForecastDto
                    {
                        DateTime = targetTime,
                        Temperature = InterpolateTemperatureForHour(dailyForecast, targetTime, sunPosition.Data),
                        Description = dailyForecast.Description,
                        Icon = dailyForecast.Icon,
                        Clouds = dailyForecast.Clouds,
                        WindSpeed = dailyForecast.Wind?.Speed ?? 0,
                        WindDirection = dailyForecast.Wind?.Direction ?? 0,
                        Humidity = dailyForecast.Humidity,
                        Pressure = dailyForecast.Pressure,
                        UvIndex = InterpolateUvIndexForHour(dailyForecast.UvIndex, targetTime, sunPosition.Data),
                        Visibility = 10000, // Default good visibility
                        ProbabilityOfPrecipitation = (double)dailyForecast.Precipitation
                    };

                    hourlyForecasts.Add(hourlyForecast);
                }

                return new HourlyWeatherForecastDto
                {
                    WeatherId = dailyWeather.Id,
                    LastUpdate = dailyWeather.LastUpdate,
                    Timezone = dailyWeather.Timezone,
                    TimezoneOffset = dailyWeather.TimezoneOffset,
                    HourlyForecasts = hourlyForecasts
                };
            }
            catch (Exception ex)
            {
                OnSystemError($"Error extrapolating hourly data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// FIXED: Get the next full hour in UTC
        /// </summary>
        private DateTime GetNextFullHourUtc()
        {
            var now = DateTime.UtcNow;
            var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

            // If we're past the current hour mark, move to next hour
            if (now.Minute > 0 || now.Second > 0)
            {
                nextHour = nextHour.AddHours(1);
            }

            return nextHour;
        }

        /// <summary>
        /// FIXED: Interpolate temperature based on time of day and sun position
        /// </summary>
        private double InterpolateTemperatureForHour(Location.Core.Domain.Entities.WeatherForecast dailyForecast, DateTime targetTime, SunPositionDto? sunPosition)
        {
            // Simple interpolation: cooler at night, warmer during day
            var minTemp = dailyForecast.MinTemperature;
            var maxTemp = dailyForecast.MaxTemperature;

            if (sunPosition?.IsAboveHorizon != true)
            {
                // Night time - closer to minimum
                return minTemp + (maxTemp - minTemp) * 0.2;
            }

            // Day time - interpolate based on sun elevation
            var elevationFactor = Math.Max(0, Math.Min(1, sunPosition.Elevation / 60.0)); // Normalize to 0-1
            return minTemp + (maxTemp - minTemp) * elevationFactor;
        }

        /// <summary>
        /// FIXED: Interpolate UV index based on sun position
        /// </summary>
        private double InterpolateUvIndexForHour(double dailyMaxUvIndex, DateTime targetTime, SunPositionDto? sunPosition)
        {
            if (sunPosition?.IsAboveHorizon != true)
            {
                return 0; // No UV at night
            }

            // Scale UV index based on sun elevation
            var elevationFactor = Math.Max(0, Math.Min(1, sunPosition.Elevation / 60.0));
            return dailyMaxUvIndex * elevationFactor;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized fallback weather data loading
        /// </summary>
        private async Task LoadFallbackWeatherDataOptimizedAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                // Try to read from database first
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                if (weather != null && weather.Forecasts.Any())
                {
                    await ProcessWeatherDataOptimizedAsync(weather);
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        WeatherLastUpdate = weather.LastUpdate;
                        WeatherDataStatus = "Using cached weather data";
                    });
                }
                else
                {
                    // Last resort: API call
                    var weatherQuery = new GetWeatherForecastQuery
                    {
                        Latitude = SelectedLocation.Latitude,
                        Longitude = SelectedLocation.Longitude,
                        Days = 5
                    };

                    var weatherResult = await _mediator.Send(weatherQuery, _cancellationTokenSource.Token);

                    if (weatherResult.IsSuccess && weatherResult.Data != null)
                    {
                        await GenerateBasicWeatherImpactOptimizedAsync(weatherResult.Data);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            WeatherLastUpdate = weatherResult.Data.LastUpdate;
                            WeatherDataStatus = "Using API weather data";
                        });
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            WeatherDataStatus = "Weather data unavailable";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherDataStatus = "Weather service error";
                    OnSystemError($"Error loading fallback weather data: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized weather data processing
        /// </summary>
        private async Task ProcessWeatherDataOptimizedAsync(Location.Core.Domain.Entities.Weather weather)
        {
            try
            {
                var weatherForecastDto = new WeatherForecastDto
                {
                    WeatherId = weather.Id,
                    LastUpdate = weather.LastUpdate,
                    Timezone = weather.Timezone,
                    TimezoneOffset = weather.TimezoneOffset,
                    DailyForecasts = weather.Forecasts.Take(5).Select(f => new DailyForecastDto
                    {
                        Date = f.Date,
                        Sunrise = f.Sunrise,
                        Sunset = f.Sunset,
                        Temperature = f.Temperature,
                        MinTemperature = f.MinTemperature,
                        MaxTemperature = f.MaxTemperature,
                        Description = f.Description,
                        Icon = f.Icon,
                        WindSpeed = f.Wind.Speed,
                        WindDirection = f.Wind.Direction,
                        WindGust = f.Wind.Gust,
                        Humidity = f.Humidity,
                        Pressure = f.Pressure,
                        Clouds = f.Clouds,
                        UvIndex = f.UvIndex,
                        Precipitation = f.Precipitation,
                        MoonRise = f.MoonRise,
                        MoonSet = f.MoonSet,
                        MoonPhase = f.MoonPhase
                    }).ToList()
                };

                await GenerateBasicWeatherImpactOptimizedAsync(weatherForecastDto);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error processing weather data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized weather impact analysis
        /// </summary>
        private async Task GenerateBasicWeatherImpactOptimizedAsync(WeatherForecastDto weatherForecast)
        {
            try
            {
                var analysisRequest = new WeatherImpactAnalysisRequest
                {
                    WeatherForecast = weatherForecast,
                    SunTimes = new Location.Photography.Domain.Models.EnhancedSunTimes(),
                    MoonData = new MoonPhaseData()
                };

                var weatherImpact = await _predictiveLightService.AnalyzeWeatherImpactAsync(analysisRequest, _cancellationTokenSource.Token);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherImpact = weatherImpact;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error generating weather impact analysis: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized weather-aware predictions
        /// </summary>
        private async Task GenerateWeatherAwarePredictionsOptimizedAsync()
        {
            try
            {
                if (SelectedLocation == null) return;

                // Check cache first
                var cacheKey = GetCacheKey();
                if (_predictionCache.TryGetValue(cacheKey, out var cachedPredictions))
                {
                    var cacheAge = DateTime.Now - WeatherLastUpdate;
                    if (cacheAge.TotalMinutes < PREDICTION_CACHE_DURATION_MINUTES)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HourlyPredictions.Clear();
                            HourlyPredictionsProgressStatus = "Loading cached predictions...";

                            foreach (var prediction in cachedPredictions)
                            {
                                HourlyPredictions.Add(prediction);
                            }

                            HourlyPredictionsProgressStatus = $"Loaded {cachedPredictions.Count} cached predictions";
                        });

                        await Task.Delay(1000);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HourlyPredictionsProgressStatus = string.Empty;
                        });
                        return;
                    }
                }

                // Generate new predictions based on available data
                if (HourlyWeatherData?.HourlyForecasts?.Any() == true)
                {
                    // We have weather data (either hourly or extrapolated from daily)
                    await GeneratePredictionsFromWeatherDataAsync();
                }
                else
                {
                    // No weather data - generate basic sun-position only predictions
                    await GenerateBasicSunPositionPredictionsAsync();
                }

                // Cache the results
                _predictionCache[cacheKey] = new List<HourlyPredictionDisplayModel>(HourlyPredictions);
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsProgressStatus = "Prediction generation cancelled";
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsProgressStatus = "Error generating predictions";
                });
                OnSystemError($"Error generating weather-aware predictions: {ex.Message}");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsHourlyForecastsLoading = false;
                });
            }
        }
        private async Task GeneratePredictionsFromWeatherDataAsync()
        {
            try
            {
                if (SelectedLocation == null) return;

                // Check cache first
                var cacheKey = GetCacheKey();
                if (_predictionCache.TryGetValue(cacheKey, out var cachedPredictions))
                {
                    var cacheAge = DateTime.Now - WeatherLastUpdate;
                    if (cacheAge.TotalMinutes < PREDICTION_CACHE_DURATION_MINUTES)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HourlyPredictions.Clear();
                            HourlyPredictionsProgressStatus = "Loading cached predictions...";

                            foreach (var prediction in cachedPredictions)
                            {
                                HourlyPredictions.Add(prediction);
                            }

                            HourlyPredictionsProgressStatus = $"Loaded {cachedPredictions.Count} cached predictions";
                        });

                        await Task.Delay(1000);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HourlyPredictionsProgressStatus = string.Empty;
                        });
                        return;
                    }
                }

                // Generate new predictions based on available data
                if (HourlyWeatherData?.HourlyForecasts?.Any() == true)
                {
                    // We have weather data (either hourly or extrapolated from daily)
                    await GenerateHourlyPredictionsFromWeatherAsync();
                }
                else
                {
                    // No weather data - generate basic sun-position only predictions
                    await GenerateBasicSunPositionPredictionsAsync();
                }

                // Cache the results
                _predictionCache[cacheKey] = new List<HourlyPredictionDisplayModel>(HourlyPredictions);
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsProgressStatus = "Prediction generation cancelled";
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsProgressStatus = "Error generating predictions";
                });
                OnSystemError($"Error generating weather-aware predictions: {ex.Message}");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsHourlyForecastsLoading = false;
                });
            }
        }
        private async Task GenerateHourlyPredictionsFromWeatherAsync()
        {
            if (HourlyWeatherData?.HourlyForecasts == null || SelectedLocation == null)
                return;

            // Clear existing predictions at start
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictions.Clear();
                HourlyPredictionsProgressStatus = "Loading weather-based predictions...";
            });

            // FIXED: Get next 24 hours starting from next full hour
            var startTime = GetNextFullHourUtc();
            var endTime = startTime.AddHours(24);

            // FIXED: Filter forecasts for next 24 hours only
            var relevantForecasts = HourlyWeatherData.HourlyForecasts
                .Where(h => h.DateTime >= startTime && h.DateTime < endTime)
                .OrderBy(h => h.DateTime)
                .ToList();

            // If we don't have enough forecasts, pad with the last available data
            while (relevantForecasts.Count < 24 && HourlyWeatherData.HourlyForecasts.Any())
            {
                var lastForecast = relevantForecasts.LastOrDefault() ?? HourlyWeatherData.HourlyForecasts.Last();
                var nextHour = (relevantForecasts.LastOrDefault()?.DateTime ?? startTime).AddHours(1);

                if (nextHour < endTime)
                {
                    // Create extrapolated forecast
                    var extrapolatedForecast = new HourlyForecastDto
                    {
                        DateTime = nextHour,
                        Temperature = lastForecast.Temperature,
                        Description = lastForecast.Description,
                        Icon = lastForecast.Icon,
                        Clouds = lastForecast.Clouds,
                        WindSpeed = lastForecast.WindSpeed,
                        WindDirection = lastForecast.WindDirection,
                        Humidity = lastForecast.Humidity,
                        Pressure = lastForecast.Pressure,
                        UvIndex = lastForecast.UvIndex,
                        Visibility = lastForecast.Visibility,
                        ProbabilityOfPrecipitation = lastForecast.ProbabilityOfPrecipitation
                    };

                    relevantForecasts.Add(extrapolatedForecast);
                }
                else
                {
                    break;
                }
            }

            var totalHours = relevantForecasts.Count;
            var processedCount = 0;

            // Process in batches to avoid UI blocking
            const int batchSize = 6;
            var batches = relevantForecasts
                .Select((forecast, index) => new { forecast, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.forecast).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                var batchPredictions = await Task.Run(async () =>
                {
                    var batchResults = new List<HourlyPredictionDisplayModel>();

                    foreach (var hourlyForecast in batch)
                    {
                        try
                        {
                            processedCount++;

                            // Update progress status with CONVERTED time for display
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                var displayTime = ConvertUtcToLocationTime(hourlyForecast.DateTime);
                                HourlyPredictionsProgressStatus = $"Processing hour {processedCount} of {totalHours} - {displayTime:HH:mm}";
                            });

                            var sunPositionQuery = new GetSunPositionQuery
                            {
                                Latitude = SelectedLocation.Latitude,
                                Longitude = SelectedLocation.Longitude,
                                DateTime = hourlyForecast.DateTime // Keep as UTC for calculation
                            };

                            var sunPositionResult = await _mediator.Send(sunPositionQuery, _cancellationTokenSource.Token);

                            if (sunPositionResult.IsSuccess && sunPositionResult.Data != null)
                            {
                                var prediction = CreateHourlyPredictionWithProperTimezone(hourlyForecast, sunPositionResult.Data);
                                batchResults.Add(prediction);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing hour {hourlyForecast.DateTime}: {ex.Message}");
                        }
                    }

                    return batchResults;
                }, _cancellationTokenSource.Token);

                // Add batch predictions immediately to UI
                foreach (var prediction in batchPredictions)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HourlyPredictions.Add(prediction);
                    });
                }

                // Small delay between batches to allow UI updates
                await Task.Delay(100);
            }

            // Final status update
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var dataSource = WeatherDataStatus.Contains("Generated") ? " (from daily data)" : "";
                HourlyPredictionsProgressStatus = $"Generated {totalHours} predictions{dataSource}";
            });

            // Clear progress status after delay
            await Task.Delay(1500);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictionsProgressStatus = string.Empty;
            });
        }

        /// <summary>
        /// NEW: Generate basic predictions when no weather data available
        /// </summary>
        private async Task GenerateBasicSunPositionPredictionsAsync()
        {
            if (SelectedLocation == null) return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictions.Clear();
                HourlyPredictionsProgressStatus = "No weather data - generating basic predictions...";
            });

            var startTime = GetNextFullHourUtc();

            for (int hour = 0; hour < 24; hour++)
            {
                var targetTime = startTime.AddHours(hour);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var displayTime = ConvertUtcToLocationTime(targetTime);
                    HourlyPredictionsProgressStatus = $"Generating basic prediction {hour + 1} of 24 - {displayTime:HH:mm}";
                });

                var sunPositionQuery = new GetSunPositionQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    DateTime = targetTime
                };

                var sunPositionResult = await _mediator.Send(sunPositionQuery, _cancellationTokenSource.Token);

                if (sunPositionResult.IsSuccess && sunPositionResult.Data != null)
                {
                    var prediction = CreateBasicPredictionWithProperTimezone(targetTime, sunPositionResult.Data);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HourlyPredictions.Add(prediction);
                    });
                }

                await Task.Delay(50); // Small delay for UI updates
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictionsProgressStatus = "Generated 24 basic predictions (no weather data)";
            });

            await Task.Delay(1500);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictionsProgressStatus = string.Empty;
            });
        }

        private HourlyPredictionDisplayModel CreateHourlyPredictionWithProperTimezone(HourlyForecastDto hourlyForecast, SunPositionDto sunPosition)
        {
            var weatherImpact = CalculateHourlyWeatherImpactOptimized(hourlyForecast);
            var lightPrediction = CreateBasicLightPredictionOptimized(hourlyForecast, sunPosition, weatherImpact);

            var weatherConditions = new WeatherConditions
            {
                CloudCover = hourlyForecast.Clouds / 100.0,
                Precipitation = hourlyForecast.ProbabilityOfPrecipitation,
                Humidity = hourlyForecast.Humidity / 100.0,
                Visibility = hourlyForecast.Visibility,
                UvIndex = hourlyForecast.UvIndex,
                WindSpeed = hourlyForecast.WindSpeed
            };

            var shootingQuality = CalculateShootingQualityScore(lightPrediction, sunPosition, weatherConditions);

            // FIXED: Proper timezone conversion for display
            return new HourlyPredictionDisplayModel
            {
                Time = hourlyForecast.DateTime, // Keep UTC for internal use
                DeviceTimeDisplay = ConvertUtcToDeviceTime(hourlyForecast.DateTime).ToString(TimeFormat),
                LocationTimeDisplay = ConvertUtcToLocationTime(hourlyForecast.DateTime).ToString(TimeFormat),
                PredictedEV = lightPrediction.PredictedEV,
                EVConfidenceMargin = lightPrediction.EVConfidenceMargin,
                SuggestedAperture = ExtractApertureValueOptimized(lightPrediction.SuggestedSettings.Aperture),
                SuggestedShutterSpeed = lightPrediction.SuggestedSettings.ShutterSpeed,
                SuggestedISO = ExtractISOValueOptimized(lightPrediction.SuggestedSettings.ISO),
                ConfidenceLevel = lightPrediction.ConfidenceLevel,
                LightQuality = lightPrediction.LightQuality.OptimalFor,
                ColorTemperature = lightPrediction.LightQuality.ColorTemperature,
                Recommendations = string.Join(", ", lightPrediction.Recommendations),
                IsOptimalTime = lightPrediction.IsOptimalForPhotography,
                TimeFormat = TimeFormat,
                WeatherDescription = hourlyForecast.Description,
                CloudCover = hourlyForecast.Clouds,
                PrecipitationProbability = hourlyForecast.ProbabilityOfPrecipitation,
                WindInfo = $"{hourlyForecast.WindSpeed:F1} mph {GetCardinalDirectionOptimized(hourlyForecast.WindDirection)}",
                UvIndex = hourlyForecast.UvIndex,
                Humidity = hourlyForecast.Humidity,
                ShootingQualityScore = shootingQuality
            };
        }

        /// <summary>
        /// FIXED: Create basic prediction when no weather data available
        /// </summary>
        private HourlyPredictionDisplayModel CreateBasicPredictionWithProperTimezone(DateTime utcTime, SunPositionDto sunPosition)
        {
            var baseEV = CalculateBaseEVFromSunPositionOptimized(sunPosition);
            var exposureSettings = CalculateExposureSettingsOptimized(baseEV);
            var lightQuality = DetermineLightQualityFromSunOnly(sunPosition);

            return new HourlyPredictionDisplayModel
            {
                Time = utcTime, // Keep UTC for internal use
                DeviceTimeDisplay = ConvertUtcToDeviceTime(utcTime).ToString(TimeFormat),
                LocationTimeDisplay = ConvertUtcToLocationTime(utcTime).ToString(TimeFormat),
                PredictedEV = Math.Round(baseEV, 1),
                EVConfidenceMargin = 1.5, // Higher uncertainty without weather data
                SuggestedAperture = ExtractApertureValueOptimized(exposureSettings.Aperture),
                SuggestedShutterSpeed = exposureSettings.ShutterSpeed,
                SuggestedISO = ExtractISOValueOptimized(exposureSettings.ISO),
                ConfidenceLevel = 0.6, // Lower confidence without weather
                LightQuality = lightQuality,
                ColorTemperature = CalculateColorTemperatureOptimized(sunPosition.Elevation, 20), // Assume light clouds
                Recommendations = "Weather data unavailable - estimate only",
                IsOptimalTime = sunPosition.Elevation > 0 && sunPosition.Elevation < 15,
                TimeFormat = TimeFormat,
                WeatherDescription = "No weather data",
                CloudCover = 0,
                PrecipitationProbability = 0,
                WindInfo = "Unknown",
                UvIndex = sunPosition.IsAboveHorizon ? 5 : 0,
                Humidity = 50, // Default
                ShootingQualityScore = sunPosition.IsAboveHorizon ? (sunPosition.Elevation < 15 ? 75 : 50) : 25
            };
        }

        /// <summary>
        /// NEW: Simple timezone conversion helpers
        /// </summary>
        private DateTime ConvertUtcToDeviceTime(DateTime utcTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, DeviceTimeZone);
        }

        private DateTime ConvertUtcToLocationTime(DateTime utcTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, LocationTimeZone);
        }

        /// <summary>
        /// NEW: Determine light quality from sun position only
        /// </summary>
        private string DetermineLightQualityFromSunOnly(SunPositionDto sunPosition)
        {
            if (!sunPosition.IsAboveHorizon) return "Night photography";
            if (sunPosition.Elevation < 10) return "Golden hour, portraits";
            if (sunPosition.Elevation > 60) return "Harsh light, use fill flash";
            return "General photography";
        }
        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized hourly weather predictions
        /// </summary>
        private async Task<List<HourlyPredictionDisplayModel>> GeneratePredictionsFromHourlyWeatherOptimizedAsync()
        {
            var predictions = new List<HourlyPredictionDisplayModel>();

            if (HourlyWeatherData?.HourlyForecasts == null || SelectedLocation == null)
                return predictions;

            // Clear existing predictions at start
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictions.Clear();
                HourlyPredictionsProgressStatus = "Loading hourly weather predictions...";
            });

            var nowUtc = DateTime.UtcNow;
            var relevantForecasts = HourlyWeatherData.HourlyForecasts
                .Where(h => h.DateTime > nowUtc)
                .Take(48) // Limit to next 48 hours for performance
                .ToList();

            var totalHours = relevantForecasts.Count;
            var processedCount = 0;

            // Process in batches to avoid UI blocking
            const int batchSize = 12;
            var batches = relevantForecasts
                .Select((forecast, index) => new { forecast, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.forecast).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                var batchPredictions = await Task.Run(async () =>
                {
                    var batchResults = new List<HourlyPredictionDisplayModel>();

                    foreach (var hourlyForecast in batch)
                    {
                        try
                        {
                            processedCount++;

                            // Update progress status
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                var timeDisplay = FormatTimeForTimezoneOptimized(hourlyForecast.DateTime, LocationTimeZone);
                                HourlyPredictionsProgressStatus = $"Processing hour {processedCount} of {totalHours} - {timeDisplay}";
                            });

                            var sunPositionQuery = new GetSunPositionQuery
                            {
                                Latitude = SelectedLocation.Latitude,
                                Longitude = SelectedLocation.Longitude,
                                DateTime = hourlyForecast.DateTime
                            };

                            var sunPositionResult = await _mediator.Send(sunPositionQuery, _cancellationTokenSource.Token);

                            if (sunPositionResult.IsSuccess && sunPositionResult.Data != null)
                            {
                                var prediction = CreateHourlyPredictionOptimized(hourlyForecast, sunPositionResult.Data);
                                batchResults.Add(prediction);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing hour {hourlyForecast.DateTime}: {ex.Message}");
                        }
                    }

                    return batchResults;
                }, _cancellationTokenSource.Token);

                // Add batch predictions immediately to UI
                foreach (var prediction in batchPredictions)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HourlyPredictions.Add(prediction);
                    });
                }

                predictions.AddRange(batchPredictions);

                // Small delay between batches to allow UI updates
                await Task.Delay(150);
            }

            // Final status update
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictionsProgressStatus = $"Generated {predictions.Count} hourly predictions";
            });

            // Clear progress status after delay
            await Task.Delay(1500);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyPredictionsProgressStatus = string.Empty;
            });

            return predictions;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized prediction creation
        /// </summary>
        private HourlyPredictionDisplayModel CreateHourlyPredictionOptimized(HourlyForecastDto hourlyForecast, SunPositionDto sunPosition)
        {
            var weatherImpact = CalculateHourlyWeatherImpactOptimized(hourlyForecast);
            var lightPrediction = CreateBasicLightPredictionOptimized(hourlyForecast, sunPosition, weatherImpact);

            var weatherConditions = new WeatherConditions
            {
                CloudCover = hourlyForecast.Clouds / 100.0,
                Precipitation = hourlyForecast.ProbabilityOfPrecipitation,
                Humidity = hourlyForecast.Humidity / 100.0,
                Visibility = hourlyForecast.Visibility,
                UvIndex = hourlyForecast.UvIndex,
                WindSpeed = hourlyForecast.WindSpeed
            };

            var shootingQuality = CalculateShootingQualityScore(lightPrediction, sunPosition, weatherConditions);
            return new HourlyPredictionDisplayModel
            {
                Time = hourlyForecast.DateTime,
                DeviceTimeDisplay = FormatTimeForTimezoneOptimized(hourlyForecast.DateTime, DeviceTimeZone),
                LocationTimeDisplay = FormatTimeForTimezoneOptimized(hourlyForecast.DateTime, LocationTimeZone),
                PredictedEV = lightPrediction.PredictedEV,
                EVConfidenceMargin = lightPrediction.EVConfidenceMargin,
                SuggestedAperture = ExtractApertureValueOptimized(lightPrediction.SuggestedSettings.Aperture),
                SuggestedShutterSpeed = lightPrediction.SuggestedSettings.ShutterSpeed,
                SuggestedISO = ExtractISOValueOptimized(lightPrediction.SuggestedSettings.ISO),
                ConfidenceLevel = lightPrediction.ConfidenceLevel,
                LightQuality = lightPrediction.LightQuality.OptimalFor,
                ColorTemperature = lightPrediction.LightQuality.ColorTemperature,
                Recommendations = string.Join(", ", lightPrediction.Recommendations),
                IsOptimalTime = lightPrediction.IsOptimalForPhotography,
                TimeFormat = TimeFormat,
                WeatherDescription = hourlyForecast.Description,
                CloudCover = hourlyForecast.Clouds,
                PrecipitationProbability = hourlyForecast.ProbabilityOfPrecipitation,
                WindInfo = $"{hourlyForecast.WindSpeed:F1} mph {GetCardinalDirectionOptimized(hourlyForecast.WindDirection)}",
                UvIndex = hourlyForecast.UvIndex,
                Humidity = hourlyForecast.Humidity,
                ShootingQualityScore = shootingQuality
            };
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized weather impact calculation
        /// </summary>
        private WeatherImpactFactor CalculateHourlyWeatherImpactOptimized(HourlyForecastDto hourlyForecast)
        {
            var cloudReduction = hourlyForecast.Clouds * 0.008;
            var precipReduction = hourlyForecast.ProbabilityOfPrecipitation > 0.3 ? 0.6 : 0;
            var humidityReduction = hourlyForecast.Humidity * 0.001;
            var visibilityReduction = hourlyForecast.Visibility < 5000 ? 0.2 : 0;

            var overallReduction = (1.0 - cloudReduction) * (1.0 - precipReduction) * (1.0 - humidityReduction) * (1.0 - visibilityReduction);
            overallReduction = Math.Max(0.1, overallReduction);

            var confidence = 0.95 - (hourlyForecast.ProbabilityOfPrecipitation * 0.4) - ((hourlyForecast.Clouds / 100.0) * 0.2);
            if (hourlyForecast.WindSpeed > 15) confidence -= 0.1;
            confidence = Math.Max(0.3, Math.Min(0.95, confidence));

            return new WeatherImpactFactor
            {
                CloudCoverReduction = cloudReduction,
                PrecipitationReduction = precipReduction,
                HumidityReduction = humidityReduction,
                VisibilityReduction = visibilityReduction,
                OverallLightReductionFactor = overallReduction,
                ConfidenceImpact = confidence
            };
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized basic light prediction
        /// </summary>
        private HourlyLightPrediction CreateBasicLightPredictionOptimized(HourlyForecastDto hourlyForecast, SunPositionDto sunPosition, WeatherImpactFactor weatherImpact)
        {
            var baseEV = CalculateBaseEVFromSunPositionOptimized(sunPosition);
            var weatherAdjustedEV = baseEV * weatherImpact.OverallLightReductionFactor;

            return new HourlyLightPrediction
            {
                DateTime = hourlyForecast.DateTime,
                PredictedEV = Math.Round(weatherAdjustedEV, 1),
                EVConfidenceMargin = CalculateEVConfidenceMarginOptimized(weatherImpact),
                SuggestedSettings = CalculateExposureSettingsOptimized(weatherAdjustedEV),
                ConfidenceLevel = weatherImpact.ConfidenceImpact,
                LightQuality = DetermineLightQualityOptimized(sunPosition, hourlyForecast),
                Recommendations = GenerateWeatherBasedRecommendationsOptimized(hourlyForecast, sunPosition),
                IsOptimalForPhotography = IsOptimalShootingConditionsOptimized(sunPosition, hourlyForecast),
                SunPosition = sunPosition
            };
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized basic predictions fallback
        /// </summary>
        private async Task<List<HourlyPredictionDisplayModel>> GenerateBasicPredictionsOptimizedAsync()
        {
            var predictions = new List<HourlyPredictionDisplayModel>();

            try
            {
                if (SelectedLocation == null) return predictions;

                // Clear existing predictions at start
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictions.Clear();
                    HourlyPredictionsProgressStatus = "Generating basic light predictions...";
                });

                var predictionRequest = new PredictiveLightRequest
                {
                    LocationId = SelectedLocation.Id,
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    TargetDate = SelectedDate,
                    WeatherImpact = WeatherImpact,
                    SunTimes = new Location.Photography.Domain.Models.EnhancedSunTimes(),
                    MoonPhase = new MoonPhaseData(),
                    LastCalibrationReading = LastLightMeterReading,
                    PredictionWindowHours = 48 // Limit for performance
                };

                var lightPredictions = await _predictiveLightService.GenerateHourlyPredictionsAsync(predictionRequest, _cancellationTokenSource.Token);
                var nowUtc = DateTime.UtcNow;
                var relevantPredictions = lightPredictions.Where(p => p.DateTime > nowUtc.AddMinutes(30)).Take(48).ToList();

                var totalPredictions = relevantPredictions.Count;
                var processedCount = 0;

                foreach (var prediction in relevantPredictions)
                {
                    processedCount++;

                    // Update progress status
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var timeDisplay = FormatTimeForTimezoneOptimized(prediction.DateTime, LocationTimeZone);
                        HourlyPredictionsProgressStatus = $"Processing prediction {processedCount} of {totalPredictions} - {timeDisplay}";
                    });

                    var confidence = CalculateEnhancedConfidenceOptimized(prediction, WeatherImpact);

                    var predictionModel = new HourlyPredictionDisplayModel
                    {
                        Time = prediction.DateTime,
                        DeviceTimeDisplay = FormatTimeForTimezoneOptimized(prediction.DateTime, DeviceTimeZone),
                        LocationTimeDisplay = FormatTimeForTimezoneOptimized(prediction.DateTime, LocationTimeZone),
                        PredictedEV = prediction.PredictedEV,
                        EVConfidenceMargin = prediction.EVConfidenceMargin,
                        SuggestedAperture = ExtractApertureValueOptimized(prediction.SuggestedSettings.Aperture),
                        SuggestedShutterSpeed = prediction.SuggestedSettings.ShutterSpeed,
                        SuggestedISO = ExtractISOValueOptimized(prediction.SuggestedSettings.ISO),
                        ConfidenceLevel = confidence,
                        LightQuality = prediction.LightQuality.OptimalFor,
                        ColorTemperature = prediction.LightQuality.ColorTemperature,
                        Recommendations = string.Join(", ", prediction.Recommendations),
                        IsOptimalTime = prediction.IsOptimalForPhotography,
                        TimeFormat = TimeFormat
                    };

                    // Add prediction immediately to UI
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HourlyPredictions.Add(predictionModel);
                    });

                    predictions.Add(predictionModel);

                    // Small delay to allow UI updates
                    await Task.Delay(100);
                }

                // Final status update
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsProgressStatus = $"Generated {predictions.Count} basic predictions";
                });

                // Clear progress status after delay
                await Task.Delay(1500);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsProgressStatus = string.Empty;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsProgressStatus = "Error generating basic predictions";
                });
                OnSystemError($"Error generating basic predictions: {ex.Message}");
            }

            return predictions;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized optimal windows calculation
        /// </summary>
        private async Task CalculateOptimalWindowsOptimizedAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                IsOptimalEventsLoading = true;

                // Clear existing windows at start
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OptimalWindows.Clear();
                    OptimalWindowsProgressStatus = "Calculating optimal shooting windows...";
                });

                // Get optimal windows starting from current time for next 24 hours
                var currentTime = DateTime.Now;
                var optimalQuery = new GetOptimalShootingTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = currentTime, // Use current date, not selected date
                    IncludeWeatherForecast = true,
                    TimeZone = LocationTimeZone.Id
                };

                var result = await _mediator.Send(optimalQuery, _cancellationTokenSource.Token);

                if (result.IsSuccess && result.Data != null)
                {
                    // Filter to only future windows within next 24 hours
                    var next24Hours = currentTime.AddHours(24);

                    var windows = result.Data
                        .Select(window => new OptimalWindowDisplayModel
                        {
                            WindowType = window.Description.ToString(),
                            StartTime = window.StartTime,
                            EndTime = window.EndTime,
                            StartTimeDisplay = FormatTimeForTimezoneOptimized(window.StartTime, LocationTimeZone),
                            EndTimeDisplay = FormatTimeForTimezoneOptimized(window.EndTime, LocationTimeZone),
                            LightQuality = window.Description,
                            OptimalFor = string.Join(", ", window.IdealFor),
                            IsCurrentlyActive = currentTime >= window.StartTime && currentTime <= window.EndTime,
                            ConfidenceLevel = window.QualityScore,
                            TimeFormat = TimeFormat
                        })
                        .ToList();

                    // Add windows progressively
                    var totalWindows = windows.Count;
                    for (int i = 0; i < windows.Count; i++)
                    {
                        var window = windows[i];

                        // Update progress status
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            OptimalWindowsProgressStatus = $"Processing window {i + 1} of {totalWindows} - {window.WindowType}";
                            OptimalWindows.Add(window);
                        });

                        // Small delay to allow UI updates
                        await Task.Delay(100);
                    }

                    // Final status update
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OptimalWindowsProgressStatus = $"Found {totalWindows} optimal shooting windows";
                    });

                    // Clear progress status after delay
                    await Task.Delay(1500);
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OptimalWindowsProgressStatus = string.Empty;
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OptimalWindowsProgressStatus = "No optimal windows found";
                    });
                }

            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OptimalWindowsProgressStatus = "Error calculating optimal windows";
                });
                OnSystemError($"Error calculating optimal windows: {ex.Message}");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsOptimalEventsLoading = false;
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized prediction display update
        /// </summary>
        private void UpdateCurrentPredictionDisplayOptimized()
        {
            try
            {
                var now = DateTime.Now;
                var currentPrediction = HourlyPredictions.FirstOrDefault(p =>
                    Math.Abs((p.Time - now).TotalMinutes) < 30);

                if (currentPrediction != null)
                {
                    var predictionText = $"At {currentPrediction.LocationTimeDisplay}, expect EV {currentPrediction.PredictedEV:F1} " +
                        $"±{currentPrediction.EVConfidenceMargin:F1}, " +
                        $"f/{currentPrediction.SuggestedAperture} @ {currentPrediction.SuggestedShutterSpeed} " +
                        $"ISO {currentPrediction.SuggestedISO}, " +
                        $"{currentPrediction.ConfidenceLevel:P0} confidence";

                    if (!string.IsNullOrEmpty(currentPrediction.WeatherDescription))
                    {
                        predictionText += $" ({currentPrediction.WeatherDescription})";
                    }

                    CurrentPredictionText = predictionText;
                }

                var nextWindow = OptimalWindows.FirstOrDefault(w => w.StartTime > now);
                if (nextWindow != null)
                {
                    var timeUntil = nextWindow.StartTime - now;
                    NextOptimalWindowText = $"Next optimal window: {nextWindow.WindowType} in {timeUntil.Hours}h {timeUntil.Minutes}m";
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating prediction display: {ex.Message}");
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZATION HELPER METHODS

        private double CalculateBaseEVFromSunPositionOptimized(SunPositionDto sunPosition)
        {
            if (sunPosition.Elevation <= 0) return 4;
            if (sunPosition.Elevation < 10) return 8;
            if (sunPosition.Elevation < 30) return 12;
            return 15;
        }

        private double CalculateEVConfidenceMarginOptimized(WeatherImpactFactor weatherImpact)
        {
            var baseMargin = 0.3;
            var uncertaintyFactor = 1.0 - weatherImpact.ConfidenceImpact;
            return baseMargin + (uncertaintyFactor * 1.0);
        }

        private ExposureTriangle CalculateExposureSettingsOptimized(double ev)
        {
            var aperture = Math.Max(1.4, Math.Min(16, ev / 2));
            var shutterSpeed = CalculateShutterSpeedOptimized(ev, aperture);
            var iso = CalculateISOOptimized(ev, aperture, shutterSpeed);
            var allApertures = Apetures.Thirds.Select(a => Convert.ToDouble(a.Replace("f/", ""))).ToList();
            var allShutterSpeeds = ShutterSpeeds.Thirds.Select(a => Convert.ToDouble(a.Replace("1/", "").Replace("\"", ""))).ToList();

            var closestAperture = allApertures.OrderBy(x => Math.Abs(x - aperture)).First();
            var closestShutter = allShutterSpeeds.OrderBy(x => Math.Abs(x - shutterSpeed)).First();

            var ec = _exposureCalculatorService.CalculateIsoAsync(new ExposureTriangleDto() { Aperture = "f/" + aperture.ToString(), Iso = iso.ToString(), ShutterSpeed = "1/" + shutterSpeed }, closestShutter.ToString(), closestAperture.ToString(), ExposureIncrements.Third);


            return new ExposureTriangle
            {
                Aperture = $"f/{ec.Result.Data.Aperture:F1}",
                ShutterSpeed = $"{ec.Result.Data.ShutterSpeed} seconds",
                ISO = $"ISO {ec.Result.Data.Iso}"
            };
        }

        private double CalculateShutterSpeedOptimized(double ev, double aperture)
        {
            var shutterTime = (aperture * aperture) / Math.Pow(2, ev);
            return Math.Max(1.0 / 4000, Math.Min(30, shutterTime));
        }

        private int CalculateISOOptimized(double ev, double aperture, double shutterSpeed)
        {
            if (ev > 12) return 100;
            if (ev > 10) return 200;
            if (ev > 8) return 400;
            if (ev > 6) return 800;
            return 1600;
        }

        private string FormatShutterSpeedOptimized(double seconds)
        {
            if (seconds >= 1) return $"{seconds:F0}s";
            var fraction = 1.0 / seconds;
            return $"1/{fraction:F0}";
        }

        private LightCharacteristics DetermineLightQualityOptimized(SunPositionDto sunPosition, HourlyForecastDto weather)
        {
            var colorTemp = CalculateColorTemperatureOptimized(sunPosition.Elevation, weather.Clouds);
            var optimalFor = DetermineOptimalPhotographyTypeOptimized(sunPosition, weather);

            return new LightCharacteristics
            {
                ColorTemperature = colorTemp,
                OptimalFor = optimalFor
            };
        }

        private double CalculateColorTemperatureOptimized(double elevation, int cloudCover)
        {
            var baseTemp = 5500;
            if (elevation < 10) baseTemp = 3000;
            else if (elevation < 20) baseTemp = 4000;

            var cloudAdjustment = cloudCover * 5;
            return Math.Max(2500, Math.Min(7000, baseTemp + cloudAdjustment));
        }

        private string DetermineOptimalPhotographyTypeOptimized(SunPositionDto sunPosition, HourlyForecastDto weather)
        {
            if (weather.ProbabilityOfPrecipitation > 0.5) return "Moody/dramatic photography";
            if (sunPosition.Elevation < 10) return "Portraits, golden hour shots";
            if (sunPosition.Elevation > 60 && weather.Clouds > 50) return "Even lighting, portraits";
            if (sunPosition.Elevation > 60) return "Landscapes, architecture";
            return "General photography";
        }

        private List<string> GenerateWeatherBasedRecommendationsOptimized(HourlyForecastDto weather, SunPositionDto sunPosition)
        {
            var recommendations = new List<string>(5);

            if (weather.ProbabilityOfPrecipitation > 0.3)
                recommendations.Add("Bring weather protection for gear");
            if (weather.WindSpeed > 10)
                recommendations.Add("Use faster shutter speeds for stability");
            if (weather.Clouds > 70)
                recommendations.Add("Great for even, soft lighting");
            if (sunPosition.Elevation < 15 && weather.Clouds < 30)
                recommendations.Add("Perfect golden hour conditions");
            if (weather.UvIndex > 7)
                recommendations.Add("Consider UV filter and sun protection");

            return recommendations;
        }

        private bool IsOptimalShootingConditionsOptimized(SunPositionDto sunPosition, HourlyForecastDto weather)
        {
            var hasGoodLight = sunPosition.Elevation > 5;
            var weatherIsManageable = weather.ProbabilityOfPrecipitation < 0.7 && weather.WindSpeed < 20;
            var isGoldenHour = sunPosition.Elevation < 15 && sunPosition.Elevation > 0;

            return hasGoodLight && weatherIsManageable || isGoldenHour;
        }

        private string GetCardinalDirectionOptimized(double degrees)
        {
            var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            var index = (int)Math.Round(degrees / 22.5) % 16;
            return directions[index];
        }

        private string ExtractApertureValueOptimized(string aperture)
        {
            return aperture?.Replace("f/", "") ?? "8";
        }

        private string ExtractISOValueOptimized(string iso)
        {
            return iso?.Replace("ISO ", "") ?? "100";
        }

        private double CalculateEnhancedConfidenceOptimized(HourlyLightPrediction prediction, WeatherImpactAnalysis weather)
        {
            double baseConfidence = 0.90;

            // Time decay: 0.2% per hour
            var hoursFromNow = (prediction.DateTime - DateTime.Now).TotalHours;
            baseConfidence -= (hoursFromNow * 0.002);

            // Precipitation uncertainty: multiply by inverse of rain chance
            if (weather.CurrentConditions?.Precipitation > 0)
            {
                var precipitationChance = weather.CurrentConditions.Precipitation;
                var precipitationUncertainty = Math.Abs(precipitationChance - 0.5) * 2; // 0 to 1 scale
                baseConfidence *= precipitationUncertainty;
            }

            // Weather data freshness: 5% penalty per 12 hours old
            var weatherAge = DateTime.Now - WeatherLastUpdate;
            var weatherAgeHours = weatherAge.TotalHours;
            var weatherAgePenalty = Math.Floor(weatherAgeHours / 12.0) * 0.05;
            baseConfidence -= weatherAgePenalty;

            // Sun position calculation uncertainty for night predictions
            if (prediction.SunPosition?.IsAboveHorizon == false)
            {
                // Night predictions have inherent uncertainty
                baseConfidence *= 0.8; // 20% reduction for night predictions
            }

            // Final bounds: confidence between 10% and 100%
            return Math.Max(0.1, Math.Min(1.0, baseConfidence));
        }
        private double CalculateShootingQualityScore(HourlyLightPrediction prediction, SunPositionDto sunPosition, WeatherConditions? weather)
        {
            double qualityScore = 0.0;
            var maxScore = 100.0;

            // 1. Sun elevation score (40% of total) - Factor in morning AND evening golden hours
            double sunScore = 0.0;
            if (sunPosition.IsAboveHorizon)
            {
                var elevation = sunPosition.Elevation;
                if (elevation <= 10) // Golden hour
                {
                    sunScore = 40.0; // Maximum sun score
                }
                else if (elevation <= 30) // Good light
                {
                    sunScore = 32.0; // 80% of max
                }
                else if (elevation <= 60) // Moderate light
                {
                    sunScore = 24.0; // 60% of max
                }
                else // Harsh overhead light
                {
                    sunScore = 16.0; // 40% of max
                }
            }
            else // Night/below horizon
            {
                sunScore = 20.0; // 50% of max for night photography
            }
            qualityScore += sunScore;

            // 2. Cloud cover score (25% of total) - Some clouds enhance drama
            double cloudScore = 0.0;
            if (weather?.CloudCover != null)
            {
                var clouds = weather.CloudCover / 100.0; // Convert to 0-1 scale
                if (clouds <= 0.2) // 0-20% clouds
                {
                    cloudScore = 20.0; // 80% of max cloud score
                }
                else if (clouds <= 0.5) // 20-50% clouds - dramatic
                {
                    cloudScore = 25.0; // Maximum cloud score
                }
                else if (clouds <= 0.8) // 50-80% clouds - overcast but workable
                {
                    cloudScore = 15.0; // 60% of max
                }
                else // 80%+ clouds - flat gray
                {
                    cloudScore = 10.0; // 40% of max
                }
            }
            else
            {
                cloudScore = 20.0; // Default moderate score if no data
            }
            qualityScore += cloudScore;

            // 3. Precipitation score (15% of total) - Use uncertainty model
            double precipScore = 0.0;
            if (weather?.Precipitation != null)
            {
                var precipChance = weather.Precipitation;
                // Distance from 50% uncertainty - closer to 0% or 100% = better for specific photo types
                var precipCertainty = Math.Abs(precipChance - 0.5) * 2; // 0 to 1 scale
                precipScore = precipCertainty * 15.0; // Scale to 15% max
            }
            else
            {
                precipScore = 15.0; // Default full score if no data
            }
            qualityScore += precipScore;

            // 4. UV Index score (10% of total) - Sun brightness
            double uvScore = 0.0;
            if (weather?.UvIndex != null)
            {
                var uvIndex = weather.UvIndex;
                if (uvIndex <= 2) // Low light
                {
                    uvScore = 5.0; // 50% of max
                }
                else if (uvIndex <= 5) // Good light
                {
                    uvScore = 8.0; // 80% of max
                }
                else if (uvIndex <= 8) // Excellent light
                {
                    uvScore = 10.0; // Maximum UV score
                }
                else // Very bright (9+)
                {
                    uvScore = 7.0; // 70% of max (may need filters)
                }
            }
            else
            {
                uvScore = 8.0; // Default good score if no data
            }
            qualityScore += uvScore;

            // 5. Visibility score (10% of total) - Atmospheric clarity
            double visibilityScore = 0.0;
            if (weather?.Visibility != null)
            {
                var visibility = weather.Visibility; // In meters
                if (visibility >= 10000) // 10km+ - excellent clarity
                {
                    visibilityScore = 10.0; // Maximum visibility score
                }
                else if (visibility >= 5000) // 5-10km - good clarity
                {
                    visibilityScore = 7.0; // 70% of max
                }
                else // <5km - reduced clarity (haze/pollution)
                {
                    visibilityScore = 4.0; // 40% of max
                }
            }
            else
            {
                visibilityScore = 8.0; // Default good score if no data
            }
            qualityScore += visibilityScore;

            // Ensure score is within bounds (0-100)
            return Math.Max(0.0, Math.Min(maxScore, qualityScore));
        }
        private string FormatTimeForTimezoneOptimized(DateTime utcTime, TimeZoneInfo timezone)
        {
            try
            {
                if (timezone == null)
                {
                    return utcTime.ToString(TimeFormat);
                }

                // Ensure we're working with UTC time
                if (utcTime.Kind != DateTimeKind.Utc)
                {
                    // If it's not UTC, assume it's device local time and convert to UTC first
                    if (utcTime.Kind == DateTimeKind.Local)
                    {
                        utcTime = utcTime.ToUniversalTime();
                    }
                    else
                    {
                        // Unspecified kind - assume UTC
                        utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
                    }
                }

                // Convert from UTC to target timezone
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timezone);
                return localTime.ToString(TimeFormat);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error formatting time: {ex.Message}");
                return utcTime.ToString(TimeFormat);
            }
        }

        #endregion

        #region Legacy Methods
        private void InitializeTimezoneDisplays()
        {
            DeviceTimeZoneDisplay = $"Device: {DeviceTimeZone.DisplayName}";
            LocationTimeZoneDisplay = $"Location: {LocationTimeZone.DisplayName}";
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            SelectedDate = DateTime.Today;
            _ = LoadLocationsAsync();
        }

        public void OnNavigatedFromAsync()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void OnSelectedLocationChanged(LocationListItemViewModel? value)
        {
            if (value != null)
            {
                CancelAllOperations();

                LocationPhoto = value.Photo ?? string.Empty;
                _ = CalculateEnhancedSunDataAsync();
            }
        }

        [RelayCommand]
        public async Task RetryLastCommandAsync()
        {
            if (LastCommand?.CanExecute(LastCommandParameter) == true)
            {
                await LastCommand.ExecuteAsync(LastCommandParameter);
            }
        }


        public async Task SynchronizeWeatherDataWithProgressAsync(IProgress<string> progress = null)
        {
            if (SelectedLocation == null) return;

            try
            {
                progress?.Report("Starting weather synchronization...");

                // Phase 1: Force update weather data
                progress?.Report("Updating weather data from API...");
                await SynchronizeWeatherDataOptimizedAsync();

                progress?.Report("Weather synchronization complete");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateCurrentPredictionDisplayOptimized();
                });
            }
            catch (OperationCanceledException)
            {
                progress?.Report("Weather synchronization cancelled");
            }
            catch (Exception ex)
            {
                progress?.Report("Weather synchronization failed");
                OnSystemError($"Weather sync error: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _operationLock?.Dispose();

            // Clear caches
            _weatherCache.Clear();
            _predictionCache.Clear();

            base.Dispose();
        }
        #endregion
    }

    public class WeatherDataResult
    {
        public bool IsSuccess { get; set; }
        public Location.Core.Domain.Entities.Weather? Weather { get; set; }
        public WeatherForecastDto? WeatherForecast { get; set; }
        public string Source { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public WeatherImpactAnalysis WeatherImpact { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    // Weather impact calculation helper
    public class WeatherImpactFactor
    {
        public double CloudCoverReduction { get; set; }
        public double PrecipitationReduction { get; set; }
        public double HumidityReduction { get; set; }
        public double VisibilityReduction { get; set; }
        public double OverallLightReductionFactor { get; set; }
        public double ConfidenceImpact { get; set; }
    }
}