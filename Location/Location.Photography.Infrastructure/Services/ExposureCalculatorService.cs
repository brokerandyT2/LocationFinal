// Location.Photography.Infrastructure/Services/ExposureCalculatorService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class ExposureCalculatorService : IExposureCalculatorService
    {
        private readonly ILogger<ExposureCalculatorService> _logger;

        public ExposureCalculatorService(ILogger<ExposureCalculatorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ExposureSettingsDto>> CalculateShutterSpeedAsync(
            ExposureTriangleDto baseExposure,
            string targetAperture,
            string targetIso,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Calculating shutter speed for aperture {Aperture} and ISO {ISO}", targetAperture, targetIso);

                // Parse base exposure values
                if (!TryParseExposureValues(baseExposure, out double baseShutterValue, out double baseApertureValue, out double baseIsoValue))
                {
                    return Result<ExposureSettingsDto>.Failure("Invalid base exposure values");
                }

                // Parse target values
                if (!TryParseAperture(targetAperture, out double targetApertureValue))
                {
                    return Result<ExposureSettingsDto>.Failure($"Invalid aperture value: {targetAperture}");
                }

                if (!TryParseIso(targetIso, out double targetIsoValue))
                {
                    return Result<ExposureSettingsDto>.Failure($"Invalid ISO value: {targetIso}");
                }

                // Calculate the stops difference
                double apertureStopsDiff = CalculateApertureStopsDifference(baseApertureValue, targetApertureValue);
                double isoStopsDiff = CalculateIsoStopsDifference(baseIsoValue, targetIsoValue);

                // Calculate the new shutter speed in stops
                double newShutterStops = apertureStopsDiff + isoStopsDiff;

                // Apply the stops difference to the base shutter speed
                double newShutterValue = AdjustShutterSpeed(baseShutterValue, newShutterStops);

                // Find the nearest valid shutter speed value
                string incrementString = GetIncrementString(increments);
                string newShutterSpeed = FindNearestShutterSpeed(newShutterValue, incrementString);

                var result = new ExposureSettingsDto
                {
                    ShutterSpeed = newShutterSpeed,
                    Aperture = targetAperture,
                    Iso = targetIso
                };

                return Result<ExposureSettingsDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shutter speed");
                return Result<ExposureSettingsDto>.Failure($"Error calculating shutter speed: {ex.Message}");
            }
        }

        public async Task<Result<ExposureSettingsDto>> CalculateApertureAsync(
            ExposureTriangleDto baseExposure,
            string targetShutterSpeed,
            string targetIso,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Calculating aperture for shutter speed {ShutterSpeed} and ISO {ISO}", targetShutterSpeed, targetIso);

                // Parse base exposure values
                if (!TryParseExposureValues(baseExposure, out double baseShutterValue, out double baseApertureValue, out double baseIsoValue))
                {
                    return Result<ExposureSettingsDto>.Failure("Invalid base exposure values");
                }

                // Parse target values
                if (!TryParseShutterSpeed(targetShutterSpeed, out double targetShutterValue))
                {
                    return Result<ExposureSettingsDto>.Failure($"Invalid shutter speed value: {targetShutterSpeed}");
                }

                if (!TryParseIso(targetIso, out double targetIsoValue))
                {
                    return Result<ExposureSettingsDto>.Failure($"Invalid ISO value: {targetIso}");
                }

                // Calculate the stops difference
                double shutterStopsDiff = CalculateShutterStopsDifference(baseShutterValue, targetShutterValue);
                double isoStopsDiff = CalculateIsoStopsDifference(baseIsoValue, targetIsoValue);

                // Calculate the new aperture in stops
                double newApertureStops = shutterStopsDiff + isoStopsDiff;

                // Apply the stops difference to the base aperture
                double newApertureValue = AdjustAperture(baseApertureValue, newApertureStops);

                // Find the nearest valid aperture value
                string incrementString = GetIncrementString(increments);
                string newAperture = FindNearestAperture(newApertureValue, incrementString);

                var result = new ExposureSettingsDto
                {
                    ShutterSpeed = targetShutterSpeed,
                    Aperture = newAperture,
                    Iso = targetIso
                };

                return Result<ExposureSettingsDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating aperture");
                return Result<ExposureSettingsDto>.Failure($"Error calculating aperture: {ex.Message}");
            }
        }

        public async Task<Result<ExposureSettingsDto>> CalculateIsoAsync(
            ExposureTriangleDto baseExposure,
            string targetShutterSpeed,
            string targetAperture,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Calculating ISO for shutter speed {ShutterSpeed} and aperture {Aperture}", targetShutterSpeed, targetAperture);

                // Parse base exposure values
                if (!TryParseExposureValues(baseExposure, out double baseShutterValue, out double baseApertureValue, out double baseIsoValue))
                {
                    return Result<ExposureSettingsDto>.Failure("Invalid base exposure values");
                }

                // Parse target values
                if (!TryParseShutterSpeed(targetShutterSpeed, out double targetShutterValue))
                {
                    return Result<ExposureSettingsDto>.Failure($"Invalid shutter speed value: {targetShutterSpeed}");
                }

                if (!TryParseAperture(targetAperture, out double targetApertureValue))
                {
                    return Result<ExposureSettingsDto>.Failure($"Invalid aperture value: {targetAperture}");
                }

                // Calculate the stops difference
                double shutterStopsDiff = CalculateShutterStopsDifference(baseShutterValue, targetShutterValue);
                double apertureStopsDiff = CalculateApertureStopsDifference(baseApertureValue, targetApertureValue);

                // Calculate the new ISO in stops
                double newIsoStops = -(shutterStopsDiff + apertureStopsDiff);

                // Apply the stops difference to the base ISO
                double newIsoValue = AdjustIso(baseIsoValue, newIsoStops);

                // Find the nearest valid ISO value
                string incrementString = GetIncrementString(increments);
                string newIso = FindNearestIso(newIsoValue, incrementString);

                var result = new ExposureSettingsDto
                {
                    ShutterSpeed = targetShutterSpeed,
                    Aperture = targetAperture,
                    Iso = newIso
                };

                return Result<ExposureSettingsDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ISO");
                return Result<ExposureSettingsDto>.Failure($"Error calculating ISO: {ex.Message}");
            }
        }

        public async Task<Result<string[]>> GetShutterSpeedsAsync(ExposureIncrements increments, CancellationToken cancellationToken = default)
        {
            try
            {
                string incrementString = GetIncrementString(increments);
                var shutterSpeeds = ShutterSpeeds.GetScale(incrementString);
                return Result<string[]>.Success(shutterSpeeds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shutter speeds");
                return Result<string[]>.Failure($"Error retrieving shutter speeds: {ex.Message}");
            }
        }

        public async Task<Result<string[]>> GetAperturesAsync(ExposureIncrements increments, CancellationToken cancellationToken = default)
        {
            try
            {
                string incrementString = GetIncrementString(increments);
                var apertures = Apetures.GetScale(incrementString);
                return Result<string[]>.Success(apertures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving apertures");
                return Result<string[]>.Failure($"Error retrieving apertures: {ex.Message}");
            }
        }

        public async Task<Result<string[]>> GetIsosAsync(ExposureIncrements increments, CancellationToken cancellationToken = default)
        {
            try
            {
                string incrementString = GetIncrementString(increments);
                var isos = ISOs.GetScale(incrementString);
                return Result<string[]>.Success(isos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ISOs");
                return Result<string[]>.Failure($"Error retrieving ISOs: {ex.Message}");
            }
        }

        #region Helper Methods

        private string GetIncrementString(ExposureIncrements increments)
        {
            return increments switch
            {
                ExposureIncrements.Full => "Full",
                ExposureIncrements.Half => "Halves",
                ExposureIncrements.Third => "Thirds",
                _ => "Full"
            };
        }

        private bool TryParseExposureValues(ExposureTriangleDto exposure, out double shutterValue, out double apertureValue, out double isoValue)
        {
            shutterValue = 0;
            apertureValue = 0;
            isoValue = 0;

            return TryParseShutterSpeed(exposure.ShutterSpeed, out shutterValue) &&
                   TryParseAperture(exposure.Aperture, out apertureValue) &&
                   TryParseIso(exposure.Iso, out isoValue);
        }

        private bool TryParseShutterSpeed(string shutterSpeed, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(shutterSpeed))
                return false;

            try
            {
                // Handle seconds notation (with quote mark)
                if (shutterSpeed.EndsWith("\""))
                {
                    var seconds = shutterSpeed.TrimEnd('\"');
                    return double.TryParse(seconds, out value);
                }
                // Handle fractional notation (e.g., 1/125)
                else if (shutterSpeed.Contains('/'))
                {
                    var parts = shutterSpeed.Split('/