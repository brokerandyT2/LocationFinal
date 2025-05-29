// Location.Photography.ViewModels/EnhancedSunCalculatorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using Location.Core.ViewModels;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using MediatR;
using System.Collections.ObjectModel;
using EnhancedSunTimes = Location.Photography.Domain.Models.EnhancedSunTimes;
using HourlyLightPrediction = Location.Photography.Application.Services.HourlyLightPrediction;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class EnhancedSunCalculatorViewModel : ViewModelBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly IPredictiveLightService _predictiveLightService;
        private CancellationTokenSource _cancellationTokenSource = new();
        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<LocationListItemViewModel> _locations = new();

        [ObservableProperty]
        private LocationListItemViewModel? _selectedLocation;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private string _locationPhoto = string.Empty;

        // Dual Timezone Support - As Required
        [ObservableProperty]
        private TimeZoneInfo _deviceTimeZone = TimeZoneInfo.Local;

        [ObservableProperty]
        private TimeZoneInfo _locationTimeZone = TimeZoneInfo.Local;

        [ObservableProperty]
        private bool _showDualTimezone = true;

        // Enhanced Sun Times with dual timezone support
        [ObservableProperty]
        private EnhancedSunTimes _deviceTimeSunTimes = new();

        [ObservableProperty]
        private EnhancedSunTimes _locationTimeSunTimes = new();

        // Formatted Properties for UI Binding - Device Time
        public string DeviceSunriseFormatted => DeviceTimeSunTimes.Sunrise.ToString("hh:mm tt");
        public string DeviceSunsetFormatted => DeviceTimeSunTimes.Sunset.ToString("hh:mm tt");
        public string DeviceGoldenHourMorningFormatted => DeviceTimeSunTimes.GoldenHourMorningStart.ToString("hh:mm tt");
        public string DeviceGoldenHourEveningFormatted => DeviceTimeSunTimes.GoldenHourEveningStart.ToString("hh:mm tt");

        // Formatted Properties for UI Binding - Location Time
        public string LocationSunriseFormatted => LocationTimeSunTimes.Sunrise.ToString("hh:mm tt");
        public string LocationSunsetFormatted => LocationTimeSunTimes.Sunset.ToString("hh:mm tt");
        public string LocationGoldenHourMorningFormatted => LocationTimeSunTimes.GoldenHourMorningStart.ToString("hh:mm tt");
        public string LocationGoldenHourEveningFormatted => LocationTimeSunTimes.GoldenHourEveningStart.ToString("hh:mm tt");

        // Weather Integration - 5 Day Alignment with Business Rules
        [ObservableProperty]
        private WeatherImpactAnalysis _weatherImpact = new();

        // Predictive Light Modeling - Option B Advanced Features
        [ObservableProperty]
        private ObservableCollection<HourlyLightPrediction> _hourlyPredictions = new();

        [ObservableProperty]
        private PredictiveLightRecommendation _lightRecommendation = new();

        [ObservableProperty]
        private OptimalShootingWindow _bestShootingWindow = new();

        [ObservableProperty]
        private ObservableCollection<OptimalShootingTime> _optimalShootingTimes = new();

        // Light Meter Integration with Calibration
        [ObservableProperty]
        private bool _isLightMeterCalibrated;

        [ObservableProperty]
        private DateTime? _lastLightMeterReading;

        [ObservableProperty]
        private double _calibrationAccuracy;

        // Moon Integration
        [ObservableProperty]
        private MoonPhaseData _moonData = new();

        // Interactive Sun Path Data
        [ObservableProperty]
        private ObservableCollection<SunPathPoint> _sunPathPoints = new();

        [ObservableProperty]
        private SunPathPoint _currentSunPosition = new();

        // Professional Photography Features
        [ObservableProperty]
        private ShadowCalculationResult _shadowData = new();

        // Current Prediction Display - Key Feature for Option B
        [ObservableProperty]
        private string _currentPredictionText = string.Empty;

        [ObservableProperty]
        private bool _isPredictionAvailable;
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Constructor
        public EnhancedSunCalculatorViewModel(
            IMediator mediator,
            IErrorDisplayService errorDisplayService,
            IPredictiveLightService predictiveLightService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService));
        }
        #endregion

        #region Commands
        [RelayCommand]
        private async Task LoadLocationsAsync()
        {
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();

                    ClearErrors();

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
                    // User cancelled - no error needed
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error loading locations: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
        }

        [RelayCommand]
        private async Task CalculateEnhancedSunDataAsync()
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

                    // Execute all enhanced calculations in sequence
                    await CalculateDualTimezoneSunTimesAsync();
                    await CalculateMoonDataAsync();
                    await GenerateSunPathDataAsync();
                    await CalculateOptimalShootingTimesAsync();
                    await CalculateShadowDataAsync();
                    await IntegrateWeatherDataAsync(); // 5-day alignment with business rules
                    await GeneratePredictiveLightRecommendationsAsync(); // Option B Advanced

                    // Update current prediction display
                    UpdateCurrentPredictionDisplay();
                }
                catch (OperationCanceledException)
                {
                    // User cancelled - no error needed  
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error calculating enhanced sun data: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
        }

        [RelayCommand]
        private async Task CalibrateLightMeterAsync(double currentEV)
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

                    // Calibrate predictive model with actual light meter reading
                    var calibrationRequest = new LightMeterCalibrationRequest
                    {
                        LocationId = SelectedLocation.Id,
                        Latitude = SelectedLocation.Latitude,
                        Longitude = SelectedLocation.Longitude,
                        DateTime = DateTime.Now,
                        ActualEV = currentEV,
                        WeatherConditions = WeatherImpact?.CurrentConditions
                    };

                    await _predictiveLightService.CalibrateWithActualReadingAsync(calibrationRequest, _cancellationTokenSource.Token);

                    IsLightMeterCalibrated = true;
                    LastLightMeterReading = DateTime.Now;

                    // Regenerate predictions with calibrated model - weighted average with more weight to recent readings
                    await GeneratePredictiveLightRecommendationsAsync();

                    // Update prediction display with calibrated data
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
        private void SetupShootingAlerts()
        {
            // Stubbed as requested - no foreground service implementation yet
            try
            {
                if (OptimalShootingTimes?.Any() != true)
                {
                    SetValidationError("No optimal shooting times available. Calculate sun data first.");
                    return;
                }

                // Prepare alert data structure for future foreground service
                var alertRequests = OptimalShootingTimes
                    .Where(t => t.StartTime > DateTime.Now)
                    .Take(3) // Limit to next 3 optimal times
                    .Select(t => new ShootingAlertRequest
                    {
                        LocationId = SelectedLocation?.Id ?? 0,
                        AlertTime = t.StartTime.AddMinutes(-30), // Alert 30 minutes before
                        ShootingWindowStart = t.StartTime,
                        ShootingWindowEnd = t.EndTime,
                        LightQuality = t.LightQuality,
                        RecommendedSettings = t.RecommendedExposure?.SuggestedSettings?.FormattedSettings,
                        Message = $"Optimal {t.LightQuality} shooting window starts in 30 minutes"
                    })
                    .ToList();

                // Store alerts for future implementation
                SetValidationError($"Alerts prepared for {alertRequests.Count} upcoming shooting windows. Foreground service coming soon!");
            }
            catch (Exception ex)
            {
                OnSystemError($"Error setting up alerts: {ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        private async Task CalculateDualTimezoneSunTimesAsync()
        {
            if (SelectedLocation == null) return;

            // Get location timezone from coordinates - simplified to local for now
            LocationTimeZone = await GetTimezoneForCoordinatesAsync(SelectedLocation.Latitude, SelectedLocation.Longitude);

            // Calculate enhanced sun times
            var sunTimesQuery = new GetEnhancedSunTimesQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate,
                UseHighPrecision = true
            };

            var result = await _mediator.Send(sunTimesQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                // Convert to both timezones for dual display
                DeviceTimeSunTimes = ConvertSunTimesToTimezone(result.Data, DeviceTimeZone);
                LocationTimeSunTimes = ConvertSunTimesToTimezone(result.Data, LocationTimeZone);

                // Notify UI of formatted property changes
                OnPropertyChanged(nameof(DeviceSunriseFormatted));
                OnPropertyChanged(nameof(DeviceSunsetFormatted));
                OnPropertyChanged(nameof(DeviceGoldenHourMorningFormatted));
                OnPropertyChanged(nameof(DeviceGoldenHourEveningFormatted));
                OnPropertyChanged(nameof(LocationSunriseFormatted));
                OnPropertyChanged(nameof(LocationSunsetFormatted));
                OnPropertyChanged(nameof(LocationGoldenHourMorningFormatted));
                OnPropertyChanged(nameof(LocationGoldenHourEveningFormatted));
            }
        }

        private async Task CalculateMoonDataAsync()
        {
            if (SelectedLocation == null) return;

            var moonQuery = new GetMoonDataQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate
            };

            var result = await _mediator.Send(moonQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                MoonData = result.Data;
            }
        }

        private async Task GenerateSunPathDataAsync()
        {
            if (SelectedLocation == null) return;

            var pathQuery = new GetSunPathDataQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate,
                IntervalMinutes = 15 // Every 15 minutes for smooth path
            };

            var result = await _mediator.Send(pathQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                SunPathPoints.Clear();
                foreach (var point in result.Data.PathPoints)
                {
                    SunPathPoints.Add(point);
                }

                CurrentSunPosition = result.Data.CurrentPosition;
            }
        }

        private async Task CalculateOptimalShootingTimesAsync()
        {
            if (SelectedLocation == null) return;

            var shootingQuery = new GetOptimalShootingTimesQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Date = SelectedDate,
                IncludeWeatherForecast = true
            };

            var result = await _mediator.Send(shootingQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                OptimalShootingTimes.Clear();
                foreach (var time in result.Data)
                {
                    OptimalShootingTimes.Add(time);
                }
            }
        }

        private async Task CalculateShadowDataAsync()
        {
            if (SelectedLocation == null) return;

            var shadowQuery = new GetShadowCalculationQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                DateTime = SelectedDate.Add(DateTime.Now.TimeOfDay),
                ObjectHeight = 6.0, // Default 6 feet person
                TerrainType = TerrainType.Flat
            };

            var result = await _mediator.Send(shadowQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                ShadowData = result.Data;
            }
        }

        private async Task IntegrateWeatherDataAsync()
        {
            if (SelectedLocation == null) return;

            // Get 5-day weather forecast to align with business rules
            var weatherQuery = new GetWeatherForecastQuery
            {
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                Days = 5 // Align with existing weather business rules
            };

            var result = await _mediator.Send(weatherQuery, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Data != null)
            {
                // Analyze weather impact on light conditions
                var analysisRequest = new WeatherImpactAnalysisRequest
                {
                    WeatherForecast = result.Data,
                    SunTimes = LocationTimeSunTimes,
                    MoonData = MoonData
                };

                WeatherImpact = await _predictiveLightService.AnalyzeWeatherImpactAsync(analysisRequest, _cancellationTokenSource.Token);
            }
        }

        private async Task GeneratePredictiveLightRecommendationsAsync()
        {
            if (SelectedLocation == null) return;

            var predictionRequest = new PredictiveLightRequest
            {
                LocationId = SelectedLocation.Id,
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                TargetDate = SelectedDate,
                WeatherImpact = WeatherImpact,
                SunTimes = LocationTimeSunTimes,
                MoonPhase = MoonData,
                LastCalibrationReading = LastLightMeterReading,
                PredictionWindowHours = 24
            };

            // Generate hourly predictions - Key feature for Option B Advanced
            var predictions = await _predictiveLightService.GenerateHourlyPredictionsAsync(predictionRequest, _cancellationTokenSource.Token);

            HourlyPredictions.Clear();
            foreach (var prediction in predictions)
            {
                HourlyPredictions.Add(prediction);
            }

            // Generate overall recommendation
            LightRecommendation = await _predictiveLightService.GenerateRecommendationAsync(predictionRequest, _cancellationTokenSource.Token);

            if (LightRecommendation.BestTimeWindow != null)
            {
                BestShootingWindow = LightRecommendation.BestTimeWindow;
                CalibrationAccuracy = LightRecommendation.CalibrationAccuracy;
            }
        }

        private void UpdateCurrentPredictionDisplay()
        {
            // Update the current prediction text for Option B Advanced display
            var currentPrediction = HourlyPredictions?.FirstOrDefault(p => p.DateTime.Hour == DateTime.Now.Hour);

            if (currentPrediction != null)
            {
                // Format: "At 6:30 PM, expect EV 11.5 ±0.5, f/4 @ 1/250s ISO 100, 85% confidence"
                CurrentPredictionText =
                    $"At {currentPrediction.DateTime:h:mm tt}, expect EV {currentPrediction.PredictedEV:F1} " +
                    $"±{currentPrediction.EVConfidenceMargin:F1}, {currentPrediction.SuggestedSettings.FormattedSettings}, " +
                    $"{currentPrediction.ConfidenceLevel:P0} confidence {currentPrediction.ConfidenceReason}";

                IsPredictionAvailable = true;
            }
            else
            {
                CurrentPredictionText = "No prediction available for current time";
                IsPredictionAvailable = false;
            }
        }

        private async Task<TimeZoneInfo> GetTimezoneForCoordinatesAsync(double latitude, double longitude)
        {
            // TODO: Implement coordinate to timezone lookup
            // For now, return local timezone as per business rules
            return TimeZoneInfo.Local;
        }

        private EnhancedSunTimes ConvertSunTimesToTimezone(EnhancedSunTimes utcTimes, TimeZoneInfo targetTimezone)
        {
            return new EnhancedSunTimes
            {
                Sunrise = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.Sunrise, targetTimezone),
                Sunset = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.Sunset, targetTimezone),
                SolarNoon = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.SolarNoon, targetTimezone),
                CivilDawn = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.CivilDawn, targetTimezone),
                CivilDusk = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.CivilDusk, targetTimezone),
                NauticalDawn = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.NauticalDawn, targetTimezone),
                NauticalDusk = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.NauticalDusk, targetTimezone),
                AstronomicalDawn = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.AstronomicalDawn, targetTimezone),
                AstronomicalDusk = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.AstronomicalDusk, targetTimezone),
                BlueHourMorning = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.BlueHourMorning, targetTimezone),
                BlueHourEvening = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.BlueHourEvening, targetTimezone),
                GoldenHourMorningStart = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.GoldenHourMorningStart, targetTimezone),
                GoldenHourMorningEnd = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.GoldenHourMorningEnd, targetTimezone),
                GoldenHourEveningStart = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.GoldenHourEveningStart, targetTimezone),
                GoldenHourEveningEnd = TimeZoneInfo.ConvertTimeFromUtc(utcTimes.GoldenHourEveningEnd, targetTimezone),
                TimeZone = targetTimezone,
                IsDaylightSavingTime = targetTimezone.IsDaylightSavingTime(DateTime.Now),
                UtcOffset = targetTimezone.GetUtcOffset(DateTime.Now)
            };
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }
        #endregion

        #region Navigation
        public void OnNavigatedToAsync()
        {
            SelectedDate = DateTime.Today;
            _ = LoadLocationsAsync();
        }

        public void OnNavigatedFromAsync()
        {
            _cancellationTokenSource?.Cancel();
        }
        #endregion

        #region Partial Methods
        partial void OnSelectedLocationChanged(LocationListItemViewModel? value)
        {
            if (value != null)
            {
                LocationPhoto = value.Photo;
                _ = CalculateEnhancedSunDataAsync();
            }
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            if (SelectedLocation != null)
            {
                _ = CalculateEnhancedSunDataAsync();
            }
        }
        #endregion

        #region Disposal
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}