// Location.Photography.Infrastructure/Services/ExposureTriangleService.cs
using Location.Photography.Application.Errors;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Services;
using System;
using System.Globalization;
using System.Linq;

namespace Location.Photography.Infrastructure.Services
{
    public class ExposureTriangleService : IExposureTriangleService
    {
        // Constants for exposure calculations
        private const double LOG2 = 0.6931471805599453;

        public string CalculateShutterSpeed(
            string baseShutterSpeed,
            string baseAperture,
            string baseIso,
            string targetAperture,
            string targetIso,
            int scale,
            double evCompensation = 0)
        {
            // Parse values
            double baseShutterValue = ParseShutterSpeed(baseShutterSpeed);
            double baseApertureValue = ParseAperture(baseAperture);
            double baseIsoValue = ParseIso(baseIso);
            double targetApertureValue = ParseAperture(targetAperture);
            double targetIsoValue = ParseIso(targetIso);

            // Calculate the EV difference 
            double evDiff = CalculateEvDifference(
                baseApertureValue, baseIsoValue,
                targetApertureValue, targetIsoValue);

            // Apply EV compensation
            evDiff += evCompensation;

            // Calculate the new shutter speed value
            double newShutterValue = baseShutterValue * Math.Pow(2, evDiff);

            // Get the appropriate scale of shutter speeds
            string[] shutterSpeeds = GetShutterSpeedScale(scale);

            // Find the closest available shutter speed
            string newShutterSpeed = FindClosestValue(shutterSpeeds, newShutterValue);

            // Check for extreme values and raise appropriate errors
            double actualNewShutterValue = ParseShutterSpeed(newShutterSpeed);
            if (actualNewShutterValue < newShutterValue * 0.75)
            {
                double stopsUnder = Math.Log(newShutterValue / actualNewShutterValue) / LOG2;
                throw new UnderexposedError(stopsUnder);
            }
            else if (actualNewShutterValue > newShutterValue * 1.5)
            {
                double stopsOver = Math.Log(actualNewShutterValue / newShutterValue) / LOG2;
                throw new OverexposedError(stopsOver);
            }

            return newShutterSpeed;
        }

        public string CalculateAperture(
            string baseShutterSpeed,
            string baseAperture,
            string baseIso,
            string targetShutterSpeed,
            string targetIso,
            int scale,
            double evCompensation = 0)
        {
            // Parse values
            double baseShutterValue = ParseShutterSpeed(baseShutterSpeed);
            double baseApertureValue = ParseAperture(baseAperture);
            double baseIsoValue = ParseIso(baseIso);
            double targetShutterValue = ParseShutterSpeed(targetShutterSpeed);
            double targetIsoValue = ParseIso(targetIso);

            // Calculate the EV difference
            double evDiff = CalculateEvDifference(
                baseShutterValue, baseIsoValue,
                targetShutterValue, targetIsoValue);

            // Apply EV compensation (note: aperture moves in opposite direction)
            evDiff -= evCompensation;

            // Calculate the new aperture value (f-number)
            // For aperture, f-stop increases by sqrt(2) for each EV step (not 2 like shutter and ISO)
            double newApertureValue = baseApertureValue * Math.Pow(Math.Sqrt(2), evDiff);

            // Get the appropriate scale of aperture values
            string[] apertures = GetApertureScale(scale);

            // Find the closest available aperture
            string newAperture = FindClosestValue(apertures, newApertureValue);

            // Check for extreme values
            double actualNewApertureValue = ParseAperture(newAperture);
            if (actualNewApertureValue < newApertureValue * 0.9)
            {
                double stopsOver = Math.Log(Math.Pow(newApertureValue / actualNewApertureValue, 2)) / LOG2;
                throw new OverexposedError(stopsOver);
            }
            else if (actualNewApertureValue > newApertureValue * 1.1)
            {
                double stopsUnder = Math.Log(Math.Pow(actualNewApertureValue / newApertureValue, 2)) / LOG2;
                throw new UnderexposedError(stopsUnder);
            }

            return newAperture;
        }

        public string CalculateIso(
            string baseShutterSpeed,
            string baseAperture,
            string baseIso,
            string targetShutterSpeed,
            string targetAperture,
            int scale,
            double evCompensation = 0)
        {
            // Parse values
            double baseShutterValue = ParseShutterSpeed(baseShutterSpeed);
            double baseApertureValue = ParseAperture(baseAperture);
            double baseIsoValue = ParseIso(baseIso);
            double targetShutterValue = ParseShutterSpeed(targetShutterSpeed);
            double targetApertureValue = ParseAperture(targetAperture);

            // Calculate the EV difference
            double evDiff = CalculateEvDifference(
                baseShutterValue, baseApertureValue,
                targetShutterValue, targetApertureValue);

            // Apply EV compensation (note: ISO moves in opposite direction compared to shutter)
            evDiff -= evCompensation;

            // Calculate the new ISO value
            double newIsoValue = baseIsoValue * Math.Pow(2, evDiff);

            // Get the appropriate scale of ISO values
            string[] isoValues = GetIsoScale(scale);

            // Find the closest available ISO
            string newIso = FindClosestValue(isoValues, newIsoValue);

            // Check for extreme values
            double actualNewIsoValue = ParseIso(newIso);
            double maxIso = ParseIso(isoValues[0]); // Assuming ISO values are ordered from highest to lowest
            double minIso = ParseIso(isoValues[isoValues.Length - 1]);

            if (newIsoValue > maxIso)
            {
                throw new ExposureParameterLimitError("ISO", newIsoValue.ToString(CultureInfo.InvariantCulture), maxIso.ToString(CultureInfo.InvariantCulture));
            }
            else if (newIsoValue < minIso)
            {
                throw new ExposureParameterLimitError("ISO", newIsoValue.ToString(CultureInfo.InvariantCulture), minIso.ToString(CultureInfo.InvariantCulture));
            }
            else if (actualNewIsoValue < newIsoValue * 0.75)
            {
                double stopsUnder = Math.Log(newIsoValue / actualNewIsoValue) / LOG2;
                throw new UnderexposedError(stopsUnder);
            }
            else if (actualNewIsoValue > newIsoValue * 1.5)
            {
                double stopsOver = Math.Log(actualNewIsoValue / newIsoValue) / LOG2;
                throw new OverexposedError(stopsOver);
            }

            return newIso;
        }

