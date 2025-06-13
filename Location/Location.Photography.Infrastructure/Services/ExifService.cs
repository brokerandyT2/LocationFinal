// Location.Photography.Infrastructure/Services/ExifService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Resources;
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
                    return Result<ExifData>.Failure(AppResources.Exif_Error_ImagePathNullOrEmpty);
                }

                if (!File.Exists(imagePath))
                {
                    return Result<ExifData>.Failure(AppResources.Exif_Error_ImageFileDoesNotExist);
                }

                return await Task.Run(async () =>
                {
                    // Retry logic to handle file locks
                    const int maxRetries = 5;
                    const int delayMs = 200;

                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var directories = ImageMetadataReader.ReadMetadata(imagePath);
                            var exifData = ExtractDataFromDirectories(directories);
                            return Result<ExifData>.Success(exifData);
                        }
                        catch (IOException ioEx) when (attempt < maxRetries - 1)
                        {
                            // File is locked, wait and retry
                            _logger.LogWarning("File locked on attempt {Attempt}, retrying in {Delay}ms: {ImagePath}",
                                attempt + 1, delayMs, imagePath);
                            await Task.Delay(delayMs * (attempt + 1), cancellationToken);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to extract EXIF data from {ImagePath} on attempt {Attempt}",
                                imagePath, attempt + 1);

                            if (attempt == maxRetries - 1)
                                return Result<ExifData>.Failure(string.Format(AppResources.Exif_Error_FailedToExtractAfterRetries, maxRetries, ex.Message));
                        }
                    }

                    return Result<ExifData>.Failure(string.Format(AppResources.Exif_Error_FailedToExtractAfterRetriesGeneric, maxRetries));
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting EXIF data from {ImagePath}", imagePath);
                return Result<ExifData>.Failure(string.Format(AppResources.Exif_Error_ExtractingExifData, ex.Message));
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
                return Result<bool>.Failure(string.Format(AppResources.Exif_Error_CheckingExifData, ex.Message));
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