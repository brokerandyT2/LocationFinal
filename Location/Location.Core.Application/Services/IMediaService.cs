using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Services
{
    /// <summary>
    /// Interface for media services (photo capture/selection)
    /// </summary>
    public interface IMediaService
    {
        /// <summary>
        /// Captures a photo using the device camera
        /// </summary>
        Task<Result<string>> CapturePhotoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Picks a photo from the device gallery
        /// </summary>
        Task<Result<string>> PickPhotoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the device has camera support
        /// </summary>
        Task<Result<bool>> IsCaptureSupported(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a photo file
        /// </summary>
        Task<Result<bool>> DeletePhotoAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the app's photo storage directory
        /// </summary>
        string GetPhotoStorageDirectory();
    }
}