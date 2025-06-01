using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using MediatR;
using System.Collections.ObjectModel;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class SunLocationViewModel : ViewModelBase, Location.Photography.ViewModels.Interfaces.ISunLocation
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly ITimezoneService _timezoneService;

        // PERFORMANCE: Cancellation and threading
        private CancellationTokenSource _cancellationTokenSource = new();
        private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private const int UPDATE_THROTTLE_MS = 500;

        // Core properties
        private ObservableCollection<LocationViewModel> _locations;
        private DateTime _selectedDate;
        private TimeSpan _selectedTime;
        private DateTime _selectedDateTime;
        private double _latitude;
        private double _longitude;
        private double _northRotationAngle;
        private double _sunDirection;
        private double _sunElevation;
        private double _deviceTilt;
        private bool _elevationMatched;
        private bool _beginMonitoring;
        private string _errorMessage;
        private ObservableCollection<TimelineEventViewModel> _timelineEvents;
        private string _weatherSummary = string.Empty;
        private double _lightReduction;
        private double _colorTemperature;
        private string _lightQuality = string.Empty;
        private double _currentEV;
        private double _nextHourEV;
        private string _recommendedSettings = string.Empty;
        private string _lightQualityDescription = string.Empty;
        private string _recommendations = string.Empty;
        private string _nextOptimalTime = string.Empty;

        [ObservableProperty]
        private string _locationPhoto = string.Empty;

        // PERFORMANCE: Cache for timeline calculations
        private readonly Dictionary<string, List<TimelineEventViewModel>> _timelineCache = new();
        private string GetTimelineCacheKey() => $"{_latitude:F4}_{_longitude:F4}_{_selectedDate:yyyyMMdd}";
        #endregion

        #region Properties
        public ObservableCollection<TimelineEventViewModel> TimelineEvents
        {
            get => _timelineEvents;
            set => SetProperty(ref _timelineEvents, value);
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

        public IRelayCommand<TimelineEventViewModel> SelectTimelineEventCommand { get; private set; }
        public ObservableCollection<LocationViewModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    _ = UpdateSelectedDateTimeOptimizedAsync();
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
                    _ = UpdateSelectedDateTimeOptimizedAsync();
                }
            }
        }

        public DateTime SelectedDateTime
        {
            get => _selectedDateTime;
            set
            {
                if (SetProperty(ref _selectedDateTime, value))
                {
                    _selectedDate = value.Date;
                    _selectedTime = value.TimeOfDay;
                    OnPropertyChanged(nameof(SelectedDate));
                    OnPropertyChanged(nameof(SelectedTime));
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
                    _ = UpdateSunPositionOptimizedAsync();
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
                    _ = UpdateSunPositionOptimizedAsync();
                }
            }
        }

        public double NorthRotationAngle
        {
            get => _northRotationAngle;
            set => SetProperty(ref _northRotationAngle, value);
        }

        public double SunDirection
        {
            get => _sunDirection;
            set => SetProperty(ref _sunDirection, value);
        }

        public double SunElevation
        {
            get => _sunElevation;
            set => SetProperty(ref _sunElevation, value);
        }

        public double DeviceTilt
        {
            get => _deviceTilt;
            set
            {
                if (SetProperty(ref _deviceTilt, value))
                {
                    UpdateElevationMatch();
                }
            }
        }

        public bool ElevationMatched
        {
            get => _elevationMatched;
            set => SetProperty(ref _elevationMatched, value);
        }

        public bool BeginMonitoring
        {
            get => _beginMonitoring;
            set
            {
                if (SetProperty(ref _beginMonitoring, value))
                {
                    if (value)
                    {
                        StartSensors();
                    }
                    else
                    {
                        StopSensors();
                    }
                }
            }
        }

        public new string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        #endregion

        #region Commands
        public IRelayCommand UpdateSunPositionCommand { get; internal set; }
        #endregion

        #region Constructors
        public SunLocationViewModel() : base(null, null)
        {
            // Design-time constructor
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            _selectedDateTime = _selectedDate.Add(_selectedTime);

            UpdateSunPositionCommand = new RelayCommand(() => _ = UpdateSunPositionOptimizedAsync());
            SelectTimelineEventCommand = new RelayCommand<TimelineEventViewModel>(OnSelectTimelineEvent);

            // Initialize collections
            _timelineEvents = new ObservableCollection<TimelineEventViewModel>();

            // Initialize mock data for design time
            InitializeMockData();
        }

        public SunLocationViewModel(IMediator mediator, ISunCalculatorService sunCalculatorService, IErrorDisplayService errorDisplayService, ITimezoneService timezoneService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));

            // Initialize commands
            UpdateSunPositionCommand = new RelayCommand(() => _ = UpdateSunPositionOptimizedAsync());
            SelectTimelineEventCommand = new RelayCommand<TimelineEventViewModel>(OnSelectTimelineEvent);

            // Initialize collections
            _timelineEvents = new ObservableCollection<TimelineEventViewModel>();

            // Set default values
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            _selectedDateTime = _selectedDate.Add(_selectedTime);

            // Initialize mock data for design time
            InitializeMockData();
        }

        private void InitializeMockData()
        {
            WeatherSummary = "☁️ 65°F, UV: 7";
            LightReduction = 0.25;
            ColorTemperature = 5200;
            LightQuality = "Soft Overcast";
            CurrentEV = 12.3;
            NextHourEV = 11.8;
            RecommendedSettings = "f/8 @ 1/250s ISO200";
            LightQualityDescription = "Golden hour approaching in 2h 15m";
            Recommendations = "• Great for portraits\n• Use faster shutter for handheld\n• Consider polarizing filter";
            NextOptimalTime = "Evening Golden Hour in 2h 15m";
        }
        #endregion

        #region PERFORMANCE OPTIMIZED METHODS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Throttled and optimized date/time updates
        /// </summary>
        private async Task UpdateSelectedDateTimeOptimizedAsync()
        {
            // Throttle rapid updates
            var now = DateTime.Now;
            if ((now - _lastUpdateTime).TotalMilliseconds < UPDATE_THROTTLE_MS)
            {
                return;
            }
            _lastUpdateTime = now;

            if (!await _updateSemaphore.WaitAsync(100))
            {
                return; // Skip if another update is in progress
            }

            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                // Batch property updates to reduce UI notifications
                BeginPropertyChangeBatch();

                // Update core datetime property
                _selectedDateTime = _selectedDate.Date.Add(_selectedTime);

                // Perform heavy operations on background thread
                var backgroundTask = Task.Run(async () =>
                {
                    try
                    {
                        var sunPositionTask = UpdateSunPositionBackgroundAsync();
                        var timelineTask = GenerateTimelineEventsBackgroundAsync();

                        await Task.WhenAll(sunPositionTask, timelineTask);

                        return new
                        {
                            SunData = await sunPositionTask,
                            TimelineEvents = await timelineTask
                        };
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Background calculation failed: {ex.Message}", ex);
                    }
                }, _cancellationTokenSource.Token);

                var results = await backgroundTask;

                // Single UI update with all changes
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested) return;

                        // Update sun position data
                        if (results.SunData.HasValue)
                        {
                            var (azimuth, elevation) = results.SunData.Value;
                            _sunDirection = azimuth;
                            _sunElevation = elevation;
                            UpdateElevationMatch();
                        }

                        // Update timeline events in batch
                        TimelineEvents.Clear();
                        foreach (var evt in results.TimelineEvents)
                        {
                            TimelineEvents.Add(evt);
                        }

                        // End batch and fire all notifications at once
                        _ = EndPropertyChangeBatchAsync();
                    }
                    catch (Exception ex)
                    {
                        OnSystemError($"Error updating UI: {ex.Message}");
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error updating date/time: {ex.Message}");
                    _ = EndPropertyChangeBatchAsync(); // Ensure batch is ended
                });
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Location change with batched updates
        /// </summary>
        public async Task OnSelectedLocationChangedOptimizedAsync(LocationListItemViewModel value)
        {
            if (value == null) return;

            if (!await _updateSemaphore.WaitAsync(100))
            {
                return; // Skip if another update is in progress
            }

            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                // Batch property updates
                BeginPropertyChangeBatch();

                // Update location properties immediately
                _locationPhoto = value.Photo;
                _latitude = value.Latitude;
                _longitude = value.Longitude;

                // Heavy calculations in background
                await Task.Run(async () =>
                {
                    await UpdateSunPositionBackgroundAsync();
                    await GenerateTimelineEventsBackgroundAsync();
                }, _cancellationTokenSource.Token);

                // End batch and update UI
                await EndPropertyChangeBatchAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error updating location: {ex.Message}");
                    _ = EndPropertyChangeBatchAsync();
                });
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Sun position calculation on background thread
        /// </summary>
        private async Task<(double azimuth, double elevation)?> UpdateSunPositionBackgroundAsync()
        {
            if (_latitude == 0 && _longitude == 0) return null;

            try
            {
                var azimuth = _sunCalculatorService.GetSolarAzimuth(SelectedDateTime, Latitude, Longitude, TimeZoneInfo.Local.ToString());
                var elevation = _sunCalculatorService.GetSolarElevation(SelectedDateTime, Latitude, Longitude, TimeZoneInfo.Local.ToString());

                return (azimuth, elevation);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Sun position calculation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Timeline generation with caching
        /// </summary>
        private async Task<List<TimelineEventViewModel>> GenerateTimelineEventsBackgroundAsync()
        {
            var cacheKey = GetTimelineCacheKey();

            // Check cache first
            if (_timelineCache.TryGetValue(cacheKey, out var cachedEvents))
            {
                return cachedEvents;
            }

            return await Task.Run(() =>
            {
                try
                {
                    var events = new List<TimelineEventViewModel>();

                    if (_latitude == 0 && _longitude == 0) return events;

                    var baseDate = SelectedDateTime.Date;

                    // Get location timezone
                    var timezoneResult = _timezoneService.GetTimezoneFromCoordinatesAsync(Latitude, Longitude).GetAwaiter().GetResult();
                    var locationTimezone = timezoneResult.IsSuccess ?
                        _timezoneService.GetTimeZoneInfo(timezoneResult.Data) :
                        TimeZoneInfo.Local;

                    var localNow = DateTime.Now;
                    var locationNow = TimeZoneInfo.ConvertTime(localNow, TimeZoneInfo.Local, locationTimezone);
                    var endTime = locationNow.AddHours(24);

                    var timezone = locationTimezone.Id;

                    // Calculate all sun times efficiently
                    var sunTimes = new Dictionary<string, DateTime>
                    {
                        ["astronomicalDawn"] = _sunCalculatorService.GetAstronomicalDawn(baseDate, Latitude, Longitude, timezone),
                        ["nauticalDawn"] = _sunCalculatorService.GetNauticalDawn(baseDate, Latitude, Longitude, timezone),
                        ["civilDawn"] = _sunCalculatorService.GetCivilDawn(baseDate, Latitude, Longitude, timezone),
                        ["sunrise"] = _sunCalculatorService.GetSunrise(baseDate, Latitude, Longitude, timezone),
                        ["solarNoon"] = _sunCalculatorService.GetSolarNoon(baseDate, Latitude, Longitude, timezone),
                        ["sunset"] = _sunCalculatorService.GetSunset(baseDate, Latitude, Longitude, timezone),
                        ["civilDusk"] = _sunCalculatorService.GetCivilDusk(baseDate, Latitude, Longitude, timezone),
                        ["nauticalDusk"] = _sunCalculatorService.GetNauticalDusk(baseDate, Latitude, Longitude, timezone),
                        ["astronomicalDusk"] = _sunCalculatorService.GetAstronomicalDusk(baseDate, Latitude, Longitude, timezone)
                    };

                    // Create event definitions
                    var eventDefinitions = new List<(string key, string name, string icon, Func<DateTime, DateTime> timeCalc)>
                    {
                        ("astronomicalDawn", "Astro Dawn", "⭐", t => t),
                        ("nauticalDawn", "Nautical Dawn", "🌊", t => t),
                        ("civilDawn", "Civil Dawn", "🌅", t => t),
                        ("civilDawn", "Blue Hour Start", "🔵", t => t),
                        ("sunrise", "Golden Start", "🌄", t => t.AddMinutes(-30)),
                        ("sunrise", "Sunrise", "🌅", t => t),
                        ("sunrise", "Golden End", "☀️", t => t.AddHours(1)),
                        ("solarNoon", "Solar Noon", "🌞", t => t),
                        ("sunset", "Golden Start", "🌇", t => t.AddHours(-1)),
                        ("sunset", "Sunset", "🌇", t => t),
                        ("sunset", "Golden End", "🌆", t => t.AddMinutes(30)),
                        ("sunset", "Blue Hour Start", "🔵", t => t),
                        ("civilDusk", "Civil Dusk", "🌃", t => t),
                        ("nauticalDusk", "Nautical Dusk", "🌊", t => t),
                        ("astronomicalDusk", "Astro Dusk", "⭐", t => t)
                    };

                    foreach (var (key, name, icon, timeCalc) in eventDefinitions)
                    {
                        if (!sunTimes.TryGetValue(key, out var baseTime)) continue;

                        var eventTime = timeCalc(baseTime);

                        // Handle next day events if needed
                        if (eventTime <= locationNow)
                        {
                            var nextDayBaseTime = key switch
                            {
                                "astronomicalDawn" => _sunCalculatorService.GetAstronomicalDawn(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "nauticalDawn" => _sunCalculatorService.GetNauticalDawn(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "civilDawn" => _sunCalculatorService.GetCivilDawn(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "sunrise" => _sunCalculatorService.GetSunrise(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "solarNoon" => _sunCalculatorService.GetSolarNoon(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "sunset" => _sunCalculatorService.GetSunset(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "civilDusk" => _sunCalculatorService.GetCivilDusk(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "nauticalDusk" => _sunCalculatorService.GetNauticalDusk(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                "astronomicalDusk" => _sunCalculatorService.GetAstronomicalDusk(baseDate.AddDays(1), Latitude, Longitude, timezone),
                                _ => eventTime
                            };
                            eventTime = timeCalc(nextDayBaseTime);
                        }

                        if (eventTime > locationNow && eventTime <= endTime)
                        {
                            events.Add(new TimelineEventViewModel
                            {
                                EventTime = eventTime,
                                EventName = name,
                                EventIcon = icon
                            });
                        }
                    }

                    var sortedEvents = events.OrderBy(e => e.EventTime).ToList();

                    // Cache the results
                    _timelineCache[cacheKey] = sortedEvents;

                    // Cleanup old cache entries (keep only last 5)
                    if (_timelineCache.Count > 5)
                    {
                        var oldestKey = _timelineCache.Keys.First();
                        _timelineCache.Remove(oldestKey);
                    }

                    return sortedEvents;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Timeline generation failed: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Throttled sun position updates
        /// </summary>
        private async Task UpdateSunPositionOptimizedAsync()
        {
            if (!await _updateSemaphore.WaitAsync(50))
            {
                return; // Skip if busy
            }

            try
            {
                var result = await UpdateSunPositionBackgroundAsync();
                if (result.HasValue)
                {
                    var (azimuth, elevation) = result.Value;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        SunDirection = azimuth;
                        SunElevation = elevation;
                        UpdateElevationMatch();
                    });
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error updating sun position: {ex.Message}");
                });
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }

        #endregion

        #region Methods
        private void OnSelectTimelineEvent(TimelineEventViewModel timelineEvent)
        {
            if (timelineEvent?.EventTime != null)
            {
                SelectedDateTime = timelineEvent.EventTime;
            }
        }

        public void UpdateSunPosition()
        {
            _ = UpdateSunPositionOptimizedAsync();
        }

        public void StartSensors()
        {
            try
            {
                // Start the compass
                if (Compass.Default.IsSupported)
                {
                    Compass.ReadingChanged += Compass_ReadingChanged;
                    Compass.Start(SensorSpeed.UI);
                }

                // Start the orientation sensor
                if (OrientationSensor.Default.IsSupported)
                {
                    OrientationSensor.ReadingChanged += OrientationSensor_ReadingChanged;
                    OrientationSensor.Start(SensorSpeed.UI);
                }

                // Update the sun position
                UpdateSunPosition();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error starting sensors: {ex.Message}");
            }
        }

        public void StopSensors()
        {
            try
            {
                // Stop the compass
                if (Compass.Default.IsSupported)
                {
                    Compass.ReadingChanged -= Compass_ReadingChanged;
                    Compass.Stop();
                }

                // Stop the orientation sensor
                if (OrientationSensor.Default.IsSupported)
                {
                    OrientationSensor.ReadingChanged -= OrientationSensor_ReadingChanged;
                    OrientationSensor.Stop();
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error stopping sensors: {ex.Message}");
            }
        }

        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            try
            {
                // Update the north direction
                NorthRotationAngle = e.Reading.HeadingMagneticNorth;
            }
            catch (Exception ex)
            {
                OnSystemError($"Error reading compass: {ex.Message}");
            }
        }

        private void OrientationSensor_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
        {
            try
            {
                // Calculate device tilt from orientation
                var orientation = e.Reading.Orientation;

                // Calculate pitch in degrees (tilt)
                // Convert quaternion to euler angles
                // This is a simplified calculation for pitch
                double x = orientation.X;
                double y = orientation.Y;
                double z = orientation.Z;
                double w = orientation.W;

                // Calculate pitch (rotation around X-axis)
                double sinp = 2 * (w * y - z * x);
                double pitch = Math.Asin(sinp) * (180 / Math.PI);

                // Update device tilt
                DeviceTilt = pitch;
            }
            catch (Exception ex)
            {
                OnSystemError($"Error reading orientation sensor: {ex.Message}");
            }
        }

        private void UpdateElevationMatch()
        {
            try
            {
                // Check if the device tilt is close to the sun elevation
                // Allow for a 5-degree margin of error
                double difference = Math.Abs(DeviceTilt - SunElevation);
                ElevationMatched = difference <= 5.0;
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating elevation match: {ex.Message}");
            }
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            UpdateSunPositionCommand = new RelayCommand(() => _ = UpdateSunPositionOptimizedAsync());
            SelectTimelineEventCommand = new RelayCommand<TimelineEventViewModel>(OnSelectTimelineEvent);

            // Set default values
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            _selectedDateTime = _selectedDate.Add(_selectedTime);
            _ = UpdateSelectedDateTimeOptimizedAsync();
            StartSensors();
        }

        public void OnNavigatedFromAsync()
        {
            StopSensors();
            _cancellationTokenSource?.Cancel();
        }

        public override void Dispose()
        {
            StopSensors();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _updateSemaphore?.Dispose();
            _timelineCache.Clear();
            base.Dispose();
        }
        #endregion
    }
}