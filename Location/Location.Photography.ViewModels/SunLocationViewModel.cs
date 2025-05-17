using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Photography.Domain.Interfaces;
using Location.Photography.Domain.Services;
using MediatR;
using System.Collections.ObjectModel;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public class SunLocationViewModel : ObservableObject, ISunLocation
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly ISunCalculatorService _sunCalculatorService;

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
        private bool _isBusy;
        private string _errorMessage;
        #endregion

        #region Properties
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
                        StartMonitoring();
                    }
                    else
                    {
                        StopMonitoring();
                    }
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }
        #endregion

        #region Events
        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        #endregion

        #region Commands
        public IRelayCommand UpdateSunPositionCommand { get; }
        #endregion

        #region Constructors
        public SunLocationViewModel()
        {
            // Design-time constructor
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            UpdateSelectedDateTime();

            UpdateSunPositionCommand = new RelayCommand(UpdateSunPosition);
        }

        public SunLocationViewModel(IMediator mediator, ISunCalculatorService sunCalculatorService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));

            // Initialize commands
            UpdateSunPositionCommand = new RelayCommand(UpdateSunPosition);

            // Set default values
            _selectedDate = DateTime.Today;
            _selectedTime = DateTime.Now.TimeOfDay;
            UpdateSelectedDateTime();
        }
        #endregion

        #region Methods
        private void UpdateSelectedDateTime()
        {
            _selectedDateTime = _selectedDate.Date.Add(_selectedTime);
            OnPropertyChanged(nameof(SelectedDateTime));
            UpdateSunPosition();
        }

        public void UpdateSunPosition()
        {
            try
            {
                if (_latitude == 0 && _longitude == 0)
                    return;

                // Use the SunCalculatorService to get the current sun position
                var azimuth = _sunCalculatorService.GetSolarAzimuth(SelectedDateTime, Latitude, Longitude);
                var elevation = _sunCalculatorService.GetSolarElevation(SelectedDateTime, Latitude, Longitude);

                // Update the sun direction (azimuth)
                SunDirection = azimuth;

                // Update the sun elevation
                SunElevation = elevation;

                // Update whether the device is matching the sun elevation
                UpdateElevationMatch();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error updating sun position");
            }
        }

        private void StartMonitoring()
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
                HandleError(ex, "Error starting sensors");
            }
        }

        private void StopMonitoring()
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
                HandleError(ex, "Error stopping sensors");
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
                HandleError(ex, "Error reading compass");
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
                HandleError(ex, "Error reading orientation sensor");
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
                HandleError(ex, "Error updating elevation match");
            }
        }

        protected virtual void OnErrorOccurred(OperationErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        private void HandleError(Exception ex, string message)
        {
            ErrorMessage = $"{message}: {ex.Message}";
            OnErrorOccurred(new OperationErrorEventArgs(OperationErrorSource.Unknown, message, ex));
        }
        #endregion
    }
}