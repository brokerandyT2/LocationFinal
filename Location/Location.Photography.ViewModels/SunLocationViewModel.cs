using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
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
        private string _locationPhoto = string.Empty; // FIXED: Changed from Lattitude to Latitude
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
                    UpdateSelectedDateTime();
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
                    UpdateSelectedDateTime();
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
                    UpdateSunPosition();
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
                    UpdateSunPosition();
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
        private ITimezoneService _timezoneService;
        #region Commands
        public IRelayCommand UpdateSunPositionCommand { get; internal set; }
        #endregion

        #region Constructors
        public SunLocationViewModel() : base(null, null)
        {
            // Design-time constructor
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            UpdateSelectedDateTime();

            UpdateSunPositionCommand = new RelayCommand(UpdateSunPosition);
            SelectTimelineEventCommand = new RelayCommand<TimelineEventViewModel>(OnSelectTimelineEvent);

            // Initialize collections
            _timelineEvents = new ObservableCollection<TimelineEventViewModel>();

            // Initialize mock data for design time
            InitializeMockData();
        }

        public SunLocationViewModel(IMediator mediator, ISunCalculatorService sunCalculatorService, IErrorDisplayService errorDisplayService, ITimezoneService timezoneserv)
    : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _timezoneService = timezoneserv ?? throw new ArgumentNullException(nameof(timezoneserv));
            // Initialize commands
            UpdateSunPositionCommand = new RelayCommand(UpdateSunPosition);
            SelectTimelineEventCommand = new RelayCommand<TimelineEventViewModel>(OnSelectTimelineEvent);

            // Initialize collections
            _timelineEvents = new ObservableCollection<TimelineEventViewModel>();

            // Set default values
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            UpdateSelectedDateTime();

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

        #region Methods
        private bool _isUpdating = false;

        private async void UpdateSelectedDateTime()
        {
            if (_isUpdating) return;

            try
            {
                _isUpdating = true;

                // Batch property updates
                _selectedDateTime = _selectedDate.Date.Add(_selectedTime);

                // Defer expensive operations to background
                await Task.Run(async () =>
                {
                    try
                    {
                        // Calculate sun position off UI thread
                        await UpdateSunPositionBackground();

                        // Generate timeline events off UI thread  
                        var timelineEvents = await GenerateTimelineEventsBackground();

                        // Single UI update with all changes
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            if (!_isUpdating) return; // Check if still valid

                            // Suppress notifications during batch update
                            SetPropertyWithoutNotification(ref _selectedDateTime, _selectedDateTime);

                            // Update timeline in batch
                            TimelineEvents.Clear();
                            foreach (var evt in timelineEvents)
                            {
                                TimelineEvents.Add(evt);
                            }

                            // Single notification for all changes
                            OnPropertyChanged(nameof(SelectedDateTime));
                            OnPropertyChanged(nameof(TimelineEvents));
                        });
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            OnSystemError($"Error updating date/time: {ex.Message}");
                        });
                    }
                }).ConfigureAwait(false);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private bool SetPropertyWithoutNotification<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            return true;
        }

        public void OnSelectedLocationChanged(LocationListItemViewModel value)
        {
            if (value != null && !_isUpdating)
            {
                // Batch location-related updates
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            _isUpdating = true;

                            // Batch property updates without individual notifications - FIXED property names
                            SetPropertyWithoutNotification(ref _locationPhoto, value.Photo);
                            SetPropertyWithoutNotification(ref _latitude, value.Latitude);  // FIXED: Latitude not Lattitude
                            SetPropertyWithoutNotification(ref _longitude, value.Longitude);

                            // Single notification for all location changes
                            OnPropertyChanged(nameof(LocationPhoto));  // This property exists in the ViewModel
                            OnPropertyChanged(nameof(Latitude));
                            OnPropertyChanged(nameof(Longitude));

                            _isUpdating = false;
                        });

                        // Heavy calculations in background - FIXED method name
                        await UpdateSunPositionBackground();

                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            _isUpdating = false;
                            OnSystemError($"Error updating location: {ex.Message}");
                        });
                    }
                });
            }
        }

        private async Task UpdateSunPositionBackground()
        {
            // Move sun position calculations to background thread
            if (_latitude == 0 && _longitude == 0) return;

            try
            {
                var azimuth = _sunCalculatorService.GetSolarAzimuth(SelectedDateTime, Latitude, Longitude, TimeZoneInfo.Local.ToString());
                var elevation = _sunCalculatorService.GetSolarElevation(SelectedDateTime, Latitude, Longitude, TimeZoneInfo.Local.ToString());

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (!_isUpdating) return;

                    SetPropertyWithoutNotification(ref _sunDirection, azimuth);
                    SetPropertyWithoutNotification(ref _sunElevation, elevation);
                    UpdateElevationMatch();
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Sun position calculation failed: {ex.Message}", ex);
            }
        }

        private async Task<List<TimelineEventViewModel>> GenerateTimelineEventsBackground()
        {
            // Move timeline generation to background thread
            return await Task.Run(() =>
            {
                try
                {
                    var events = new List<TimelineEventViewModel>();

                    // All the existing timeline generation logic here
                    // (moving the entire GenerateTimelineEvents implementation to background)

                    return events.OrderBy(e => e.EventTime).ToList();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Timeline generation failed: {ex.Message}", ex);
                }
            });
        }
        private async void GenerateTimelineEvents()
        {
            try
            {
                // Show loading state immediately
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Could add loading indicator here if needed
                });

                // Perform heavy calculations on background thread
                var timelineEvents = await Task.Run(() =>
                {
                    if (_timelineEvents == null)
                        return new List<TimelineEventViewModel>();

                    var baseDate = SelectedDateTime.Date;
                    var events = new List<TimelineEventViewModel>();

                    // Get location timezone - use TimezoneService from the project
                    var timezoneResult = _timezoneService.GetTimezoneFromCoordinatesAsync(Latitude, Longitude).GetAwaiter().GetResult();
                    var locationTimezone = timezoneResult.IsSuccess ?
                        _timezoneService.GetTimeZoneInfo(timezoneResult.Data) :
                        TimeZoneInfo.Local;

                    // Convert local DateTime.Now to location timezone
                    var localNow = DateTime.Now;
                    var locationNow = TimeZoneInfo.ConvertTime(localNow, TimeZoneInfo.Local, locationTimezone);
                    var endTime = locationNow.AddHours(24);

                    // Calculate all sun times for the selected date using location timezone
                    var timezone = locationTimezone.Id;
                    var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(baseDate, Latitude, Longitude, timezone);
                    var nauticalDawn = _sunCalculatorService.GetNauticalDawn(baseDate, Latitude, Longitude, timezone);
                    var civilDawn = _sunCalculatorService.GetCivilDawn(baseDate, Latitude, Longitude, timezone);
                    var sunrise = _sunCalculatorService.GetSunrise(baseDate, Latitude, Longitude, timezone);
                    var solarNoon = _sunCalculatorService.GetSolarNoon(baseDate, Latitude, Longitude, timezone);
                    var sunset = _sunCalculatorService.GetSunset(baseDate, Latitude, Longitude, timezone);
                    var civilDusk = _sunCalculatorService.GetCivilDusk(baseDate, Latitude, Longitude, timezone);
                    var nauticalDusk = _sunCalculatorService.GetNauticalDusk(baseDate, Latitude, Longitude, timezone);
                    var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(baseDate, Latitude, Longitude, timezone);

                    // Calculate derived events
                    var goldenHourMorningStart = sunrise.AddMinutes(-30);
                    var goldenHourMorningEnd = sunrise.AddHours(1);
                    var goldenHourEveningStart = sunset.AddHours(-1);
                    var goldenHourEveningEnd = sunset.AddMinutes(30);

                    // Create list of all events with their types
                    var allEvents = new List<(DateTime time, string name, string icon, string eventType)>
            {
                (astronomicalDawn, "Astro Dawn", "⭐", "astronomicalDawn"),
                (nauticalDawn, "Nautical Dawn", "🌊", "nauticalDawn"),
                (civilDawn, "Civil Dawn", "🌅", "civilDawn"),
                (civilDawn, "Blue Hour Start", "🔵", "blueHourStart"),
                (goldenHourMorningStart, "Golden Start", "🌄", "goldenMorningStart"),
                (sunrise, "Sunrise", "🌅", "sunrise"),
                (goldenHourMorningEnd, "Golden End", "☀️", "goldenMorningEnd"),
                (solarNoon, "Solar Noon", "🌞", "solarNoon"),
                (goldenHourEveningStart, "Golden Start", "🌇", "goldenEveningStart"),
                (sunset, "Sunset", "🌇", "sunset"),
                (goldenHourEveningEnd, "Golden End", "🌆", "goldenEveningEnd"),
                (sunset, "Blue Hour Start", "🔵", "blueHourEveningStart"),
                (civilDusk, "Civil Dusk", "🌃", "civilDusk"),
                (nauticalDusk, "Nautical Dusk", "🌊", "nauticalDusk"),
                (astronomicalDusk, "Astro Dusk", "⭐", "astronomicalDusk")
            };

                    // Process each event
                    foreach (var (time, name, icon, eventType) in allEvents)
                    {
                        DateTime eventTime = time;

                        // If event has passed in location time, recalculate for next day
                        if (eventTime <= locationNow)
                        {
                            var nextDay = baseDate.AddDays(1);

                            eventTime = eventType switch
                            {
                                "astronomicalDawn" => _sunCalculatorService.GetAstronomicalDawn(nextDay, Latitude, Longitude, timezone),
                                "nauticalDawn" => _sunCalculatorService.GetNauticalDawn(nextDay, Latitude, Longitude, timezone),
                                "civilDawn" => _sunCalculatorService.GetCivilDawn(nextDay, Latitude, Longitude, timezone),
                                "blueHourStart" => _sunCalculatorService.GetCivilDawn(nextDay, Latitude, Longitude, timezone),
                                "goldenMorningStart" => _sunCalculatorService.GetSunrise(nextDay, Latitude, Longitude, timezone).AddMinutes(-30),
                                "sunrise" => _sunCalculatorService.GetSunrise(nextDay, Latitude, Longitude, timezone),
                                "goldenMorningEnd" => _sunCalculatorService.GetSunrise(nextDay, Latitude, Longitude, timezone).AddHours(1),
                                "solarNoon" => _sunCalculatorService.GetSolarNoon(nextDay, Latitude, Longitude, timezone),
                                "goldenEveningStart" => _sunCalculatorService.GetSunset(nextDay, Latitude, Longitude, timezone).AddHours(-1),
                                "sunset" => _sunCalculatorService.GetSunset(nextDay, Latitude, Longitude, timezone),
                                "goldenEveningEnd" => _sunCalculatorService.GetSunset(nextDay, Latitude, Longitude, timezone).AddMinutes(30),
                                "blueHourEveningStart" => _sunCalculatorService.GetSunset(nextDay, Latitude, Longitude, timezone),
                                "civilDusk" => _sunCalculatorService.GetCivilDusk(nextDay, Latitude, Longitude, timezone),
                                "nauticalDusk" => _sunCalculatorService.GetNauticalDusk(nextDay, Latitude, Longitude, timezone),
                                "astronomicalDusk" => _sunCalculatorService.GetAstronomicalDusk(nextDay, Latitude, Longitude, timezone),
                                _ => eventTime
                            };
                        }

                        // Add event if it's within the next 24 hours in location time
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

                    // Sort events by time
                    return events.OrderBy(e => e.EventTime).ToList();
                }).ConfigureAwait(false);

                // Update UI on main thread with batch operation
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        TimelineEvents.Clear();
                        foreach (var evt in timelineEvents)
                        {
                            TimelineEvents.Add(evt);
                        }

                        OnPropertyChanged(nameof(TimelineEvents));
                    }
                    catch (Exception ex)
                    {
                        OnSystemError($"Error updating timeline UI: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Error generating timeline events: {ex.Message}");
                });
            }
        }
        private void OnSelectTimelineEvent(TimelineEventViewModel timelineEvent)
        {
            if (timelineEvent?.EventTime != null)
            {
                SelectedDateTime = timelineEvent.EventTime;
            }
        }
        public void UpdateSunPosition()
        {
            try
            {
                if (_latitude == 0 && _longitude == 0)
                    return;

                // Use the SunCalculatorService to get the current sun position
                var azimuth = _sunCalculatorService.GetSolarAzimuth(SelectedDateTime, Latitude, Longitude, TimeZoneInfo.Local.ToString());
                var elevation = _sunCalculatorService.GetSolarElevation(SelectedDateTime, Latitude, Longitude, TimeZoneInfo.Local.ToString());

                // Update the sun direction (azimuth)
                SunDirection = azimuth;

                // Update the sun elevation
                SunElevation = elevation;

                // Update whether the device is matching the sun elevation
                UpdateElevationMatch();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating sun position: {ex.Message}");
            }
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
            UpdateSunPositionCommand = new RelayCommand(UpdateSunPosition);
            SelectTimelineEventCommand = new RelayCommand<TimelineEventViewModel>(OnSelectTimelineEvent);

            // Set default values
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            UpdateSelectedDateTime();
            StartSensors();
        }

        public void OnNavigatedFromAsync()
        {
            StopSensors();
            //throw new NotImplementedException();
        }
        #endregion
    }
}