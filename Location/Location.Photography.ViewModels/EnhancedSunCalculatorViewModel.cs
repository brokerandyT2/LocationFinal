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

        [ObservableProperty]
        private DateTime _sunriseUtc;

        [ObservableProperty]
        private DateTime _sunsetUtc;

        [ObservableProperty]
        private DateTime _solarNoonUtc;

        // Enhanced weather integration properties
        [ObservableProperty]
        private HourlyWeatherForecastDto? _hourlyWeatherData;

        [ObservableProperty]
        private string _weatherDataStatus = "Loading...";

    
        private DateTime _weatherLastUpdate;

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
                    WeatherDataStatus = "Loading enhanced weather data...";

                    await LoadLocationTimezoneAsync();
                    await CalculateSunTimesAsync();
                    await GenerateSunPathPointsAsync();

                    // FIXED: Ensure weather data is persisted BEFORE reading it
                    await EnsureWeatherDataAsync();

                    // Enhanced: Load hourly weather data first, then generate predictions
                    await LoadHourlyWeatherDataAsync();
                    await GenerateWeatherAwarePredictionsAsync();
                    await CalculateOptimalWindowsAsync();

                    UpdateCurrentPredictionDisplay();

                    WeatherDataStatus = $"Updated {WeatherLastUpdate:HH:mm}";
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    WeatherDataStatus = "Weather data unavailable";
                    OnSystemError($"Error calculating enhanced sun data: {ex.Message}");
                }
            });
            
            await ExecuteAndTrackAsync(command);
        }
        private async Task EnsureWeatherDataAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                // STEP 1: Force update weather data (this persists to database)
                var updateCommand = new UpdateWeatherCommand
                {
                    LocationId = SelectedLocation.Id,
                    ForceUpdate = true
                };

                var updateResult = await _mediator.Send(updateCommand, _cancellationTokenSource.Token);

                if (updateResult.IsSuccess && updateResult.Data != null)
                {
                    WeatherLastUpdate = updateResult.Data.LastUpdate;
                    WeatherDataStatus = "Weather data updated and cached";
                }
                else
                {
                    WeatherDataStatus = "Weather update failed - using cached data if available";
                    OnSystemError($"Weather update failed: {updateResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                WeatherDataStatus = "Weather service unavailable";
                OnSystemError($"Error updating weather data: {ex.Message}");
            }
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

                    // Regenerate predictions with calibration
                    await GenerateWeatherAwarePredictionsAsync();
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

        // ENHANCED: Load hourly weather data for accurate predictions
        private async Task LoadHourlyWeatherDataAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                // FIXED: This now reads from database (after EnsureWeatherDataAsync persisted it)
                var hourlyQuery = new GetHourlyForecastQuery
                {
                    LocationId = SelectedLocation.Id,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddDays(5) // Align with 5-day business rule
                };

                var hourlyResult = await _mediator.Send(hourlyQuery, _cancellationTokenSource.Token);

                if (hourlyResult.IsSuccess && hourlyResult.Data != null)
                {
                    HourlyWeatherData = hourlyResult.Data;
                    WeatherLastUpdate = hourlyResult.Data.LastUpdate;
                    WeatherDataStatus = "Hourly weather data loaded from cache";
                }
                else
                {
                    // Fallback: try to get weather forecast if hourly data unavailable
                    await LoadFallbackWeatherDataAsync();
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading hourly weather data: {ex.Message}");
                await LoadFallbackWeatherDataAsync();
            }
        }

        private async Task LoadFallbackWeatherDataAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                // FIXED: First try to read from database (after persistence)
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                if (weather != null && weather.Forecasts.Any())
                {
                    // Use persisted weather data
                    WeatherLastUpdate = weather.LastUpdate;
                    WeatherDataStatus = "Using cached weather data";

                    // Convert to WeatherForecastDto for compatibility
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

                    await GenerateBasicWeatherImpactAsync(weatherForecastDto);
                }
                else
                {
                    // Last resort: API call (but this won't persist - should not happen after EnsureWeatherDataAsync)
                    var weatherQuery = new GetWeatherForecastQuery
                    {
                        Latitude = SelectedLocation.Latitude,
                        Longitude = SelectedLocation.Longitude,
                        Days = 5 // Business rule alignment
                    };

                    var weatherResult = await _mediator.Send(weatherQuery, _cancellationTokenSource.Token);

                    if (weatherResult.IsSuccess && weatherResult.Data != null)
                    {
                        WeatherLastUpdate = weatherResult.Data.LastUpdate;
                        WeatherDataStatus = "Using API weather data (not cached)";
                        await GenerateBasicWeatherImpactAsync(weatherResult.Data);
                    }
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading fallback weather data: {ex.Message}");
            }
        }

        // ENHANCED: Generate predictions using actual hourly weather data
        private async Task GenerateWeatherAwarePredictionsAsync()
        {
            try
            {
                if (SelectedLocation == null) return;

                _hourlyPredictions.Clear();

                if (HourlyWeatherData?.HourlyForecasts?.Any() == true)
                {
                    // Use actual hourly weather data for predictions
                    await GeneratePredictionsFromHourlyWeatherAsync();
                }
                else
                {
                    // Fallback to basic predictions without hourly data
                    await GenerateBasicPredictionsAsync();
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error generating weather-aware predictions: {ex.Message}");
            }
        }

        private async Task GeneratePredictionsFromHourlyWeatherAsync()
        {
            if (HourlyWeatherData?.HourlyForecasts == null || SelectedLocation == null) return;

            var nowUtc = DateTime.UtcNow;

            foreach (var hourlyForecast in HourlyWeatherData.HourlyForecasts.Where(h => h.DateTime > nowUtc))
            {
                try
                {
                    // Calculate sun position for this hour
                    var sunPositionQuery = new GetSunPositionQuery
                    {
                        Latitude = SelectedLocation.Latitude,
                        Longitude = SelectedLocation.Longitude,
                        DateTime = hourlyForecast.DateTime
                    };

                    var sunPositionResult = await _mediator.Send(sunPositionQuery, _cancellationTokenSource.Token);

                    if (sunPositionResult.IsSuccess && sunPositionResult.Data != null)
                    {
                        var sunPosition = sunPositionResult.Data;

                        // Enhanced weather impact calculation using actual conditions
                        var weatherImpact = CalculateHourlyWeatherImpact(hourlyForecast);

                        // Generate predictive light recommendation
                        var lightPrediction = await GenerateHourlyLightPredictionAsync(
                            hourlyForecast,
                            sunPosition,
                            weatherImpact);

                        var displayModel = new HourlyPredictionDisplayModel
                        {
                            Time = hourlyForecast.DateTime,
                            DeviceTimeDisplay = FormatTimeForTimezone(hourlyForecast.DateTime, DeviceTimeZone),
                            LocationTimeDisplay = FormatTimeForTimezone(hourlyForecast.DateTime, LocationTimeZone),
                            PredictedEV = lightPrediction.PredictedEV,
                            EVConfidenceMargin = lightPrediction.EVConfidenceMargin,
                            SuggestedAperture = ExtractApertureValue(lightPrediction.SuggestedSettings.Aperture),
                            SuggestedShutterSpeed = lightPrediction.SuggestedSettings.ShutterSpeed,
                            SuggestedISO = ExtractISOValue(lightPrediction.SuggestedSettings.ISO),
                            ConfidenceLevel = lightPrediction.ConfidenceLevel,
                            LightQuality = lightPrediction.LightQuality.OptimalFor,
                            ColorTemperature = lightPrediction.LightQuality.ColorTemperature,
                            Recommendations = string.Join(", ", lightPrediction.Recommendations),
                            IsOptimalTime = lightPrediction.IsOptimalForPhotography,
                            TimeFormat = TimeFormat,

                            // Enhanced: Weather-specific data
                            WeatherDescription = hourlyForecast.Description,
                            CloudCover = hourlyForecast.Clouds,
                            PrecipitationProbability = hourlyForecast.ProbabilityOfPrecipitation,
                            WindInfo = $"{hourlyForecast.WindSpeed:F1} mph {GetCardinalDirection(hourlyForecast.WindDirection)}",
                            UvIndex = hourlyForecast.UvIndex,
                            Humidity = hourlyForecast.Humidity
                        };

                        _hourlyPredictions.Add(displayModel);
                    }
                }
                catch (Exception ex)
                {
                    // Log individual hour failure but continue processing
                    System.Diagnostics.Debug.WriteLine($"Error processing hour {hourlyForecast.DateTime}: {ex.Message}");
                }
            }
        }

        private WeatherImpactFactor CalculateHourlyWeatherImpact(HourlyForecastDto hourlyForecast)
        {
            return new WeatherImpactFactor
            {
                CloudCoverReduction = hourlyForecast.Clouds * 0.008, // 10% clouds = 8% light reduction
                PrecipitationReduction = hourlyForecast.ProbabilityOfPrecipitation > 0.3 ? 0.6 : 0, // Heavy reduction if >30% chance
                HumidityReduction = hourlyForecast.Humidity * 0.001, // Subtle haze effect
                VisibilityReduction = hourlyForecast.Visibility < 5000 ? 0.2 : 0, // Reduced visibility impact
                OverallLightReductionFactor = CalculateOverallLightReduction(hourlyForecast),
                ConfidenceImpact = CalculateWeatherConfidenceImpact(hourlyForecast)
            };
        }

        private double CalculateOverallLightReduction(HourlyForecastDto hourlyForecast)
        {
            double reduction = 1.0; // Start with 100% light

            // Cloud cover impact (progressive)
            reduction *= (1.0 - (hourlyForecast.Clouds * 0.008));

            // Precipitation impact
            if (hourlyForecast.ProbabilityOfPrecipitation > 0.3)
                reduction *= 0.4; // Heavy reduction for likely precipitation

            // Humidity/haze impact
            reduction *= (1.0 - (hourlyForecast.Humidity * 0.001));

            // Visibility impact
            if (hourlyForecast.Visibility < 5000)
                reduction *= 0.8; // Reduced visibility affects light quality

            return Math.Max(0.1, reduction); // Never go below 10% light
        }

        private double CalculateWeatherConfidenceImpact(HourlyForecastDto hourlyForecast)
        {
            double confidence = 0.95; // Start with high confidence

            // Reduce confidence based on uncertainty factors
            confidence -= hourlyForecast.ProbabilityOfPrecipitation * 0.4; // Rain reduces confidence
            confidence -= (hourlyForecast.Clouds / 100.0) * 0.2; // Clouds add uncertainty
            confidence -= hourlyForecast.WindSpeed > 15 ? 0.1 : 0; // High wind adds uncertainty

            return Math.Max(0.3, Math.Min(0.95, confidence)); // Keep between 30% and 95%
        }

        private async Task<HourlyLightPrediction> GenerateHourlyLightPredictionAsync(
            HourlyForecastDto hourlyForecast,
            SunPositionDto sunPosition,
            WeatherImpactFactor weatherImpact)
        {
            // Enhanced prediction using actual weather conditions
            var request = new PredictiveLightRequest
            {
                LocationId = SelectedLocation.Id,
                Latitude = SelectedLocation.Latitude,
                Longitude = SelectedLocation.Longitude,
                TargetDate = hourlyForecast.DateTime,
                WeatherImpact = new WeatherImpactAnalysis
                {
                    CurrentConditions = new WeatherConditions
                    {
                        CloudCover = hourlyForecast.Clouds / 100.0,
                        Precipitation = hourlyForecast.ProbabilityOfPrecipitation,
                        Humidity = hourlyForecast.Humidity / 100.0,
                        WindSpeed = hourlyForecast.WindSpeed,
                        Visibility = hourlyForecast.Visibility,
                        UvIndex = hourlyForecast.UvIndex
                    },
                    OverallLightReductionFactor = weatherImpact.OverallLightReductionFactor
                },
                SunTimes = new Location.Photography.Domain.Models.EnhancedSunTimes(),
                MoonPhase = new MoonPhaseData(),
                LastCalibrationReading = LastLightMeterReading,
                PredictionWindowHours = 48
            };

            var predictions = await _predictiveLightService.GenerateHourlyPredictionsAsync(request, _cancellationTokenSource.Token);
            var matchingPrediction = predictions.FirstOrDefault(p =>
                Math.Abs((p.DateTime - hourlyForecast.DateTime).TotalMinutes) < 30);

            if (matchingPrediction != null)
            {
                // Apply weather-based confidence adjustment
                matchingPrediction.ConfidenceLevel *= weatherImpact.ConfidenceImpact;
                return matchingPrediction;
            }

            // Fallback: create basic prediction
            return CreateBasicLightPrediction(hourlyForecast, sunPosition, weatherImpact);
        }

        private HourlyLightPrediction CreateBasicLightPrediction(
            HourlyForecastDto hourlyForecast,
            SunPositionDto sunPosition,
            WeatherImpactFactor weatherImpact)
        {
            // Basic EV calculation based on sun position and weather
            var baseEV = CalculateBaseEVFromSunPosition(sunPosition);
            var weatherAdjustedEV = baseEV * weatherImpact.OverallLightReductionFactor;

            return new HourlyLightPrediction
            {
                DateTime = hourlyForecast.DateTime,
                PredictedEV = Math.Round(weatherAdjustedEV, 1),
                EVConfidenceMargin = CalculateEVConfidenceMargin(weatherImpact),
                SuggestedSettings = CalculateExposureSettings(weatherAdjustedEV),
                ConfidenceLevel = weatherImpact.ConfidenceImpact,
                LightQuality = DetermineLightQuality(sunPosition, hourlyForecast),
                Recommendations = GenerateWeatherBasedRecommendations(hourlyForecast, sunPosition),
                IsOptimalForPhotography = IsOptimalShootingConditions(sunPosition, hourlyForecast),
                SunPosition = sunPosition
            };
        }

        private double CalculateBaseEVFromSunPosition(SunPositionDto sunPosition)
        {
            // Simplified EV calculation based on sun elevation
            if (sunPosition.Elevation <= 0) return 4; // Night/twilight
            if (sunPosition.Elevation < 10) return 8; // Low sun
            if (sunPosition.Elevation < 30) return 12; // Moderate sun
            return 15; // High sun
        }

        private double CalculateEVConfidenceMargin(WeatherImpactFactor weatherImpact)
        {
            // Higher uncertainty = larger confidence margin
            var baseMargin = 0.3;
            var uncertaintyFactor = 1.0 - weatherImpact.ConfidenceImpact;
            return baseMargin + (uncertaintyFactor * 1.0); // Up to ±1.3 EV in very uncertain conditions
        }

        private ExposureTriangle CalculateExposureSettings(double ev)
        {
            // Simplified exposure calculation
            var aperture = Math.Max(1.4, Math.Min(16, ev / 2));
            var shutterSpeed = CalculateShutterSpeed(ev, aperture);
            var iso = CalculateISO(ev, aperture, shutterSpeed);

            return new ExposureTriangle
            {
                Aperture = $"f/{aperture:F1}",
                ShutterSpeed = FormatShutterSpeed(shutterSpeed),
                ISO = $"ISO {iso}"
            };
        }

        private double CalculateShutterSpeed(double ev, double aperture)
        {
            // Basic exposure relationship: EV = log2(N²/t) where N=aperture, t=shutter time
            var shutterTime = (aperture * aperture) / Math.Pow(2, ev);
            return Math.Max(1.0 / 4000, Math.Min(30, shutterTime));
        }

        private int CalculateISO(double ev, double aperture, double shutterSpeed)
        {
            // Keep ISO low when possible, increase for low light
            if (ev > 12) return 100;
            if (ev > 10) return 200;
            if (ev > 8) return 400;
            if (ev > 6) return 800;
            return 1600;
        }

        private string FormatShutterSpeed(double seconds)
        {
            if (seconds >= 1) return $"{seconds:F0}s";
            var fraction = 1.0 / seconds;
            return $"1/{fraction:F0}";
        }

        private LightCharacteristics DetermineLightQuality(SunPositionDto sunPosition, HourlyForecastDto weather)
        {
            var colorTemp = CalculateColorTemperature(sunPosition.Elevation, weather.Clouds);
            var optimalFor = DetermineOptimalPhotographyType(sunPosition, weather);

            return new LightCharacteristics
            {
                ColorTemperature = colorTemp,
                OptimalFor = optimalFor
            };
        }

        private double CalculateColorTemperature(double elevation, int cloudCover)
        {
            // Color temperature varies with sun angle and clouds
            var baseTemp = 5500; // Daylight

            if (elevation < 10) baseTemp = 3000; // Golden hour
            else if (elevation < 20) baseTemp = 4000; // Early morning/late afternoon

            // Clouds make light cooler
            var cloudAdjustment = cloudCover * 5; // Up to 500K cooler with full clouds

            return Math.Max(2500, Math.Min(7000, baseTemp + cloudAdjustment));
        }

        private string DetermineOptimalPhotographyType(SunPositionDto sunPosition, HourlyForecastDto weather)
        {
            if (weather.ProbabilityOfPrecipitation > 0.5) return "Moody/dramatic photography";
            if (sunPosition.Elevation < 10) return "Portraits, golden hour shots";
            if (sunPosition.Elevation > 60 && weather.Clouds > 50) return "Even lighting, portraits";
            if (sunPosition.Elevation > 60) return "Landscapes, architecture";
            return "General photography";
        }

        private List<string> GenerateWeatherBasedRecommendations(HourlyForecastDto weather, SunPositionDto sunPosition)
        {
            var recommendations = new List<string>();

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

        private bool IsOptimalShootingConditions(SunPositionDto sunPosition, HourlyForecastDto weather)
        {
            // Optimal conditions: good light, manageable weather
            var hasGoodLight = sunPosition.Elevation > 5; // Above horizon
            var weatherIsManageable = weather.ProbabilityOfPrecipitation < 0.7 && weather.WindSpeed < 20;
            var isGoldenHour = sunPosition.Elevation < 15 && sunPosition.Elevation > 0;

            return hasGoodLight && weatherIsManageable || isGoldenHour;
        }

        private string GetCardinalDirection(double degrees)
        {
            var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            var index = (int)Math.Round(degrees / 22.5) % 16;
            return directions[index];
        }

        // Rest of the existing methods remain the same...
        private async Task LoadLocationTimezoneAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                var weather = await _unitOfWork.Weather.GetByLocationIdAsync(SelectedLocation.Id, _cancellationTokenSource.Token);

                if (weather != null && !string.IsNullOrEmpty(weather.Timezone))
                {
                    try
                    {
                        LocationTimeZone = _timezoneService.GetTimeZoneInfo(weather.Timezone);
                    }
                    catch
                    {
                        await DetermineTimezoneFromCoordinatesAsync();
                    }
                }
                else
                {
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
                SunriseUtc = sunTimes.Sunrise;
                SunsetUtc = sunTimes.Sunset;
                SolarNoonUtc = sunTimes.SolarNoon;
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

        private async Task GenerateBasicWeatherImpactAsync(WeatherForecastDto weatherForecast)
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
                WeatherDataStatus = "Weather impact analysis complete";
            }
            catch (Exception ex)
            {
                OnSystemError($"Error generating weather impact analysis: {ex.Message}");
            }
        }

        private async Task GenerateBasicPredictionsAsync()
        {
            try
            {
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
                    PredictionWindowHours = 120 // 5 days = 120 hours
                };

                var predictions = await _predictiveLightService.GenerateHourlyPredictionsAsync(predictionRequest, _cancellationTokenSource.Token);
                var nowUtc = DateTime.UtcNow;

                _hourlyPredictions.Clear();
                foreach (var prediction in predictions)
                {
                    var predictionUtc = prediction.DateTime.Kind == DateTimeKind.Utc
                        ? prediction.DateTime
                        : DateTime.SpecifyKind(prediction.DateTime, DateTimeKind.Utc);

                    if (predictionUtc > nowUtc.AddMinutes(30))
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
                OnSystemError($"Error generating basic predictions: {ex.Message}");
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
            double baseConfidence = 0.90;

            var hoursFromNow = (prediction.DateTime - DateTime.Now).TotalHours;
            var daysFromNow = hoursFromNow / 24.0;
            baseConfidence -= (daysFromNow * 0.05);

            if (weather.CurrentConditions != null)
            {
                baseConfidence -= (weather.CurrentConditions.CloudCover * 0.3);

                if (weather.CurrentConditions.Precipitation > 0)
                {
                    baseConfidence -= 0.4;
                }
            }

            var hour = prediction.DateTime.Hour;
            if (hour <= 6 || hour >= 18)
            {
                baseConfidence -= 0.2;
            }

            if (prediction.SunPosition?.IsAboveHorizon == false)
            {
                var dayOfMonth = prediction.DateTime.Day;
                var approximateMoonPhase = Math.Abs(dayOfMonth - 15) / 15.0;

                if (approximateMoonPhase < 0.2)
                    baseConfidence = Math.Max(baseConfidence, 0.7);
                else if (approximateMoonPhase > 0.8)
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

                // Enhanced: Add weather context if available
                if (!string.IsNullOrEmpty(currentPrediction.WeatherDescription))
                {
                    CurrentPredictionText += $" ({currentPrediction.WeatherDescription})";
                }
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