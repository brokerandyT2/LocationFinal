// Location.Photography.Application/Services/IExifService.cs
using Location.Core.Application.Common.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface IExifService
    {
        /// <summary>
        /// Extracts EXIF data from an image file
        /// </summary>
        Task<Result<ExifData>> ExtractExifDataAsync(string imagePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts EXIF data from an image stream
        /// </summary>
        Task<Result<ExifData>> ExtractExifDataAsync(Stream imageStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the image contains the required EXIF data for camera calibration
        /// </summary>
        Task<Result<bool>> HasRequiredExifDataAsync(string imagePath, CancellationToken cancellationToken = default);
    }

    public class ExifData
    {
        public double? FocalLength { get; set; }
        public string? CameraModel { get; set; }
        public string? CameraMake { get; set; }
        public DateTime? DateTaken { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public double? Aperture { get; set; }
        public string? LensModel { get; set; }
        public bool HasValidFocalLength => FocalLength.HasValue && FocalLength.Value > 0;
        public string FullCameraModel => $"{CameraMake} {CameraModel}".Trim();
    }
}