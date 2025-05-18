// Location.Photography.Infrastructure/Services/ExposureCalculatorService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Errors;
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
        private readonly IExposureTriangleService _exposureTriangleService;
        public ExposureCalculatorService(ILogger<ExposureCalculatorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public ExposureCalculatorService(ILogger<ExposureCalculatorService> logger, IExposureTriangleService exposureTriangleService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exposureTriangleService = exposureTriangleService ?? throw new ArgumentNullException(nameof(exposureTriangleService));
        }
        public async Task<Result<ExposureSettingsDto>> CalculateShutterSpeedAsync(
            ExposureTriangleDto baseExposure,
            string targetAperture,
            string targetIso,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default,
            double evCompensation = 0)
        {
            try
            {
                _logger.LogInformation("Calculating shutter speed for aperture {Aperture} and ISO {ISO} with EV {EV}",
                    targetAperture, targetIso, evCompensation);

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

                // Apply EV compensation to the base values
                DistributeEVCompensation(evCompensation, ref baseShutterValue, ref baseApertureValue, ref baseIsoValue);

                // Calculate the stops difference
                double apertureStopsDiff = CalculateApertureStopsDifference(baseApertureValue, targetApertureValue);
                double isoStopsDiff = CalculateIsoStopsDifference(baseIsoValue, targetIsoValue);

                // Calculate the new shutter speed in stops
                double newShutterStops = apertureStopsDiff + isoStopsDiff;

                // Apply the stops difference to the base shutter speed
                double newShutterValue = AdjustShutterSpeed(baseShutterValue, newShutterStops);

                string incrementString = GetIncrementString(increments);
                string newShutterSpeed;

                try
                {
                    // Find the nearest valid shutter speed value
                    newShutterSpeed = FindNearestShutterSpeed(newShutterValue, incrementString);
                }
                catch (ExposureParameterLimitError ex)
                {
                    // If there's a limit error, use the closest available value and include a warning
                    newShutterSpeed = ex.AvailableLimit;
                    _logger.LogWarning("Using closest available shutter speed: {ShutterSpeed}", newShutterSpeed);
                }

                var result = new ExposureSettingsDto
                {
                    ShutterSpeed = newShutterSpeed,
                    Aperture = targetAperture,
                    Iso = targetIso
                };

                // Check if the resulting exposure is within acceptable limits
                try
                {
                    CheckExposureLimits(newShutterSpeed, targetAperture, targetIso);
                }
                catch (OverexposedError ex)
                {
                    // Include overexposure warning in result
                    result.ErrorMessage = ex.Message;
                    _logger.LogWarning("Overexposure warning: {Message}", ex.Message);
                }
                catch (UnderexposedError ex)
                {
                    // Include underexposure warning in result
                    result.ErrorMessage = ex.Message;
                    _logger.LogWarning("Underexposure warning: {Message}", ex.Message);
                }

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
            CancellationToken cancellationToken = default,
            double evCompensation = 0)
        {
            try
            {
                _logger.LogInformation("Calculating aperture for shutter speed {ShutterSpeed} and ISO {ISO} with EV {EV}",
                    targetShutterSpeed, targetIso, evCompensation);

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

                // Apply EV compensation to the base values
                DistributeEVCompensation(evCompensation, ref baseShutterValue, ref baseApertureValue, ref baseIsoValue);

                // Calculate the stops difference
                double shutterStopsDiff = CalculateShutterStopsDifference(baseShutterValue, targetShutterValue);
                double isoStopsDiff = CalculateIsoStopsDifference(baseIsoValue, targetIsoValue);

                // Calculate the new aperture in stops
                double newApertureStops = shutterStopsDiff + isoStopsDiff;

                // Apply the stops difference to the base aperture
                double newApertureValue = AdjustAperture(baseApertureValue, newApertureStops);

                string incrementString = GetIncrementString(increments);
                string newAperture;

                try
                {
                    // Find the nearest valid aperture value
                    newAperture = FindNearestAperture(newApertureValue, incrementString);
                }
                catch (ExposureParameterLimitError ex)
                {
                    // If there's a limit error, use the closest available value and include a warning
                    newAperture = ex.AvailableLimit;
                    _logger.LogWarning("Using closest available aperture: {Aperture}", newAperture);
                }

                var result = new ExposureSettingsDto
                {
                    ShutterSpeed = targetShutterSpeed,
                    Aperture = newAperture,
                    Iso = targetIso
                };

                // Check if the resulting exposure is within acceptable limits
                try
                {
                    CheckExposureLimits(targetShutterSpeed, newAperture, targetIso);
                }
                catch (OverexposedError ex)
                {
                    // Include overexposure warning in result
                    result.ErrorMessage = ex.Message;
                    _logger.LogWarning("Overexposure warning: {Message}", ex.Message);
                }
                catch (UnderexposedError ex)
                {
                    // Include underexposure warning in result
                    result.ErrorMessage = ex.Message;
                    _logger.LogWarning("Underexposure warning: {Message}", ex.Message);
                }

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
            CancellationToken cancellationToken = default,
            double evCompensation = 0)
        {
            try
            {
                _logger.LogInformation("Calculating ISO for shutter speed {ShutterSpeed} and aperture {Aperture} with EV {EV}",
                    targetShutterSpeed, targetAperture, evCompensation);

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

                // Apply EV compensation to the base values
                DistributeEVCompensation(evCompensation, ref baseShutterValue, ref baseApertureValue, ref baseIsoValue);

                // Calculate the stops difference
                double shutterStopsDiff = CalculateShutterStopsDifference(baseShutterValue, targetShutterValue);
                double apertureStopsDiff = CalculateApertureStopsDifference(baseApertureValue, targetApertureValue);

                // Calculate the new ISO in stops
                double newIsoStops = -(shutterStopsDiff + apertureStopsDiff);

                // Apply the stops difference to the base ISO
                double newIsoValue = AdjustIso(baseIsoValue, newIsoStops);

                string incrementString = GetIncrementString(increments);
                string newIso;

                try
                {
                    // Find the nearest valid ISO value
                    newIso = FindNearestIso(newIsoValue, incrementString);
                }
                catch (ExposureParameterLimitError ex)
                {
                    // If there's a limit error, use the closest available value and include a warning
                    newIso = ex.AvailableLimit;
                    _logger.LogWarning("Using closest available ISO: {ISO}", newIso);
                }

                var result = new ExposureSettingsDto
                {
                    ShutterSpeed = targetShutterSpeed,
                    Aperture = targetAperture,
                    Iso = newIso
                };

                // Check if the resulting exposure is within acceptable limits
                try
                {
                    CheckExposureLimits(targetShutterSpeed, targetAperture, newIso);
                }
                catch (OverexposedError ex)
                {
                    // Include overexposure warning in result
                    result.ErrorMessage = ex.Message;
                    _logger.LogWarning("Overexposure warning: {Message}", ex.Message);
                }
                catch (UnderexposedError ex)
                {
                    // Include underexposure warning in result
                    result.ErrorMessage = ex.Message;
                    _logger.LogWarning("Underexposure warning: {Message}", ex.Message);
                }

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
                    var parts = shutterSpeed.Split('/');
                    if (parts.Length != 2 || !double.TryParse(parts[0], out double numerator) || !double.TryParse(parts[1], out double denominator))
                        return false;

                    value = numerator / denominator;
                    return true;
                }
                // Handle decimal notation (e.g., 0.5)
                else
                {
                    return double.TryParse(shutterSpeed, out value);
                }
            }
            catch
            {
                return false;
            }
        }

        private bool TryParseAperture(string aperture, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(aperture))
                return false;

            try
            {
                // Handle f-stop notation (e.g., f/2.8)
                if (aperture.StartsWith("f/") && aperture.Length > 2)
                {
                    var fStopValue = aperture.Substring(2);
                    return double.TryParse(fStopValue, out value);
                }
                // Handle numeric notation (e.g., 2.8)
                else
                {
                    return double.TryParse(aperture, out value);
                }
            }
            catch
            {
                return false;
            }
        }
        private bool TryParseIso(string iso, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(iso))
                return false;

            // ISO is just a number
            return double.TryParse(iso, out value);
        }

        private double CalculateShutterStopsDifference(double baseValue, double targetValue)
        {
            // Shutter speed follows a power-of-2 scale
            // e.g., 1/60 to 1/30 is +1 stop, 1/60 to 1/125 is -1 stop
            if (baseValue <= 0 || targetValue <= 0)
                throw new ArgumentException("Shutter speed values must be positive");

            return Math.Log2(baseValue / targetValue);
        }

        private double CalculateApertureStopsDifference(double baseValue, double targetValue)
        {
            // Aperture (f-stop) follows a sqrt(2) scale
            // e.g., f/4 to f/2.8 is +1 stop, f/4 to f/5.6 is -1 stop
            if (baseValue <= 0 || targetValue <= 0)
                throw new ArgumentException("Aperture values must be positive");

            // The formula is 2*log2(target/base) because f-stops are calculated as sqrt(2^n)
            return 2 * Math.Log2(baseValue / targetValue);
        }

        private double CalculateIsoStopsDifference(double baseValue, double targetValue)
        {
            // ISO follows a power-of-2 scale
            // e.g., ISO 100 to ISO 200 is +1 stop, ISO 100 to ISO 50 is -1 stop
            if (baseValue <= 0 || targetValue <= 0)
                throw new ArgumentException("ISO values must be positive");

            return Math.Log2(targetValue / baseValue);
        }

        private double AdjustShutterSpeed(double baseValue, double stopsDifference)
        {
            // Apply stop difference to shutter speed
            // Positive stops = slower shutter (more light), Negative stops = faster shutter (less light)
            return baseValue * Math.Pow(2, -stopsDifference);
        }

        private double AdjustAperture(double baseValue, double stopsDifference)
        {
            // Apply stop difference to aperture
            // Positive stops = smaller aperture (less light), Negative stops = larger aperture (more light)
            return baseValue * Math.Pow(Math.Sqrt(2), stopsDifference);
        }

        private double AdjustIso(double baseValue, double stopsDifference)
        {
            // Apply stop difference to ISO
            // Positive stops = higher ISO (more sensitivity), Negative stops = lower ISO (less sensitivity)
            return baseValue * Math.Pow(2, stopsDifference);
        }

        private string FindNearestShutterSpeed(double shutterValue, string incrementType)
        {
            string[] shutterSpeeds = ShutterSpeeds.GetScale(incrementType);

            // Convert all shutter speeds to numeric values for comparison
            var shutterValuePairs = new List<(double Value, string Text)>();

            foreach (var speed in shutterSpeeds)
            {
                if (TryParseShutterSpeed(speed, out double value))
                {
                    shutterValuePairs.Add((value, speed));
                }
            }

            // Find the nearest value
            shutterValuePairs = shutterValuePairs.OrderBy(p => Math.Abs(Math.Log2(p.Value / shutterValue))).ToList();

            // Check if the requested value exceeds the limits
            double slowestShutter = shutterValuePairs.Max(p => p.Value);
            double fastestShutter = shutterValuePairs.Min(p => p.Value);

            if (shutterValue > slowestShutter * 1.5)
            {
                double stopsOver = Math.Log2(shutterValue / slowestShutter);
                _logger.LogWarning("Requested shutter speed {ShutterValue} exceeds slowest available ({SlowLimit})",
                    shutterValue, shutterValuePairs.First(p => p.Value == slowestShutter).Text);

                throw new ExposureParameterLimitError(
                    "shutter speed",
                    FormatShutterSpeed(shutterValue),
                    shutterValuePairs.First(p => p.Value == slowestShutter).Text);
            }
            else if (shutterValue < fastestShutter / 1.5)
            {
                double stopsUnder = Math.Log2(fastestShutter / shutterValue);
                _logger.LogWarning("Requested shutter speed {ShutterValue} exceeds fastest available ({FastLimit})",
                    shutterValue, shutterValuePairs.First(p => p.Value == fastestShutter).Text);

                throw new ExposureParameterLimitError(
                    "shutter speed",
                    FormatShutterSpeed(shutterValue),
                    shutterValuePairs.First(p => p.Value == fastestShutter).Text);
            }

            return shutterValuePairs.First().Text;
        }

        private string FindNearestAperture(double apertureValue, string incrementType)
        {
            string[] apertures = Apetures.GetScale(incrementType);

            // Convert all apertures to numeric values for comparison
            var apertureValuePairs = new List<(double Value, string Text)>();

            foreach (var aperture in apertures)
            {
                if (TryParseAperture(aperture, out double value))
                {
                    apertureValuePairs.Add((value, aperture));
                }
            }

            // Find the nearest value (using logarithmic distance which is more appropriate for f-stops)
            apertureValuePairs = apertureValuePairs.OrderBy(p => Math.Abs(Math.Log2(p.Value / apertureValue))).ToList();

            // Check if the requested value exceeds the limits
            double widestAperture = apertureValuePairs.Min(p => p.Value); // Lower f-number = wider aperture
            double narrowestAperture = apertureValuePairs.Max(p => p.Value); // Higher f-number = narrower aperture

            if (apertureValue < widestAperture / 1.2) // About 1/3 stop tolerance
            {
                double stopsWider = Math.Log2(Math.Pow(widestAperture / apertureValue, 2));
                _logger.LogWarning("Requested aperture {ApertureValue} is wider than available ({WideLimit})",
                    apertureValue, apertureValuePairs.First(p => p.Value == widestAperture).Text);

                throw new ExposureParameterLimitError(
                    "aperture",
                    $"f/{apertureValue:F1}",
                    apertureValuePairs.First(p => p.Value == widestAperture).Text);
            }
            else if (apertureValue > narrowestAperture * 1.2)
            {
                double stopsNarrower = Math.Log2(Math.Pow(apertureValue / narrowestAperture, 2));
                _logger.LogWarning("Requested aperture {ApertureValue} is narrower than available ({NarrowLimit})",
                    apertureValue, apertureValuePairs.First(p => p.Value == narrowestAperture).Text);

                throw new ExposureParameterLimitError(
                    "aperture",
                    $"f/{apertureValue:F1}",
                    apertureValuePairs.First(p => p.Value == narrowestAperture).Text);
            }

            return apertureValuePairs.First().Text;
        }

        private string FindNearestIso(double isoValue, string incrementType)
        {
            string[] isos = ISOs.GetScale(incrementType);

            // Convert all ISOs to numeric values for comparison
            var isoValuePairs = new List<(double Value, string Text)>();

            foreach (var iso in isos)
            {
                if (TryParseIso(iso, out double value))
                {
                    isoValuePairs.Add((value, iso));
                }
            }

            // Find the nearest value
            isoValuePairs = isoValuePairs.OrderBy(p => Math.Abs(Math.Log2(p.Value / isoValue))).ToList();

            // Check if the requested value exceeds the limits
            double lowestIso = isoValuePairs.Min(p => p.Value);
            double highestIso = isoValuePairs.Max(p => p.Value);

            if (isoValue < lowestIso / 1.2) // About 1/3 stop tolerance
            {
                double stopsLower = Math.Log2(lowestIso / isoValue);
                _logger.LogWarning("Requested ISO {IsoValue} is lower than available ({LowLimit})",
                    isoValue, isoValuePairs.First(p => p.Value == lowestIso).Text);

                throw new ExposureParameterLimitError(
                    "ISO",
                    isoValue.ToString("F0"),
                    isoValuePairs.First(p => p.Value == lowestIso).Text);
            }
            else if (isoValue > highestIso * 1.2)
            {
                double stopsHigher = Math.Log2(isoValue / highestIso);
                _logger.LogWarning("Requested ISO {IsoValue} is higher than available ({HighLimit})",
                    isoValue, isoValuePairs.First(p => p.Value == highestIso).Text);

                throw new ExposureParameterLimitError(
                    "ISO",
                    isoValue.ToString("F0"),
                    isoValuePairs.First(p => p.Value == highestIso).Text);
            }

            return isoValuePairs.First().Text;
        }

        private string FormatShutterSpeed(double seconds)
        {
            if (seconds >= 1.0)
            {
                if (seconds >= 10.0)
                    return $"{seconds:F0}\"";
                else
                    return $"{seconds:F1}\"";
            }
            else if (seconds >= 0.5)
            {
                return $"{seconds:F1}";
            }
            else
            {
                // For fractions of a second, convert to standard format (1/X)
                int denominator = (int)Math.Round(1.0 / seconds);
                return $"1/{denominator}";
            }
        }

        /// <summary>
        /// Checks if the calculated exposure values indicate over or under exposure
        /// </summary>
        private void CheckExposureLimits(string shutterSpeed, string aperture, string iso)
        {
            // Get the maximum and minimum values from the available scales
            var shutterSpeeds = ShutterSpeeds.Thirds;
            var apertures = Apetures.Thirds;
            var isos = ISOs.Thirds;

            // Parse the calculated values
            if (!TryParseShutterSpeed(shutterSpeed, out double shutterValue) ||
                !TryParseAperture(aperture, out double apertureValue) ||
                !TryParseIso(iso, out double isoValue))
            {
                return; // Skip checking if parsing fails
            }

            // Parse the extreme values from the scales
            if (!TryParseShutterSpeed(shutterSpeeds.First(), out double slowestShutter) ||
                !TryParseShutterSpeed(shutterSpeeds.Last(), out double fastestShutter) ||
                !TryParseAperture(apertures.First(), out double widestAperture) ||
                !TryParseAperture(apertures.Last(), out double narrowestAperture) ||
                !TryParseIso(isos.First(), out double highestIso) ||
                !TryParseIso(isos.Last(), out double lowestIso))
            {
                return; // Skip checking if parsing fails
            }

            // Calculate the exposure value of the current settings
            double ev = CalculateEV(shutterValue, apertureValue, isoValue);

            // Calculate the minimum achievable EV with available settings
            double minEV = CalculateEV(slowestShutter, widestAperture, highestIso);

            // Calculate the maximum achievable EV with available settings
            double maxEV = CalculateEV(fastestShutter, narrowestAperture, lowestIso);

            // Check if our calculated exposure is outside the achievable range
            if (ev < minEV)
            {
                double stopsUnderexposed = minEV - ev;
                _logger.LogWarning("Calculated exposure is {Stops:F1} stops underexposed", stopsUnderexposed);
                throw new UnderexposedError(stopsUnderexposed);
            }
            else if (ev > maxEV)
            {
                double stopsOverexposed = ev - maxEV;
                _logger.LogWarning("Calculated exposure is {Stops:F1} stops overexposed", stopsOverexposed);
                throw new OverexposedError(stopsOverexposed);
            }
        }

        /// <summary>
        /// Calculates the exposure value (EV) for a given combination of settings
        /// </summary>
        private double CalculateEV(double shutterSpeed, double aperture, double iso)
        {
            // Standard EV formula: EV = log2(aperture²/shutter) - log2(ISO/100)
            double ev = Math.Log2(Math.Pow(aperture, 2) / shutterSpeed) - Math.Log2(iso / 100);
            return ev;
        }

        /// <summary>
        /// Distributes EV compensation across exposure settings
        /// </summary>
        /// <param name="evValue">The EV compensation value</param>
        /// <param name="shutterSpeed">Reference to shutter speed value (will be modified)</param>
        /// <param name="aperture">Reference to aperture value (will be modified)</param>
        /// <param name="iso">Reference to ISO value (will be modified)</param>
        /// <param name="distribution">How to distribute EV: 0=balanced, 1=shutter priority, 2=aperture priority, 3=ISO priority</param>
        private void DistributeEVCompensation(double evValue, ref double shutterSpeed, ref double aperture, ref double iso, int distribution = 0)
        {
            // If EV value is negligible, do nothing
            if (Math.Abs(evValue) < 0.01)
                return;

            switch (distribution)
            {
                case 1: // Shutter priority
                    // Apply all EV to shutter speed
                    shutterSpeed = AdjustShutterSpeed(shutterSpeed, -evValue);
                    break;

                case 2: // Aperture priority
                    // Apply all EV to aperture
                    aperture = AdjustAperture(aperture, evValue);
                    break;

                case 3: // ISO priority
                    // Apply all EV to ISO
                    iso = AdjustIso(iso, evValue);
                    break;

                case 0: // Balanced (default)
                default:
                    // Distribute EV evenly among the three parameters
                    double evPerComponent = evValue / 3.0;

                    // Adjust shutter speed: +EV = faster shutter (less light)
                    shutterSpeed = AdjustShutterSpeed(shutterSpeed, -evPerComponent);

                    // Adjust aperture: +EV = smaller aperture (less light)
                    aperture = AdjustAperture(aperture, evPerComponent);

                    // Adjust ISO: +EV = lower ISO (less sensitivity)
                    iso = AdjustIso(iso, evPerComponent);
                    break;
            }
        }
        #endregion
    }
}