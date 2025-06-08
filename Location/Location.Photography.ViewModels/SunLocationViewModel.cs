// Location.Photography.ViewModels/SunLocationViewModel.cs
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Application.Weather.Queries.GetHourlyForecast;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using Location.Photography.ViewModels.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.ViewModels
{
    public partial class SunLocationViewModel : ViewModelBase, INotifyPropertyChanged
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly ITimezoneService _timezoneService;
        private readonly IExposureCalculatorService _exposureCalculatorService;

        private readonly ILogger<SunLocationViewModel>? _logger;

        // Compass and smoothing fields
        private Timer? _compassTimer;
        private double _currentCompassHeading = 0;
        private double _targetSunDirection = 0;
        private double _currentSunDirection = 0;
        private double _sunAzimuth = 0;
        private bool _isCompassActive = false;
        private readonly object _smoothingLock = new object();

        // Smoothing parameters
        private const double SMOOTHING_FACTOR = 0.15; // Lower = smoother, higher = more responsive
        private const int COMPASS_UPDATE_INTERVAL_MS = 100; // 10 FPS for smooth movement
        private const double MIN_MOVEMENT_THRESHOLD = 0.5; // Minimum degrees to move arrow

        // Observable properties
        private ObservableCollection<LocationViewModel> _locations = new();
        private LocationViewModel? _selectedLocation;
        private DateTime _selectedDate = DateTime.Today;
        private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;
        private DateTime _selectedDateTime => _selectedDate.Date.Add(_selectedTime);
        private double _sunDirection = 0;
        private double _sunElevation = 0;
        private double _deviceTilt = 0;
        private bool _elevationMatched = false;
        private string _errorMessage = string.Empty;
        private bool _isBusy = false;
        private string _weatherSummary = "Loading weather...";
        private double _lightReduction = 0;
        private double _colorTemperature = 5500;
        private string _lightQuality = "Unknown";
        private double _currentEV = 0;
        private double _nextHourEV = 0;
        private string _recommendedSettings = "f/8 @ 1/125 ISO 100";
        private string _lightQualityDescription = "Calculating light conditions...";
        private string _recommendations = "Loading recommendations...";
        private string _nextOptimalTime = "Calculating optimal times...";
        private ObservableCollection<TimelineEventViewModel> _timelineEvents = new();
        private string _timeFormat = "HH:mm";
        private TimeZoneInfo _deviceTimeZone = TimeZoneInfo.Local;
        private TimeZoneInfo _locationTimeZone = TimeZoneInfo.Local;
        #endregion

        #region Properties
        public ObservableCollection<LocationViewModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }

        public LocationViewModel? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (SetProperty(ref _selectedLocation, value))
                {
                    OnSelectedLocationChanged();
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    _ = UpdateSunPositionAsync();
                }
            }
        }

        public TimeSpan SelectedTime
        {
            get => _selectedTime;
            set
            {
                if (SetProperty(ref _selectedTime, value))
                {
                    _ = UpdateSunPositionAsync();
                }
            }
        }

        public double SunDirection
        {
            get => _sunDirection;
            set => SetProperty(ref _sunDirection, value);
        }

        public double SunElevation
        {
            get => _sunElevation;
            set
            {
                if (SetProperty(ref _sunElevation, value))
                {
                    CheckElevationAlignment();
                }
            }
        }

        public double DeviceTilt
        {
            get => _deviceTilt;
            set
            {
                if (SetProperty(ref _deviceTilt, value))
                {
                    CheckElevationAlignment();
                }
            }
        }

        public bool ElevationMatched
        {
            get => _elevationMatched;
            set => SetProperty(ref _elevationMatched, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string WeatherSummary
        {
            get => _weatherSummary;
            set => SetProperty(ref _weatherSummary, value);
        }

        public double LightReduction
        {
            get => _lightReduction;
            set => SetProperty(ref _lightReduction, value);
        }

        public double ColorTemperature
        {
            get => _colorTemperature;
            set => SetProperty(ref _colorTemperature, value);
        }

        public string LightQuality
        {
            get => _lightQuality;
            set => SetProperty(ref _lightQuality, value);
        }

        public double CurrentEV
        {
            get => _currentEV;
            set => SetProperty(ref _currentEV, value);
        }

        public double NextHourEV
        {
            get => _nextHourEV;
            set => SetProperty(ref _nextHourEV, value);
        }

        public string RecommendedSettings
        {
            get => _recommendedSettings;
            set => SetProperty(ref _recommendedSettings, value);
        }

        public string LightQualityDescription
        {
            get => _lightQualityDescription;
            set => SetProperty(ref _lightQualityDescription, value);
        }

        public string Recommendations
        {
            get => _recommendations;
            set => SetProperty(ref _recommendations, value);
        }

        public string NextOptimalTime
        {
            get => _nextOptimalTime;
            set => SetProperty(ref _nextOptimalTime, value);
        }

        public ObservableCollection<TimelineEventViewModel> TimelineEvents
        {
            get => _timelineEvents;
            set => SetProperty(ref _timelineEvents, value);
        }

        public bool BeginMonitoring { get; set; }
        #endregion

        #region Events
        public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Constructor
        public SunLocationViewModel(
   IMediator mediator,
   ISunCalculatorService sunCalculatorService,
   IErrorDisplayService errorDisplayService,
   ITimezoneService timezoneService, IExposureCalculatorService exposureCalculatorService,
   ILogger<SunLocationViewModel>? logger = null)
   : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));
            _logger = logger;
            _exposureCalculatorService = exposureCalculatorService??throw new ArgumentNullException(nameof(exposureCalculatorService));
            InitializeTimelineEvents();
            LoadUserSettingsAsync();
        }
        #endregion

        #region Compass and Sun Direction Methods


        private async Task<string> GetNormalizedCameraSettingsAsync(double ev, SunPositionDto sunPosition, WeatherImpactFactor weatherImpact)
        {
            try
            {
                // Calculate base settings using existing logic
                var baseAperture = Math.Max(1.4, Math.Min(16, ev / 2));
                var baseShutterSpeed = CalculateShutterSpeedOptimized(ev, baseAperture);
                var baseISO = CalculateISOOptimized(ev, baseAperture, baseShutterSpeed);

                // Get standardized lists
                var allApertures = Apetures.Thirds.Select(a => Convert.ToDouble(a.Replace("f/", ""))).ToList();
                var allShutterSpeeds = ShutterSpeeds.Thirds.Select(s => ParseShutterSpeed(s)).ToList();
                var allISOs = ISOs.Thirds.Select(i => Convert.ToInt32(i)).ToList();

                // Find closest standardized values
                var closestAperture = allApertures.OrderBy(x => Math.Abs(x - baseAperture)).First();
                var closestShutter = allShutterSpeeds.OrderBy(x => Math.Abs(x - baseShutterSpeed)).First();
                var closestISO = allISOs.OrderBy(x => Math.Abs(x - baseISO)).First();

                // Create exposure triangle DTO for normalization
                var exposureDto = new ExposureTriangleDto
                {
                    Aperture = $"f/{baseAperture:F1}",
                    Iso = baseISO.ToString(),
                    ShutterSpeed = FormatShutterSpeed(baseShutterSpeed)
                };

                // Use ExposureCalculatorService to normalize the triangle
                var normalizedResult = await _exposureCalculatorService.CalculateIsoAsync(
                    exposureDto,
                    FormatShutterSpeed(closestShutter),
                    $"f/{closestAperture:F1}",
                    ExposureIncrements.Third);

                if (normalizedResult.IsSuccess && normalizedResult.Data != null)
                {
                    return $"{normalizedResult.Data.Aperture}, {normalizedResult.Data.ShutterSpeed}, ISO {normalizedResult.Data.Iso}";
                }

                // Fallback to standardized values without service normalization
                return $"f/{closestAperture:F1}, {FormatShutterSpeed(closestShutter)}, ISO {closestISO}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error normalizing camera settings");
                return $"f/{8:F1}, 1/125, ISO 400"; // Safe fallback
            }
        }

        private double ParseShutterSpeed(string shutterSpeed)
        {
            try
            {
                if (shutterSpeed.Contains("/"))
                {
                    var parts = shutterSpeed.Replace("1/", "").Split('/');
                    if (parts.Length == 1 && double.TryParse(parts[0], out var denominator))
                    {
                        return 1.0 / denominator;
                    }
                }
                else if (shutterSpeed.Contains("\""))
                {
                    var seconds = shutterSpeed.Replace("\"", "");
                    if (double.TryParse(seconds, out var sec))
                    {
                        return sec;
                    }
                }
                else if (double.TryParse(shutterSpeed, out var value))
                {
                    return value;
                }

                return 1.0 / 125.0; // Default fallback
            }
            catch
            {
                return 1.0 / 125.0;
            }
        }

        private string FormatShutterSpeed(double seconds)
        {
            if (seconds >= 1)
                return $"{seconds:F0}\"";
            else
                return $"1/{Math.Round(1.0 / seconds):F0}";
        }


        public void StartAccelerometer()
        {
            try
            {
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                    _logger?.LogDebug("Accelerometer started successfully");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start accelerometer");
                OnSystemError($"Failed to start accelerometer: {ex.Message}");
            }
        }

        public void StopAccelerometer()
        {
            try
            {
                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
                    Accelerometer.Default.Stop();
                    _logger?.LogDebug("Accelerometer stopped successfully");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping accelerometer");
            }
        }

        private void OnAccelerometerReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            try
            {
                // Calculate device tilt from accelerometer data
                var reading = e.Reading;
                var tiltAngle = Math.Atan2(reading.Acceleration.Y, reading.Acceleration.Z) * 180.0 / Math.PI;

                // Normalize to 0-90 degrees (phone held vertically to horizontally)
                tiltAngle = Math.Abs(tiltAngle);
                if (tiltAngle > 90) tiltAngle = 180 - tiltAngle;

                DeviceTilt = tiltAngle;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing accelerometer reading");
            }
        }

        private void CheckElevationAlignment()
        {
            var difference = Math.Abs(DeviceTilt - SunElevation);
            var wasMatched = ElevationMatched;

            ElevationMatched = difference <= 5.0;

            // Trigger haptic feedback when alignment is achieved (but wasn't before)
            if (ElevationMatched && !wasMatched)
            {
                TriggerElevationMatchVibration();
            }
        }

        private void TriggerElevationMatchVibration()
        {
            try
            {
                // Two quick vibrations to indicate elevation match
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                Task.Delay(100).ContinueWith(_ =>
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error triggering vibration");
            }
        }
        private DateTime ConvertLocalTimeToUtc(DateTime localDateTime)
        {
            try
            {
                // Ensure the DateTime has the correct DateTimeKind
                if (localDateTime.Kind == DateTimeKind.Utc)
                {
                    return localDateTime; // Already UTC
                }

                // If unspecified, assume it's local time from user input
                if (localDateTime.Kind == DateTimeKind.Unspecified)
                {
                    localDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Local);
                }

                // Convert local time to UTC for sun calculations
                return localDateTime.ToUniversalTime();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error converting local time to UTC");
                // Fallback - treat as UTC if conversion fails
                return DateTime.SpecifyKind(localDateTime, DateTimeKind.Utc);
            }
        }
        public void StartSensors()
        {
            try
            {
                if (!_isCompassActive && Compass.IsSupported)
                {
                    Compass.ReadingChanged += OnCompassReadingChanged;
                    Compass.Start(SensorSpeed.UI);
                    _isCompassActive = true;

                    // Start smoothing timer
                    _compassTimer = new Timer(UpdateSmoothSunDirection, null, 0, COMPASS_UPDATE_INTERVAL_MS);

                    _logger?.LogDebug("Compass service started successfully");
                }

                // Start accelerometer for tilt detection
                StartAccelerometer();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start sensors");
                OnSystemError($"Failed to start sensors: {ex.Message}");
            }
        }

        public void StopSensors()
        {
            try
            {
                if (_isCompassActive)
                {
                    Compass.ReadingChanged -= OnCompassReadingChanged;
                    Compass.Stop();
                    _isCompassActive = false;
                }

                _compassTimer?.Dispose();
                _compassTimer = null;

                // Stop accelerometer
                StopAccelerometer();

                _logger?.LogDebug("Sensors stopped successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping sensors");
            }
        }

        

        private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            try
            {
                lock (_smoothingLock)
                {
                    _currentCompassHeading = e.Reading.HeadingMagneticNorth;
                    CalculateTargetSunDirection();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing compass reading");
            }
        }

        private void CalculateTargetSunDirection()
        {
            // Calculate relative sun direction: sun_azimuth - compass_heading
            // This makes the arrow always point toward the sun regardless of device orientation
            var relativeSunDirection = _sunAzimuth - _currentCompassHeading;

            // Normalize to 0-360 degrees
            while (relativeSunDirection < 0)
                relativeSunDirection += 360;
            while (relativeSunDirection >= 360)
                relativeSunDirection -= 360;

            _targetSunDirection = relativeSunDirection;
        }

        private void UpdateSmoothSunDirection(object? state)
        {
            try
            {
                lock (_smoothingLock)
                {
                    var difference = CalculateShortestAngleDifference(_currentSunDirection, _targetSunDirection);

                    // Only update if movement is significant enough
                    if (Math.Abs(difference) > MIN_MOVEMENT_THRESHOLD)
                    {
                        // Apply smoothing using exponential moving average
                        var newDirection = _currentSunDirection + (difference * SMOOTHING_FACTOR);

                        // Normalize to 0-360 degrees
                        while (newDirection < 0)
                            newDirection += 360;
                        while (newDirection >= 360)
                            newDirection -= 360;

                        _currentSunDirection = newDirection;

                        // Update UI on main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            SunDirection = _currentSunDirection;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating smooth sun direction");
            }
        }

        private static double CalculateShortestAngleDifference(double current, double target)
        {
            var difference = target - current;

            // Handle wrap-around for shortest path
            if (difference > 180)
                difference -= 360;
            else if (difference < -180)
                difference += 360;

            return difference;
        }
        #endregion

        #region Sun Position Methods
        [RelayCommand]
        public async Task UpdateSunPositionAsync()
        {
            if (SelectedLocation == null)
            {
                SetValidationError("Please select a location");
                return;
            }

            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    ClearErrors();

                    // Convert user's local time to UTC for accurate sun calculations
                    var utcDateTime = ConvertLocalTimeToUtc(_selectedDateTime);

                    var query = new GetCurrentSunPositionQuery
                    {
                        Latitude = SelectedLocation.Lattitude,
                        Longitude = SelectedLocation.Longitude,
                        DateTime = utcDateTime
                    };

                    var result = await _mediator.Send(query);

                    if (result.IsSuccess && result.Data != null)
                    {
                        // Store sun azimuth for compass calculations
                        lock (_smoothingLock)
                        {
                            _sunAzimuth = result.Data.Azimuth;
                            CalculateTargetSunDirection();
                        }

                        SunElevation = result.Data.Elevation;

                        // Update enhanced calculations
                        await UpdateEnhancedCalculationsAsync(result.Data);
                        await UpdateSunTimesAsync();
                        UpdateTimelineEvents();
                    }
                    else
                    {
                        OnSystemError(result.ErrorMessage ?? "Failed to calculate sun position");
                    }
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error calculating sun position: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
        }

        private async Task UpdateEnhancedCalculationsAsync(SunPositionDto sunPosition)
        {
            try
            {
                // Calculate enhanced light conditions using integrated algorithms
                var baseEV = CalculateBaseEVFromSunPositionOptimized(sunPosition);
                var weatherImpact = await GetWeatherImpactAsync();
                var adjustedEV = baseEV * weatherImpact.OverallLightReductionFactor;

                CurrentEV = Math.Round(adjustedEV, 1);

                // Calculate next hour EV
                var nextHourDateTime = _selectedDateTime.AddHours(1);
                var nextHourPosition = await GetSunPositionForTimeAsync(nextHourDateTime);
                if (nextHourPosition != null)
                {
                    var nextBaseEV = CalculateBaseEVFromSunPositionOptimized(nextHourPosition);
                    NextHourEV = Math.Round(nextBaseEV * weatherImpact.OverallLightReductionFactor, 1);
                }

                // Calculate normalized exposure settings
                var normalizedSettings = await GetNormalizedCameraSettingsAsync(adjustedEV, sunPosition, weatherImpact);
                RecommendedSettings = normalizedSettings;

                // Calculate light quality
                var lightCharacteristics = DetermineLightQualityOptimized(sunPosition, weatherImpact);
                LightQuality = lightCharacteristics.OptimalFor;
                ColorTemperature = lightCharacteristics.ColorTemperature;
                LightReduction = 1.0 - weatherImpact.OverallLightReductionFactor;

                // Generate recommendations
                Recommendations = GenerateRecommendationsOptimized(sunPosition, weatherImpact, lightCharacteristics);

                // Update light quality description
                LightQualityDescription = GenerateLightQualityDescriptionOptimized(sunPosition, lightCharacteristics);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating enhanced calculations");
            }
        }

        private async Task<SunPositionDto?> GetSunPositionForTimeAsync(DateTime dateTime)
        {
            if (SelectedLocation == null) return null;

            try
            {
                var query = new GetCurrentSunPositionQuery
                {
                    Latitude = SelectedLocation.Lattitude,
                    Longitude = SelectedLocation.Longitude,
                    DateTime = dateTime
                };

                var result = await _mediator.Send(query);
                return result.IsSuccess ? result.Data : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<WeatherImpactFactor> GetWeatherImpactAsync()
        {
            try
            {
                if (SelectedLocation == null)
                    return CreateDefaultWeatherImpact();

                // Try to get hourly weather data
                var hourlyQuery = new GetHourlyForecastQuery
                {
                    LocationId = SelectedLocation.Id,
                    StartTime = _selectedDateTime.AddHours(-1),
                    EndTime = _selectedDateTime.AddHours(1)
                };

                var hourlyResult = await _mediator.Send(hourlyQuery);

                if (hourlyResult.IsSuccess && hourlyResult.Data?.HourlyForecasts?.Any() == true)
                {
                    var closestForecast = hourlyResult.Data.HourlyForecasts
                        .OrderBy(h => Math.Abs((h.DateTime - _selectedDateTime).TotalMinutes))
                        .First();

                    return CalculateHourlyWeatherImpactOptimized(closestForecast);
                }

                return CreateDefaultWeatherImpact();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting weather impact");
                return CreateDefaultWeatherImpact();
            }
        }

        private async Task UpdateSunTimesAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                await LoadLocationTimezoneAsync();

                var query = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Lattitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = SelectedDate
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Data != null)
                {
                    var sunTimes = result.Data;

                    // Update weather summary with DEVICE timezone times (not location timezone)
                    var sunriseLocal = FormatTimeForTimezoneOptimized(sunTimes.Sunrise, _deviceTimeZone);
                    var sunsetLocal = FormatTimeForTimezoneOptimized(sunTimes.Sunset, _deviceTimeZone);
                    WeatherSummary = $"Sunrise: {sunriseLocal} | Sunset: {sunsetLocal}";

                    // Find next optimal time
                    var nextOptimal = FindNextOptimalTime(sunTimes);
                    NextOptimalTime = nextOptimal ?? "No optimal times in next 24 hours";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating sun times");
            }
        }

        private string? FindNextOptimalTime(SunTimesDto sunTimes)
        {
            var now = DateTime.Now;
            var optimalTimes = new List<(DateTime time, string description)>
           {
               (sunTimes.GoldenHourMorningStart, "Golden Hour Morning"),
               (sunTimes.GoldenHourEveningStart, "Golden Hour Evening"),
               (sunTimes.CivilDawn, "Blue Hour Morning"),
               (sunTimes.Sunset, "Blue Hour Evening")
           };

            var nextTime = optimalTimes
                .Where(t => t.time > now)
                .OrderBy(t => t.time)
                .FirstOrDefault();

            if (nextTime.time != default)
            {
                var timeUntil = nextTime.time - now;
                return $"Next: {nextTime.description} in {timeUntil.Hours}h {timeUntil.Minutes}m";
            }

            return null;
        }


        #endregion

        #region Enhanced Calculation Methods (Integrated from EnhancedSunCalculatorViewModel)
        private double CalculateBaseEVFromSunPositionOptimized(SunPositionDto sunPosition)
        {
            if (sunPosition.Elevation <= 0) return 4;
            if (sunPosition.Elevation < 10) return 8;
            if (sunPosition.Elevation < 30) return 12;
            return 15;
        }

        private WeatherImpactFactor CalculateHourlyWeatherImpactOptimized(Location.Core.Application.Weather.DTOs.HourlyForecastDto hourlyForecast)
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

        private WeatherImpactFactor CreateDefaultWeatherImpact()
        {
            return new WeatherImpactFactor
            {
                CloudCoverReduction = 0.1,
                PrecipitationReduction = 0,
                HumidityReduction = 0.05,
                VisibilityReduction = 0,
                OverallLightReductionFactor = 0.85,
                ConfidenceImpact = 0.8
            };
        }

        private ExposureTriangle CalculateExposureSettingsOptimized(double ev)
        {
            var aperture = Math.Max(1.4, Math.Min(16, ev / 2));
            var shutterSpeed = CalculateShutterSpeedOptimized(ev, aperture);
            var iso = CalculateISOOptimized(ev, aperture, shutterSpeed);

            return new ExposureTriangle
            {
                Aperture = $"f/{aperture:F1}",
                ShutterSpeed = FormatShutterSpeedOptimized(shutterSpeed),
                ISO = $"ISO {iso}"
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

        private LightCharacteristics DetermineLightQualityOptimized(SunPositionDto sunPosition, WeatherImpactFactor weatherImpact)
        {
            var colorTemp = CalculateColorTemperatureOptimized(sunPosition.Elevation, weatherImpact.CloudCoverReduction * 100);
            var optimalFor = DetermineOptimalPhotographyTypeOptimized(sunPosition, weatherImpact);

            return new LightCharacteristics
            {
                ColorTemperature = colorTemp,
                OptimalFor = optimalFor,
                SoftnessFactor = CalculateSoftnessFactor(sunPosition, weatherImpact)
            };
        }

        private double CalculateColorTemperatureOptimized(double elevation, double cloudCover)
        {
            var baseTemp = 5500;
            if (elevation < 10) baseTemp = 3000;
            else if (elevation < 20) baseTemp = 4000;

            var cloudAdjustment = cloudCover * 5;
            return Math.Max(2500, Math.Min(7000, baseTemp + cloudAdjustment));
        }

        private string DetermineOptimalPhotographyTypeOptimized(SunPositionDto sunPosition, WeatherImpactFactor weatherImpact)
        {
            if (weatherImpact.PrecipitationReduction > 0.5) return "Moody/dramatic photography";
            if (sunPosition.Elevation < 10) return "Portraits, golden hour shots";
            if (sunPosition.Elevation > 60 && weatherImpact.CloudCoverReduction > 0.5) return "Even lighting, portraits";
            if (sunPosition.Elevation > 60) return "Landscapes, architecture";
            return "General photography";
        }

        private double CalculateSoftnessFactor(SunPositionDto sunPosition, WeatherImpactFactor weatherImpact)
        {
            var softness = 0.3; // Base hardness

            // Clouds act as giant softbox
            if (weatherImpact.CloudCoverReduction > 0)
            {
                softness += weatherImpact.CloudCoverReduction * 0.6;
            }

            // Lower sun angle = softer light due to atmospheric scattering
            if (sunPosition.Elevation < 30)
            {
                softness += (30 - sunPosition.Elevation) / 30.0 * 0.3;
            }

            return Math.Max(0.1, Math.Min(1.0, softness));
        }

        private string GenerateRecommendationsOptimized(SunPositionDto sunPosition, WeatherImpactFactor weatherImpact, LightCharacteristics lightCharacteristics)
        {
            var recommendations = new List<string>();

            if (weatherImpact.PrecipitationReduction > 0.3)
                recommendations.Add("Bring weather protection for gear");

            if (sunPosition.Elevation < 15 && weatherImpact.CloudCoverReduction < 0.3)
                recommendations.Add("Perfect golden hour conditions");

            if (weatherImpact.CloudCoverReduction > 0.7)
                recommendations.Add("Great for even, soft lighting");

            if (sunPosition.Elevation > 60)
                recommendations.Add("Watch for harsh shadows, consider diffuser");

            if (lightCharacteristics.ColorTemperature < 4000)
                recommendations.Add("Warm light - great for portraits");

            return recommendations.Any() ? string.Join(", ", recommendations) : "Standard photography conditions";
        }

        private string GenerateLightQualityDescriptionOptimized(SunPositionDto sunPosition, LightCharacteristics lightCharacteristics)
        {
            if (sunPosition.Elevation <= 0)
                return "Night - Use artificial lighting or long exposures";

            if (sunPosition.Elevation < 10)
                return $"Golden Hour - {lightCharacteristics.OptimalFor}";

            if (sunPosition.Elevation < 30)
                return $"Good Light - {lightCharacteristics.OptimalFor}";

            return $"Bright Daylight - {lightCharacteristics.OptimalFor}";
        }

        private string FormatTimeForTimezoneOptimized(DateTime utcTime, TimeZoneInfo timezone)
        {
            try
            {
                if (timezone == null)
                {
                    return utcTime.ToString(_timeFormat);
                }

                // Ensure we're working with UTC time
                if (utcTime.Kind != DateTimeKind.Utc)
                {
                    if (utcTime.Kind == DateTimeKind.Local)
                    {
                        utcTime = utcTime.ToUniversalTime();
                    }
                    else
                    {
                        utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
                    }
                }

                // Convert from UTC to target timezone
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timezone);
                return localTime.ToString(_timeFormat);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error formatting time");
                return utcTime.ToString(_timeFormat);
            }
        }
        #endregion

        #region Timeline Methods
        private void InitializeTimelineEvents()
        {
            TimelineEvents = new ObservableCollection<TimelineEventViewModel>();
        }

        private void UpdateTimelineEvents()
        {
            if (SelectedLocation == null) return;

            try
            {
                TimelineEvents.Clear();
                var now = DateTime.Now;

                // Add major sun events for next 24 hours
                var events = new List<TimelineEventViewModel>
               {
                   new TimelineEventViewModel
                   {
                       EventName = "Sunrise",
                       EventIcon = "🌅",
                       EventTime = now.Date.AddDays(1).AddHours(6.5)
                   },
                   new TimelineEventViewModel
                   {
                       EventName = "Golden Hour",
                       EventIcon = "🌇",
                       EventTime = now.Date.AddDays(1).AddHours(7.5)
                   },
                   new TimelineEventViewModel
                   {
                       EventName = "Noon",
                       EventIcon = "☀️",
                       EventTime = now.Date.AddDays(1).AddHours(12)
                   },
                   new TimelineEventViewModel
                   {
                       EventName = "Golden Hour",
                       EventIcon = "🌇",
                       EventTime = now.Date.AddDays(1).AddHours(17)
                   },
                   new TimelineEventViewModel
                   {
                       EventName = "Sunset",
                       EventIcon = "🌆",
                       EventTime = now.Date.AddDays(1).AddHours(19)
                   }
               };

                foreach (var evt in events)
                {
                    TimelineEvents.Add(evt);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating timeline events");
            }
        }
        #endregion

        #region Settings and Timezone Methods
        private async Task LoadUserSettingsAsync()
        {
            try
            {
                var timeFormatResult = await _mediator.Send(new GetSettingByKeyQuery { Key = "TimeFormat" });
                if (timeFormatResult.IsSuccess && timeFormatResult.Data != null)
                {
                    _timeFormat = timeFormatResult.Data.Value;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading user settings");
            }
        }

        private async Task LoadLocationTimezoneAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                var timezoneResult = await _timezoneService.GetTimezoneFromCoordinatesAsync(
                    SelectedLocation.Lattitude,
                    SelectedLocation.Longitude);

                if (timezoneResult.IsSuccess)
                {
                    _locationTimeZone = _timezoneService.GetTimeZoneInfo(timezoneResult.Data);
                }
                else
                {
                    _locationTimeZone = TimeZoneInfo.Local;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading location timezone");
                _locationTimeZone = TimeZoneInfo.Local;
            }
        }
        #endregion

        #region Helper Methods
        private void OnSelectedLocationChanged()
        {
            if (SelectedLocation != null)
            {
                _ = UpdateSunPositionAsync();
            }
        }



        private void OnSystemError(string message)
        {
            ErrorMessage = message;
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public async Task RetryLastCommandAsync()
        {
            if (LastCommand?.CanExecute(LastCommandParameter) == true)
            {
                await LastCommand.ExecuteAsync(LastCommandParameter);
            }
        }
        #endregion

        #region IDisposable
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopSensors();
                _compassTimer?.Dispose();
            }
            base.Dispose();
        }
        #endregion
    }

    #region Supporting Classes
   

   

    // Weather impact calculation helper
   
    #endregion
}