using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using Location.Core.ViewModels;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using MediatR;
using System.Collections.ObjectModel;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;
using Location.Core.Application.Weather.DTOs;

namespace Location.Photography.ViewModels
{
    public partial class EnhancedSunCalculatorViewModel : ViewModelBase
    {
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly IPredictiveLightService _predictiveLightService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITimezoneService _timezoneService;
        private CancellationTokenSource _cancellationTokenSource = new();


        private ObservableCollection<LocationListItemViewModel> _locations = new();
        public ObservableCollection<LocationListItemViewModel> Locations
        {
            get => _locations;
            set { SetProperty(ref _locations, value); OnPropertyChanged(nameof(Locations)); }
        }
        [ObservableProperty]
        private LocationListItemViewModel? _selectedLocation;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private string _locationPhoto = string.Empty;

        [ObservableProperty]
        private string _timeFormat = "HH:mm";

        [ObservableProperty]
        private string _dateFormat = "MM/dd/yyyy";

        [ObservableProperty]
        private TimeZoneInfo _deviceTimeZone = TimeZoneInfo.Local;

        [ObservableProperty]
        private TimeZoneInfo _locationTimeZone = TimeZoneInfo.Local;

        [ObservableProperty]
        private string _deviceTimeZoneDisplay = string.Empty;

        [ObservableProperty]
        private string _locationTimeZoneDisplay = string.Empty;

        [ObservableProperty]
        private string _currentPredictionText = string.Empty;

        [ObservableProperty]
        private string _nextOptimalWindowText = string.Empty;

        private ObservableCollection<HourlyPredictionDisplayModel> _hourlyPredictions = new();

        [ObservableProperty]
        private ObservableCollection<SunPathPoint> _sunPathPoints = new();

        [ObservableProperty]
        private ObservableCollection<OptimalWindowDisplayModel> _optimalWindows = new();

        [ObservableProperty]
        private string _sunriseDeviceTime = string.Empty;

        [ObservableProperty]
        private string _sunriseLocationTime = string.Empty;

        [ObservableProperty]
        private string _sunsetDeviceTime = string.Empty;

        [ObservableProperty]
        private string _sunsetLocationTime = string.Empty;

        [ObservableProperty]
        private string _solarNoonDeviceTime = string.Empty;

        [ObservableProperty]
        private string _solarNoonLocationTime = string.Empty;

        [ObservableProperty]
        private double _currentAzimuth;

        [ObservableProperty]
        private double _currentElevation;

        [ObservableProperty]
        private bool _isSunUp;

        [ObservableProperty]
        private WeatherImpactAnalysis _weatherImpact = new();

        [ObservableProperty]
        private bool _isLightMeterCalibrated;

        [ObservableProperty]
        private DateTime? _lastLightMeterReading;

        [ObservableProperty]
        private double _calibrationAccuracy;

        public ObservableCollection<HourlyPredictionDisplayModel> HourlyPredictions { get => _hourlyPredictions; set { _hourlyPredictions = value; OnPropertyChanged(nameof(HourlyPredictions)); } }

        public new event EventHandler<OperationErrorEventArgs>? ErrorOccurred;

