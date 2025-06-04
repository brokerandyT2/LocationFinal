// Location.Photography.Application/Services/IFOVCalculationService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface IFOVCalculationService
    {
        /// <summary>
        /// Calculates horizontal field of view from focal length and sensor width
        /// </summary>
        double CalculateHorizontalFOV(double focalLength, double sensorWidth);

        /// <summary>
        /// Calculates vertical field of view from focal length and sensor height
        /// </summary>
        double CalculateVerticalFOV(double focalLength, double sensorHeight);

        /// <summary>
        /// Estimates sensor dimensions based on phone model if available
        /// </summary>
        Task<Result<SensorDimensions>> EstimateSensorDimensionsAsync(string phoneModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a phone camera profile from EXIF data
        /// </summary>
        Task<Result<PhoneCameraProfile>> CreatePhoneCameraProfileAsync(
            string phoneModel,
            double focalLength,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates overlay box dimensions for FOV preview
        /// </summary>
        OverlayBox CalculateOverlayBox(double phoneFOV, double cameraFOV, Size screenSize);
    }

    public class SensorDimensions
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public string SensorType { get; set; } = "Unknown";

        public SensorDimensions(double width, double height, string sensorType = "Unknown")
        {
            Width = width;
            Height = height;
            SensorType = sensorType;
        }
    }

    public class OverlayBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class Size
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}