using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Infrastructure.Services
{
    /// <summary>
    /// Service for accessing meteor shower data from embedded JSON resource
    /// </summary>
    public class MeteorShowerDataService : IMeteorShowerDataService
    {
        private readonly ILogger<MeteorShowerDataService> _logger;
        private readonly StellariumMeteorShowerParser _parser;

        // Lazy-loaded data with thread safety
        private static readonly Lazy<MeteorShowerData> _meteorShowerData = new(() => LoadMeteorShowerData());
        private static ILogger<MeteorShowerDataService>? _staticLogger;

        // Resource path for embedded JSON file
        private const string RESOURCE_PATH = "Location.Photography.Infrastructure.Resources.meteor_showers.json";

        public MeteorShowerDataService(
            ILogger<MeteorShowerDataService> logger,
            StellariumMeteorShowerParser parser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _staticLogger ??= _logger; // Set static logger for lazy loading
        }

        /// <summary>
        /// Gets all meteor showers active on the specified date
        /// </summary>
        /// <param name="date">Date to check for active showers</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active meteor showers</returns>
        public async Task<List<MeteorShower>> GetActiveShowersAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await GetMeteorShowerDataAsync(cancellationToken);
                var activeShowers = data.GetActiveShowers(date);

                _logger.LogDebug("Found {Count} active meteor showers for {Date}",
                    activeShowers.Count, date.ToString("yyyy-MM-dd"));

                return activeShowers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active showers for date {Date}", date);
                return new List<MeteorShower>();
            }
        }

        /// <summary>
        /// Gets meteor showers active on the specified date with minimum ZHR threshold
        /// </summary>
        /// <param name="date">Date to check for active showers</param>
        /// <param name="minZHR">Minimum ZHR threshold</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active meteor showers meeting ZHR threshold</returns>
        public async Task<List<MeteorShower>> GetActiveShowersAsync(DateTime date, int minZHR, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await GetMeteorShowerDataAsync(cancellationToken);
                var activeShowers = data.GetActiveShowers(date, minZHR);

                _logger.LogDebug("Found {Count} active meteor showers for {Date} with ZHR >= {MinZHR}",
                    activeShowers.Count, date.ToString("yyyy-MM-dd"), minZHR);

                return activeShowers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active showers for date {Date} with ZHR >= {MinZHR}", date, minZHR);
                return new List<MeteorShower>();
            }
        }

        /// <summary>
        /// Gets a meteor shower by its code identifier
        /// </summary>
        /// <param name="code">Shower code (e.g., "PER", "GEM", "LYR")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Meteor shower or null if not found</returns>
        public async Task<MeteorShower?> GetShowerByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    _logger.LogWarning("GetShowerByCodeAsync called with null or empty code");
                    return null;
                }

                var data = await GetMeteorShowerDataAsync(cancellationToken);
                var shower = data.GetShowerByCode(code);

                if (shower != null)
                {
                    _logger.LogDebug("Found meteor shower: {Code} - {Designation}", code, shower.Designation);
                }
                else
                {
                    _logger.LogDebug("Meteor shower not found for code: {Code}", code);
                }

                return shower;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shower by code {Code}", code);
                return null;
            }
        }

        /// <summary>
        /// Gets all available meteor showers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all meteor showers</returns>
        public async Task<List<MeteorShower>> GetAllShowersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await GetMeteorShowerDataAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} total meteor showers", data.Showers.Count);

                return data.Showers.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all meteor showers");
                return new List<MeteorShower>();
            }
        }

        /// <summary>
        /// Gets meteor showers that will be active within the specified date range
        /// </summary>
        /// <param name="startDate">Start date of range</param>
        /// <param name="endDate">End date of range</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of meteor showers active within the date range</returns>
        public async Task<List<MeteorShower>> GetShowersInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (endDate < startDate)
                {
                    _logger.LogWarning("GetShowersInDateRangeAsync called with end date before start date");
                    return new List<MeteorShower>();
                }

                var data = await GetMeteorShowerDataAsync(cancellationToken);
                var showersInRange = new List<MeteorShower>();

                // Check each day in the range for active showers
                var currentDate = startDate.Date;
                var showerCodes = new HashSet<string>(); // Avoid duplicates

                while (currentDate <= endDate.Date)
                {
                    var dailyShowers = data.GetActiveShowers(currentDate);
                    foreach (var shower in dailyShowers)
                    {
                        if (showerCodes.Add(shower.Code))
                        {
                            showersInRange.Add(shower);
                        }
                    }
                    currentDate = currentDate.AddDays(1);
                }

                _logger.LogDebug("Found {Count} meteor showers active between {StartDate} and {EndDate}",
                    showersInRange.Count, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

                return showersInRange.OrderBy(s => s.Designation).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting showers in date range {StartDate} to {EndDate}", startDate, endDate);
                return new List<MeteorShower>();
            }
        }

        /// <summary>
        /// Checks if a specific shower is active on the given date
        /// </summary>
        /// <param name="showerCode">Shower code to check</param>
        /// <param name="date">Date to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if shower is active on the date</returns>
        public async Task<bool> IsShowerActiveAsync(string showerCode, DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var shower = await GetShowerByCodeAsync(showerCode, cancellationToken);
                return shower?.IsActiveOn(date) ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if shower {Code} is active on {Date}", showerCode, date);
                return false;
            }
        }

        /// <summary>
        /// Gets the expected ZHR for a shower on a specific date
        /// </summary>
        /// <param name="showerCode">Shower code</param>
        /// <param name="date">Date to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Expected ZHR or 0 if shower not active</returns>
        public async Task<double> GetExpectedZHRAsync(string showerCode, DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var shower = await GetShowerByCodeAsync(showerCode, cancellationToken);
                return shower?.GetExpectedZHR(date) ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expected ZHR for shower {Code} on {Date}", showerCode, date);
                return 0;
            }
        }

        /// <summary>
        /// Gets meteor shower data with lazy loading and caching
        /// </summary>
        private async Task<MeteorShowerData> GetMeteorShowerDataAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() => _meteorShowerData.Value, cancellationToken);
        }

        /// <summary>
        /// Loads meteor shower data from embedded resource (called once via Lazy<T>)
        /// </summary>
        private static MeteorShowerData LoadMeteorShowerData()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Check if resource exists
                var resourceNames = assembly.GetManifestResourceNames();
                if (!resourceNames.Contains(RESOURCE_PATH))
                {
                    var availableResources = string.Join(", ", resourceNames);
                    _staticLogger?.LogError("Meteor shower resource not found. Expected: {ExpectedPath}. Available resources: {AvailableResources}",
                        RESOURCE_PATH, availableResources);
                    return new MeteorShowerData();
                }

                // Load embedded resource
                using var stream = assembly.GetManifestResourceStream(RESOURCE_PATH);
                if (stream == null)
                {
                    _staticLogger?.LogError("Failed to load meteor shower resource stream from {ResourcePath}", RESOURCE_PATH);
                    return new MeteorShowerData();
                }

                using var reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _staticLogger?.LogError("Meteor shower resource file is empty or contains only whitespace");
                    return new MeteorShowerData();
                }

                // Parse using the Stellarium parser
                // Note: We need a parser instance, but we're in static context
                // For now, we'll create a temporary instance with null logger
                var tempParser = new StellariumMeteorShowerParser(
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<StellariumMeteorShowerParser>.Instance);

                var data = tempParser.ParseStellariumData(jsonContent);

                _staticLogger?.LogInformation("Successfully loaded {Count} meteor showers from embedded resource",
                    data.Showers.Count);

                return data;
            }
            catch (Exception ex)
            {
                _staticLogger?.LogError(ex, "Critical error loading meteor shower data from embedded resource");
                return new MeteorShowerData();
            }
        }
    }
}