        public EnhancedSunCalculatorViewModel(
      IMediator mediator,
      IErrorDisplayService errorDisplayService,
      IPredictiveLightService predictiveLightService,
      IUnitOfWork unitOfWork,
      ITimezoneService timezoneService)
      : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));

            InitializeTimezoneDisplays();
        }

        [RelayCommand]
        public async Task LoadLocationsAsync()
        {
            var command = new AsyncRelayCommand(async () =>
             {
                 try
                 {
                     _cancellationTokenSource?.Cancel();
                     _cancellationTokenSource = new CancellationTokenSource();

                     ClearErrors();
                     await LoadUserSettingsAsync();

                     var query = new GetLocationsQuery
                     {
                         PageNumber = 1,
                         PageSize = 100,
                         IncludeDeleted = false
                     };

                     var result = await _mediator.Send(query, _cancellationTokenSource.Token);

                     if (result.IsSuccess && result.Data != null)
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

                         if (Locations.Count > 0)
                         {
                             SelectedLocation = Locations[0];
                         }
                     }
                     else
                     {
                         OnSystemError(result.ErrorMessage ?? "Failed to load locations");
                     }
                 }
                 catch (OperationCanceledException)
                 {
                 }
                 catch (Exception ex)
                 {
                     OnSystemError($"Error loading locations: {ex.Message}");
                 }
             });

            await ExecuteAndTrackAsync(command);
        }

        [RelayCommand]
        public async Task CalculateEnhancedSunDataAsync()
        {
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    if (SelectedLocation == null)
                    {
                        SetValidationError("Please select a location");
                        return;
                    }

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();

                    ClearErrors();

                    await LoadLocationTimezoneAsync();
                    await CalculateSunTimesAsync();
                    await GenerateSunPathPointsAsync();
                    await LoadWeatherAndPredictionsAsync();
                    await CalculateOptimalWindowsAsync();
                    UpdateCurrentPredictionDisplay();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error calculating enhanced sun data: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
        }

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

                    await LoadWeatherAndPredictionsAsync();
                    UpdateCurrentPredictionDisplay();
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

        private async Task LoadLocationTimezoneAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                // First, check weather repository for existing timezone data
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                if (weather != null && !string.IsNullOrEmpty(weather.Timezone))
                {
                    // Use timezone from weather data
                    try
                    {
                        LocationTimeZone = _timezoneService.GetTimeZoneInfo(weather.Timezone);
                    }
                    catch
                    {
                        // If timezone ID is invalid, determine timezone from coordinates
                        await DetermineTimezoneFromCoordinatesAsync();
                    }
                }
                else
                {
                    // No weather data exists yet, determine timezone from coordinates
                    await DetermineTimezoneFromCoordinatesAsync();
                }

                LocationTimeZoneDisplay = $"Location: {LocationTimeZone.DisplayName}";
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading location timezone: {ex.Message}");
                LocationTimeZone = TimeZoneInfo.Local;
                LocationTimeZoneDisplay = $"Location: {LocationTimeZone.DisplayName}";
            }
        }

        private async Task DetermineTimezoneFromCoordinatesAsync()
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
                // Fallback to device timezone
                LocationTimeZone = TimeZoneInfo.Local;
            }
        }

        private async Task LoadUserSettingsAsync()
        {
            try
            {
                var timeFormatQuery = new GetSettingByKeyQuery { Key = "TimeFormat" };
                var dateFormatQuery = new GetSettingByKeyQuery { Key = "DateFormat" };

                var timeFormatResult = await _mediator.Send(timeFormatQuery, _cancellationTokenSource.Token);
                var dateFormatResult = await _mediator.Send(dateFormatQuery, _cancellationTokenSource.Token);

                if (timeFormatResult.IsSuccess && timeFormatResult.Data != null)
                {
                    TimeFormat = timeFormatResult.Data.Value;
                }

                if (dateFormatResult.IsSuccess && dateFormatResult.Data != null)
                {
                    DateFormat = dateFormatResult.Data.Value;
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading user settings: {ex.Message}");
            }
        }

        private void InitializeTimezoneDisplays()
        {
            DeviceTimeZoneDisplay = $"Device: {DeviceTimeZone.DisplayName}";
            LocationTimeZoneDisplay = $"Location: {LocationTimeZone.DisplayName}";
        }

        private async Task CalculateSunTimesAsync()
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

                SunriseDeviceTime = FormatTimeForTimezone(sunTimes.Sunrise, DeviceTimeZone);
                SunriseLocationTime = FormatTimeForTimezone(sunTimes.Sunrise, LocationTimeZone);

                SunsetDeviceTime = FormatTimeForTimezone(sunTimes.Sunset, DeviceTimeZone);
                SunsetLocationTime = FormatTimeForTimezone(sunTimes.Sunset, LocationTimeZone);

                SolarNoonDeviceTime = FormatTimeForTimezone(sunTimes.SolarNoon, DeviceTimeZone);
                SolarNoonLocationTime = FormatTimeForTimezone(sunTimes.SolarNoon, LocationTimeZone);

                var currentPositionQuery = new GetSunPositionQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    DateTime = DateTime.Now
                };

                var positionResult = await _mediator.Send(currentPositionQuery, _cancellationTokenSource.Token);

                if (positionResult.IsSuccess && positionResult.Data != null)
                {
                    CurrentAzimuth = positionResult.Data.Azimuth;
                    CurrentElevation = positionResult.Data.Elevation;
                    IsSunUp = positionResult.Data.Elevation > 0;
                }
            }
        }

        private async Task GenerateSunPathPointsAsync()
        {
            if (SelectedLocation == null) return;

            var pathQuery = new GetSunPathDataQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate,
                IntervalMinutes = 15
            };

            var result = await _mediator.Send(pathQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                SunPathPoints.Clear();
                foreach (var point in result.Data.PathPoints)
                {
                    SunPathPoints.Add(new SunPathPoint
                    {
                        Time = point.Time,
                        Azimuth = point.Azimuth,
                        Elevation = point.Elevation,
                        IsCurrentPosition = Math.Abs((point.Time - DateTime.Now).TotalMinutes) < 15
                    });
                }
            }
        }

        private async Task LoadWeatherAndPredictionsAsync()
        {
            if (SelectedLocation == null) return;

            WeatherForecastDto weatherForecast = null;

            // Step 1: Check local weather repository first (per business rules)
            var existingWeather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

            if (existingWeather != null)
            {
                // Check if existing data is fresh enough (1 hour max age for predictions)
                if ((DateTime.UtcNow - existingWeather.LastUpdate).TotalHours < 1)
                {
                    // Use existing weather data - but we need to get forecast data via query
                    weatherForecast = await FetchWeatherForecastDataAsync();
                }
                else
                {
                    // Data is stale, fetch fresh data
                    weatherForecast = await FetchWeatherForecastDataAsync();
                }
            }
            else
            {
                // No existing weather data, fetch fresh data
                weatherForecast = await FetchWeatherForecastDataAsync();
            }

            // Step 2: Generate predictions if we have valid weather data
            if (weatherForecast != null)
            {
                await GeneratePredictionsFromWeatherAsync(weatherForecast);
            }
            else
            {
                OnSystemError("Unable to load weather data for predictions");
            }
        }

        private async Task<WeatherForecastDto> FetchWeatherForecastDataAsync()
        {
            if (SelectedLocation == null) return null;

            try
            {
                var weatherQuery = new GetWeatherForecastQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Days = 5 // 5-day forecast per business rules
                };

                var weatherResult = await _mediator.Send(weatherQuery, _cancellationTokenSource.Token);

                if (weatherResult.IsSuccess && weatherResult.Data != null)
                {
                    // Weather service will handle storing to local repository
                    return weatherResult.Data;
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error fetching weather data: {ex.Message}");
            }

            return null;
        }

        private async Task GeneratePredictionsFromWeatherAsync(WeatherForecastDto weatherForecast)
        {
            try
            {
                var analysisRequest = new WeatherImpactAnalysisRequest
                {
                    WeatherForecast = weatherForecast,
                    SunTimes = new Location.Photography.Domain.Models.EnhancedSunTimes(),
                    MoonData = new MoonPhaseData()
                };

                WeatherImpact = await _predictiveLightService.AnalyzeWeatherImpactAsync(analysisRequest, _cancellationTokenSource.Token);

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
                    PredictionWindowHours = 48
                };

                var predictions = await _predictiveLightService.GenerateHourlyPredictionsAsync(predictionRequest, _cancellationTokenSource.Token);
                var nowTick = DateTime.Now;
                
                _hourlyPredictions.Clear();
                foreach (var prediction in predictions)
                {
                    var predTick = prediction.DateTime;



                    if (prediction.DateTime > nowTick)
                    {
                        var confidence = CalculateEnhancedConfidence(prediction, WeatherImpact);

                        _hourlyPredictions.Add(new HourlyPredictionDisplayModel
                        {
                            Time = prediction.DateTime,
                            DeviceTimeDisplay = FormatTimeForTimezone(prediction.DateTime, DeviceTimeZone),
                            LocationTimeDisplay = FormatTimeForTimezone(prediction.DateTime, LocationTimeZone),
                            PredictedEV = prediction.PredictedEV,
                            EVConfidenceMargin = prediction.EVConfidenceMargin,
                            SuggestedAperture = ExtractApertureValue(prediction.SuggestedSettings.Aperture),
                            SuggestedShutterSpeed = prediction.SuggestedSettings.ShutterSpeed,
                            SuggestedISO = ExtractISOValue(prediction.SuggestedSettings.ISO),
                            ConfidenceLevel = confidence,
                            LightQuality = prediction.LightQuality.OptimalFor,
                            ColorTemperature = prediction.LightQuality.ColorTemperature,
                            Recommendations = string.Join(", ", prediction.Recommendations),
                            IsOptimalTime = prediction.IsOptimalForPhotography,
                            TimeFormat = TimeFormat
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error generating predictions: {ex.Message}");
            }
        }

        private string ExtractApertureValue(string aperture)
        {
            return aperture?.Replace("f/", "") ?? "8";
        }

        private string ExtractISOValue(string iso)
        {
            return iso?.Replace("ISO ", "") ?? "100";
        }

        private double CalculateEnhancedConfidence(HourlyLightPrediction prediction, WeatherImpactAnalysis weather)
        {
            double baseConfidence = 0.90; // Start with 90%

            // Reduce by time into future (5% per day)
            var hoursFromNow = (prediction.DateTime - DateTime.Now).TotalHours;
            var daysFromNow = hoursFromNow / 24.0;
            baseConfidence -= (daysFromNow * 0.05);

            // Weather factors
            if (weather.CurrentConditions != null)
            {
                // Cloud cover uncertainty (10-30% reduction)
                baseConfidence -= (weather.CurrentConditions.CloudCover * 0.3);

                // Precipitation reduces confidence by 40%
                if (weather.CurrentConditions.Precipitation > 0)
                {
                    baseConfidence -= 0.4;
                }
            }

            // Time of day factors
            var hour = prediction.DateTime.Hour;
            if (hour <= 6 || hour >= 18) // Twilight periods less predictable
            {
                baseConfidence -= 0.2;
            }

            // After sunset - base on moon phase
            if (prediction.SunPosition?.IsAboveHorizon == false)
            {
                // Simplified moon phase calculation
                var dayOfMonth = prediction.DateTime.Day;
                var approximateMoonPhase = Math.Abs(dayOfMonth - 15) / 15.0; // 0 = full, 1 = new

                if (approximateMoonPhase < 0.2) // Near full moon
                    baseConfidence = Math.Max(baseConfidence, 0.7);
                else if (approximateMoonPhase > 0.8) // Near new moon  
                    baseConfidence = Math.Min(baseConfidence, 0.3);
                else
                    baseConfidence = Math.Min(baseConfidence, 0.5);
            }

            return Math.Max(0.1, Math.Min(1.0, baseConfidence));
        }

        private async Task CalculateOptimalWindowsAsync()
        {
            if (SelectedLocation == null) return;

            var optimalQuery = new GetOptimalShootingTimesQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate,
                IncludeWeatherForecast = true,
                TimeZone = LocationTimeZone.Id
            };

            var result = await _mediator.Send(optimalQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                OptimalWindows.Clear();
                foreach (var window in result.Data)
                {
                    if (window.StartTime > DateTime.Now)
                    {
                        OptimalWindows.Add(new OptimalWindowDisplayModel
                        {
                            WindowType = window.LightQuality.ToString(),
                            StartTime = window.StartTime,
                            EndTime = window.EndTime,
                            StartTimeDisplay = FormatTimeForTimezone(window.StartTime, LocationTimeZone),
                            EndTimeDisplay = FormatTimeForTimezone(window.EndTime, LocationTimeZone),
                            LightQuality = window.Description,
                            OptimalFor = string.Join(", ", window.IdealFor),
                            IsCurrentlyActive = DateTime.Now >= window.StartTime && DateTime.Now <= window.EndTime,

                            ConfidenceLevel = window.QualityScore,
                            TimeFormat = TimeFormat
                        });
                    }
                }
            }
        }

        private void UpdateCurrentPredictionDisplay()
        {
            var now = DateTime.Now;
            var currentPrediction = HourlyPredictions.FirstOrDefault(p =>
                Math.Abs((p.Time - now).TotalMinutes) < 30);

            if (currentPrediction != null)
            {
                CurrentPredictionText =
                    $"At {currentPrediction.LocationTimeDisplay}, expect EV {currentPrediction.PredictedEV:F1} " +
                    $"±{currentPrediction.EVConfidenceMargin:F1}, " +
                    $"f/{currentPrediction.SuggestedAperture} @ {currentPrediction.SuggestedShutterSpeed} " +
                    $"ISO {currentPrediction.SuggestedISO}, " +
                    $"{currentPrediction.ConfidenceLevel:P0} confidence";
            }

            var nextWindow = OptimalWindows.FirstOrDefault(w => w.StartTime > now);
            if (nextWindow != null)
            {
                var timeUntil = nextWindow.StartTime - now;
                NextOptimalWindowText =
                    $"Next optimal window: {nextWindow.WindowType} in {timeUntil.Hours}h {timeUntil.Minutes}m";
            }
        }

        private string FormatTimeForTimezone(DateTime utcTime, TimeZoneInfo timezone)
        {
            try
            {
                if (timezone == null)
                {
                    throw new ArgumentNullException(nameof(timezone), "Timezone cannot be null");
                }

                DateTime localTime = utcTime;
                try
                {
                    if (utcTime.Hour == 0 && utcTime.Minute == 0 && utcTime.Second == 0)
                    {
                        // Handle default DateTime values
                    }
                    else
                    {
                        utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
                        localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timezone);
                    }
                }
                catch (Exception)
                {
                    // Use original time if conversion fails
                }
                return localTime.ToString(TimeFormat);
            }
            catch (ArgumentNullException ex)
            {
                OnErrorOccurred($"Error formatting time: {ex.Message}");
                return utcTime.ToString(TimeFormat);
            }
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

        partial void OnSelectedLocationChanged(LocationListItemViewModel? value)
        {
            if (value != null)
            {
                LocationPhoto = value.Photo;
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
        partial void OnSelectedDateChanged(DateTime value)
        {
            if (SelectedLocation != null)
            {
                _ = CalculateEnhancedSunDataAsync();
            }
        }

        public override void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.Dispose();
        }
    }
}