        #region Helper Methods

        private double CalculateEvDifference(
            double baseParam1, double baseParam2,
            double targetParam1, double targetParam2)
        {
            // For shutter: more time = more light = negative EV
            // For aperture: higher f-number = less light = positive EV (squared due to area)
            // For ISO: higher ISO = more sensitivity = negative EV

            // Calculate EV difference for param1
            double evParam1 = 0;
            if (baseParam1 != 0 && targetParam1 != 0)
            {
                evParam1 = Math.Log(baseParam1 / targetParam1) / LOG2;
            }

            // Calculate EV difference for param2
            double evParam2 = 0;
            if (baseParam2 != 0 && targetParam2 != 0)
            {
                evParam2 = Math.Log(baseParam2 / targetParam2) / LOG2;
            }

            // Total EV difference
            return evParam1 + evParam2;
        }

        private double ParseShutterSpeed(string shutterSpeed)
        {
            if (string.IsNullOrWhiteSpace(shutterSpeed))
                return 0;

            // Handle fractional shutter speeds like "1/125"
            if (shutterSpeed.Contains('/'))
            {
                string[] parts = shutterSpeed.Split('/');
                if (parts.Length == 2 && double.TryParse(parts[0], out double numerator) &&
                    double.TryParse(parts[1], out double denominator) && denominator != 0)
                {
                    return numerator / denominator;
                }
            }
            // Handle speeds with seconds mark like "30""
            else if (shutterSpeed.EndsWith("\""))
            {
                string value = shutterSpeed.TrimEnd('\"');
                if (double.TryParse(value, out double seconds))
                {
                    return seconds;
                }
            }
            // Handle regular decimal values
            else if (double.TryParse(shutterSpeed, out double value))
            {
                return value;
            }

            throw new ArgumentException($"Invalid shutter speed format: {shutterSpeed}");
        }

        private double ParseAperture(string aperture)
        {
            if (string.IsNullOrWhiteSpace(aperture))
                return 0;

            // Handle f-stop format like "f/2.8"
            if (aperture.StartsWith("f/"))
            {
                string value = aperture.Substring(2);
                if (double.TryParse(value, out double fNumber))
                {
                    return fNumber;
                }
            }
            // Handle raw numbers
            else if (double.TryParse(aperture, out double value))
            {
                return value;
            }

            throw new ArgumentException($"Invalid aperture format: {aperture}");
        }

        private double ParseIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
                return 0;

            if (double.TryParse(iso, out double value))
            {
                return value;
            }

            throw new ArgumentException($"Invalid ISO format: {iso}");
        }

        private string[] GetShutterSpeedScale(int scale)
        {
            return scale switch
            {
                1 => ShutterSpeeds.Full,
                2 => ShutterSpeeds.Halves,
                3 => ShutterSpeeds.Thirds,
                _ => ShutterSpeeds.Full
            };
        }

        private string[] GetApertureScale(int scale)
        {
            return scale switch
            {
                1 => Apetures.Full,
                2 => Apetures.Halves,
                3 => Apetures.Thirds,
                _ => Apetures.Full
            };
        }

        private string[] GetIsoScale(int scale)
        {
            return scale switch
            {
                1 => ISOs.Full,
                2 => ISOs.Halves,
                3 => ISOs.Thirds,
                _ => ISOs.Full
            };
        }

        private string FindClosestValue(string[] values, double target)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array is empty or null");

            string closest = values[0];
            double closestValue = double.MaxValue;
            double targetValue = 0;

            // For shutter speeds
            if (values[0].Contains('/') || values[0].EndsWith("\""))
            {
                targetValue = ParseShutterSpeed(values[0]);
                closestValue = Math.Abs(Math.Log(ParseShutterSpeed(closest) / target));

                foreach (var value in values)
                {
                    double current = ParseShutterSpeed(value);
                    double diff = Math.Abs(Math.Log(current / target));

                    if (diff < closestValue)
                    {
                        closest = value;
                        closestValue = diff;
                    }
                }
            }
            // For apertures
            else if (values[0].StartsWith("f/"))
            {
                targetValue = ParseAperture(values[0]);
                closestValue = Math.Abs(Math.Log(ParseAperture(closest) / target));

                foreach (var value in values)
                {
                    double current = ParseAperture(value);
                    double diff = Math.Abs(Math.Log(current / target));

                    if (diff < closestValue)
                    {
                        closest = value;
                        closestValue = diff;
                    }
                }
            }
            // For ISOs
            else
            {
                targetValue = ParseIso(values[0]);
                closestValue = Math.Abs(Math.Log(ParseIso(closest) / target));

                foreach (var value in values)
                {
                    double current = ParseIso(value);
                    double diff = Math.Abs(Math.Log(current / target));

                    if (diff < closestValue)
                    {
                        closest = value;
                        closestValue = diff;
                    }
                }
            }

            return closest;
        }

        #endregion
    }
}