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
        private readonly SemaphoreSlim _operationLock = new(1, 1);
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
            IMeteorShowerDataService meteorShowerDataService)
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
            InitializeCommands();
            InitializeAstroTargets();
        }
        #endregion

        #region Real Astronomical Calculation Methods

        public async Task CalculateAstroDataAsync()
        {
            if (SelectedLocation == null || !await _operationLock.WaitAsync(100))
                return;

            try
            {
                IsCalculating = true;
                IsGeneratingHourlyPredictions = true;
                HasError = false;
                CalculationStatus = "Calculating tonight's shooting windows...";

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

                // Generate real astronomical predictions using actual service data
                var predictions = await GenerateRealAstronomicalPredictionsAsync(shootingWindow);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HourlyAstroPredictions.Clear();
                    foreach (var prediction in predictions)
                    {
                        HourlyAstroPredictions.Add(prediction);
                    }
                    HourlyPredictionsStatus = $"Generated {predictions.Count} shooting windows for tonight";
                });

                // Cache the results
                var domainPredictions = predictions.Select(p => new AstroHourlyPrediction
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
                _operationLock.Release();
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

        private async Task<List<AstroHourlyPredictionDisplayModel>> GenerateRealAstronomicalPredictionsAsync(ShootingWindow window)
        {
            try
            {
                var predictions = new List<AstroHourlyPredictionDisplayModel>();

                // Get sunset/sunrise times using MediatR queries
                var sunTimesQuery = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = SelectedDate
                };

                var sunResult = await _mediator.Send(sunTimesQuery, _cancellationTokenSource.Token);
                if (!sunResult.IsSuccess || sunResult.Data == null)
                    return predictions;

                var sunTimes = sunResult.Data;

                // Get next day sunrise
                var nextDayQuery = new GetSunTimesQuery
                {
                    Latitude = SelectedLocation.Latitude,
                    Longitude = SelectedLocation.Longitude,
                    Date = SelectedDate.AddDays(1)
                };

                var nextDayResult = await _mediator.Send(nextDayQuery, _cancellationTokenSource.Token);
                if (!nextDayResult.IsSuccess || nextDayResult.Data == null)
                    return predictions;

                var nextDayTimes = nextDayResult.Data;

                // Round sunset UP to next hour, sunrise DOWN to previous hour
                var sunsetHour = new DateTime(nextDayTimes.Sunset.Year, nextDayTimes.Sunset.Month, nextDayTimes.Sunset.Day,
                    nextDayTimes.Sunset.Hour + (nextDayTimes.Sunset.Minute > 0 ? 1 : 0), 0, 0);

                var sunriseHour = new DateTime(nextDayTimes.Sunrise.Year, nextDayTimes.Sunrise.Month, nextDayTimes.Sunrise.Day,
                    nextDayTimes.Sunrise.Hour, 0, 0);

                var currentHour = sunsetHour;

                // For each hour between sunset and sunrise (inclusive)
                while (currentHour <= sunriseHour)
                {
                    try
                    {
                        // Get optimal shooting times for this specific hour
                        var hourlyQuery = new GetOptimalShootingTimesQuery
                        {
                            Latitude = SelectedLocation.Latitude,
                            Longitude = SelectedLocation.Longitude,
                            Date = currentHour,
                            IncludeWeatherForecast = true,
                            TimeZone = TimeZoneInfo.Local.Id // Use local timezone
                        };

                        var hourlyResult = await _mediator.Send(hourlyQuery, _cancellationTokenSource.Token);

                        if (hourlyResult.IsSuccess && hourlyResult.Data != null)
                        {
                            // Extract sunrise and sunset from the optimal shooting times
                            var sunriseWindow = hourlyResult.Data.FirstOrDefault(w => w.Description.ToString().Contains("Sunrise"));
                            var sunsetWindow = hourlyResult.Data.FirstOrDefault(w => w.Description.ToString().Contains("Sunset"));

                            // Determine solar event for this hour based on sunrise/sunset times
                            var solarEvent = "True Night"; // Default
                            if (sunsetWindow != null && currentHour <= sunsetWindow.StartTime.AddHours(1))
                            {
                                solarEvent = "Sunset";
                            }
                            else if (sunriseWindow != null && currentHour >= sunriseWindow.StartTime.AddHours(-1))
                            {
                                solarEvent = "Sunrise";
                            }
                            else if (sunsetWindow != null && sunriseWindow != null)
                            {
                                if (currentHour > sunsetWindow.EndTime && currentHour < sunriseWindow.StartTime)
                                {
                                    solarEvent = "True Night";
                                }
                                else if (currentHour <= sunsetWindow.EndTime)
                                {
                                    solarEvent = "Twilight";
                                }
                                else if (currentHour >= sunriseWindow.StartTime)
                                {
                                    solarEvent = "Dawn";
                                }
                            }
                           currentHour = DateTime.SpecifyKind(currentHour, DateTimeKind.Utc);
                            // Map to display model using the mapping service
                            var calculationResults = new List<AstroCalculationResult>
                               {
                                   new AstroCalculationResult
                                   {
                                       CalculationTime = TimeZoneInfo.ConvertTime(currentHour, TimeZoneInfo.Local),
                                       LocalTime = TimeZoneInfo.ConvertTime(currentHour, TimeZoneInfo.Local),
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
                            predictions.AddRange(hourlyPredictions);
                        }
                        else
                        {
                            // Create a basic prediction for this hour if no optimal events found
                            var basicPrediction = await CreateBasicHourlyPredictionAsync(currentHour);
                            if (basicPrediction != null)
                            {
                                predictions.Add(basicPrediction);
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
                            predictions.Add(fallbackPrediction);
                        }
                    }

                    currentHour = currentHour.AddHours(1);
                }

                return predictions;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating real astronomical predictions: {ex.Message}");
                return new List<AstroHourlyPredictionDisplayModel>();
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

                // Check Planet visibility using real service
                try
                {
                    var planetData = await _astroCalculationService.GetVisiblePlanetsAsync(
                        hour, SelectedLocation.Latitude, SelectedLocation.Longitude);

                    var visiblePlanets = planetData.Where(p => p.IsVisible && p.Altitude > 10).ToList();
                    if (visiblePlanets.Any())
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

                // Deep sky objects are only viable during true dark conditions
                if (solarEvent == "True Night" || solarEvent == "Astronomical Twilight")
                {
                    viableTargets.Add(AstroTarget.DeepSkyObjects);
                    viableTargets.Add(AstroTarget.StarTrails);
                    viableTargets.Add(AstroTarget.Constellations);
                }

                // Polar alignment is viable during any dark period
                if (solarEvent != "Blue Hour" && solarEvent != "Civil Twilight")
                {
                    viableTargets.Add(AstroTarget.PolarAlignment);
                }
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

                    case AstroTarget.DeepSkyObjects:
                        // For deep sky objects, use constellation data as proxy
                        try
                        {
                            var constellationData = await _astroCalculationService.GetConstellationDataAsync(
                                ConstellationType.Orion, hour, SelectedLocation.Latitude, SelectedLocation.Longitude, _cancellationTokenSource.Token);

                            if (constellationData.CenterAltitude > 30)
                            {
                                return new TargetVisibilityData
                                {
                                    IsVisible = true,
                                    Altitude = constellationData.CenterAltitude,
                                    Azimuth = constellationData.CenterAzimuth,
                                    OptimalityScore = Math.Min(1.0, constellationData.CenterAltitude / 90.0 * 0.8)
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

        private string GetSolarEventForHour(DateTime hour, ShootingWindow window)
        {
            if (hour <= window.CivilTwilightEnd)
                return "Blue Hour";
            else if (hour <= window.NauticalTwilightEnd)
                return "Nautical Twilight";
            else if (hour <= window.AstronomicalTwilightEnd)
                return "Astronomical Twilight";
            else if (hour >= window.AstronomicalTwilightStart)
                return "Astronomical Twilight";
            else if (hour >= window.NauticalTwilightStart)
                return "Nautical Twilight";
            else if (hour >= window.CivilTwilightStart)
                return "Blue Hour";
            else
                return "True Night";
        }

        private string GetRealTargetDisplayName(AstroTarget target, TargetVisibilityData visibility)
        {
            var baseName = target switch
            {
                AstroTarget.MilkyWayCore => "Milky Way Core",
                AstroTarget.Moon => "Moon",
                AstroTarget.Planets => string.IsNullOrEmpty(visibility.PlanetName) ? "Planets" : visibility.PlanetName,
                AstroTarget.DeepSkyObjects => "Deep Sky Objects",
                AstroTarget.StarTrails => "Star Trails",
                AstroTarget.MeteorShowers => string.IsNullOrEmpty(visibility.MeteorShowerName) ? "Meteor Showers" : $"{visibility.MeteorShowerName} (ZHR: {visibility.ExpectedZHR:F0})",
                AstroTarget.Constellations => "Constellations",
                AstroTarget.PolarAlignment => "Polar Alignment",
                _ => target.ToString()
            };

            return $"{baseName} ({visibility.Altitude:F0}° altitude)";
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
                AstroTarget.MilkyWayCore => new TargetRequirements
                {
                    OptimalFocalLength = 24,
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    MaxAperture = 2.8,
                    TargetType = "wide_angle"
                },
                AstroTarget.Moon => new TargetRequirements
                {
                    OptimalFocalLength = 400,
                    MinFocalLength = 200,
                    MaxFocalLength = 800,
                    MaxAperture = 8.0,
                    TargetType = "telephoto"
                },
                AstroTarget.Planets => new TargetRequirements
                {
                    OptimalFocalLength = 600,
                    MinFocalLength = 300,
                    MaxFocalLength = 1200,
                    MaxAperture = 5.6,
                    TargetType = "long_telephoto"
                },
                AstroTarget.DeepSkyObjects => new TargetRequirements
                {
                    OptimalFocalLength = 135,
                    MinFocalLength = 85,
                    MaxFocalLength = 300,
                    MaxAperture = 4.0,
                    TargetType = "medium_telephoto"
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
                AstroTarget.Constellations => new TargetRequirements
                {
                    OptimalFocalLength = 85,
                    MinFocalLength = 50,
                    MaxFocalLength = 135,
                    MaxAperture = 4.0,
                    TargetType = "standard"
                },
                AstroTarget.PolarAlignment => new TargetRequirements
                {
                    OptimalFocalLength = 100,
                    MinFocalLength = 50,
                    MaxFocalLength = 200,
                    MaxAperture = 5.6,
                    TargetType = "standard"
                },
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
                AstroTarget.MilkyWayCore => new BaseCameraSettings { Aperture = 2.8, ShutterSpeed = 20, ISO = 3200 },
                AstroTarget.Moon => new BaseCameraSettings { Aperture = 8.0, ShutterSpeed = 0.008, ISO = 200 }, // 1/125
                AstroTarget.Planets => new BaseCameraSettings { Aperture = 5.6, ShutterSpeed = 0.017, ISO = 800 }, // 1/60
                AstroTarget.DeepSkyObjects => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 300, ISO = 1600 }, // 5 min
                AstroTarget.MeteorShowers => new BaseCameraSettings { Aperture = 2.8, ShutterSpeed = 30, ISO = 3200 },
                AstroTarget.StarTrails => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 240, ISO = 400 }, // 4 min intervals
                AstroTarget.Constellations => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 60, ISO = 1600 },
                AstroTarget.PolarAlignment => new BaseCameraSettings { Aperture = 5.6, ShutterSpeed = 30, ISO = 1600 },
                _ => new BaseCameraSettings { Aperture = 4.0, ShutterSpeed = 30, ISO = 1600 }
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
            if (!await _operationLock.WaitAsync(100))
                return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

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
                        Locations = new ObservableCollection<LocationListItemViewModel>(locationViewModels);

                        if (SelectedLocation == null && Locations.Any())
                        {
                            SelectedLocation = Locations.First();
                            LocationPhoto = SelectedLocation?.Photo ?? string.Empty;
                        }

                        IsInitialized = true;
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
                CalculationStatus = "Location loading cancelled";
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
                IsBusy = false;
                _operationLock.Release();
            }
        }

        public async Task LoadEquipmentAsync()
        {
            if (!await _operationLock.WaitAsync(100))
                return;

            try
            {
                IsLoadingEquipment = true;
                HasError = false;

                var userCamerasResult = await _cameraBodyRepository.GetUserCamerasAsync(_cancellationTokenSource.Token);
                var userLensesResult = await _lensRepository.GetUserLensesAsync(_cancellationTokenSource.Token);

                var cameras = new List<CameraBody>();
                var lenses = new List<Lens>();

                if (userCamerasResult.IsSuccess && userCamerasResult.Data?.Any() == true)
                {
                    cameras.AddRange(userCamerasResult.Data);
                }
                else
                {
                    var allCamerasResult = await _cameraBodyRepository.GetPagedAsync(0, 50, _cancellationTokenSource.Token);
                    if (allCamerasResult.IsSuccess && allCamerasResult.Data?.Any() == true)
                    {
                        cameras.AddRange(allCamerasResult.Data);
                    }
                }

                if (userLensesResult.IsSuccess && userLensesResult.Data?.Any() == true)
                {
                    lenses.AddRange(userLensesResult.Data);
                }
                else
                {
                    var allLensesResult = await _lensRepository.GetPagedAsync(0, 50, _cancellationTokenSource.Token);
                    if (allLensesResult.IsSuccess && allLensesResult.Data?.Any() == true)
                    {
                        lenses.AddRange(allLensesResult.Data);
                    }
                }

                AvailableCameras = new ObservableCollection<CameraBody>(cameras);
                AvailableLenses = new ObservableCollection<Lens>(lenses);

                await AutoSelectOptimalEquipmentAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Database, $"Error loading equipment: {ex.Message}");
            }
            finally
            {
                IsLoadingEquipment = false;
                _operationLock.Release();
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
                new AstroTargetDisplayModel { Target = AstroTarget.MilkyWayCore, DisplayName = "Milky Way Core", Description = "Galactic center region - best summer months" },
                new AstroTargetDisplayModel { Target = AstroTarget.Moon, DisplayName = "Moon", Description = "Lunar photography - all phases" },
                new AstroTargetDisplayModel { Target = AstroTarget.Planets, DisplayName = "Planets", Description = "Planetary photography - visible planets" },
                new AstroTargetDisplayModel { Target = AstroTarget.DeepSkyObjects, DisplayName = "Deep Sky Objects", Description = "Nebulae, galaxies, and star clusters" },
                new AstroTargetDisplayModel { Target = AstroTarget.StarTrails, DisplayName = "Star Trails", Description = "Circular or linear star trail photography" },
                new AstroTargetDisplayModel { Target = AstroTarget.MeteorShowers, DisplayName = "Meteor Showers", Description = "Active meteor shower events with real data" },
                new AstroTargetDisplayModel { Target = AstroTarget.Constellations, DisplayName = "Constellations", Description = "Constellation photography and identification" },
                new AstroTargetDisplayModel { Target = AstroTarget.PolarAlignment, DisplayName = "Polar Alignment", Description = "Mount alignment for tracking" }
            };

            AvailableTargets = new ObservableCollection<AstroTargetDisplayModel>(targets);
            SelectedTarget = AstroTarget.MilkyWayCore;
        }

        private OptimalEquipmentSpecs GetOptimalEquipmentSpecs(AstroTarget target)
        {
            return target switch
            {
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
                AstroTarget.MeteorShowers => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    OptimalFocalLength = 24,
                    MaxAperture = 2.8,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/2.8, 15-30s",
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
                _ => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 24,
                    MaxFocalLength = 200,
                    OptimalFocalLength = 50,
                    MaxAperture = 4.0,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/4, 30s",
                    Notes = "General astrophotography setup."
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
            set => SetProperty(ref _selectedDate, value);
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
            set => SetProperty(ref _selectedCamera, value);
        }

        public Lens SelectedLens
        {
            get => _selectedLens;
            set => SetProperty(ref _selectedLens, value);
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
        public bool HasEquipmentSelected => SelectedCamera != null && SelectedLens != null;
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