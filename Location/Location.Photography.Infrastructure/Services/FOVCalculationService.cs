// Location.Photography.Infrastructure/Services/FOVCalculationService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class FOVCalculationService : IFOVCalculationService
    {
        private readonly ILogger<FOVCalculationService> _logger;

        // Common phone sensor dimensions (in mm)
        private readonly Dictionary<string, SensorDimensions> _phoneSensorDatabase = new()
        {
            { "iphone", new SensorDimensions(7.0, 5.3, "1/2.55\"") },
            { "samsung galaxy", new SensorDimensions(7.2, 5.4, "1/2.4\"") },
            { "google pixel", new SensorDimensions(7.4, 5.6, "1/2.3\"") },
            { "oneplus", new SensorDimensions(7.0, 5.3, "1/2.55\"") },
            { "xiaomi", new SensorDimensions(7.2, 5.4, "1/2.4\"") },
            // Default fallback
            { "default", new SensorDimensions(7.0, 5.3, "1/2.55\"") }
        };

        public FOVCalculationService(ILogger<FOVCalculationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public double CalculateHorizontalFOV(double focalLength, double sensorWidth)
        {
            if (focalLength <= 0 || sensorWidth <= 0)
            {
                throw new ArgumentException(AppResources.FOVCalculation_Error_FocalLengthAndSensorWidthMustBePositive);
            }

            // FOV = 2 * arctan(sensor_dimension / (2 * focal_length))
            var fovRadians = 2 * Math.Atan(sensorWidth / (2 * focalLength));
            var fovDegrees = fovRadians * (180.0 / Math.PI);

            return fovDegrees;
        }

        public double CalculateVerticalFOV(double focalLength, double sensorHeight)
        {
            if (focalLength <= 0 || sensorHeight <= 0)
            {
                throw new ArgumentException(AppResources.FOVCalculation_Error_FocalLengthAndSensorHeightMustBePositive);
            }

            var fovRadians = 2 * Math.Atan(sensorHeight / (2 * focalLength));
            var fovDegrees = fovRadians * (180.0 / Math.PI);

            return fovDegrees;
        }

        public async Task<Result<SensorDimensions>> EstimateSensorDimensionsAsync(string phoneModel, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(phoneModel))
                {
                    return Result<SensorDimensions>.Success(_phoneSensorDatabase["default"]);
                }

                return await Task.Run(() =>
                {
                    var modelLower = phoneModel.ToLowerInvariant();

                    // Try to match known phone models
                    foreach (var kvp in _phoneSensorDatabase)
                    {
                        if (modelLower.Contains(kvp.Key))
                        {
                            _logger.LogDebug("Found sensor dimensions for {PhoneModel}: {SensorType}",
                                phoneModel, kvp.Value.SensorType);
                            return Result<SensorDimensions>.Success(kvp.Value);
                        }
                    }

                    // Default fallback
                    _logger.LogDebug("Using default sensor dimensions for unknown phone model: {PhoneModel}", phoneModel);
                    return Result<SensorDimensions>.Success(_phoneSensorDatabase["default"]);

                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating sensor dimensions for {PhoneModel}", phoneModel);
                return Result<SensorDimensions>.Failure(string.Format(AppResources.FOVCalculation_Error_EstimatingSensorDimensions, ex.Message));
            }
        }

        public async Task<Result<PhoneCameraProfile>> CreatePhoneCameraProfileAsync(
            string phoneModel,
            double focalLength,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(phoneModel))
                {
                    return Result<PhoneCameraProfile>.Failure(AppResources.FOVCalculation_Error_PhoneModelCannotBeNullOrEmpty);
                }

                if (focalLength <= 0)
                {
                    return Result<PhoneCameraProfile>.Failure(AppResources.FOVCalculation_Error_FocalLengthMustBePositive);
                }

                // Get sensor dimensions for this phone model
                var sensorResult = await EstimateSensorDimensionsAsync(phoneModel, cancellationToken);
                if (!sensorResult.IsSuccess)
                {
                    return Result<PhoneCameraProfile>.Failure(string.Format(AppResources.FOVCalculation_Error_FailedToGetSensorDimensions, sensorResult.ErrorMessage));
                }

                var sensor = sensorResult.Data;

                // Calculate FOV
                var horizontalFOV = CalculateHorizontalFOV(focalLength, sensor.Width);

                // Create the phone camera profile
                var profile = new PhoneCameraProfile(
                    phoneModel,
                    focalLength,
                    horizontalFOV);

                _logger.LogInformation("Created phone camera profile for {PhoneModel}: {FocalLength}mm, {FOV}° FOV",
                    phoneModel, focalLength, horizontalFOV);

                return Result<PhoneCameraProfile>.Success(profile);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating phone camera profile for {PhoneModel}", phoneModel);
                return Result<PhoneCameraProfile>.Failure(string.Format(AppResources.FOVCalculation_Error_CreatingCameraProfile, ex.Message));
            }
        }

        public OverlayBox CalculateOverlayBox(double phoneFOV, double cameraFOV, Size screenSize)
        {
            if (phoneFOV <= 0 || cameraFOV <= 0)
            {
                throw new ArgumentException(AppResources.FOVCalculation_Error_FOVValuesMustBePositive);
            }

            if (screenSize.Width <= 0 || screenSize.Height <= 0)
            {
                throw new ArgumentException(AppResources.FOVCalculation_Error_ScreenSizeMustBePositive);
            }

            // Calculate scale factor based on FOV ratio
            var scaleFactor = cameraFOV / phoneFOV;

            // Calculate overlay box dimensions
            var boxWidth = (int)(screenSize.Width * scaleFactor);
            var boxHeight = (int)(screenSize.Height * scaleFactor);

            // Center the overlay box
            var x = (screenSize.Width - boxWidth) / 2;
            var y = (screenSize.Height - boxHeight) / 2;

            // Ensure the box stays within screen bounds
            boxWidth = Math.Min(boxWidth, screenSize.Width);
            boxHeight = Math.Min(boxHeight, screenSize.Height);
            x = Math.Max(0, x);
            y = Math.Max(0, y);

            return new OverlayBox
            {
                X = x,
                Y = y,
                Width = boxWidth,
                Height = boxHeight
            };
        }
    }
}