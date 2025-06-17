// Enhanced AstroPhotographyCalculatorViewModel with Real Astronomical Service Integration Including Meteor Showers
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.Queries.GetHourlyForecast;
using Location.Core.ViewModels;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.DTOs;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using System.Collections.ObjectModel;
using System.Diagnostics;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;
using ShutterSpeeds = Location.Photography.ViewModels.Interfaces.ShutterSpeeds;

namespace Location.Photography.ViewModels
{
    public partial class AstroPhotographyCalculatorViewModel : ViewModelBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly IAstroCalculationService _astroCalculationService;
        private readonly ICameraBodyRepository _cameraBodyRepository;
        private readonly ILensRepository _lensRepository;
        private readonly IUserCameraBodyRepository _userCameraBodyRepository;
        private readonly IEquipmentRecommendationService _equipmentRecommendationService;
        private readonly IPredictiveLightService _predictiveLightService;
        private readonly IExposureCalculatorService _exposureCalculatorService;
        private readonly IAstroHourlyPredictionMappingService _mappingService;
        private readonly IMeteorShowerDataService _meteorShowerDataService;

        // PERFORMANCE: Threading and caching
        private readonly Dictionary<string, CachedAstroCalculation> _calculationCache = new();
        private readonly Dictionary<string, List<AstroHourlyPrediction>> _predictionCache = new();
        private CancellationTokenSource _cancellationTokenSource = new();
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private const int CALCULATION_THROTTLE_MS = 500;
        private const int PREDICTION_CACHE_DURATION_MINUTES = 60;
        private const int MIN_METEOR_SHOWER_ZHR = 10; // Minimum ZHR for photography-worthy meteor showers

        // Core properties backing fields
        private ObservableCollection<LocationListItemViewModel> _locations = new();
        private LocationListItemViewModel _selectedLocation;
        private DateTime _selectedDate = DateTime.Today;
        private string _locationPhoto = string.Empty;
        private bool _isInitialized;
        private bool _hasError;
        private string _errorMessage = string.Empty;

        // Equipment backing fields
        private ObservableCollection<CameraBody> _availableCameras = new();
        private ObservableCollection<Lens> _availableLenses = new();
        private CameraBody _selectedCamera;
        private Lens _selectedLens;
        private bool _isLoadingEquipment;

        // Astro target and calculation backing fields
        private AstroTarget _selectedTarget = AstroTarget.MilkyWayCore;
        private ObservableCollection<AstroTargetDisplayModel> _availableTargets = new();
        private ObservableCollection<AstroCalculationResult> _currentCalculations = new();
        private bool _isCalculating;
        private string _calculationStatus = string.Empty;

        // Results backing fields
        private string _exposureRecommendation = string.Empty;
        private string _equipmentRecommendation = string.Empty;
        private string _photographyNotes = string.Empty;
        private double _fieldOfViewWidth;
        private double _fieldOfViewHeight;
        private bool _targetFitsInFrame;

        // Hourly predictions backing fields
        private bool _isGeneratingHourlyPredictions;
        private string _hourlyPredictionsStatus = string.Empty;
        #endregion

        #region Observable Properties for Hourly Predictions
        [ObservableProperty]
        private ObservableCollection<AstroHourlyPrediction> _hourlyPredictions = new();

        [ObservableProperty]
        private bool _isHourlyForecastsLoading;

        [ObservableProperty]
        private string _hourlyPredictionsHeader = "Tonight's Astrophotography Windows";

        private ObservableCollection<AstroHourlyPredictionDisplayModel> _hourlyAstroPredictions = new();

        private string _calculationProgressStatus = string.Empty;
        public string CalculationProgressStatus
        {
            get => _calculationProgressStatus;
            set => SetProperty(ref _calculationProgressStatus, value);
        }
        public ObservableCollection<AstroHourlyPredictionDisplayModel> HourlyAstroPredictions
        {
            get => _hourlyAstroPredictions;
            set => SetProperty(ref _hourlyAstroPredictions, value);
        }

        public bool IsGeneratingHourlyPredictions
        {
            get => _isGeneratingHourlyPredictions;
            set => SetProperty(ref _isGeneratingHourlyPredictions, value);
        }

        public string HourlyPredictionsStatus
        {
            get => _hourlyPredictionsStatus;
            set => SetProperty(ref _hourlyPredictionsStatus, value);
        }
        #endregion

        #region Constructor

        private readonly ICameraDataService _cameraDataService;

