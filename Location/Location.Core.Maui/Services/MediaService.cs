using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;

namespace Location.Core.Maui.Services
{
    public class MediaService : IMediaService
    {
        private readonly string _photoStorageDirectory;

        public MediaService()
        {
            _photoStorageDirectory = Path.Combine(
                FileSystem.AppDataDirectory,
                "Photos");

            // Ensure the directory exists
            if (!Directory.Exists(_photoStorageDirectory))
            {
                Directory.CreateDirectory(_photoStorageDirectory);
            }
        }

        public async Task<Result<string>> CapturePhotoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var captureSupported = await IsCaptureSupported(cancellationToken);
                if (!captureSupported.IsSuccess || !captureSupported.Data)
                {
                    return Result<string>.Failure("Photo capture is not supported on this device");
                }

                var photo = await MediaPicker.CapturePhotoAsync();
                if (photo == null)
                {
                    return Result<string>.Failure("No photo was captured");
                }

                // Create a unique filename
                var fileName = Path.GetRandomFileName() + Path.GetExtension(photo.FileName);
                var filePath = Path.Combine(_photoStorageDirectory, fileName);

                // Save the file
                using (var sourceStream = await photo.OpenReadAsync())
                using (var destinationStream = File.Create(filePath))
                {
                    await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                }

                return Result<string>.Success(filePath);
            }
            catch (FeatureNotSupportedException)
            {
                return Result<string>.Failure("Photo capture is not supported on this device");
            }
            catch (PermissionException)
            {
                return Result<string>.Failure("Permission to capture photos was denied");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error capturing photo: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeletePhotoAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return Result<bool>.Failure("Photo not found");
                }

                File.Delete(filePath);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error deleting photo: {ex.Message}");
            }
        }

        public string GetPhotoStorageDirectory()
        {
            return _photoStorageDirectory;
        }

        public async Task<Result<bool>> IsCaptureSupported(CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        return Result<bool>.Failure("Camera permission denied");
                    }
                }

                return Result<bool>.Success(MediaPicker.IsCaptureSupported);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error checking capture support: {ex.Message}");
            }
        }

        public async Task<Result<string>> PickPhotoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                if (photo == null)
                {
                    return Result<string>.Failure("No photo was selected");
                }

                // Create a unique filename
                var fileName = Path.GetRandomFileName() + Path.GetExtension(photo.FileName);
                var filePath = Path.Combine(_photoStorageDirectory, fileName);

                // Save the file
                using (var sourceStream = await photo.OpenReadAsync())
                using (var destinationStream = File.Create(filePath))
                {
                    await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                }

                return Result<string>.Success(filePath);
            }
            catch (FeatureNotSupportedException)
            {
                return Result<string>.Failure("Photo picking is not supported on this device");
            }
            catch (PermissionException)
            {
                return Result<string>.Failure("Permission to access photos was denied");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error picking photo: {ex.Message}");
            }
        }
    }
}