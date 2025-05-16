// Location.Photography.ViewModels/Interfaces/ISunLocation.cs
using System;

namespace Location.Photography.ViewModels.Interfaces
{
    public interface ISunLocation
    {
        DateTime SelectedDateTime { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
        double NorthRotationAngle { get; set; }
        double SunDirection { get; set; }
        double SunElevation { get; set; }
        double DeviceTilt { get; set; }
        bool ElevationMatched { get; set; }

        event EventHandler<Location.Photography.ViewModels.Events.OperationErrorEventArgs> ErrorOccurred;

        void StartSensors();
        void StopSensors();
    }
}