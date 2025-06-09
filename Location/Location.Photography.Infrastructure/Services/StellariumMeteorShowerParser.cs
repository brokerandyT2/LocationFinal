using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Location.Photography.Infrastructure.Services
{
    /// <summary>
    /// Parses Stellarium meteor shower JSON data into our domain models
    /// </summary>
    public class StellariumMeteorShowerParser
    {
        private readonly ILogger<StellariumMeteorShowerParser> _logger;
        private const int MIN_ZHR_THRESHOLD = 5;

        public StellariumMeteorShowerParser(ILogger<StellariumMeteorShowerParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parses Stellarium JSON format into our simplified domain models
        /// </summary>
        /// <param name="stellariumJson">Raw Stellarium JSON string</param>
        /// <returns>Parsed meteor shower data</returns>
        public MeteorShowerData ParseStellariumData(string stellariumJson)
        {
            try
            {
                var stellariumData = JsonSerializer.Deserialize<StellariumRoot>(stellariumJson);
                if (stellariumData?.Showers == null)
                {
                    _logger.LogWarning("Failed to parse Stellarium data or no showers found");
                    return new MeteorShowerData();
                }

                var showers = new List<MeteorShower>();

                foreach (var (code, showerData) in stellariumData.Showers)
                {
                    try
                    {
                        var parsedShower = ParseSingleShower(code, showerData);
                        if (parsedShower != null)
                        {
                            showers.Add(parsedShower);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse shower {Code}", code);
                    }
                }

                _logger.LogInformation("Successfully parsed {Count} meteor showers from Stellarium data", showers.Count);

                return new MeteorShowerData { Showers = showers };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Stellarium JSON data");
                return new MeteorShowerData();
            }
        }

        private MeteorShower? ParseSingleShower(string code, StellariumShower showerData)
        {
            // Get the generic activity data (baseline)
            var genericActivity = showerData.Activity?.FirstOrDefault(a => a.Year == "generic");
            if (genericActivity == null)
            {
                _logger.LogDebug("Skipping shower {Code} - no generic activity data", code);
                return null;
            }

            // Parse and validate activity dates
            if (!TryParseActivityDates(genericActivity, out var start, out var peak, out var finish))
            {
                _logger.LogDebug("Skipping shower {Code} - invalid activity dates", code);
                return null;
            }

            // Parse ZHR with filtering
            var zhr = ParseZHR(genericActivity, code);
            if (zhr < MIN_ZHR_THRESHOLD)
            {
                _logger.LogDebug("Skipping shower {Code} - ZHR {ZHR} below threshold {Threshold}",
                    code, zhr, MIN_ZHR_THRESHOLD);
                return null;
            }

            // Parse radiant coordinates
            if (!TryParseRadiantCoordinates(showerData, out var radiantRA, out var radiantDec))
            {
                _logger.LogDebug("Skipping shower {Code} - invalid radiant coordinates", code);
                return null;
            }

            // Create meteor shower object
            var shower = new MeteorShower
            {
                Code = code,
                Designation = showerData.Designation ?? code,
                Activity = new MeteorShowerActivity
                {
                    Start = start,
                    Peak = peak,
                    Finish = finish,
                    ZHR = zhr
                },
                RadiantRA = radiantRA,
                RadiantDec = radiantDec,
                SpeedKmS = showerData.Speed ?? 0,
                ParentBody = CleanParentBodyName(showerData.ParentObj)
            };

            _logger.LogDebug("Parsed shower: {Code} - {Designation} (ZHR: {ZHR})",
                code, shower.Designation, zhr);

            return shower;
        }

        private bool TryParseActivityDates(StellariumActivity activity,
            out string start, out string peak, out string finish)
        {
            start = peak = finish = string.Empty;

            try
            {
                start = ConvertDateFormat(activity.Start);
                peak = ConvertDateFormat(activity.Peak);
                finish = ConvertDateFormat(activity.Finish);

                return !string.IsNullOrEmpty(start) &&
                       !string.IsNullOrEmpty(peak) &&
                       !string.IsNullOrEmpty(finish);
            }
            catch
            {
                return false;
            }
        }

        private string ConvertDateFormat(string? stellariumDate)
        {
            if (string.IsNullOrEmpty(stellariumDate))
                return string.Empty;

            // Convert from "MM.DD" to "MM-DD"
            var converted = stellariumDate.Replace(".", "-");

            // Ensure zero-padding for single digits
            var parts = converted.Split('-');
            if (parts.Length == 2)
            {
                var month = parts[0].PadLeft(2, '0');
                var day = parts[1].PadLeft(2, '0');
                return $"{month}-{day}";
            }

            return converted;
        }

        private int ParseZHR(StellariumActivity activity, string code)
        {
            // Handle explicit ZHR value
            if (activity.ZHR.HasValue && activity.ZHR.Value > 0)
            {
                return activity.ZHR.Value;
            }

            // Handle variable ZHR (e.g., "5-20", "0-100")
            if (!string.IsNullOrEmpty(activity.Variable))
            {
                return ParseVariableZHR(activity.Variable, code);
            }

            // Default to 1 if no ZHR data available
            _logger.LogDebug("No ZHR data for shower {Code}, defaulting to 1", code);
            return 1;
        }

        private int ParseVariableZHR(string variable, string code)
        {
            try
            {
                // Handle formats like "5-20", "0-100", "1-4", etc.
                var parts = variable.Split('-');
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
                    {
                        // Use the maximum value for filtering purposes
                        // Photography is planned for peak conditions
                        var maxZHR = Math.Max(min, max);
                        _logger.LogDebug("Shower {Code} has variable ZHR {Variable}, using max value {MaxZHR}",
                            code, variable, maxZHR);
                        return maxZHR;
                    }
                }

                // Handle special cases like "5-20*" with asterisks or other characters
                var cleanVariable = new string(variable.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleanVariable, out var parsed))
                {
                    return parsed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse variable ZHR {Variable} for shower {Code}", variable, code);
            }

            return 1; // Default fallback
        }

        private bool TryParseRadiantCoordinates(StellariumShower showerData,
            out double radiantRA, out double radiantDec)
        {
            radiantRA = radiantDec = 0;

            try
            {
                if (string.IsNullOrEmpty(showerData.RadiantAlpha) ||
                    string.IsNullOrEmpty(showerData.RadiantDelta))
                {
                    return false;
                }

                // Parse RA (remove any + prefix)
                var raStr = showerData.RadiantAlpha.TrimStart('+');
                if (!double.TryParse(raStr, out radiantRA))
                {
                    return false;
                }

                // Parse Dec (handle + and - prefixes)
                var decStr = showerData.RadiantDelta;
                if (!double.TryParse(decStr, out radiantDec))
                {
                    return false;
                }

                // Validate ranges
                if (radiantRA < 0 || radiantRA >= 360)
                {
                    _logger.LogDebug("Invalid RA value: {RA}", radiantRA);
                    return false;
                }

                if (radiantDec < -90 || radiantDec > 90)
                {
                    _logger.LogDebug("Invalid Dec value: {Dec}", radiantDec);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse radiant coordinates: RA={RA}, Dec={Dec}",
                    showerData.RadiantAlpha, showerData.RadiantDelta);
                return false;
            }
        }

        private string CleanParentBodyName(string? parentObj)
        {
            if (string.IsNullOrEmpty(parentObj))
                return string.Empty;

            // Clean up common prefixes and formatting
            var cleaned = parentObj
                .Replace("Comet ", "")
                .Replace("Minor planet ", "")
                .Trim();

            return cleaned;
        }
    }

    #region Stellarium JSON Structure Classes

    /// <summary>
    /// Root structure of Stellarium meteor shower JSON
    /// </summary>
    public class StellariumRoot
    {
        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("showers")]
        public Dictionary<string, StellariumShower>? Showers { get; set; }
    }

    /// <summary>
    /// Individual shower data from Stellarium
    /// </summary>
    public class StellariumShower
    {
        [JsonPropertyName("designation")]
        public string? Designation { get; set; }

        [JsonPropertyName("activity")]
        public List<StellariumActivity>? Activity { get; set; }

        [JsonPropertyName("radiantAlpha")]
        public string? RadiantAlpha { get; set; }

        [JsonPropertyName("radiantDelta")]
        public string? RadiantDelta { get; set; }

        [JsonPropertyName("speed")]
        public int? Speed { get; set; }

        [JsonPropertyName("parentObj")]
        public string? ParentObj { get; set; }

        [JsonPropertyName("driftAlpha")]
        public string? DriftAlpha { get; set; }

        [JsonPropertyName("driftDelta")]
        public string? DriftDelta { get; set; }

        [JsonPropertyName("pidx")]
        public double? Pidx { get; set; }

        [JsonPropertyName("colors")]
        public List<StellariumColor>? Colors { get; set; }
    }

    /// <summary>
    /// Activity period data from Stellarium
    /// </summary>
    public class StellariumActivity
    {
        [JsonPropertyName("year")]
        public string? Year { get; set; }

        [JsonPropertyName("zhr")]
        public int? ZHR { get; set; }

        [JsonPropertyName("variable")]
        public string? Variable { get; set; }

        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("finish")]
        public string? Finish { get; set; }

        [JsonPropertyName("peak")]
        public string? Peak { get; set; }
    }

    /// <summary>
    /// Color information from Stellarium (not used in our model)
    /// </summary>
    public class StellariumColor
    {
        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("intensity")]
        public int Intensity { get; set; }
    }

    #endregion
}