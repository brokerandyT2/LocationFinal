// Location.Photography.Shared.ViewModels/SunLocationViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Innovative.SolarCalculator;
using Location.Core.Domain.Entities;
using Location.Core.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Location.Photography.ViewModels
{
    public partial class SunLocationViewModel : ViewModelBase, ISunLocation
    {
        #region Fields
        private DateTime _selectedDateTime = DateTime.Now;
    private double _latitude;
    private double _longitude;
    private double _northRotationAngle;
    private double _sunDirection;
    private double _sunElevation;
    private double _deviceTilt;
    private bool _elevationMatched;
    private DateTime _selectedDate = DateTime.Now;
    private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;
    private ObservableCollection<LocationViewModel> _locations;
    private bool _beginMonitoring;

    private const double SunSmoothingFactor = 0.1;
    private const double NorthSmoothingFactor = 0.1;
    #endregion

    #region Events
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
    #endregion

    #region Properties
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged();
                SelectedDateTime = new DateTime(_selectedDate.Year, _selectedDate.Month, _selectedDate.Day,
                    _selectedTime.Hours, _selectedTime.Minutes, _selectedTime.Seconds);
            }
        }
    }

    public TimeSpan SelectedTime
    {
        get => _selectedTime;
        set
        {
            if (_selectedTime != value)
            {
                _selectedTime = value;
                OnPropertyChanged();
                SelectedDateTime = new DateTime(SelectedDateTime.Year, SelectedDateTime.Month, SelectedDateTime.Day,
                    _selectedTime.Hours, _selectedTime.Minutes, _selectedTime.Seconds);
            }
        }
    }

    public bool BeginMonitoring
    {
        get => _beginMonitoring;
        set
        {
            if (_beginMonitoring != value)
            {
                _beginMonitoring = value;
                OnPropertyChanged();

                if (_beginMonitoring)
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

    public ObservableCollection<LocationViewModel> Locations
    {
        get => _locations;
        set
        {
            if (_locations != value)
            {
                _locations = value;
                OnPropertyChanged();
            }
        }
    }

    // Interface compatibility properties

    public DateTime SelectedDateTime
    {
        get => _selectedDateTime;
        set
        {
            if (_selectedDateTime != value)
            {
                _selectedDateTime = value;
                OnPropertyChanged();
                CalculateSunDirection(NorthRotationAngle);
            }
        }
    }

    public double Latitude
    {
        get => _latitude;
        set
        {
            if (_latitude != value)
            {
                _latitude = value;
                OnPropertyChanged();
                CalculateSunDirection(NorthRotationAngle);
            }
        }
    }

    public double Longitude
    {
        get => _longitude;
        set
        {
            if (_longitude != value)
            {
                _longitude = value;
                OnPropertyChanged();
                CalculateSunDirection(NorthRotationAngle);
            }
        }
    }

    public double NorthRotationAngle
    {
        get => _northRotationAngle;
        set
        {
            if (_northRotationAngle != value)
            {
                _northRotationAngle = value;
                OnPropertyChanged();
            }
        }
    }

    public double SunDirection
    {
        get => _sunDirection;
        set
        {
            if (_sunDirection != value)
            {
                _sunDirection = value;
                OnPropertyChanged();
            }
        }
    }

    public double SunElevation
    {
        get => _sunElevation;
        set
        {
            if (_sunElevation != value)
            {
                _sunElevation = value;
                OnPropertyChanged();
            }
        }
    }

    public double DeviceTilt
    {
        get => _deviceTilt;
        set
        {
            if (_deviceTilt != value)
            {
                _deviceTilt = value;
                OnPropertyChanged();
                CheckElevationMatch();
            }
        }
    }

    public bool ElevationMatched
    {
        get => _elevationMatched;
        set
        {
            if (_elevationMatched != value)
            {
                _elevationMatched = value;
                OnPropertyChanged();
            }
        }
    }

    // Commands
    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }
    #endregion

    #region Constructors
    /// <summary>
    /// Default constructor for design-time and when created by SQLite
    /// </summary>
    public SunLocation()
    {
        _locations = new ObservableCollection<LocationViewModel>();
        _selectedDate = DateTime.Now;
        _selectedTime = DateTime.Now.TimeOfDay;

        // Initialize commands
        StartMonitoringCommand = new RelayCommand(() => BeginMonitoring = true, () => !_beginMonitoring && !IsBusy);
        StopMonitoringCommand = new RelayCommand(() => BeginMonitoring = false, () => _beginMonitoring && !IsBusy);
    }
    #endregion

    #region Methods
    private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
    {
        if (BeginMonitoring)
        {
            var heading = e.Reading.HeadingMagneticNorth;
            UpdateNorthRotationAngle(heading);
            CalculateSunDirection(heading);
        }
    }

    private void UpdateNorthRotationAngle(double rawHeading)
    {
        NorthRotationAngle = SmoothAngle(NorthRotationAngle, rawHeading, NorthSmoothingFactor);
    }

    private void CalculateSunDirection(double heading)
    {
        try
        {
            var dt = SelectedDateTime;
            var solarTimes = new SolarTimes(dt, Latitude, Longitude);
            double solarAzimuth = Math.Round((double)solarTimes.SolarAzimuth, 0);
            SunElevation = solarTimes.SolarElevation;

            double angleDiff = NormalizeAngle(solarAzimuth - heading);
            SunDirection = SmoothAngle(SunDirection, angleDiff, SunSmoothingFactor);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error calculating sun direction: {ex.Message}";
            OnErrorOccurred(new OperationErrorEventArgs(
                OperationErrorSource.Unknown,
                ErrorMessage,
                ex));
        }
    }

    public void StartSensors()
    {
        try
        {
            if (Compass.Default.IsSupported && !Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged += Compass_ReadingChanged;
                Compass.Default.Start(SensorSpeed.UI);
            }

            if (Accelerometer.Default.IsSupported && !Accelerometer.Default.IsMonitoring)
            {
                Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error starting sensors: {ex.Message}";
            OnErrorOccurred(new OperationErrorEventArgs(
                OperationErrorSource.Unknown,
                ErrorMessage,
                ex));
        }
    }

    public void StopSensors()
    {
        try
        {
            if (Compass.Default.IsSupported && Compass.Default.IsMonitoring)
            {
                Compass.Default.Stop();
                Compass.Default.ReadingChanged -= Compass_ReadingChanged;
            }

            if (Accelerometer.Default.IsSupported && Accelerometer.Default.IsMonitoring)
            {
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error stopping sensors: {ex.Message}";
            OnErrorOccurred(new OperationErrorEventArgs(
                OperationErrorSource.Unknown,
                ErrorMessage,
                ex));
        }
    }

    private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
    {
        if (BeginMonitoring)
        {
            var z = e.Reading.Acceleration.Z;
            var tilt = Math.Acos(z) * 180 / Math.PI;
            DeviceTilt = tilt;
        }
    }

    private async void CheckElevationMatch()
    {
        if (BeginMonitoring)
        {
            if (Math.Abs(DeviceTilt - SunElevation) <= 3)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (Vibration.Default.IsSupported)
                        {
                            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                            await Task.Delay(100);
                            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                        }
                        ElevationMatched = true;
                    }
                    catch (Exception ex)
                    {
                        // Swallow vibration errors as they're not critical
                        System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                    }
                });
            }
            else
            {
                ElevationMatched = false;
            }
        }
    }

    private double SmoothAngle(double current, double target, double smoothingFactor)
    {
        double difference = ((target - current + 540) % 360) - 180;
        return (current + difference * smoothingFactor + 360) % 360;
    }

    private double NormalizeAngle(double angle)
    {
        angle = angle % 360;
        if (angle < 0) angle += 360;
        return angle;
    }

    /// <summary>
    /// Raise the error event
    /// </summary>
    protected virtual void OnErrorOccurred(OperationErrorEventArgs e)
    {
        ErrorOccurred?.Invoke(this, e);
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}
}