// Location.Photography.ViewModels/AstroLocationViewModel.cs
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Photography.Application.Queries.AstroLocation;
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
    public partial class AstroLocationViewModel : ViewModelBase, INotifyPropertyChanged
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<AstroLocationViewModel>? _logger;

        // Compass and smoothing fields (reused from SunLocationViewModel)
        private Timer? _compassTimer;
        private double _currentCompassHeading = 0;
        private double _targetDirection = 0;
        private double _currentDirection = 0;
        private double _targetAzimuth = 0;
        private bool _isCompassActive = false;
        private readonly object _smoothingLock = new object();

        // Smoothing parameters
        private const double SMOOTHING_FACTOR = 0.15;
        private const int COMPASS_UPDATE_INTERVAL_MS = 100;
        private const double MIN_MOVEMENT_THRESHOLD = 0.5;

        // Observable properties
        private ObservableCollection<LocationViewModel> _locations = new();
        private LocationViewModel? _selectedLocation;
        private DateTime _selectedDate = DateTime.Today;
        private ObservableCollection<AstroEventViewModel> _astroEvents = new();
        private AstroEventViewModel? _selectedEvent;
        private DateTime _selectedDateTime => _selectedDate.Date.Add(_selectedEvent?.GetOptimalTime().TimeOfDay ?? TimeSpan.Zero);
        private double _targetElevation = 0;
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
        private string _recommendedSettings = "f/4 @ 60s ISO 1600";
        private string _lightQualityDescription = "Calculating light conditions...";
        private string _recommendations = "Loading recommendations...";
        private string _nextOptimalTime = "Calculating optimal times...";
        private string _timeFormat = "HH:mm";
        private TimeZoneInfo _deviceTimeZone = TimeZoneInfo.Local;
        private TimeZoneInfo _locationTimeZone = TimeZoneInfo.Local;

        // Equipment recommendation properties
        private string _recommendedCamera = "Loading...";
        private string _recommendedLens = "Loading...";
        private double _fieldOfViewWidth = 0;
        private double _fieldOfViewHeight = 0;
        private bool _targetFitsInFrame = false;
        private double _targetCoveragePercentage = 0;
        private string _exposureSettings = "Loading...";
        private string _focusDistance = "Infinity";
        private bool _trackingRequired = false;
        private string _stackingRecommendation = "Loading...";
        private string _calibrationFrames = "Loading...";
        private string _expectedQuality = "Loading...";
        private string _totalExposureTime = "Loading...";
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
                    _ = LoadAstroEventsAsync();
                }
            }
        }

        public ObservableCollection<AstroEventViewModel> AstroEvents
        {
            get => _astroEvents;
            set => SetProperty(ref _astroEvents, value);
        }

        public AstroEventViewModel? SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                if (SetProperty(ref _selectedEvent, value))
                {
                    OnSelectedEventChanged();
                }
            }
        }

        public double TargetDirection
        {
            get => _targetDirection;
            set => SetProperty(ref _targetDirection, value);
        }

        public double TargetElevation
        {
            get => _targetElevation;
            set
            {
                if (SetProperty(ref _targetElevation, value))
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

        // Equipment recommendation properties
        public string RecommendedCamera
        {
            get => _recommendedCamera;
            set => SetProperty(ref _recommendedCamera, value);
        }

        public string RecommendedLens
        {
            get => _recommendedLens;
            set => SetProperty(ref _recommendedLens, value);
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

        public double TargetCoveragePercentage
        {
            get => _targetCoveragePercentage;
            set => SetProperty(ref _targetCoveragePercentage, value);
        }

        public string ExposureSettings
        {
            get => _exposureSettings;
            set => SetProperty(ref _exposureSettings, value);
        }

        public string FocusDistance
        {
            get => _focusDistance;
            set => SetProperty(ref _focusDistance, value);
        }

        public bool TrackingRequired
        {
            get => _trackingRequired;
            set => SetProperty(ref _trackingRequired, value);
        }

        public string StackingRecommendation
        {
            get => _stackingRecommendation;
            set => SetProperty(ref _stackingRecommendation, value);
        }

        public string CalibrationFrames
        {
            get => _calibrationFrames;
            set => SetProperty(ref _calibrationFrames, value);
        }

        public string ExpectedQuality
        {
            get => _expectedQuality;
            set => SetProperty(ref _expectedQuality, value);
        }

        public string TotalExposureTime
        {
            get => _totalExposureTime;
            set => SetProperty(ref _totalExposureTime, value);
        }
        private string _dateformat;
        private string _timeformat;
        public string EventTimeLabel => SelectedEvent?.GetFormattedTime() ?? "--:--";

        public bool BeginMonitoring { get; set; }
        #endregion

        #region Events
        public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Constructor
        public AstroLocationViewModel(
            IMediator mediator,
            ISunCalculatorService sunCalculatorService,
            IErrorDisplayService errorDisplayService,
            ITimezoneService timezoneService,
            string dateformat, string timeformat,
            ILogger<AstroLocationViewModel>? logger = null)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _timezoneService = timezoneService ?? throw new ArgumentNullException(nameof(timezoneService));
            _logger = logger;
            _dateformat = dateformat;
            _timeformat = timeformat;
            LoadUserSettingsAsync();
        }
        #endregion

        #region Commands
        [RelayCommand]
        private async Task UpdateTargetPositionAsync()
        {
            if (SelectedLocation == null || SelectedEvent == null)
                return;

            try
            {
                IsBusy = true;
                ClearErrors();

                // Calculate target position at the optimal time
                var optimalTime = SelectedEvent.GetOptimalTime();

                // Update target direction and elevation
                TargetDirection = SelectedEvent.Azimuth;
                TargetElevation = SelectedEvent.Altitude;

                // Update compass to point to target
                _targetAzimuth = SelectedEvent.Azimuth;
                UpdateCompassDirection();

                // Load equipment recommendations
                await LoadEquipmentRecommendationsAsync();

                // Update weather and light predictions (reuse from SunLocationViewModel logic)
                await UpdateWeatherAndLightAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating target position: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Compass and Sensor Methods (Reused from SunLocationViewModel)
        public void StartSensors()
        {
            try
            {
                if (!_isCompassActive && Compass.IsSupported)
                {
                    Compass.ReadingChanged += OnCompassReadingChanged;
                    Compass.Start(SensorSpeed.UI);
                    _isCompassActive = true;

                    _compassTimer = new Timer(UpdateSmoothDirection, null, 0, COMPASS_UPDATE_INTERVAL_MS);
                    _logger?.LogDebug("Compass service started successfully");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start compass service");
                OnSystemError($"Failed to start compass: {ex.Message}");
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
                _logger?.LogDebug("Compass service stopped successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping compass service");
            }
        }

        private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            _currentCompassHeading = e.Reading.HeadingMagneticNorth;
        }

        private void UpdateSmoothDirection(object? state)
        {
            lock (_smoothingLock)
            {
                var targetDirection = (_targetAzimuth - _currentCompassHeading + 360) % 360;
                if (targetDirection > 180) targetDirection -= 360;

                var diff = targetDirection - _currentDirection;
                if (Math.Abs(diff) > MIN_MOVEMENT_THRESHOLD)
                {
                    _currentDirection += diff * SMOOTHING_FACTOR;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TargetDirection = _currentDirection;
                    });
                }
            }
        }

        private void UpdateCompassDirection()
        {
            lock (_smoothingLock)
            {
                _targetDirection = (_targetAzimuth - _currentCompassHeading + 360) % 360;
                if (_targetDirection > 180) _targetDirection -= 360;
            }
        }
        #endregion

        #region Helper Methods
        private async Task LoadAstroEventsAsync()
        {
            if (SelectedLocation == null) return;

            try
            {
                IsBusy = true;
                ClearErrors();

                var query = new GetAstroEventsForDateQuery
                {
                    Date = SelectedDate,
                    Latitude = SelectedLocation.Lattitude,
                    Longitude = SelectedLocation.Longitude,
                    MinimumAltitude = 10,
                    IncludeDayTimeEvents = false
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Data != null)
                {
                    // Convert DTOs to ViewModels
                    var events = result.Data.Select(dto => new AstroEventViewModel
                    {
                        Name = dto.Name,
                        Target = dto.Target,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        PeakTime = dto.PeakTime,
                        Azimuth = dto.Azimuth,
                        Altitude = dto.Altitude,
                        Magnitude = dto.Magnitude,
                        Description = dto.Description,
                        Constellation = dto.Constellation,
                        IsVisible = dto.IsVisible,
                        EventType = dto.EventType,
                        AngularSize = dto.AngularSize,
                        RecommendedEquipment = dto.RecommendedEquipment,
                        _dateFormat = _dateformat,
                        _timeFormat = _timeFormat
                    }).ToList();

                    AstroEvents = new ObservableCollection<AstroEventViewModel>(events);

                    // Auto-select the best event
                    if (AstroEvents.Count > 0)
                    {
                        SelectedEvent = AstroEvents.OrderByDescending(e => e.GetVisibilityScore()).First();
                    }
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Failed to load astro events";
                    AstroEvents.Clear();
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error loading astro events: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEquipmentRecommendationsAsync()
        {
            if (SelectedLocation == null || SelectedEvent == null) return;

            try
            {
                var query = new GetAstroEquipmentRecommendationQuery
                {
                    Target = SelectedEvent.Target,
                    DateTime = SelectedEvent.GetOptimalTime(),
                    Latitude = SelectedLocation.Lattitude,
                    Longitude = SelectedLocation.Longitude,
                    TargetAltitude = SelectedEvent.Altitude,
                    TargetAzimuth = SelectedEvent.Azimuth,
                    TargetMagnitude = SelectedEvent.Magnitude,
                    TargetAngularSize = SelectedEvent.AngularSize
                };

                var result = await _mediator.Send(query);

                if (result.IsSuccess && result.Data != null)
                {
                    var data = result.Data;
                    RecommendedCamera = data.RecommendedCamera;
                    RecommendedLens = data.RecommendedLens;
                    FieldOfViewWidth = data.FieldOfViewWidth;
                    FieldOfViewHeight = data.FieldOfViewHeight;
                    TargetFitsInFrame = data.TargetFitsInFrame;
                    TargetCoveragePercentage = data.TargetCoveragePercentage;
                    ExposureSettings = data.ExposureSettings;
                    FocusDistance = data.FocusDistance;
                    TrackingRequired = data.TrackingRequired;

                    StackingRecommendation = data.StackingRecommendations.GetFormattedRecommendation();
                    CalibrationFrames = data.StackingRecommendations.CalibrationFrames;
                    ExpectedQuality = data.StackingRecommendations.ExpectedQuality;
                    TotalExposureTime = data.StackingRecommendations.GetFormattedTotalTime();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading equipment recommendations");
            }
        }

        private async Task UpdateWeatherAndLightAsync()
        {
            // Placeholder for weather and light calculations
            // Could reuse logic from SunLocationViewModel or EnhancedSunCalculatorViewModel
            WeatherSummary = "Clear skies predicted";
            LightReduction = 0.1;
            ColorTemperature = 3000; // Night sky
            LightQuality = "Excellent for astrophotography";
            LightQualityDescription = "Dark skies with minimal light pollution";
            Recommendations = "Perfect conditions for deep sky imaging";
        }

        private void OnSelectedLocationChanged()
        {
            if (SelectedLocation != null)
            {
                _ = LoadAstroEventsAsync();
            }
        }

        private void OnSelectedEventChanged()
        {
            if (SelectedEvent != null)
            {
                _ = UpdateTargetPositionAsync();
                OnPropertyChanged(nameof(EventTimeLabel));
            }
        }

        private void CheckElevationAlignment()
        {
            ElevationMatched = Math.Abs(DeviceTilt - TargetElevation) < 5.0;
        }

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
}