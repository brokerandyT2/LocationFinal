// Location.Photography.Infrastructure/Services/ExifService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class ExifService : IExifService
    {
        private readonly ILogger<ExifService> _logger;

        public ExifService(ILogger<ExifService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ExifData>> ExtractExifDataAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    return Result<ExifData>.Failure("Image path cannot be null or empty");
                }

                if (!File.Exists(imagePath))
                {
                    return Result<ExifData>.Failure("Image file does not exist");
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        var directories = ImageMetadataReader.ReadMetadata(imagePath);
                        var exifData = ExtractDataFromDirectories(directories);
                        return Result<ExifData>.Success(exifData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract EXIF data from {ImagePath}", imagePath);
                        return Result<ExifData>.Failure($"Failed to extract EXIF data: {ex.Message}");
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting EXIF data from {ImagePath}", imagePath);
                return Result<ExifData>.Failure($"Error extracting EXIF data: {ex.Message}");
            }
        }

        public async Task<Result<ExifData>> ExtractExifDataAsync(Stream imageStream, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (imageStream == null)
                {
                    return Result<ExifData>.Failure("Image stream cannot be null");
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        var directories = ImageMetadataReader.ReadMetadata(imageStream);
                        var exifData = ExtractDataFromDirectories(directories);
                        return Result<ExifData>.Success(exifData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract EXIF data from stream");
                        return Result<ExifData>.Failure($"Failed to extract EXIF data: {ex.Message}");
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting EXIF data from stream");
                return Result<ExifData>.Failure($"Error extracting EXIF data: {ex.Message}");
            }
        }

        public async Task<Result<bool>> HasRequiredExifDataAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var exifResult = await ExtractExifDataAsync(imagePath, cancellationToken);

                if (!exifResult.IsSuccess)
                {
                    return Result<bool>.Success(false);
                }

                var hasRequired = exifResult.Data.HasValidFocalLength &&
                                !string.IsNullOrEmpty(exifResult.Data.FullCameraModel);

                return Result<bool>.Success(hasRequired);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking EXIF data requirements for {ImagePath}", imagePath);
                return Result<bool>.Failure($"Error checking EXIF data: {ex.Message}");
            }
        }

        private ExifData ExtractDataFromDirectories(IEnumerable<MetadataExtractor.Directory> directories)
        {
            var exifData = new ExifData();

            foreach (var directory in directories)
            {
                if (directory is ExifIfd0Directory ifd0Directory)
                {
                    // Camera make and model
                    if (ifd0Directory.HasTagName(ExifDirectoryBase.TagMake))
                        exifData.CameraMake = ifd0Directory.GetString(ExifDirectoryBase.TagMake)?.Trim();

                    if (ifd0Directory.HasTagName(ExifDirectoryBase.TagModel))
                        exifData.CameraModel = ifd0Directory.GetString(ExifDirectoryBase.TagModel)?.Trim();

                    // Date taken
                    if (ifd0Directory.HasTagName(ExifDirectoryBase.TagDateTime))
                    {
                        if (ifd0Directory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
                            exifData.DateTaken = dateTime;
                    }
                }
                else if (directory is ExifSubIfdDirectory subIfdDirectory)
                {
                    // Focal length
                    if (subIfdDirectory.HasTagName(ExifDirectoryBase.TagFocalLength))
                    {
                        if (subIfdDirectory.TryGetRational(ExifDirectoryBase.TagFocalLength, out var focalLengthRational))
                            exifData.FocalLength = focalLengthRational.ToDouble();
                    }

                    // Aperture
                    if (subIfdDirectory.HasTagName(ExifDirectoryBase.TagFNumber))
                    {
                        if (subIfdDirectory.TryGetRational(ExifDirectoryBase.TagFNumber, out var apertureRational))
                            exifData.Aperture = apertureRational.ToDouble();
                    }

                    // Image dimensions
                    if (subIfdDirectory.HasTagName(ExifDirectoryBase.TagExifImageWidth))
                    {
                        if (subIfdDirectory.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out var width))
                            exifData.ImageWidth = width;
                    }

                    if (subIfdDirectory.HasTagName(ExifDirectoryBase.TagExifImageHeight))
                    {
                        if (subIfdDirectory.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out var height))
                            exifData.ImageHeight = height;
                    }

                    // Lens model
                    if (subIfdDirectory.HasTagName(ExifDirectoryBase.TagLensModel))
                        exifData.LensModel = subIfdDirectory.GetString(ExifDirectoryBase.TagLensModel)?.Trim();
                }
            }

            return exifData;
        }
    }
}