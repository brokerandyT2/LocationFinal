using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.ViewModels;
using Location.Photography.Domain.Interfaces;
using Microsoft.Maui.Devices.Sensors;
using System;
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
        private bool _beginMonitoring;

        private const double SunSmoothingFactor = 0.1;
        private const double NorthSmoothingFactor = 0.1;
        #endregion

        #region Properties
        public DateTime SelectedDateTime
        {
            get => _selectedDateTime;
            set
            {
                if (SetProperty(ref _selectedDateTime, value))
                {
                    CalculateSunDirection(NorthRotationAngle);
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
                    CalculateSunDirection(NorthRotationAngle);
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
                    CalculateSunDirection(NorthRotationAngle);
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
            set
            {
                if (SetProperty(ref _sunElevation, value))
                {
                    CheckElevationMatch();
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
                    CheckElevationMatch();
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
        #endregion

        #region Commands
        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }
        #endregion

        #region Constructor
        public SunLocationViewModel()
        {
            // Initialize commands
            StartMonitoringCommand = new RelayCommand(() => BeginMonitoring = true, () => !BeginMonitoring && !IsBusy);
            StopMonitoringCommand = new RelayCommand(() => BeginMonitoring = false, () => BeginMonitoring && !IsBusy);
        }
        #endregion

        #region Methods
        public void CalculateSunDirection(double heading)
        {
            try
            {
                UpdateNorthRotationAngle(heading);

                // Simplified placeholder calculation
                var dt = SelectedDateTime;
                double solarAzimuth = 180; // This would be calculated by your ISunCalculatorService
                double solarElevation = 45; // This would be calculated by your ISunCalculatorService

                double angleDiff = NormalizeAngle(solarAzimuth - heading);
                SunDirection = SmoothAngle(SunDirection, angleDiff, SunSmoothingFactor);
                SunElevation = solarElevation;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error calculating sun direction: {ex.Message}";
                IsError = true;
            }
        }

        private void UpdateNorthRotationAngle(double rawHeading)
        {
            NorthRotationAngle = SmoothAngle(NorthRotationAngle, rawHeading, NorthSmoothingFactor);
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
                IsError = true;
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
                IsError = true;
            }
        }

        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            if (BeginMonitoring)
            {
                var heading = e.Reading.HeadingMagneticNorth;
                UpdateNorthRotationAngle(heading);
                CalculateSunDirection(heading);
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

        private void CheckElevationMatch()
        {
            if (BeginMonitoring)
            {
                if (Math.Abs(DeviceTilt - SunElevation) <= 3)
                {
                    // On the main thread check for vibration support and provide haptic feedback
                    MainThread.BeginInvokeOnMainThread(async () =>
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
        #endregion
    }
}