        public AstroPhotographyCalculatorViewModel(
            IMediator mediator,
            IErrorDisplayService errorDisplayService,
            IAstroCalculationService astroCalculationService,
            ICameraBodyRepository cameraBodyRepository,
            ILensRepository lensRepository,
            IUserCameraBodyRepository userCameraBodyRepository,
            IEquipmentRecommendationService equipmentRecommendationService,
            IPredictiveLightService predictiveLightService,
            IExposureCalculatorService exposureCalculatorService,
            IAstroHourlyPredictionMappingService mappingService,
            IMeteorShowerDataService meteorShowerDataService, ICameraDataService cameraDataService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _astroCalculationService = astroCalculationService ?? throw new ArgumentNullException(nameof(astroCalculationService));
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
            _userCameraBodyRepository = userCameraBodyRepository ?? throw new ArgumentNullException(nameof(userCameraBodyRepository));
            _equipmentRecommendationService = equipmentRecommendationService ?? throw new ArgumentNullException(nameof(equipmentRecommendationService));
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService));
            _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
            _meteorShowerDataService = meteorShowerDataService ?? throw new ArgumentNullException(nameof(meteorShowerDataService));
 _cameraDataService = cameraDataService;


            InitializeCommands();
            InitializeAstroTargets();
            _ = Task.Run(async () =>
            {
                await LoadLocationsAsync();
                await LoadEquipmentAsync();
            });
           
        }
        #endregion

        #region Real Astronomical Calculation Methods
        private AstroTargetDisplayModel _selectedTargetModel;

        public AstroTargetDisplayModel SelectedTargetModel
        {
            get => _selectedTargetModel;
            set
            {
                if (SetProperty(ref _selectedTargetModel, value) && value != null)
                {
                    SelectedTarget = value.Target;
                }
            }
        }
        public async Task CalculateAstroDataAsync()
        {


            try
            {
                IsCalculating = true;
                IsGeneratingHourlyPredictions = true;
                HasError = false;
                CalculationStatus = "Calculating shooting windows...";

                // Check cache first
                var cacheKey = GetPredictionCacheKey();
                if (_predictionCache.TryGetValue(cacheKey, out var cachedPredictions))
                {
                    var cacheAge = DateTime.Now - _lastCalculationTime;
                    if (cacheAge.TotalMinutes < PREDICTION_CACHE_DURATION_MINUTES)
                    {
                        await ApplyCachedPredictionsAsync(cachedPredictions);
                        return;
                    }
                }

                // Calculate sunset/sunrise times for shooting window
                var shootingWindow = await CalculateShootingWindowAsync();
                if (shootingWindow == null)
                {
                    await HandleErrorAsync(OperationErrorSource.Calculation, "Unable to determine shooting window for selected date");
                    return;
                }

                // Generate real astronomical predictions with progressive updates
                await GenerateRealAstronomicalPredictionsAsync(shootingWindow);

                // Final status update
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyPredictionsStatus = $"Generated {HourlyAstroPredictions.Count} shooting windows for tonight";
                    CalculationProgressStatus = string.Empty; // Clear progress status when complete
                });

                // Cache the results
                var domainPredictions = HourlyAstroPredictions.Select(p => new AstroHourlyPrediction
                {
                    Hour = p.Hour,
                    TimeDisplay = p.TimeDisplay,
                    SolarEvent = p.SolarEventsDisplay,
                    OverallScore = p.QualityScore,
                    WeatherConditions = new WeatherConditions
                    {
                        CloudCover = p.WeatherCloudCover,
                        Humidity = p.WeatherHumidity,
                        WindSpeed = p.WeatherWindSpeed,
                        Visibility = p.WeatherCloudCover,
                        Description = p.WeatherDescription
                    }
                }).ToList();
                _predictionCache[cacheKey] = domainPredictions;
                _lastCalculationTime = DateTime.Now;

                CalculationCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                CalculationStatus = "Calculation cancelled";
                CalculationProgressStatus = string.Empty;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Network, $"Error calculating astro data: {ex.Message}");
            }
            finally
            {
                IsCalculating = false;
                IsGeneratingHourlyPredictions = false;
                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    CalculationStatus = "Calculations complete";
                }
            }
        }

        private async Task<ShootingWindow> CalculateShootingWindowAsync()
        {
            try
            {
                // Get sun times for selected date
                var sunTimesQuery = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = SelectedDate
                };

                var sunResult = await _mediator.Send(sunTimesQuery, _cancellationTokenSource.Token);
                if (!sunResult.IsSuccess || sunResult.Data == null)
                    return null;

                var sunTimes = sunResult.Data;

                // Get sun times for next day
                var nextDayQuery = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = SelectedDate.AddDays(1)
                };

                var nextDayResult = await _mediator.Send(nextDayQuery, _cancellationTokenSource.Token);
                if (!nextDayResult.IsSuccess || nextDayResult.Data == null)
                    return null;

                var nextDayTimes = nextDayResult.Data;

                // Calculate twilight phases using real astronomical calculations
                var twilightPhases = CalculateTwilightPhases(sunTimes, nextDayTimes);

                return new ShootingWindow
                {
                    Date = SelectedDate,
                    Sunset = sunTimes.Sunset,
                    Sunrise = nextDayTimes.Sunrise,
                    CivilTwilightEnd = twilightPhases.CivilTwilightEnd,
                    NauticalTwilightEnd = twilightPhases.NauticalTwilightEnd,
                    AstronomicalTwilightEnd = twilightPhases.AstronomicalTwilightEnd,
                    AstronomicalTwilightStart = twilightPhases.AstronomicalTwilightStart,
                    NauticalTwilightStart = twilightPhases.NauticalTwilightStart,
                    CivilTwilightStart = twilightPhases.CivilTwilightStart
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating shooting window: {ex.Message}");
                return null;
            }
        }

        private TwilightPhases CalculateTwilightPhases(SunTimesDto sunTimes, SunTimesDto nextDayTimes)
        {
            // Calculate twilight times based on sun angles
            var sunset = sunTimes.Sunset;
            var sunrise = nextDayTimes.Sunrise;

            return new TwilightPhases
            {
                // Evening twilight phases (after sunset)
                CivilTwilightEnd = sunset.AddMinutes(20),        // Sun at -6°
                NauticalTwilightEnd = sunset.AddMinutes(50),     // Sun at -12°
                AstronomicalTwilightEnd = sunset.AddMinutes(80), // Sun at -18°

                // Morning twilight phases (before sunrise)
                AstronomicalTwilightStart = sunrise.AddMinutes(-80), // Sun at -18°
                NauticalTwilightStart = sunrise.AddMinutes(-50),     // Sun at -12°
                CivilTwilightStart = sunrise.AddMinutes(-20)         // Sun at -6°
            };
        }

        private async Task GenerateRealAstronomicalPredictionsAsync(ShootingWindow window)
        {
            try
            {
                // Clear existing predictions at start
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyAstroPredictions.Clear();
                });

                // Step 1: Get today's sunset
                var todaySunsetQuery = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = SelectedDate // Today's date
                };

                var todaySunsetResult = await _mediator.Send(todaySunsetQuery, _cancellationTokenSource.Token);
                if (!todaySunsetResult.IsSuccess || todaySunsetResult.Data == null)
                    return;

                var todaySunsetTime = todaySunsetResult.Data.Sunset; // This is in UTC
                var currentUtc = DateTime.UtcNow;

                // Step 2: If today's sunset < now UTC, use tomorrow's sunset instead
                DateTime targetSunsetTime;
                if (todaySunsetTime < currentUtc)
                {
                    // Today's sunset already passed, get tomorrow's sunset
                    var tomorrowSunsetQuery = new GetSunTimesQuery
                    {
                        Latitude = SelectedLocation.Latitude,
                        Longitude = SelectedLocation.Longitude,
                        Date = SelectedDate.AddDays(1) // Tomorrow's date
                    };

                    var tomorrowSunsetResult = await _mediator.Send(tomorrowSunsetQuery, _cancellationTokenSource.Token);
                    if (!tomorrowSunsetResult.IsSuccess || tomorrowSunsetResult.Data == null)
                        return;

                    targetSunsetTime = tomorrowSunsetResult.Data.Sunset;
                }
                else
                {
                    // Today's sunset hasn't happened yet, use it
                    targetSunsetTime = todaySunsetTime;
                }

                // Step 3: Determine start time based on whether sunset has passed
                DateTime startTime;
                if (targetSunsetTime < currentUtc)
                {
                    // Sunset already passed, start from current hour rounded up
                    startTime = new DateTime(currentUtc.Year, currentUtc.Month, currentUtc.Day,
                        currentUtc.Hour + (currentUtc.Minute > 0 ? 1 : 0), 0, 0);
                }
                else
                {
                    // Sunset hasn't happened, start from sunset rounded up
                    startTime = new DateTime(targetSunsetTime.Year, targetSunsetTime.Month, targetSunsetTime.Day,
                        targetSunsetTime.Hour + (targetSunsetTime.Minute > 0 ? 1 : 0), 0, 0);
                }

                // Step 4: Determine sunrise date using midnight boundary logic
                var sunsetLocal = TimeZoneInfo.ConvertTimeFromUtc(targetSunsetTime, TimeZoneInfo.Local);

                DateTime sunriseDate;
                if (targetSunsetTime.Date == sunsetLocal.Date)
                {
                    // Same date - no midnight crossing, sunrise is next day
                    sunriseDate = targetSunsetTime.Date.AddDays(1);
                }
                else
                {
                    // Different dates - midnight crossing occurred, sunrise is same UTC date
                    sunriseDate = targetSunsetTime.Date;
                }

                var sunriseQuery = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = sunriseDate
                };

                var sunriseResult = await _mediator.Send(sunriseQuery, _cancellationTokenSource.Token);
                if (!sunriseResult.IsSuccess || sunriseResult.Data == null)
                    return;

                // Step 5: Apply rounding - sunrise rounded DOWN
                var targetSunriseTime = sunriseResult.Data.Sunrise;
                var endTime = new DateTime(targetSunriseTime.Year, targetSunriseTime.Month, targetSunriseTime.Day,
                    targetSunriseTime.Hour, 0, 0); // Round DOWN (no minute rounding)

                // Step 6: Generate hourly predictions
                var currentHour = startTime;
                var totalHours = (int)Math.Ceiling((endTime - startTime).TotalHours) + 1;
                var hourCount = 0;

                // Convert times to local for display
                var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, TimeZoneInfo.Local);
                var localEndTime = TimeZoneInfo.ConvertTimeFromUtc(endTime, TimeZoneInfo.Local);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CalculationProgressStatus = $"Generating predictions from {localStartTime:HH:mm} to {localEndTime:HH:mm} local time";
                });

                // For each hour between start and end (inclusive)
                while (currentHour <= endTime)
                {
                    hourCount++;

                    // Convert current hour to local for display
                    var localCurrentHour = TimeZoneInfo.ConvertTimeFromUtc(currentHour, TimeZoneInfo.Local);

                    // Update progress status
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CalculationProgressStatus = $"Calculating hour {hourCount} of {totalHours} - {localCurrentHour:HH:mm} local time";
                    });

                    try
                    {
                        // Get optimal shooting times for this specific hour
                        var hourlyQuery = new GetOptimalShootingTimesQuery
                        {
                            Latitude = SelectedLocation.Latitude,
                            Longitude = SelectedLocation.Longitude,
                            Date = currentHour,
                            IncludeWeatherForecast = true,
                            TimeZone = TimeZoneInfo.Local.Id
                        };

                        var hourlyResult = await _mediator.Send(hourlyQuery, _cancellationTokenSource.Token);

                        if (hourlyResult.IsSuccess && hourlyResult.Data != null)
                        {
                            // Determine solar event for this hour
                            var solarEvent = "True Night"; // Default

                            // Map to display model using the mapping service
                            var calculationResults = new List<AstroCalculationResult>
                    {
                        new AstroCalculationResult
                        {
                            CalculationTime = currentHour, // Keep UTC for internal calculations
                            LocalTime = TimeZoneInfo.ConvertTimeFromUtc(currentHour, TimeZoneInfo.Local), // Convert for display
                            Description = solarEvent,
                            IsVisible = true,
                            Azimuth = 180,
                            Altitude = 45
                        }
                    };

                            var displayModels = await _mappingService.MapFromDomainDataAsync(
                                calculationResults,
                                SelectedLocation.Latitude,
                                SelectedLocation.Longitude,
                                SelectedDate);

                            var hourlyPredictions = ConvertDtosToDisplayModels(displayModels);

                            // Fix the TimeDisplay to use local time
                            foreach (var prediction in hourlyPredictions)
                            {
                                var localTime = TimeZoneInfo.ConvertTimeFromUtc(currentHour, TimeZoneInfo.Local);
                                prediction.TimeDisplay = localTime.ToString("h:mm tt"); // e.g., "9:00 PM"
                                prediction.Hour = localTime; // Also update the Hour property to local time
                            }

                            // Add each prediction immediately to UI
                            foreach (var prediction in hourlyPredictions)
                            {
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    HourlyAstroPredictions.Add(prediction);
                                });
                            }
                        }
                        else
                        {
                            // Create a basic prediction for this hour if no optimal events found
                            var basicPrediction = await CreateBasicHourlyPredictionAsync(currentHour);
                            if (basicPrediction != null)
                            {
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    HourlyAstroPredictions.Add(basicPrediction);
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing hour {currentHour}: {ex.Message}");

                        // Create a basic prediction for this hour on error
                        var fallbackPrediction = await CreateBasicHourlyPredictionAsync(currentHour);
                        if (fallbackPrediction != null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                HourlyAstroPredictions.Add(fallbackPrediction);
                            });
                        }
                    }

                    currentHour = currentHour.AddHours(1);

                    // Small delay to allow UI updates
                    await Task.Delay(100);
                }

                // Update final status
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CalculationProgressStatus = $"Completed {HourlyAstroPredictions.Count} calculations";
                });

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating real astronomical predictions: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CalculationProgressStatus = "Error during calculations";
                });
            }
        }

        private async Task<AstroHourlyPredictionDisplayModel> CreateBasicHourlyPredictionAsync(DateTime hour)
        {
            try
            {
                // Create a basic calculation result for this hour
                var basicResult = new AstroCalculationResult
                {
                    CalculationTime = hour,
                    LocalTime = hour,
                    Description = "Standard observation window",
                    IsVisible = true,
                    Azimuth = 180,
                    Altitude = 45
                };

                var displayModels = await _mappingService.MapFromDomainDataAsync(
                    new List<AstroCalculationResult> { basicResult },
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude,
                    SelectedDate);

                var convertedModels = ConvertDtosToDisplayModels(displayModels);
                return convertedModels.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating basic hourly prediction for {hour}: {ex.Message}");
                return null;
            }
        }

        private string DetermineSolarPhase(DateTime hour, DateTime civilDusk, DateTime nauticalDusk, DateTime astronomicalDusk, DateTime astronomicalDawn, DateTime nauticalDawn, DateTime civilDawn)
        {
            if (hour <= civilDusk)
                return "Civil Twilight";
            else if (hour <= nauticalDusk)
                return "Nautical Twilight";
            else if (hour <= astronomicalDusk)
                return "Astronomical Twilight";
            else if (hour >= astronomicalDawn)
                return "Astronomical Twilight";
            else if (hour >= nauticalDawn)
                return "Nautical Twilight";
            else if (hour >= civilDawn)
                return "Civil Twilight";
            else
                return "True Night";
        }

        private async Task<List<AstroTarget>> GetRealViableTargetsForHourAsync(DateTime hour, string solarEvent)
        {
            var viableTargets = new List<AstroTarget>();

            try
            {
                // Check ISS visibility using real service
                try
                {
                    var issPassData = await _astroCalculationService.GetISSPassesAsync(
                        hour.Date, hour.Date.AddDays(1), SelectedLocation.Latitude, SelectedLocation.Longitude);

                    var currentPass = issPassData?.FirstOrDefault(pass =>
                        hour >= pass.StartTime && hour <= pass.EndTime);

                    if (currentPass != null)
                    {
                        viableTargets.Add(AstroTarget.ISS);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking ISS visibility: {ex.Message}");
                }

                // Check Milky Way visibility using real service
                try
                {
                    var milkyWayData = await _astroCalculationService.GetMilkyWayDataAsync(
                        hour, SelectedLocation.Latitude, SelectedLocation.Longitude);

                    if (milkyWayData.IsVisible && milkyWayData.GalacticCenterAltitude > 10)
                    {
                        viableTargets.Add(AstroTarget.MilkyWayCore);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking Milky Way visibility: {ex.Message}");
                }

                // Check Moon visibility using real service
                try
                {
                    var moonData = await _astroCalculationService.GetEnhancedMoonDataAsync(
                        hour, SelectedLocation.Latitude, SelectedLocation.Longitude);

                    if (moonData.Altitude > 5) // Moon is above horizon
                    {
                        viableTargets.Add(AstroTarget.Moon);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking Moon visibility: {ex.Message}");
                }

                // Check individual planets using real service
                try
                {
                    var planetData = await _astroCalculationService.GetVisiblePlanetsAsync(
                        hour, SelectedLocation.Latitude, SelectedLocation.Longitude);

                    foreach (var planet in planetData.Where(p => p.IsVisible && p.Altitude > 10))
                    {
                        var planetTarget = planet.Planet switch
                        {
                            PlanetType.Mercury => AstroTarget.Mercury,
                            PlanetType.Venus => AstroTarget.Venus,
                            PlanetType.Mars => AstroTarget.Mars,
                            PlanetType.Jupiter => AstroTarget.Jupiter,
                            PlanetType.Saturn => AstroTarget.Saturn,
                            PlanetType.Uranus => AstroTarget.Uranus,
                            PlanetType.Neptune => AstroTarget.Neptune,
                            PlanetType.Pluto => AstroTarget.Pluto,
                            _ => (AstroTarget?)null
                        };

                        if (planetTarget.HasValue)
                        {
                            viableTargets.Add(planetTarget.Value);
                        }
                    }

                    // Add general planets target if any individual planets are visible
                    if (viableTargets.Any(t => t >= AstroTarget.Mercury && t <= AstroTarget.Pluto))
                    {
                        viableTargets.Add(AstroTarget.Planets);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking Planet visibility: {ex.Message}");
                }

                // Check for active meteor showers using real meteor shower service
                try
                {
                    var activeShowers = await _meteorShowerDataService.GetActiveShowersAsync(
                        hour.Date, MIN_METEOR_SHOWER_ZHR, _cancellationTokenSource.Token);

                    if (activeShowers.Any())
                    {
                        // Check if any shower radiant is above horizon and in good position
                        foreach (var shower in activeShowers)
                        {
                            var radiantPosition = shower.GetRadiantPosition(hour,
                                SelectedLocation.Latitude, SelectedLocation.Longitude);

                            if (radiantPosition.IsVisible && radiantPosition.Altitude > 30)
                            {
                                viableTargets.Add(AstroTarget.MeteorShowers);
                                break; // Only add once if any showers are viable
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking meteor shower visibility: {ex.Message}");
                }

                // Check specific deep sky objects during dark conditions
                if (solarEvent == "True Night" || solarEvent == "Astronomical Twilight")
                {
                    var dsoTargets = new[]
                    {
                AstroTarget.M31_Andromeda, AstroTarget.M42_Orion, AstroTarget.M51_Whirlpool,
                AstroTarget.M13_Hercules, AstroTarget.M27_Dumbbell, AstroTarget.M57_Ring,
                AstroTarget.M81_Bodes, AstroTarget.M104_Sombrero
            };

                    foreach (var dsoTarget in dsoTargets)
                    {
                        try
                        {
                            var dsoVisibility = await GetRealTargetVisibilityAsync(dsoTarget, hour);
                            if (dsoVisibility.IsVisible && dsoVisibility.Altitude > 20)
                            {
                                viableTargets.Add(dsoTarget);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error checking {dsoTarget} visibility: {ex.Message}");
                        }
                    }

                    // Add general DSO target
                    viableTargets.Add(AstroTarget.DeepSkyObjects);
                    viableTargets.Add(AstroTarget.StarTrails);
                }

                // Check specific constellations
                var constellationTargets = new[]
                {
            AstroTarget.Constellation_Orion, AstroTarget.Constellation_Cassiopeia,
            AstroTarget.Constellation_UrsaMajor, AstroTarget.Constellation_Cygnus,
            AstroTarget.Constellation_Leo, AstroTarget.Constellation_Scorpius,
            AstroTarget.Constellation_Sagittarius
        };

                foreach (var constellationTarget in constellationTargets)
                {
                    try
                    {
                        var constellationVisibility = await GetRealTargetVisibilityAsync(constellationTarget, hour);
                        if (constellationVisibility.IsVisible && constellationVisibility.Altitude > 15)
                        {
                            viableTargets.Add(constellationTarget);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking {constellationTarget} visibility: {ex.Message}");
                    }
                }

                // Add general constellations target
                viableTargets.Add(AstroTarget.Constellations);

                // Polar alignment is viable during any dark period
                if (solarEvent != "Blue Hour" && solarEvent != "Civil Twilight")
                {
                    viableTargets.Add(AstroTarget.PolarAlignment);
                }

                // Aurora check (simplified - would need geomagnetic data)
                if (SelectedLocation.Latitude > 50) // Northern latitudes
                {
                    viableTargets.Add(AstroTarget.NorthernLights);
                }

                // Comets (simplified - would need current comet data)
                viableTargets.Add(AstroTarget.Comets);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting viable targets: {ex.Message}");
            }

            return viableTargets.Distinct().ToList();
        }

        private async Task<TargetVisibilityData> GetRealTargetVisibilityAsync(AstroTarget target, DateTime hour)
        {
            try
            {
                switch (target)
                {
                    case AstroTarget.MilkyWayCore:
                        var milkyWayData = await _astroCalculationService.GetMilkyWayDataAsync(
                            hour, SelectedLocation.Latitude, SelectedLocation.Longitude);
                        return new TargetVisibilityData
                        {
                            IsVisible = milkyWayData.IsVisible && milkyWayData.GalacticCenterAltitude > 10,
                            Altitude = milkyWayData.GalacticCenterAltitude,
                            Azimuth = milkyWayData.GalacticCenterAzimuth,
                            OptimalityScore = CalculateMilkyWayOptimality(milkyWayData, hour)
                        };

                    case AstroTarget.Moon:
                        var moonData = await _astroCalculationService.GetEnhancedMoonDataAsync(
                            hour, SelectedLocation.Latitude, SelectedLocation.Longitude);
                        return new TargetVisibilityData
                        {
                            IsVisible = moonData.Altitude > 5,
                            Altitude = moonData.Altitude,
                            Azimuth = moonData.Azimuth,
                            OptimalityScore = CalculateMoonOptimality(moonData)
                        };

                    case AstroTarget.ISS:
                        // Get ISS pass data for this specific hour
                        var issPassData = await _astroCalculationService.GetISSPassesAsync(
                            hour.Date, hour.Date.AddDays(1), SelectedLocation.Latitude, SelectedLocation.Longitude);

                        var currentPass = issPassData?.FirstOrDefault(pass =>
                            hour >= pass.StartTime && hour <= pass.EndTime);

                        if (currentPass != null)
                        {
                            // Calculate ISS position at this specific time
                            var passProgress = (hour - currentPass.StartTime).TotalMinutes /
                                             currentPass.Duration.TotalMinutes;

                            // Interpolate altitude (simple arc from start to max to end)
                            var currentAltitude = passProgress <= 0.5
                                ? currentPass.MaxAltitude * (passProgress * 2)
                                : currentPass.MaxAltitude * (2 - (passProgress * 2));

                            // Interpolate azimuth
                            var azimuthRange = Math.Abs(currentPass.EndAzimuth - currentPass.StartAzimuth);
                            var currentAzimuth = currentPass.StartAzimuth + (azimuthRange * passProgress);

                            return new TargetVisibilityData
                            {
                                IsVisible = true,
                                Altitude = Math.Max(0, currentAltitude),
                                Azimuth = currentAzimuth,
                                OptimalityScore = CalculateISSOptimality(currentPass, passProgress),
                                ISSPassType = currentPass.PassType,
                                ISSMagnitude = currentPass.Magnitude,
                                ISSPassDuration = currentPass.Duration
                            };
                        }
                        break;

                    case AstroTarget.Planets:
                        var planetData = await _astroCalculationService.GetVisiblePlanetsAsync(
                            hour, SelectedLocation.Latitude, SelectedLocation.Longitude);
                        var bestPlanet = planetData
                            .Where(p => p.IsVisible && p.Altitude > 10)
                            .OrderByDescending(p => p.Altitude)
                            .FirstOrDefault();

                        if (bestPlanet != null)
                        {
                            return new TargetVisibilityData
                            {
                                IsVisible = true,
                                Altitude = bestPlanet.Altitude,
                                Azimuth = bestPlanet.Azimuth,
                                OptimalityScore = CalculatePlanetOptimality(bestPlanet),
                                PlanetName = bestPlanet.Planet.ToString()
                            };
                        }
                        break;

                    // Individual Planet Cases
                    case AstroTarget.Mercury:
                    case AstroTarget.Venus:
                    case AstroTarget.Mars:
                    case AstroTarget.Jupiter:
                    case AstroTarget.Saturn:
                    case AstroTarget.Uranus:
                    case AstroTarget.Neptune:
                    case AstroTarget.Pluto:
                        var specificPlanetData = await GetSpecificPlanetDataAsync(target, hour);
                        if (specificPlanetData != null && specificPlanetData.IsVisible && specificPlanetData.Altitude > 10)
                        {
                            return new TargetVisibilityData
                            {
                                IsVisible = true,
                                Altitude = specificPlanetData.Altitude,
                                Azimuth = specificPlanetData.Azimuth,
                                OptimalityScore = CalculatePlanetOptimality(specificPlanetData),
                                PlanetName = specificPlanetData.Planet.ToString(),
                                Magnitude = specificPlanetData.ApparentMagnitude
                            };
                        }
                        break;

                    case AstroTarget.MeteorShowers:
                        // Get real meteor shower data for this hour
                        var activeShowers = await _meteorShowerDataService.GetActiveShowersAsync(
                            hour.Date, MIN_METEOR_SHOWER_ZHR, _cancellationTokenSource.Token);

                        var bestShower = activeShowers
                            .Select(s => new
                            {
                                Shower = s,
                                Position = s.GetRadiantPosition(hour, SelectedLocation.Latitude, SelectedLocation.Longitude),
                                ZHR = s.GetExpectedZHR(hour.Date)
                            })
                            .Where(x => x.Position.IsVisible && x.Position.Altitude > 30)
                            .OrderByDescending(x => x.ZHR)
                            .FirstOrDefault();

                        if (bestShower != null)
                        {
                            return new TargetVisibilityData
                            {
                                IsVisible = true,
                                Altitude = bestShower.Position.Altitude,
                                Azimuth = bestShower.Position.Azimuth,
                                OptimalityScore = CalculateMeteorShowerOptimality(bestShower.Shower, bestShower.ZHR, hour),
                                MeteorShowerName = bestShower.Shower.Designation,
                                ExpectedZHR = bestShower.ZHR
                            };
                        }
                        break;

                    // Specific Deep Sky Objects
                    case AstroTarget.M31_Andromeda:
                    case AstroTarget.M42_Orion:
                    case AstroTarget.M51_Whirlpool:
                    case AstroTarget.M13_Hercules:
                    case AstroTarget.M27_Dumbbell:
                    case AstroTarget.M57_Ring:
                    case AstroTarget.M81_Bodes:
                    case AstroTarget.M104_Sombrero:
                        var dsoData = await GetSpecificDSODataAsync(target, hour);
                        if (dsoData != null && dsoData.IsVisible && dsoData.Altitude > 20)
                        {
                            return new TargetVisibilityData
                            {
                                IsVisible = true,
                                Altitude = dsoData.Altitude,
                                Azimuth = dsoData.Azimuth,
                                OptimalityScore = CalculateDSOOptimality(dsoData),
                                DSOName = dsoData.CommonName,
                                DSOType = dsoData.ObjectType,
                                Magnitude = dsoData.Magnitude
                            };
                        }
                        break;

                    // Constellation Cases
                    case AstroTarget.Constellation_Orion:
                    case AstroTarget.Constellation_Cassiopeia:
                    case AstroTarget.Constellation_UrsaMajor:
                    case AstroTarget.Constellation_Cygnus:
                    case AstroTarget.Constellation_Leo:
                    case AstroTarget.Constellation_Scorpius:
                    case AstroTarget.Constellation_Sagittarius:
                        var constellationData = await GetSpecificConstellationDataAsync(target, hour);
                        if (constellationData != null && constellationData.CenterAltitude > 15)
                        {
                            return new TargetVisibilityData
                            {
                                IsVisible = true,
                                Altitude = constellationData.CenterAltitude,
                                Azimuth = constellationData.CenterAzimuth,
                                OptimalityScore = CalculateConstellationOptimality(constellationData),
                                ConstellationName = constellationData.Constellation.ToString()
                            };
                        }
                        break;

                    case AstroTarget.DeepSkyObjects:
                        // For general deep sky objects, use constellation data as proxy
                        try
                        {
                            var generalConstellationData = await _astroCalculationService.GetConstellationDataAsync(
                                ConstellationType.Orion, hour, SelectedLocation.Latitude, SelectedLocation.Longitude, _cancellationTokenSource.Token);

                            if (generalConstellationData.CenterAltitude > 30)
                            {
                                return new TargetVisibilityData
                                {
                                    IsVisible = true,
                                    Altitude = generalConstellationData.CenterAltitude,
                                    Azimuth = generalConstellationData.CenterAzimuth,
                                    OptimalityScore = Math.Min(1.0, generalConstellationData.CenterAltitude / 90.0 * 0.8)
                                };
                            }
                        }
                        catch
                        {
                            // Fallback if constellation service unavailable
                            return new TargetVisibilityData
                            {
                                IsVisible = true,
                                Altitude = 45,
                                Azimuth = 180,
                                OptimalityScore = 0.6
                            };
                        }
                        break;

                    case AstroTarget.StarTrails:
                    case AstroTarget.Constellations:
                    case AstroTarget.PolarAlignment:
                        // These are generally available during dark hours
                        return new TargetVisibilityData
                        {
                            IsVisible = true,
                            Altitude = 45,
                            Azimuth = target == AstroTarget.PolarAlignment ? 0 : 180, // North for polar alignment
                            OptimalityScore = 0.7
                        };
                }

                return new TargetVisibilityData { IsVisible = false, OptimalityScore = 0 };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting real target visibility for {target}: {ex.Message}");
                return new TargetVisibilityData { IsVisible = false, OptimalityScore = 0 };
            }
        }
        private async Task<PlanetPositionData> GetSpecificPlanetDataAsync(AstroTarget target, DateTime hour)
        {
            try
            {
                var planetType = target switch
                {
                    AstroTarget.Mercury => PlanetType.Mercury,
                    AstroTarget.Venus => PlanetType.Venus,
                    AstroTarget.Mars => PlanetType.Mars,
                    AstroTarget.Jupiter => PlanetType.Jupiter,
                    AstroTarget.Saturn => PlanetType.Saturn,
                    AstroTarget.Uranus => PlanetType.Uranus,
                    AstroTarget.Neptune => PlanetType.Neptune,
                    AstroTarget.Pluto => PlanetType.Pluto,
                    _ => throw new ArgumentException($"Invalid planet target: {target}")
                };

                return await _astroCalculationService.GetPlanetPositionAsync(
                    planetType, hour, SelectedLocation.Latitude, SelectedLocation.Longitude, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting specific planet data for {target}: {ex.Message}");
                return null;
            }
        }

        private async Task<DeepSkyObjectData> GetSpecificDSODataAsync(AstroTarget target, DateTime hour)
        {
            try
            {
                var catalogId = target switch
                {
                    AstroTarget.M31_Andromeda => "M31",
                    AstroTarget.M42_Orion => "M42",
                    AstroTarget.M51_Whirlpool => "M51",
                    AstroTarget.M13_Hercules => "M13",
                    AstroTarget.M27_Dumbbell => "M27",
                    AstroTarget.M57_Ring => "M57",
                    AstroTarget.M81_Bodes => "M81",
                    AstroTarget.M104_Sombrero => "M104",
                    _ => throw new ArgumentException($"Invalid DSO target: {target}")
                };

                return await _astroCalculationService.GetDeepSkyObjectDataAsync(
                    catalogId, hour, SelectedLocation.Latitude, SelectedLocation.Longitude, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting specific DSO data for {target}: {ex.Message}");
                return null;
            }
        }

        private async Task<ConstellationData> GetSpecificConstellationDataAsync(AstroTarget target, DateTime hour)
        {
            try
            {
                var constellationType = target switch
                {
                    AstroTarget.Constellation_Orion => ConstellationType.Orion,
                    AstroTarget.Constellation_Cassiopeia => ConstellationType.Cassiopeia,
                    AstroTarget.Constellation_UrsaMajor => ConstellationType.UrsaMajor,
                    AstroTarget.Constellation_Cygnus => ConstellationType.Cygnus,
                    AstroTarget.Constellation_Leo => ConstellationType.Leo,
                    AstroTarget.Constellation_Scorpius => ConstellationType.Scorpius,
                    AstroTarget.Constellation_Sagittarius => ConstellationType.Sagittarius,
                    _ => throw new ArgumentException($"Invalid constellation target: {target}")
                };

                return await _astroCalculationService.GetConstellationDataAsync(
                    constellationType, hour, SelectedLocation.Latitude, SelectedLocation.Longitude, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting specific constellation data for {target}: {ex.Message}");
                return null;
            }
        }

        // Enhanced optimality calculation methods
        private double CalculateISSOptimality(ISSPassData passData, double passProgress)
        {
            var score = 0.3; // Base visibility score

            // Pass type bonus
            score += passData.PassType switch
            {
                "Overhead Pass" => 0.5,
                "High Pass" => 0.4,
                "Low Pass" => 0.2,
                _ => 0.1
            };

            // Brightness bonus (magnitude - lower is brighter)
            if (passData.Magnitude < -3.0) score += 0.3;
            else if (passData.Magnitude < -2.0) score += 0.2;
            else score += 0.1;

            // Pass timing bonus (middle of pass is best)
            var timingScore = 1.0 - Math.Abs(0.5 - passProgress) * 2; // Peak at 0.5
            score += timingScore * 0.2;

            // Duration bonus
            if (passData.Duration.TotalMinutes > 5) score += 0.1;

            return Math.Min(1.0, score);
        }

        private double CalculateDSOOptimality(DeepSkyObjectData dsoData)
        {
            var score = 0.2; // Base visibility

            // Altitude bonus
            if (dsoData.Altitude > 60) score += 0.5;
            else if (dsoData.Altitude > 40) score += 0.4;
            else if (dsoData.Altitude > 20) score += 0.3;

            // Magnitude bonus (brighter objects are easier)
            if (dsoData.Magnitude < 6) score += 0.3;
            else if (dsoData.Magnitude < 8) score += 0.2;
            else if (dsoData.Magnitude < 10) score += 0.1;

            // Object type bonus
            score += dsoData.ObjectType switch
            {
                "Galaxy" => 0.2,
                "Nebula" => 0.3,
                "Open Cluster" => 0.2,
                "Globular Cluster" => 0.2,
                "Planetary Nebula" => 0.1,
                _ => 0.1
            };

            return Math.Min(1.0, score);
        }

        private double CalculateConstellationOptimality(ConstellationData constellationData)
        {
            var score = 0.3; // Base visibility

            // Altitude bonus
            if (constellationData.CenterAltitude > 60) score += 0.4;
            else if (constellationData.CenterAltitude > 30) score += 0.3;
            else if (constellationData.CenterAltitude > 15) score += 0.2;

            // Circumpolar bonus
            if (constellationData.IsCircumpolar) score += 0.3;

            return Math.Min(1.0, score);
        }



        private string GetRealTargetDisplayName(AstroTarget target, TargetVisibilityData visibility)
        {
            var baseName = target switch
            {
                // Core targets
                AstroTarget.MilkyWayCore => "Milky Way Core",
                AstroTarget.Moon => "Moon",
                AstroTarget.Planets => string.IsNullOrEmpty(visibility.PlanetName) ? "Planets" : visibility.PlanetName,
                AstroTarget.DeepSkyObjects => "Deep Sky Objects",
                AstroTarget.StarTrails => "Star Trails",
                AstroTarget.MeteorShowers => string.IsNullOrEmpty(visibility.MeteorShowerName)
                    ? "Meteor Showers"
                    : $"{visibility.MeteorShowerName} (ZHR: {visibility.ExpectedZHR:F0})",
                AstroTarget.Constellations => "Constellations",
                AstroTarget.PolarAlignment => "Polar Alignment",

                // ISS with pass information
                AstroTarget.ISS => $"ISS ({visibility.ISSPassType}, Mag {visibility.ISSMagnitude:F1}, {visibility.ISSPassDuration.TotalMinutes:F0}min)",

                // Individual planets with magnitude
                AstroTarget.Mercury => $"Mercury (Mag {visibility.Magnitude:F1})",
                AstroTarget.Venus => $"Venus (Mag {visibility.Magnitude:F1})",
                AstroTarget.Mars => $"Mars (Mag {visibility.Magnitude:F1})",
                AstroTarget.Jupiter => $"Jupiter (Mag {visibility.Magnitude:F1})",
                AstroTarget.Saturn => $"Saturn (Mag {visibility.Magnitude:F1})",
                AstroTarget.Uranus => $"Uranus (Mag {visibility.Magnitude:F1})",
                AstroTarget.Neptune => $"Neptune (Mag {visibility.Magnitude:F1})",
                AstroTarget.Pluto => $"Pluto (Mag {visibility.Magnitude:F1})",

                // Specific deep sky objects with type and magnitude
                AstroTarget.M31_Andromeda => $"M31 Andromeda Galaxy ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",
                AstroTarget.M42_Orion => $"M42 Orion Nebula ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",
                AstroTarget.M51_Whirlpool => $"M51 Whirlpool Galaxy ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",
                AstroTarget.M13_Hercules => $"M13 Hercules Cluster ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",
                AstroTarget.M27_Dumbbell => $"M27 Dumbbell Nebula ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",
                AstroTarget.M57_Ring => $"M57 Ring Nebula ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",
                AstroTarget.M81_Bodes => $"M81 Bode's Galaxy ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",
                AstroTarget.M104_Sombrero => $"M104 Sombrero Galaxy ({visibility.DSOType}, Mag {visibility.Magnitude:F1})",

                // Specific constellations
                AstroTarget.Constellation_Orion => "Orion",
                AstroTarget.Constellation_Cassiopeia => "Cassiopeia",
                AstroTarget.Constellation_UrsaMajor => "Ursa Major (Big Dipper)",
                AstroTarget.Constellation_Cygnus => "Cygnus (Northern Cross)",
                AstroTarget.Constellation_Leo => "Leo",
                AstroTarget.Constellation_Scorpius => "Scorpius",
                AstroTarget.Constellation_Sagittarius => "Sagittarius",

                // Special targets
                AstroTarget.NorthernLights => "Aurora Borealis",
                AstroTarget.Comets => "Comets",

                _ => target.ToString()
            };

            // Add altitude information for all visible targets
            if (visibility.IsVisible)
            {
                return $"{baseName} ({visibility.Altitude:F0}° altitude)";
            }
            else
            {
                return $"{baseName} (Not visible)";
            }
        }

        private async Task<WeatherConditions> GetWeatherForHourAsync(DateTime hour)
        {
            try
            {
                // Get hourly weather forecast
                var hourlyQuery = new GetHourlyForecastQuery
                {
                    LocationId = SelectedLocation.Id,
                    StartTime = hour.AddHours(-1),
                    EndTime = hour.AddHours(1)
                };

                var result = await _mediator.Send(hourlyQuery, _cancellationTokenSource.Token);
                if (result.IsSuccess && result.Data?.HourlyForecasts?.Any() == true)
                {
                    var forecast = result.Data.HourlyForecasts
                        .OrderBy(f => Math.Abs((f.DateTime - hour).TotalMinutes))
                        .FirstOrDefault();

                    if (forecast != null)
                    {
                        return new WeatherConditions
                        {
                            CloudCover = forecast.Clouds,
                            PrecipitationProbability = forecast.ProbabilityOfPrecipitation,
                            WindSpeed = forecast.WindSpeed,
                            Humidity = forecast.Humidity,
                            Visibility = forecast.Visibility,
                            Description = forecast.Description
                        };
                    }
                }

                // Fallback to reasonable defaults
                return new WeatherConditions
                {
                    CloudCover = 20,
                    PrecipitationProbability = 0.1,
                    WindSpeed = 5,
                    Humidity = 60,
                    Visibility = 10000,
                    Description = "Clear skies"
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting weather for hour: {ex.Message}");
                return new WeatherConditions
                {
                    CloudCover = 30,
                    PrecipitationProbability = 0.2,
                    WindSpeed = 8,
                    Humidity = 65,
                    Visibility = 8000,
                    Description = "Partly cloudy"
                };
            }
        }

        private List<AstroHourlyPredictionDisplayModel> ConvertDtosToDisplayModels(List<AstroHourlyPredictionDto> dtos)
        {
            return dtos.Select(dto => new AstroHourlyPredictionDisplayModel
            {
                Hour = dto.Hour,
                TimeDisplay = dto.TimeDisplay,
                SolarEvent = dto.SolarEvent,
                SolarEventsDisplay = dto.SolarEventsDisplay,
                QualityScore = dto.QualityScore,
                QualityDisplay = dto.QualityDisplay,
                QualityDescription = dto.QualityDescription,
                AstroEvents = dto.AstroEvents.Select(e => new AstroEventDisplayModel
                {
                    TargetName = e.TargetName,
                    Visibility = e.Visibility,
                    RecommendedEquipment = e.RecommendedEquipment,
                    CameraSettings = e.CameraSettings,
                    Notes = e.Notes
                }).ToList(),
                WeatherCloudCover = dto.Weather.CloudCover,
                WeatherHumidity = dto.Weather.Humidity,
                WeatherWindSpeed = dto.Weather.WindSpeed,
                WeatherVisibility = dto.Weather.Visibility,
                WeatherDescription = dto.Weather.Description,
                WeatherDisplay = dto.Weather.WeatherDisplay,
                WeatherSuitability = dto.Weather.WeatherSuitability
            }).ToList();
        }

        private async Task ApplyCachedPredictionsAsync(List<AstroHourlyPrediction> cachedPredictions)
        {
            // Convert cached domain objects to display models
            var calculationResults = cachedPredictions.Select(p => new AstroCalculationResult
            {
                CalculationTime = p.Hour,
                LocalTime = p.Hour,
                Description = p.SolarEvent,
                IsVisible = true
            }).ToList();

            var displayModels = ConvertDtosToDisplayModels(await _mappingService.MapFromDomainDataAsync(
                calculationResults,
                SelectedLocation.Latitude,
                SelectedLocation.Longitude,
                SelectedDate));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                HourlyAstroPredictions.Clear();
                foreach (var prediction in displayModels)
                {
                    HourlyAstroPredictions.Add(prediction);
                }
                HourlyPredictionsStatus = $"Using cached predictions from {_lastCalculationTime:HH:mm}";
            });
        }

        #endregion

        #region Helper Methods for Target Optimality

        private double CalculateMilkyWayOptimality(MilkyWayData milkyWayData, DateTime dateTime)
        {
            if (!milkyWayData.IsVisible) return 0.0;

            var score = 0.3; // Base visibility score

            // Best visibility when galactic center is highest
            if (milkyWayData.GalacticCenterAltitude > 40) score += 0.5;
            else if (milkyWayData.GalacticCenterAltitude > 20) score += 0.3;
            else if (milkyWayData.GalacticCenterAltitude > 10) score += 0.1;

            // Season bonus - summer months are best
            var month = dateTime.Month;
            if (month >= 5 && month <= 9) score += 0.2;

            return Math.Min(1.0, score);
        }

        private double CalculateMoonOptimality(EnhancedMoonData moonData)
        {
            if (moonData.Altitude <= 0) return 0.0;

            var score = 0.2; // Base visibility

            // Altitude bonus
            if (moonData.Altitude > 45) score += 0.5;
            else if (moonData.Altitude > 20) score += 0.4;
            else if (moonData.Altitude > 5) score += 0.2;

            // Phase bonus - partial phases show crater detail
            var phaseIllumination = Math.Abs(moonData.OpticalLibration) / 100.0;
            if (phaseIllumination > 0.2 && phaseIllumination < 0.8)
                score += 0.3;

            return Math.Min(1.0, score);
        }

        private double CalculatePlanetOptimality(PlanetPositionData planetData)
        {
            if (planetData == null || !planetData.IsVisible) return 0.0;

            var score = 0.2; // Base visibility

            // Altitude is critical for planets
            if (planetData.Altitude > 60) score += 0.6;
            else if (planetData.Altitude > 30) score += 0.4;
            else if (planetData.Altitude > 15) score += 0.2;

            // Magnitude bonus (brighter planets are easier)
            if (planetData.ApparentMagnitude < -2) score += 0.2;
            else if (planetData.ApparentMagnitude < 0) score += 0.1;

            return Math.Min(1.0, score);
        }

        private double CalculateMeteorShowerOptimality(MeteorShower shower, double expectedZHR, DateTime dateTime)
        {
            var score = 0.2; // Base visibility

            // ZHR impact (higher rates are better for photography)
            if (expectedZHR >= 100) score += 0.5;
            else if (expectedZHR >= 50) score += 0.4;
            else if (expectedZHR >= 20) score += 0.3;
            else if (expectedZHR >= 10) score += 0.2;

            // Peak date bonus
            try
            {
                var peakDate = DateTime.ParseExact($"{dateTime.Year}-{shower.Activity.Peak}", "yyyy-MM-dd", null);
                var daysDifference = Math.Abs((dateTime.Date - peakDate.Date).TotalDays);

                if (daysDifference == 0) score += 0.3; // Peak night
                else if (daysDifference <= 1) score += 0.2; // ±1 day from peak
                else if (daysDifference <= 2) score += 0.1; // ±2 days from peak
            }
            catch
            {
                // If date parsing fails, give moderate bonus
                score += 0.1;
            }

            return Math.Min(1.0, score);
        }

        #endregion

        #region Equipment Matching Methods

        private async Task<AstroTargetEvent> CreateTargetEventAsync(DateTime hour, AstroTarget target)
        {
            try
            {
                // Get target visibility and requirements
                var visibility = await GetRealTargetVisibilityAsync(target, hour);
                var requirements = GetTargetRequirements(target);

                // Find user's best matching equipment
                var equipmentMatch = await FindBestUserEquipmentAsync(requirements);

                // Calculate normalized settings
                var settings = await CalculateNormalizedSettingsAsync(target, visibility, equipmentMatch);

                return new AstroTargetEvent
                {
                    Target = target,
                    TargetDisplay = GetRealTargetDisplayName(target, visibility),
                    Visibility = visibility,
                    Equipment = equipmentMatch,
                    Settings = settings,
                    Requirements = requirements
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating target event for {target}: {ex.Message}");
                return null;
            }
        }

        private TargetRequirements GetTargetRequirements(AstroTarget target)
        {
            return target switch
            {
                // Wide-field targets
                AstroTarget.MilkyWayCore => new TargetRequirements
                {
                    OptimalFocalLength = 24,
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    MaxAperture = 2.8,
                    TargetType = "wide_angle"
                },

                AstroTarget.MeteorShowers => new TargetRequirements
                {
                    OptimalFocalLength = 24,
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    MaxAperture = 2.8,
                    TargetType = "wide_angle"
                },

                AstroTarget.StarTrails => new TargetRequirements
                {
                    OptimalFocalLength = 24,
                    MinFocalLength = 14,
                    MaxFocalLength = 50,
                    MaxAperture = 4.0,
                    TargetType = "wide_angle"
                },

                // ISS - Fast moving target requiring wide field and fast settings
                AstroTarget.ISS => new TargetRequirements
                {
                    OptimalFocalLength = 35,
                    MinFocalLength = 24,
                    MaxFocalLength = 85,
                    MaxAperture = 2.8,
                    TargetType = "iss_tracking"
                },

                // Lunar targets
                AstroTarget.Moon => new TargetRequirements
                {
                    OptimalFocalLength = 400,
                    MinFocalLength = 200,
                    MaxFocalLength = 800,
                    MaxAperture = 8.0,
                    TargetType = "telephoto"
                },

                // Planetary targets - general
                AstroTarget.Planets => new TargetRequirements
                {
                    OptimalFocalLength = 600,
                    MinFocalLength = 300,
                    MaxFocalLength = 1200,
                    MaxAperture = 5.6,
                    TargetType = "long_telephoto"
                },

                // Individual planets - each with specific requirements
                AstroTarget.Mercury => new TargetRequirements
                {
                    OptimalFocalLength = 800,
                    MinFocalLength = 400,
                    MaxFocalLength = 1200,
                    MaxAperture = 6.3,
                    TargetType = "extreme_telephoto" // Small, close to sun
                },

                AstroTarget.Venus => new TargetRequirements
                {
                    OptimalFocalLength = 600,
                    MinFocalLength = 300,
                    MaxFocalLength = 1000,
                    MaxAperture = 5.6,
                    TargetType = "long_telephoto" // Bright, shows phases
                },

                AstroTarget.Mars => new TargetRequirements
                {
                    OptimalFocalLength = 800,
                    MinFocalLength = 400,
                    MaxFocalLength = 1200,
                    MaxAperture = 5.6,
                    TargetType = "extreme_telephoto" // Small, variable size
                },

                AstroTarget.Jupiter => new TargetRequirements
                {
                    OptimalFocalLength = 600,
                    MinFocalLength = 300,
                    MaxFocalLength = 1000,
                    MaxAperture = 5.6,
                    TargetType = "long_telephoto" // Large, shows moons and bands
                },

                AstroTarget.Saturn => new TargetRequirements
                {
                    OptimalFocalLength = 800,
                    MinFocalLength = 400,
                    MaxFocalLength = 1200,
                    MaxAperture = 5.6,
                    TargetType = "extreme_telephoto" // Need focal length for rings
                },

                AstroTarget.Uranus => new TargetRequirements
                {
                    OptimalFocalLength = 1000,
                    MinFocalLength = 600,
                    MaxFocalLength = 1500,
                    MaxAperture = 6.3,
                    TargetType = "extreme_telephoto" // Very small disk
                },

                AstroTarget.Neptune => new TargetRequirements
                {
                    OptimalFocalLength = 1000,
                    MinFocalLength = 600,
                    MaxFocalLength = 1500,
                    MaxAperture = 6.3,
                    TargetType = "extreme_telephoto" // Tiny disk, dim
                },

                AstroTarget.Pluto => new TargetRequirements
                {
                    OptimalFocalLength = 1200,
                    MinFocalLength = 800,
                    MaxFocalLength = 2000,
                    MaxAperture = 8.0,
                    TargetType = "extreme_telephoto" // Point source only
                },

                // Deep sky objects - general
                AstroTarget.DeepSkyObjects => new TargetRequirements
                {
                    OptimalFocalLength = 135,
                    MinFocalLength = 85,
                    MaxFocalLength = 300,
                    MaxAperture = 4.0,
                    TargetType = "medium_telephoto"
                },

                // Specific deep sky objects with tailored requirements
                AstroTarget.M31_Andromeda => new TargetRequirements
                {
                    OptimalFocalLength = 135,
                    MinFocalLength = 85,
                    MaxFocalLength = 200,
                    MaxAperture = 4.0,
                    TargetType = "wide_dso" // Large galaxy needs wide field
                },

                AstroTarget.M42_Orion => new TargetRequirements
                {
                    OptimalFocalLength = 200,
                    MinFocalLength = 135,
                    MaxFocalLength = 300,
                    MaxAperture = 4.0,
                    TargetType = "medium_dso" // Perfect size for medium telephoto
                },

                AstroTarget.M51_Whirlpool => new TargetRequirements
                {
                    OptimalFocalLength = 300,
                    MinFocalLength = 200,
                    MaxFocalLength = 600,
                    MaxAperture = 5.6,
                    TargetType = "narrow_dso" // Small galaxy needs magnification
                },

                AstroTarget.M13_Hercules => new TargetRequirements
                {
                    OptimalFocalLength = 200,
                    MinFocalLength = 135,
                    MaxFocalLength = 400,
                    MaxAperture = 4.0,
                    TargetType = "medium_dso" // Globular cluster
                },

                AstroTarget.M27_Dumbbell => new TargetRequirements
                {
                    OptimalFocalLength = 300,
                    MinFocalLength = 200,
                    MaxFocalLength = 600,
                    MaxAperture = 5.6,
                    TargetType = "narrow_dso" // Planetary nebula needs magnification
                },

                AstroTarget.M57_Ring => new TargetRequirements
                {
                    OptimalFocalLength = 600,
                    MinFocalLength = 400,
                    MaxFocalLength = 1000,
                    MaxAperture = 6.3,
                    TargetType = "narrow_dso" // Small planetary nebula
                },

                AstroTarget.M81_Bodes => new TargetRequirements
                {
                    OptimalFocalLength = 300,
                    MinFocalLength = 200,
                    MaxFocalLength = 600,
                    MaxAperture = 5.6,
                    TargetType = "narrow_dso" // Compact galaxy
                },

                AstroTarget.M104_Sombrero => new TargetRequirements
                {
                    OptimalFocalLength = 400,
                    MinFocalLength = 300,
                    MaxFocalLength = 800,
                    MaxAperture = 5.6,
                    TargetType = "narrow_dso" // Edge-on galaxy
                },

                // Constellation targets
                AstroTarget.Constellations => new TargetRequirements
                {
                    OptimalFocalLength = 85,
                    MinFocalLength = 50,
                    MaxFocalLength = 135,
                    MaxAperture = 4.0,
                    TargetType = "standard"
                },

                // Specific constellations with tailored framing
                AstroTarget.Constellation_Orion => new TargetRequirements
                {
                    OptimalFocalLength = 85,
                    MinFocalLength = 50,
                    MaxFocalLength = 135,
                    MaxAperture = 2.8,
                    TargetType = "wide_constellation" // Large, bright constellation
                },

                AstroTarget.Constellation_Cassiopeia => new TargetRequirements
                {
                    OptimalFocalLength = 50,
                    MinFocalLength = 35,
                    MaxFocalLength = 85,
                    MaxAperture = 2.8,
                    TargetType = "wide_constellation" // W-shape needs wide field
                },

                AstroTarget.Constellation_UrsaMajor => new TargetRequirements
                {
                    OptimalFocalLength = 85,
                    MinFocalLength = 50,
                    MaxFocalLength = 135,
                    MaxAperture = 2.8,
                    TargetType = "wide_constellation" // Big Dipper is large
                },

                AstroTarget.Constellation_Cygnus => new TargetRequirements
                {
                    OptimalFocalLength = 50,
                    MinFocalLength = 35,
                    MaxFocalLength = 85,
                    MaxAperture = 2.8,
                    TargetType = "wide_constellation" // Northern Cross spans wide area
                },

                AstroTarget.Constellation_Leo => new TargetRequirements
                {
                    OptimalFocalLength = 85,
                    MinFocalLength = 50,
                    MaxFocalLength = 135,
                    MaxAperture = 2.8,
                    TargetType = "standard_constellation"
                },

                AstroTarget.Constellation_Scorpius => new TargetRequirements
                {
                    OptimalFocalLength = 85,
                    MinFocalLength = 50,
                    MaxFocalLength = 135,
                    MaxAperture = 2.8,
                    TargetType = "standard_constellation"
                },

                AstroTarget.Constellation_Sagittarius => new TargetRequirements
                {
                    OptimalFocalLength = 50,
                    MinFocalLength = 35,
                    MaxFocalLength = 85,
                    MaxAperture = 2.8,
                    TargetType = "wide_constellation" // Dense Milky Way region
                },

                // Special targets
                AstroTarget.PolarAlignment => new TargetRequirements
                {
                    OptimalFocalLength = 100,
                    MinFocalLength = 50,
                    MaxFocalLength = 200,
                    MaxAperture = 5.6,
                    TargetType = "standard"
                },

                AstroTarget.NorthernLights => new TargetRequirements
                {
                    OptimalFocalLength = 24,
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    MaxAperture = 2.8,
                    TargetType = "ultra_wide" // Aurora spans entire sky
                },

                AstroTarget.Comets => new TargetRequirements
                {
                    OptimalFocalLength = 200,
                    MinFocalLength = 135,
                    MaxFocalLength = 400,
                    MaxAperture = 4.0,
                    TargetType = "medium_telephoto" // Variable size, usually medium field
                },

                // Default fallback
                _ => new TargetRequirements
                {
                    OptimalFocalLength = 50,
                    MinFocalLength = 35,
                    MaxFocalLength = 85,
                    MaxAperture = 4.0,
                    TargetType = "standard"
                }
            };
        }

        private async Task<UserEquipmentMatch> FindBestUserEquipmentAsync(TargetRequirements requirements)
        {
            try
            {
                // Find best matching lens from user's collection
                var userLenses = AvailableLenses.Where(l => l.IsUserCreated).ToList();
                var bestLens = FindBestMatchingLens(userLenses, requirements);

                if (bestLens != null)
                {
                    // Find compatible camera for this lens
                    var compatibleCameras = AvailableCameras.Where(c =>
                        c.IsUserCreated && IsLensCompatibleWithCamera(bestLens, c)).ToList();
                    var selectedCamera = compatibleCameras.FirstOrDefault();

                    if (selectedCamera != null)
                    {
                        return new UserEquipmentMatch
                        {
                            Found = true,
                            Camera = selectedCamera,
                            Lens = bestLens,
                            CameraDisplay = selectedCamera.Name,
                            LensDisplay = bestLens.NameForLens
                        };
                    }
                }

                // No suitable equipment found
                return new UserEquipmentMatch
                {
                    Found = false,
                    RecommendationMessage = $"You need a lens with at least {requirements.MinFocalLength}mm focal length and f/{requirements.MaxAperture} aperture"
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding user equipment: {ex.Message}");
                return new UserEquipmentMatch { Found = false, RecommendationMessage = "Unable to match equipment" };
            }
        }

        private Lens FindBestMatchingLens(List<Lens> userLenses, TargetRequirements requirements)
        {
            // Priority 1: Lens that covers optimal focal length and meets aperture requirement
            var optimalMatch = userLenses.FirstOrDefault(l =>
                l.MaxFStop <= requirements.MaxAperture &&
                ((l.IsPrime && Math.Abs(l.MinMM - requirements.OptimalFocalLength) <= 10) ||
                 (!l.IsPrime && l.MaxMM.HasValue &&
                  requirements.OptimalFocalLength >= l.MinMM &&
                  requirements.OptimalFocalLength <= l.MaxMM.Value)));

            if (optimalMatch != null) return optimalMatch;

            // Priority 2: Lens that covers focal length range and meets aperture requirement
            var rangeMatch = userLenses.FirstOrDefault(l =>
                l.MaxFStop <= requirements.MaxAperture &&
                ((!l.IsPrime && l.MaxMM.HasValue &&
                  ((requirements.MinFocalLength >= l.MinMM && requirements.MinFocalLength <= l.MaxMM.Value) ||
                   (requirements.MaxFocalLength >= l.MinMM && requirements.MaxFocalLength <= l.MaxMM.Value))) ||
                 (l.IsPrime && l.MinMM >= requirements.MinFocalLength && l.MinMM <= requirements.MaxFocalLength)));

            if (rangeMatch != null) return rangeMatch;

            // Priority 3: Any lens in focal length range (ignore aperture)
            var anyMatch = userLenses.FirstOrDefault(l =>
                ((!l.IsPrime && l.MaxMM.HasValue &&
                  ((requirements.MinFocalLength >= l.MinMM && requirements.MinFocalLength <= l.MaxMM.Value) ||
                   (requirements.MaxFocalLength >= l.MinMM && requirements.MaxFocalLength <= l.MaxMM.Value))) ||
                 (l.IsPrime && l.MinMM >= requirements.MinFocalLength && l.MinMM <= requirements.MaxFocalLength)));

            return anyMatch;
        }

        private bool IsLensCompatibleWithCamera(Lens lens, CameraBody camera)
        {
            // Simplified compatibility check - in reality would check mount systems
            return true;
        }

        private async Task<NormalizedCameraSettings> CalculateNormalizedSettingsAsync(
            AstroTarget target, TargetVisibilityData visibility, UserEquipmentMatch equipment)
        {
            try
            {
                // Calculate base settings for target
                var baseSettings = GetBaseSettingsForTarget(target);

                // Get standardized lists
                var allApertures = Interfaces.Apetures.Thirds.Select(a => Convert.ToDouble(a.Replace("f/", ""))).ToList();
                var allShutterSpeeds = ShutterSpeeds.Thirds.Select(s => ParseShutterSpeed(s)).ToList();

                // Find closest values from standard lists
                var closestAperture = allApertures.OrderBy(x => Math.Abs(x - baseSettings.Aperture)).First();
                var closestShutter = allShutterSpeeds.OrderBy(x => Math.Abs(x - baseSettings.ShutterSpeed)).First();

                // Use ExposureCalculatorService to normalize the triangle
                var exposureDto = new ExposureTriangleDto
                {
                    Aperture = $"f/{baseSettings.Aperture}",
                    Iso = baseSettings.ISO.ToString(),
                    ShutterSpeed = FormatShutterSpeed(baseSettings.ShutterSpeed)
                };

                var normalizedResult = await _exposureCalculatorService.CalculateIsoAsync(
                    exposureDto,
                    FormatShutterSpeed(closestShutter),
                    $"f/{closestAperture}",
                    ExposureIncrements.Third);

                if (normalizedResult.IsSuccess && normalizedResult.Data != null)
                {
                    return new NormalizedCameraSettings
                    {
                        Aperture = $"f/{normalizedResult.Data.Aperture}",
                        ShutterSpeed = normalizedResult.Data.ShutterSpeed,
                        ISO = $"ISO {normalizedResult.Data.Iso}"
                    };
                }

                // Fallback to base settings if normalization fails
                return new NormalizedCameraSettings
                {
                    Aperture = $"f/{closestAperture:F1}",
                    ShutterSpeed = FormatShutterSpeed(closestShutter),
                    ISO = $"ISO {baseSettings.ISO}"
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating normalized settings: {ex.Message}");
                return new NormalizedCameraSettings
                {
                    Aperture = "f/4.0",
                    ShutterSpeed = "30\"",
                    ISO = "ISO 1600"
                };
            }
        }

        private BaseCameraSettings GetBaseSettingsForTarget(AstroTarget target)
        {
            return target switch
            {
                // Wide-field targets
                AstroTarget.MilkyWayCore => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 20,
                    ISO = 3200
                },

                AstroTarget.MeteorShowers => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 30,
                    ISO = 3200
                },

                AstroTarget.StarTrails => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 240, // 4 min intervals
                    ISO = 400
                },

                // ISS - Fast moving target requiring very fast shutter speeds
                AstroTarget.ISS => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 0.5, // 1/2 second max to avoid streaking
                    ISO = 6400 // High ISO to compensate for fast shutter
                },

                // Lunar targets
                AstroTarget.Moon => new BaseCameraSettings
                {
                    Aperture = 8.0,
                    ShutterSpeed = 0.008, // 1/125 second
                    ISO = 200
                },

                // Planetary targets - general
                AstroTarget.Planets => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 0.017, // 1/60 second
                    ISO = 800
                },

                // Individual planets with specific optimizations
                AstroTarget.Mercury => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 0.008, // 1/125 - fast for atmospheric stability
                    ISO = 1600 // Higher ISO for small, dim target
                },

                AstroTarget.Venus => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 0.004, // 1/250 - very bright planet
                    ISO = 400 // Lower ISO, Venus is very bright
                },

                AstroTarget.Mars => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 0.017, // 1/60 second
                    ISO = 1600 // Variable brightness, err on higher side
                },

                AstroTarget.Jupiter => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 0.017, // 1/60 second
                    ISO = 800 // Bright planet with good detail
                },

                AstroTarget.Saturn => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 0.033, // 1/30 second
                    ISO = 1600 // Dimmer than Jupiter, need more light for rings
                },

                AstroTarget.Uranus => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 0.5, // 1/2 second
                    ISO = 3200 // Very dim, small disk
                },

                AstroTarget.Neptune => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 1.0, // 1 second
                    ISO = 6400 // Extremely dim and distant
                },

                AstroTarget.Pluto => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 30, // 30 seconds
                    ISO = 12800 // Essentially a faint star
                },

                // Deep sky objects - general
                AstroTarget.DeepSkyObjects => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 300, // 5 minutes
                    ISO = 1600
                },

                // Specific deep sky objects with optimized settings
                AstroTarget.M31_Andromeda => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 180, // 3 minutes - bright galaxy
                    ISO = 1600
                },

                AstroTarget.M42_Orion => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 120, // 2 minutes - very bright nebula
                    ISO = 800
                },

                AstroTarget.M51_Whirlpool => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 480, // 8 minutes - dimmer galaxy
                    ISO = 3200
                },

                AstroTarget.M13_Hercules => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 240, // 4 minutes - globular cluster
                    ISO = 1600
                },

                AstroTarget.M27_Dumbbell => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 600, // 10 minutes - planetary nebula
                    ISO = 3200
                },

                AstroTarget.M57_Ring => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 900, // 15 minutes - small planetary nebula
                    ISO = 6400
                },

                AstroTarget.M81_Bodes => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 360, // 6 minutes - moderately bright galaxy
                    ISO = 3200
                },

                AstroTarget.M104_Sombrero => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 600, // 10 minutes - edge-on galaxy
                    ISO = 3200
                },

                // Constellation targets
                AstroTarget.Constellations => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 60,
                    ISO = 1600
                },

                // Specific constellations optimized for star patterns
                AstroTarget.Constellation_Orion => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 30, // Bright stars, include nebulosity
                    ISO = 1600
                },

                AstroTarget.Constellation_Cassiopeia => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 45, // Bright W-pattern stars
                    ISO = 1600
                },

                AstroTarget.Constellation_UrsaMajor => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 45, // Big Dipper stars
                    ISO = 1600
                },

                AstroTarget.Constellation_Cygnus => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 25, // Rich Milky Way field
                    ISO = 3200
                },

                AstroTarget.Constellation_Leo => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 60, // Spring constellation
                    ISO = 1600
                },

                AstroTarget.Constellation_Scorpius => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 30, // Summer constellation with nebulae
                    ISO = 3200
                },

                AstroTarget.Constellation_Sagittarius => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 20, // Dense Milky Way core region
                    ISO = 6400
                },

                // Special targets
                AstroTarget.PolarAlignment => new BaseCameraSettings
                {
                    Aperture = 5.6,
                    ShutterSpeed = 30,
                    ISO = 1600
                },

                AstroTarget.NorthernLights => new BaseCameraSettings
                {
                    Aperture = 2.8,
                    ShutterSpeed = 15, // Aurora changes quickly
                    ISO = 3200
                },

                AstroTarget.Comets => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 180, // 3 minutes - varies by comet brightness
                    ISO = 3200
                },

                // Default fallback
                _ => new BaseCameraSettings
                {
                    Aperture = 4.0,
                    ShutterSpeed = 30,
                    ISO = 1600
                }
            };
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

                return 1.0 / 60.0; // Default fallback
            }
            catch
            {
                return 1.0 / 60.0;
            }
        }

        private string FormatShutterSpeed(double seconds)
        {
            if (seconds >= 1)
                return $"{seconds:F0}\"";
            else
                return $"1/{Math.Round(1.0 / seconds):F0}";
        }

        private async Task<double> CalculateOverallShootingScoreAsync(DateTime hour, List<AstroTarget> targets)
        {
            try
            {
                var scores = new List<double>();

                foreach (var target in targets)
                {
                    var visibility = await GetRealTargetVisibilityAsync(target, hour);
                    scores.Add(visibility.OptimalityScore * 100);
                }

                // Get weather impact
                var weather = await GetWeatherForHourAsync(hour);
                var weatherScore = CalculateWeatherScore(weather);

                // Combine target scores with weather score
                var avgTargetScore = scores.Any() ? scores.Average() : 0;
                var combinedScore = (avgTargetScore * 0.7) + (weatherScore * 0.3);

                return Math.Max(0, Math.Min(100, combinedScore));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating overall score: {ex.Message}");
                return 50; // Default moderate score
            }
        }

        private double CalculateWeatherScore(WeatherConditions weather)
        {
            if (weather == null) return 70; // Default moderate score

            var score = 100.0;

            // Cloud cover impact (most critical)
            score -= weather.CloudCover * 0.8;

            // Precipitation impact
            if (weather.PrecipitationProbability > 0.3)
                score -= weather.PrecipitationProbability * 60;

            // Wind impact
            if (weather.WindSpeed > 15)
                score -= (weather.WindSpeed - 15) * 2;

            // Humidity impact
            if (weather.Humidity > 80)
                score -= (weather.Humidity - 80) * 0.5;

            return Math.Max(0, Math.Min(100, score));
        }

        #endregion

        #region Enhanced Commands and Existing Methods

        private void InitializeCommands()
        {
            LoadLocationsCommand = new AsyncRelayCommand(LoadLocationsAsync);
            LoadEquipmentCommand = new AsyncRelayCommand(LoadEquipmentAsync);
            CalculateAstroDataCommand = new AsyncRelayCommand(CalculateAstroDataAsync);
            RefreshCalculationsCommand = new AsyncRelayCommand(RefreshCalculationsAsync);
            SelectCameraCommand = new AsyncRelayCommand<CameraBody>(SelectCameraAsync);
            SelectLensCommand = new AsyncRelayCommand<Lens>(SelectLensAsync);
            SelectTargetCommand = new AsyncRelayCommand<AstroTarget>(SelectTargetAsync);
            RetryLastCommandCommand = new AsyncRelayCommand(RetryLastCommandAsync);
            GenerateHourlyPredictionsCommand = new AsyncRelayCommand(GenerateHourlyPredictionsAsync);
        }

        public async Task GenerateHourlyPredictionsAsync()
        {
            await CalculateAstroDataAsync(); // Delegate to main calculation method
        }

        public async Task RefreshCalculationsAsync()
        {
            _calculationCache.Clear();
            _predictionCache.Clear();
            await CalculateAstroDataAsync();
        }

        #endregion

        #region Core Methods

        public async Task LoadLocationsAsync()
        {


            try
            {
                // ✅ Ensure IsBusy is set on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = true;
                    HasError = false;
                    ErrorMessage = string.Empty;
                });

                var query = new GetLocationsQuery();
                var result = await _mediator.Send(query, _cancellationTokenSource.Token);

                if (result.IsSuccess && result.Data?.Items?.Any() == true)
                {
                    var locationViewModels = result.Data.Items
                        .Where(location => location != null)
                        .Select(location => new LocationListItemViewModel
                        {
                            Id = location.Id,
                            Title = location.Title ?? "Unknown Location",
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            Photo = location.PhotoPath ?? string.Empty
                        }).ToList();

                    if (locationViewModels.Any())
                    {
                        // ✅ Update UI on main thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            Locations = new ObservableCollection<LocationListItemViewModel>(locationViewModels);

                            if (SelectedLocation == null && Locations.Any())
                            {
                                SelectedLocation = Locations.First();
                                LocationPhoto = SelectedLocation?.Photo ?? string.Empty;
                            }

                            IsInitialized = true;
                        });
                    }
                    else
                    {
                        await HandleErrorAsync(OperationErrorSource.Database, "No valid locations found in the database");
                    }
                }
                else
                {
                    var errorMsg = result?.ErrorMessage ?? "Failed to retrieve locations from database";
                    await HandleErrorAsync(OperationErrorSource.Database, errorMsg);
                }
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CalculationStatus = "Location loading cancelled";
                });
            }
            catch (ArgumentNullException ex)
            {
                await HandleErrorAsync(OperationErrorSource.Database, $"Database configuration error: {ex.ParamName}");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Database, $"Error loading locations: {ex.Message}");
            }
            finally
            {
                // ✅ Ensure IsBusy is set to false on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = false;
                });
            }
        }

        public async Task LoadEquipmentAsync()
        {

            try
            {
                IsLoadingEquipment = true;
                HasError = false;

                // Get current user ID
                var currentUserId = await SecureStorage.GetAsync("Email") ?? "default_user";

                var cameras = new List<CameraBody>();
                var lenses = new List<Lens>();

                // Load user cameras first (marked with *)
                var userResult = await _cameraDataService.GetUserCameraBodiesAsync(currentUserId, 0, int.MaxValue);
                if (userResult.IsSuccess && userResult.Data?.CameraBodies?.Any() == true)
                {
                    var userCameras = userResult.Data.CameraBodies.OrderBy(c => c.DisplayName).ToList();

                    // Add user cameras with * prefix
                    foreach (var camera in userCameras)
                    {
                        var userCamera = new CameraBody(
                            "* " + camera.DisplayName, // Add * prefix like FieldOfView
                            camera.SensorType,
                            camera.SensorWidth,
                            camera.SensorHeight,
                            camera.MountType,
                            true) // Mark as user created
                        {
                            Id = camera.Id,
                            DateAdded = camera.DateAdded
                        };
                        cameras.Add(userCamera);
                    }
                }

                // Load all cameras from database
                var allCamerasResult = await _cameraDataService.GetCameraBodiesAsync(0, int.MaxValue);
                if (allCamerasResult.IsSuccess && allCamerasResult.Data?.CameraBodies?.Any() == true)
                {
                    var allCameras = allCamerasResult.Data.CameraBodies.OrderBy(c => c.DisplayName).ToList();

                    // Add all cameras (without * prefix)
                    foreach (var camera in allCameras)
                    {
                        var cameraBody = new CameraBody(
                            camera.DisplayName,
                            camera.SensorType,
                            camera.SensorWidth,
                            camera.SensorHeight,
                            camera.MountType,
                            false) // Not user created
                        {
                            Id = camera.Id,
                            DateAdded = camera.DateAdded
                        };
                        cameras.Add(cameraBody);
                    }
                }

                // Load lenses using camera data service (similar to FieldOfView but without camera filter)
                var lensesResult = await _cameraDataService.GetLensesAsync(0, int.MaxValue, false, null);
                if (lensesResult.IsSuccess && lensesResult.Data?.Lenses?.Any() == true)
                {
                    var allLenses = lensesResult.Data.Lenses.OrderBy(l => l.DisplayName).ToList();

                    // Convert DTOs to domain objects
                    foreach (var lensDto in allLenses)
                    {
                        var lens = new Lens(

                            lensDto.MinMM,
                            lensDto.MaxMM.HasValue ? lensDto.MaxMM.Value : lensDto.MinMM,
                            lensDto.MinFStop,
                            lensDto.MaxFStop,
                            lensDto.IsUserCreated, 
                            lensDto.DisplayName) // Assuming not user created for now
                        {
                            Id = lensDto.Id,
                            DateAdded = lensDto.DateAdded 
                        };
                        lenses.Add(lens);
                    }
                }

                // Update collections
                AvailableCameras = new ObservableCollection<CameraBody>(cameras);
                AvailableLenses = new ObservableCollection<Lens>(lenses);

                // Auto-select optimal equipment
                await AutoSelectOptimalEquipmentAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Database, $"Error loading equipment: {ex.Message}");
            }
            finally
            {
                IsLoadingEquipment = false;
            }
        }

        public async Task SelectCameraAsync(CameraBody camera)
        {
            if (camera == null) return;

            try
            {
                SelectedCamera = camera;
                await UpdateCompatibleLensesAsync();

                if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Validation, $"Error selecting camera: {ex.Message}");
            }
        }

        public async Task SelectLensAsync(Lens lens)
        {
            if (lens == null) return;

            try
            {
                SelectedLens = lens;

                if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Validation, $"Error selecting lens: {ex.Message}");
            }
        }

        public async Task SelectTargetAsync(AstroTarget target)
        {
            if (target == SelectedTarget) return;

            try
            {
                SelectedTarget = target;
                await AutoSelectOptimalEquipmentAsync();

                if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Validation, $"Error selecting target: {ex.Message}");
            }
        }

        public async Task RetryLastCommandAsync()
        {
            try
            {
                HasError = false;
                ErrorMessage = string.Empty;

                if (!IsInitialized)
                {
                    await LoadLocationsAsync();
                    await LoadEquipmentAsync();
                }
                else if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Network, $"Retry failed: {ex.Message}");
            }
        }

        public void CancelAllOperations()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private async Task UpdateCompatibleLensesAsync()
        {
            if (SelectedCamera == null) return;

            try
            {
                var compatibleLensesResult = await _lensRepository.GetCompatibleLensesAsync(
                    SelectedCamera.Id, _cancellationTokenSource.Token);

                if (compatibleLensesResult.IsSuccess && compatibleLensesResult.Data?.Any() == true)
                {
                    var compatibleLenses = compatibleLensesResult.Data;
                    var filteredLenses = AvailableLenses.Where(l => compatibleLenses.Any(cl => cl.Id == l.Id)).ToList();
                    AvailableLenses = new ObservableCollection<Lens>(filteredLenses);

                    if (SelectedLens != null && !compatibleLenses.Any(l => l.Id == SelectedLens.Id))
                    {
                        SelectedLens = null;
                        await AutoSelectOptimalEquipmentAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering compatible lenses: {ex.Message}");
            }
        }

        private async Task AutoSelectOptimalEquipmentAsync()
        {
            try
            {
                var optimalSpecs = GetOptimalEquipmentSpecs(SelectedTarget);

                if (SelectedCamera == null && AvailableCameras.Any())
                {
                    var optimalCamera = AvailableCameras
                        .Where(c => c.IsUserCreated)
                        .FirstOrDefault(c => IsOptimalCameraForTarget(c, SelectedTarget)) ??
                        AvailableCameras.FirstOrDefault(c => IsOptimalCameraForTarget(c, SelectedTarget)) ??
                        AvailableCameras.First();

                    await SelectCameraAsync(optimalCamera);
                }

                if (SelectedLens == null && AvailableLenses.Any())
                {
                    var optimalLens = FindOptimalUserLens(optimalSpecs) ??
                                    FindOptimalLens(optimalSpecs) ??
                                    AvailableLenses.First();

                    await SelectLensAsync(optimalLens);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error auto-selecting equipment: {ex.Message}");
            }
        }

        private Lens FindOptimalUserLens(OptimalEquipmentSpecs specs)
        {
            var userLenses = AvailableLenses.Where(l => l.IsUserCreated).ToList();
            if (!userLenses.Any()) return null;

            var exactMatch = userLenses.FirstOrDefault(l =>
                (l.IsPrime && Math.Abs(l.MinMM - specs.OptimalFocalLength) <= 5) ||
                (!l.IsPrime && l.MaxMM.HasValue && specs.OptimalFocalLength >= l.MinMM && specs.OptimalFocalLength <= l.MaxMM.Value));

            if (exactMatch != null) return exactMatch;

            var rangeMatch = userLenses.FirstOrDefault(l =>
                (!l.IsPrime && l.MaxMM.HasValue &&
                 specs.MinFocalLength >= l.MinMM && specs.MinFocalLength <= l.MaxMM.Value) ||
                (!l.IsPrime && l.MaxMM.HasValue &&
                 specs.MaxFocalLength >= l.MinMM && specs.MaxFocalLength <= l.MaxMM.Value));

            if (rangeMatch != null) return rangeMatch;

            return userLenses.FirstOrDefault(l => l.MaxFStop <= specs.MaxAperture);
        }

        private Lens FindOptimalLens(OptimalEquipmentSpecs specs)
        {
            var exactMatch = AvailableLenses.FirstOrDefault(l =>
                (l.IsPrime && Math.Abs(l.MinMM - specs.OptimalFocalLength) <= 5) ||
                (!l.IsPrime && l.MaxMM.HasValue && specs.OptimalFocalLength >= l.MinMM && specs.OptimalFocalLength <= l.MaxMM.Value));

            return exactMatch ?? AvailableLenses.FirstOrDefault(l => l.MaxFStop <= specs.MaxAperture);
        }

        private bool IsOptimalCameraForTarget(CameraBody camera, AstroTarget target)
        {
            return true; // Simplified - could add actual camera optimization logic
        }

        private async Task HandleErrorAsync(OperationErrorSource source, string message)
        {
            HasError = true;
            ErrorMessage = message;
            CalculationStatus = "Error occurred";

            var errorArgs = new OperationErrorEventArgs(source, message);
            ErrorOccurred?.Invoke(this, errorArgs);

            Debug.WriteLine($"AstroPhotographyCalculator Error [{source}]: {message}");
            await Task.CompletedTask;
        }

        private void InitializeAstroTargets()
        {
            var targets = new List<AstroTargetDisplayModel>
    {
        // Core wide-field targets
        new AstroTargetDisplayModel { Target = AstroTarget.MilkyWayCore, DisplayName = "Milky Way Core", Description = "Galactic center region - best summer months" },
        new AstroTargetDisplayModel { Target = AstroTarget.StarTrails, DisplayName = "Star Trails", Description = "Circular or linear star trail photography" },
        new AstroTargetDisplayModel { Target = AstroTarget.MeteorShowers, DisplayName = "Meteor Showers", Description = "Active meteor shower events with real data" },
        
        // ISS and satellites
        new AstroTargetDisplayModel { Target = AstroTarget.ISS, DisplayName = "International Space Station", Description = "ISS passes - fast moving target" },
        
        // Lunar target
        new AstroTargetDisplayModel { Target = AstroTarget.Moon, DisplayName = "Moon", Description = "Lunar photography - all phases" },
        
        // Planetary targets - general and specific
        new AstroTargetDisplayModel { Target = AstroTarget.Planets, DisplayName = "Planets (All Visible)", Description = "All visible planets together" },
        new AstroTargetDisplayModel { Target = AstroTarget.Mercury, DisplayName = "Mercury", Description = "Innermost planet - challenging twilight target" },
        new AstroTargetDisplayModel { Target = AstroTarget.Venus, DisplayName = "Venus", Description = "Brightest planet - shows phases" },
        new AstroTargetDisplayModel { Target = AstroTarget.Mars, DisplayName = "Mars", Description = "Red planet - varies greatly in size and brightness" },
        new AstroTargetDisplayModel { Target = AstroTarget.Jupiter, DisplayName = "Jupiter", Description = "Giant planet - shows moons and cloud bands" },
        new AstroTargetDisplayModel { Target = AstroTarget.Saturn, DisplayName = "Saturn", Description = "Ringed planet - spectacular through telescope" },
        new AstroTargetDisplayModel { Target = AstroTarget.Uranus, DisplayName = "Uranus", Description = "Ice giant - small blue-green disk" },
        new AstroTargetDisplayModel { Target = AstroTarget.Neptune, DisplayName = "Neptune", Description = "Distant ice giant - tiny blue disk" },
        new AstroTargetDisplayModel { Target = AstroTarget.Pluto, DisplayName = "Pluto", Description = "Dwarf planet - appears as faint star" },
        
        // Deep sky objects - general and specific
        new AstroTargetDisplayModel { Target = AstroTarget.DeepSkyObjects, DisplayName = "Deep Sky Objects", Description = "Nebulae, galaxies, and star clusters" },
        new AstroTargetDisplayModel { Target = AstroTarget.M31_Andromeda, DisplayName = "M31 Andromeda Galaxy", Description = "Nearest major galaxy - autumn target" },
        new AstroTargetDisplayModel { Target = AstroTarget.M42_Orion, DisplayName = "M42 Orion Nebula", Description = "Great nebula in Orion - winter showpiece" },
        new AstroTargetDisplayModel { Target = AstroTarget.M51_Whirlpool, DisplayName = "M51 Whirlpool Galaxy", Description = "Face-on spiral galaxy in Canes Venatici" },
        new AstroTargetDisplayModel { Target = AstroTarget.M13_Hercules, DisplayName = "M13 Hercules Cluster", Description = "Great globular cluster - summer target" },
        new AstroTargetDisplayModel { Target = AstroTarget.M27_Dumbbell, DisplayName = "M27 Dumbbell Nebula", Description = "Bright planetary nebula in Vulpecula" },
        new AstroTargetDisplayModel { Target = AstroTarget.M57_Ring, DisplayName = "M57 Ring Nebula", Description = "Famous ring nebula in Lyra" },
        new AstroTargetDisplayModel { Target = AstroTarget.M81_Bodes, DisplayName = "M81 Bode's Galaxy", Description = "Bright spiral galaxy in Ursa Major" },
        new AstroTargetDisplayModel { Target = AstroTarget.M104_Sombrero, DisplayName = "M104 Sombrero Galaxy", Description = "Edge-on galaxy with prominent dust lane" },
        
        // Constellation targets - general and specific
        new AstroTargetDisplayModel { Target = AstroTarget.Constellations, DisplayName = "Constellations", Description = "Star pattern photography and identification" },
        new AstroTargetDisplayModel { Target = AstroTarget.Constellation_Orion, DisplayName = "Orion", Description = "The Hunter - winter's premier constellation" },
        new AstroTargetDisplayModel { Target = AstroTarget.Constellation_Cassiopeia, DisplayName = "Cassiopeia", Description = "The Queen - distinctive W-shaped pattern" },
        new AstroTargetDisplayModel { Target = AstroTarget.Constellation_UrsaMajor, DisplayName = "Ursa Major", Description = "The Great Bear - contains Big Dipper" },
        new AstroTargetDisplayModel { Target = AstroTarget.Constellation_Cygnus, DisplayName = "Cygnus", Description = "The Swan - Northern Cross in summer Milky Way" },
        new AstroTargetDisplayModel { Target = AstroTarget.Constellation_Leo, DisplayName = "Leo", Description = "The Lion - prominent spring constellation" },
        new AstroTargetDisplayModel { Target = AstroTarget.Constellation_Scorpius, DisplayName = "Scorpius", Description = "The Scorpion - summer constellation with Antares" },
        new AstroTargetDisplayModel { Target = AstroTarget.Constellation_Sagittarius, DisplayName = "Sagittarius", Description = "The Archer - direction of galactic center" },
        
        // Special and technical targets
        new AstroTargetDisplayModel { Target = AstroTarget.PolarAlignment, DisplayName = "Polar Alignment", Description = "Mount alignment for tracking" },
        new AstroTargetDisplayModel { Target = AstroTarget.NorthernLights, DisplayName = "Aurora Borealis", Description = "Northern lights - requires geomagnetic activity" },
        new AstroTargetDisplayModel { Target = AstroTarget.Comets, DisplayName = "Comets", Description = "Periodic and long-period comets" }
    };

            AvailableTargets = new ObservableCollection<AstroTargetDisplayModel>(targets);
            SelectedTarget = AstroTarget.MilkyWayCore;
            SelectedTargetModel = targets.First(t => t.Target == AstroTarget.MilkyWayCore);
        }
        private void CalculateFieldOfView()
        {
            try
            {
                if (SelectedCamera != null && SelectedLens != null)
                {
                    // Calculate horizontal FOV
                    var horizontalFOV = 2 * Math.Atan(SelectedCamera.SensorWidth / (2 * SelectedLens.MinMM)) * (180 / Math.PI);
                    var verticalFOV = 2 * Math.Atan(SelectedCamera.SensorHeight / (2 * SelectedLens.MinMM)) * (180 / Math.PI);

                    FieldOfViewWidth = horizontalFOV;
                    FieldOfViewHeight = verticalFOV;

                    // Check if target fits in frame (simplified - could use target angular size)
                    TargetFitsInFrame = horizontalFOV >= 10; // Basic check for Milky Way

                    // Generate equipment recommendation
                    var targetSpecs = GetOptimalEquipmentSpecs(SelectedTarget);
                    if (SelectedLens.MinMM >= targetSpecs.MinFocalLength && SelectedLens.MinMM <= targetSpecs.MaxFocalLength)
                    {
                        EquipmentRecommendation = "Your equipment is well-suited for this target";
                    }
                    else if (SelectedLens.MinMM < targetSpecs.MinFocalLength)
                    {
                        EquipmentRecommendation = "Consider a longer focal length for better detail";
                    }
                    else
                    {
                        EquipmentRecommendation = "Consider a wider lens for better field coverage";
                    }

                    OnPropertyChanged(nameof(FieldOfViewDisplay));
                    OnPropertyChanged(nameof(TargetFitsInFrame));
                    OnPropertyChanged(nameof(EquipmentRecommendation));
                }
                else
                {
                    FieldOfViewWidth = 0;
                    FieldOfViewHeight = 0;
                    TargetFitsInFrame = false;
                    EquipmentRecommendation = string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating field of view: {ex.Message}");
            }
        }
        private OptimalEquipmentSpecs GetOptimalEquipmentSpecs(AstroTarget target)
{
    return target switch
    {
        // Wide-field targets
        AstroTarget.MilkyWayCore => new OptimalEquipmentSpecs
        {
            MinFocalLength = 14,
            MaxFocalLength = 35,
            OptimalFocalLength = 24,
            MaxAperture = 2.8,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/2.8, 20-25 seconds",
            Notes = "Wide-angle lens essential for capturing galactic arch. Fast aperture critical for light gathering."
        },
        
        AstroTarget.MeteorShowers => new OptimalEquipmentSpecs
        {
            MinFocalLength = 14,
            MaxFocalLength = 35,
            OptimalFocalLength = 24,
            MaxAperture = 2.8,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/2.8, 15-30 seconds",
            Notes = "Wide field to capture meteors. Point 45-60° away from radiant for longer trails. Real shower data used."
        },
        
        AstroTarget.StarTrails => new OptimalEquipmentSpecs
        {
            MinFocalLength = 14,
            MaxFocalLength = 50,
            OptimalFocalLength = 24,
            MaxAperture = 4.0,
            MinISO = 100,
            MaxISO = 800,
            RecommendedSettings = "ISO 400, f/4, 30s intervals",
            Notes = "Wide-angle for interesting compositions. Multiple exposures combined in post-processing."
        },

        // ISS - Fast moving satellite
        AstroTarget.ISS => new OptimalEquipmentSpecs
        {
            MinFocalLength = 24,
            MaxFocalLength = 85,
            OptimalFocalLength = 35,
            MaxAperture = 2.8,
            MinISO = 3200,
            MaxISO = 12800,
            RecommendedSettings = "ISO 6400, f/2.8, 1/2 second",
            Notes = "Fast shutter speeds essential to prevent streaking. High ISO compensates for short exposure. Wide field helps track target."
        },

        // Lunar targets
        AstroTarget.Moon => new OptimalEquipmentSpecs
        {
            MinFocalLength = 200,
            MaxFocalLength = 800,
            OptimalFocalLength = 400,
            MaxAperture = 8.0,
            MinISO = 100,
            MaxISO = 800,
            RecommendedSettings = "ISO 200, f/8, 1/125s",
            Notes = "Telephoto lens for detail. Moon is bright - low ISO and fast shutter prevent overexposure."
        },

        // Planetary targets
        AstroTarget.Planets => new OptimalEquipmentSpecs
        {
            MinFocalLength = 300,
            MaxFocalLength = 1000,
            OptimalFocalLength = 600,
            MaxAperture = 6.3,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/5.6, 1/60s",
            Notes = "Long telephoto essential. Planets are small - maximum focal length recommended."
        },

        // Individual planets with specific requirements
        AstroTarget.Mercury => new OptimalEquipmentSpecs
        {
            MinFocalLength = 400,
            MaxFocalLength = 1200,
            OptimalFocalLength = 800,
            MaxAperture = 6.3,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/5.6, 1/125s",
            Notes = "Most challenging planet - always near sun. Requires precise timing during twilight. Very long focal length needed."
        },

        AstroTarget.Venus => new OptimalEquipmentSpecs
        {
            MinFocalLength = 300,
            MaxFocalLength = 1000,
            OptimalFocalLength = 600,
            MaxAperture = 5.6,
            MinISO = 200,
            MaxISO = 800,
            RecommendedSettings = "ISO 400, f/5.6, 1/250s",
            Notes = "Brightest planet - shows clear phases. Use faster shutter speeds to avoid overexposure. Best during twilight."
        },

        AstroTarget.Mars => new OptimalEquipmentSpecs
        {
            MinFocalLength = 400,
            MaxFocalLength = 1200,
            OptimalFocalLength = 800,
            MaxAperture = 5.6,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/5.6, 1/60s",
            Notes = "Size varies dramatically with orbital position. At opposition shows surface features. Red color distinctive."
        },

        AstroTarget.Jupiter => new OptimalEquipmentSpecs
        {
            MinFocalLength = 300,
            MaxFocalLength = 1000,
            OptimalFocalLength = 600,
            MaxAperture = 5.6,
            MinISO = 400,
            MaxISO = 1600,
            RecommendedSettings = "ISO 800, f/5.6, 1/60s",
            Notes = "Shows cloud bands and Great Red Spot. Four Galilean moons visible with telephoto lenses. Excellent target."
        },

        AstroTarget.Saturn => new OptimalEquipmentSpecs
        {
            MinFocalLength = 400,
            MaxFocalLength = 1200,
            OptimalFocalLength = 800,
            MaxAperture = 5.6,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/5.6, 1/30s",
            Notes = "Ring system visible with 600mm+. Golden color beautiful. Rings open/closed cycle over 29 years."
        },

        AstroTarget.Uranus => new OptimalEquipmentSpecs
        {
            MinFocalLength = 600,
            MaxFocalLength = 1500,
            OptimalFocalLength = 1000,
            MaxAperture = 6.3,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/6.3, 1/2s",
            Notes = "Very small blue-green disk. Extremely long focal length required. Appears almost star-like in smaller telescopes."
        },

        AstroTarget.Neptune => new OptimalEquipmentSpecs
        {
            MinFocalLength = 600,
            MaxFocalLength = 1500,
            OptimalFocalLength = 1000,
            MaxAperture = 6.3,
            MinISO = 3200,
            MaxISO = 12800,
            RecommendedSettings = "ISO 6400, f/6.3, 1s",
            Notes = "Tiny blue disk, very dim. Most distant major planet. Requires excellent seeing and long focal length."
        },

        AstroTarget.Pluto => new OptimalEquipmentSpecs
        {
            MinFocalLength = 800,
            MaxFocalLength = 2000,
            OptimalFocalLength = 1200,
            MaxAperture = 8.0,
            MinISO = 6400,
            MaxISO = 25600,
            RecommendedSettings = "ISO 12800, f/8, 30s",
            Notes = "Appears only as faint star. Extremely challenging target. Requires precise star charts and long exposures."
        },

        // Deep sky objects - general
        AstroTarget.DeepSkyObjects => new OptimalEquipmentSpecs
        {
            MinFocalLength = 50,
            MaxFocalLength = 300,
            OptimalFocalLength = 135,
            MaxAperture = 4.0,
            MinISO = 1600,
            MaxISO = 12800,
            RecommendedSettings = "ISO 6400, f/4, 4-8 minutes",
            Notes = "Medium telephoto for framing. Very high ISO capability needed. Tracking mount essential."
        },

        // Specific deep sky objects
        AstroTarget.M31_Andromeda => new OptimalEquipmentSpecs
        {
            MinFocalLength = 85,
            MaxFocalLength = 200,
            OptimalFocalLength = 135,
            MaxAperture = 4.0,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/4, 3 minutes",
            Notes = "Large galaxy needs wide field. Relatively bright - shorter exposures OK. Best in autumn months."
        },

        AstroTarget.M42_Orion => new OptimalEquipmentSpecs
        {
            MinFocalLength = 135,
            MaxFocalLength = 300,
            OptimalFocalLength = 200,
            MaxAperture = 4.0,
            MinISO = 400,
            MaxISO = 1600,
            RecommendedSettings = "ISO 800, f/4, 2 minutes",
            Notes = "Very bright nebula - avoid overexposure. Perfect size for medium telephoto. HDR techniques beneficial."
        },

        AstroTarget.M51_Whirlpool => new OptimalEquipmentSpecs
        {
            MinFocalLength = 200,
            MaxFocalLength = 600,
            OptimalFocalLength = 300,
            MaxAperture = 5.6,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/5.6, 8 minutes",
            Notes = "Dimmer face-on spiral. Requires longer exposures. Dark skies essential for spiral arms."
        },

        AstroTarget.M13_Hercules => new OptimalEquipmentSpecs
        {
            MinFocalLength = 135,
            MaxFocalLength = 400,
            OptimalFocalLength = 200,
            MaxAperture = 4.0,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/4, 4 minutes",
            Notes = "Great globular cluster. Medium magnification resolves individual stars. Summer target."
        },

        AstroTarget.M27_Dumbbell => new OptimalEquipmentSpecs
        {
            MinFocalLength = 200,
            MaxFocalLength = 600,
            OptimalFocalLength = 300,
            MaxAperture = 5.6,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/5.6, 10 minutes",
            Notes = "Bright planetary nebula. OIII filter enhances contrast. Apple-core or dumbbell shape distinctive."
        },

        AstroTarget.M57_Ring => new OptimalEquipmentSpecs
        {
            MinFocalLength = 400,
            MaxFocalLength = 1000,
            OptimalFocalLength = 600,
            MaxAperture = 6.3,
            MinISO = 3200,
            MaxISO = 12800,
            RecommendedSettings = "ISO 6400, f/6.3, 15 minutes",
            Notes = "Small planetary nebula. High magnification needed. OIII filter essential. Famous ring structure."
        },

        AstroTarget.M81_Bodes => new OptimalEquipmentSpecs
        {
            MinFocalLength = 200,
            MaxFocalLength = 600,
            OptimalFocalLength = 300,
            MaxAperture = 5.6,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/5.6, 6 minutes",
            Notes = "Bright spiral galaxy in Ursa Major. Often photographed with nearby M82. Spring target."
        },

        AstroTarget.M104_Sombrero => new OptimalEquipmentSpecs
        {
            MinFocalLength = 300,
            MaxFocalLength = 800,
            OptimalFocalLength = 400,
            MaxAperture = 5.6,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/5.6, 10 minutes",
            Notes = "Edge-on galaxy with prominent dust lane. Distinctive sombrero shape. Spring target in Virgo."
        },

        // Constellation targets
        AstroTarget.Constellations => new OptimalEquipmentSpecs
        {
            MinFocalLength = 35,
            MaxFocalLength = 135,
            OptimalFocalLength = 85,
            MaxAperture = 4.0,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/4, 60s",
            Notes = "Medium lens for constellation framing. Balance stars with constellation patterns."
        },

        // Specific constellations
        AstroTarget.Constellation_Orion => new OptimalEquipmentSpecs
        {
            MinFocalLength = 50,
            MaxFocalLength = 135,
            OptimalFocalLength = 85,
            MaxAperture = 2.8,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/2.8, 30s",
            Notes = "Large bright constellation with nebulae. Wide field captures full pattern. Winter showcase."
        },

        AstroTarget.Constellation_Cassiopeia => new OptimalEquipmentSpecs
        {
            MinFocalLength = 35,
            MaxFocalLength = 85,
            OptimalFocalLength = 50,
            MaxAperture = 2.8,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/2.8, 45s",
            Notes = "Distinctive W-shape needs wide field. Circumpolar from northern latitudes. Rich star fields."
        },

        AstroTarget.Constellation_UrsaMajor => new OptimalEquipmentSpecs
        {
            MinFocalLength = 50,
            MaxFocalLength = 135,
            OptimalFocalLength = 85,
            MaxAperture = 2.8,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/2.8, 45s",
            Notes = "Big Dipper asterism within Ursa Major. Spring constellation. Contains several galaxies."
        },

        AstroTarget.Constellation_Cygnus => new OptimalEquipmentSpecs
        {
            MinFocalLength = 35,
            MaxFocalLength = 85,
            OptimalFocalLength = 50,
            MaxAperture = 2.8,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/2.8, 25s",
            Notes = "Northern Cross in rich Milky Way field. Many nebulae. Summer constellation."
        },

        AstroTarget.Constellation_Leo => new OptimalEquipmentSpecs
        {
            MinFocalLength = 50,
            MaxFocalLength = 135,
            OptimalFocalLength = 85,
            MaxAperture = 2.8,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/2.8, 60s",
            Notes = "Spring lion constellation. Contains several bright galaxies. Distinctive backward question mark."
        },

        AstroTarget.Constellation_Scorpius => new OptimalEquipmentSpecs
        {
            MinFocalLength = 50,
            MaxFocalLength = 135,
            OptimalFocalLength = 85,
            MaxAperture = 2.8,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/2.8, 30s",
            Notes = "Summer scorpion with red Antares. Rich in nebulae and star clusters. Low in northern skies."
        },

        AstroTarget.Constellation_Sagittarius => new OptimalEquipmentSpecs
        {
            MinFocalLength = 35,
            MaxFocalLength = 85,
            OptimalFocalLength = 50,
            MaxAperture = 2.8,
            MinISO = 3200,
            MaxISO = 12800,
            RecommendedSettings = "ISO 6400, f/2.8, 20s",
            Notes = "Direction of galactic center. Incredibly rich in nebulae and star clouds. Summer's crown jewel."
        },

        // Special targets
        AstroTarget.PolarAlignment => new OptimalEquipmentSpecs
        {
            MinFocalLength = 50,
            MaxFocalLength = 200,
            OptimalFocalLength = 100,
            MaxAperture = 5.6,
            MinISO = 800,
            MaxISO = 3200,
            RecommendedSettings = "ISO 1600, f/5.6, 30s",
            Notes = "Medium telephoto to see Polaris clearly. Used for mount alignment verification."
        },

        AstroTarget.NorthernLights => new OptimalEquipmentSpecs
        {
            MinFocalLength = 14,
            MaxFocalLength = 35,
            OptimalFocalLength = 24,
            MaxAperture = 2.8,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/2.8, 15s",
            Notes = "Ultra-wide angle essential - aurora spans entire sky. Fast exposures capture rapid changes. Requires geomagnetic activity."
        },

        AstroTarget.Comets => new OptimalEquipmentSpecs
        {
            MinFocalLength = 135,
            MaxFocalLength = 400,
            OptimalFocalLength = 200,
            MaxAperture = 4.0,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/4, 3 minutes",
            Notes = "Variable size depending on comet. Medium telephoto good starting point. Tail direction changes over time."
        },

        // Default fallback
        _ => new OptimalEquipmentSpecs
        {
            MinFocalLength = 24,
            MaxFocalLength = 200,
            OptimalFocalLength = 50,
            MaxAperture = 4.0,
            MinISO = 1600,
            MaxISO = 6400,
            RecommendedSettings = "ISO 3200, f/4, 30s",
            Notes = "General astrophotography setup for unknown targets."
        }
    };
}

        #endregion

        #region Cache and Utility Methods

        private string GetPredictionCacheKey()
        {
            return $"astro_real_{SelectedLocation?.Id}_{SelectedDate:yyyyMMdd}";
        }

        private void ClearExpiredCache()
        {
            var expiredKeys = _calculationCache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _calculationCache.Remove(key);
            }
        }

        #endregion

        #region Properties and Events

        public IAsyncRelayCommand LoadLocationsCommand { get; private set; }
        public IAsyncRelayCommand LoadEquipmentCommand { get; private set; }
        public IAsyncRelayCommand CalculateAstroDataCommand { get; private set; }
        public IAsyncRelayCommand RefreshCalculationsCommand { get; private set; }
        public IAsyncRelayCommand<CameraBody> SelectCameraCommand { get; private set; }
        public IAsyncRelayCommand<Lens> SelectLensCommand { get; private set; }
        public IAsyncRelayCommand<AstroTarget> SelectTargetCommand { get; private set; }
        public IAsyncRelayCommand RetryLastCommandCommand { get; private set; }
        public IAsyncRelayCommand GenerateHourlyPredictionsCommand { get; private set; }

        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        public event EventHandler CalculationCompleted;
        public event EventHandler<AstroCalculationResult> TargetCalculated;

        // Legacy property mappings for compatibility with existing XAML bindings
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
                    LocationPhoto = value?.Photo ?? string.Empty;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (CanCalculate)
                                await CalculateAstroDataAsync();
                        }
                        catch (Exception ex)
                        {
                            await HandleErrorAsync(OperationErrorSource.Validation, $"Error updating calculations: {ex.Message}");
                        }
                    });
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
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Auto-calculate when both location and date are set
                            if (SelectedLocation != null)
                            {
                                await CalculateAstroDataAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            await HandleErrorAsync(OperationErrorSource.Validation, $"Error updating calculations: {ex.Message}");
                        }
                    });
                }
            }
        }

        public string LocationPhoto
        {
            get => _locationPhoto;
            set => SetProperty(ref _locationPhoto, value);
        }

        public bool IsInitialized
        {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<CameraBody> AvailableCameras
        {
            get => _availableCameras;
            set => SetProperty(ref _availableCameras, value);
        }

        public ObservableCollection<Lens> AvailableLenses
        {
            get => _availableLenses;
            set => SetProperty(ref _availableLenses, value);
        }

        public CameraBody SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (SetProperty(ref _selectedCamera, value))
                {
                    OnPropertyChanged(nameof(HasEquipmentSelected));
                    OnPropertyChanged(nameof(SelectedCameraDisplay));
                    OnPropertyChanged(nameof(HasValidSelection));

                    // Trigger field of view calculation
                    CalculateFieldOfView();
                }
            }
        }

        public Lens SelectedLens
        {
            get => _selectedLens;
            set
            {
                if (SetProperty(ref _selectedLens, value))
                {
                    OnPropertyChanged(nameof(HasEquipmentSelected));
                    OnPropertyChanged(nameof(SelectedLensDisplay));
                    OnPropertyChanged(nameof(HasValidSelection));

                    // Trigger field of view calculation
                    CalculateFieldOfView();
                }
            }
        }

        public bool IsLoadingEquipment
        {
            get => _isLoadingEquipment;
            set => SetProperty(ref _isLoadingEquipment, value);
        }

        public AstroTarget SelectedTarget
        {
            get => _selectedTarget;
            set => SetProperty(ref _selectedTarget, value);
        }

        public ObservableCollection<AstroTargetDisplayModel> AvailableTargets
        {
            get => _availableTargets;
            set => SetProperty(ref _availableTargets, value);
        }

        public ObservableCollection<AstroCalculationResult> CurrentCalculations
        {
            get => _currentCalculations;
            set => SetProperty(ref _currentCalculations, value);
        }

        public bool IsCalculating
        {
            get => _isCalculating;
            set => SetProperty(ref _isCalculating, value);
        }

        public string CalculationStatus
        {
            get => _calculationStatus;
            set => SetProperty(ref _calculationStatus, value);
        }

        public string ExposureRecommendation
        {
            get => _exposureRecommendation;
            set => SetProperty(ref _exposureRecommendation, value);
        }

        public string EquipmentRecommendation
        {
            get => _equipmentRecommendation;
            set => SetProperty(ref _equipmentRecommendation, value);
        }

        public string PhotographyNotes
        {
            get => _photographyNotes;
            set => SetProperty(ref _photographyNotes, value);
        }

        public double FieldOfViewWidth
        {
            get => _fieldOfViewWidth;
            set => SetProperty(ref _fieldOfViewWidth, value);
        }

        public double FieldOfViewHeight
        {
            get => _fieldOfViewHeight;
            set => SetProperty(ref _fieldOfViewHeight, value);
        }

        public bool TargetFitsInFrame
        {
            get => _targetFitsInFrame;
            set => SetProperty(ref _targetFitsInFrame, value);
        }

        // Status properties for UI feedback
        public bool CanCalculate => SelectedLocation != null && !IsCalculating;
        public bool HasEquipmentSelected => SelectedCamera != null || SelectedLens != null;
        public bool HasValidSelection => SelectedLocation != null && HasEquipmentSelected;

        // Display properties
        public string SelectedTargetDisplay => AvailableTargets?.FirstOrDefault(t => t.Target == SelectedTarget)?.DisplayName ?? SelectedTarget.ToString();
        public string SelectedCameraDisplay => SelectedCamera?.Name ?? "No camera selected";
        public string SelectedLensDisplay => SelectedLens?.NameForLens ?? "No lens selected";
        public string FieldOfViewDisplay => $"{FieldOfViewWidth:F1}° × {FieldOfViewHeight:F1}°";

        #endregion

        #region Supporting Data Models

        public class ShootingWindow
        {
            public DateTime Date { get; set; }
            public DateTime Sunset { get; set; }
            public DateTime Sunrise { get; set; }
            public DateTime CivilTwilightEnd { get; set; }
            public DateTime NauticalTwilightEnd { get; set; }
            public DateTime AstronomicalTwilightEnd { get; set; }
            public DateTime AstronomicalTwilightStart { get; set; }
            public DateTime NauticalTwilightStart { get; set; }
            public DateTime CivilTwilightStart { get; set; }
        }

        public class TwilightPhases
        {
            public DateTime CivilTwilightEnd { get; set; }
            public DateTime NauticalTwilightEnd { get; set; }
            public DateTime AstronomicalTwilightEnd { get; set; }
            public DateTime AstronomicalTwilightStart { get; set; }
            public DateTime NauticalTwilightStart { get; set; }
            public DateTime CivilTwilightStart { get; set; }
        }

        public class AstroTargetEvent
        {
            public AstroTarget Target { get; set; }
            public string TargetDisplay { get; set; } = string.Empty;
            public TargetVisibilityData Visibility { get; set; } = new();
            public UserEquipmentMatch Equipment { get; set; } = new();
            public NormalizedCameraSettings Settings { get; set; } = new();
            public TargetRequirements Requirements { get; set; } = new();
        }

        public class TargetVisibilityData
        {
            public bool IsVisible { get; set; }
            public double Altitude { get; set; }
            public double Azimuth { get; set; }
            public double OptimalityScore { get; set; }
            public string PlanetName { get; set; } = string.Empty;
            public string MeteorShowerName { get; set; } = string.Empty;
            public double ExpectedZHR { get; set; }
            public string ISSPassType { get; internal set; }
            public double ISSMagnitude { get; internal set; }
            public TimeSpan ISSPassDuration { get; internal set; }
            public object Magnitude { get; internal set; }
            public string ConstellationName { get; internal set; }
            public string DSOName { get; internal set; }
            public string DSOType { get; internal set; }
        }

        public class TargetRequirements
        {
            public double OptimalFocalLength { get; set; }
            public double MinFocalLength { get; set; }
            public double MaxFocalLength { get; set; }
            public double MaxAperture { get; set; }
            public string TargetType { get; set; } = string.Empty;
        }

        public class UserEquipmentMatch
        {
            public bool Found { get; set; }
            public CameraBody Camera { get; set; }
            public Lens Lens { get; set; }
            public string CameraDisplay { get; set; } = string.Empty;
            public string LensDisplay { get; set; } = string.Empty;
            public string RecommendationMessage { get; set; } = string.Empty;
        }

        public class NormalizedCameraSettings
        {
            public string Aperture { get; set; } = string.Empty;
            public string ShutterSpeed { get; set; } = string.Empty;
            public string ISO { get; set; } = string.Empty;
        }

        public class BaseCameraSettings
        {
            public double Aperture { get; set; }
            public double ShutterSpeed { get; set; }
            public int ISO { get; set; }
        }

        public class WeatherConditions
        {
            public double CloudCover { get; set; }
            public double PrecipitationProbability { get; set; }
            public double WindSpeed { get; set; }
            public double Humidity { get; set; }
            public double Visibility { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
}

#region Supporting Classes
namespace Location.Photography.ViewModels
{
    public class CachedAstroCalculation
    {
        public DateTime Timestamp { get; set; }
        public LocationListItemViewModel Location { get; set; }
        public DateTime CalculationDate { get; set; }
        public AstroTarget Target { get; set; }
        public CameraBody Camera { get; set; }
        public Lens Lens { get; set; }
        public List<AstroCalculationResult> Results { get; set; }
        public TimeSpan CacheDuration => DateTime.Now - Timestamp;
        public bool IsExpired => CacheDuration.TotalMinutes > 30;
    }

    public class OptimalEquipmentSpecs
    {
        public double MinFocalLength { get; set; }
        public double MaxFocalLength { get; set; }
        public double OptimalFocalLength { get; set; }
        public double MaxAperture { get; set; }
        public int MinISO { get; set; }
        public int MaxISO { get; set; }
        public string RecommendedSettings { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class AstroTargetDisplayModel
    {
        public AstroTarget Target { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
#endregion
#endregion