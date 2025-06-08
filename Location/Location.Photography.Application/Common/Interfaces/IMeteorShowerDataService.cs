using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Photography.Domain.Entities;

namespace Location.Photography.Application.Common.Interfaces
{
    /// <summary>
    /// Service interface for accessing meteor shower data
    /// </summary>
    public interface IMeteorShowerDataService
    {
        /// <summary>
        /// Gets all meteor showers active on the specified date
        /// </summary>
        /// <param name="date">Date to check for active showers</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active meteor showers, ordered by expected ZHR descending</returns>
        Task<List<MeteorShower>> GetActiveShowersAsync(DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets meteor showers active on the specified date with minimum ZHR threshold
        /// </summary>
        /// <param name="date">Date to check for active showers</param>
        /// <param name="minZHR">Minimum ZHR threshold for photography worthiness</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active meteor showers meeting ZHR threshold, ordered by expected ZHR descending</returns>
        Task<List<MeteorShower>> GetActiveShowersAsync(DateTime date, int minZHR, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific meteor shower by its code identifier
        /// </summary>
        /// <param name="code">Three-letter shower code (e.g., "PER" for Perseids, "GEM" for Geminids)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Meteor shower if found, null otherwise</returns>
        Task<MeteorShower?> GetShowerByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available meteor showers in the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Complete list of all meteor showers, ordered by designation</returns>
        Task<List<MeteorShower>> GetAllShowersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets meteor showers that will be active within the specified date range
        /// </summary>
        /// <param name="startDate">Start date of the range (inclusive)</param>
        /// <param name="endDate">End date of the range (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of meteor showers active at any point within the date range</returns>
        Task<List<MeteorShower>> GetShowersInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a specific meteor shower is active on the given date
        /// </summary>
        /// <param name="showerCode">Three-letter shower code to check</param>
        /// <param name="date">Date to check for activity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the shower is active on the specified date</returns>
        Task<bool> IsShowerActiveAsync(string showerCode, DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the expected Zenith Hourly Rate (ZHR) for a shower on a specific date
        /// </summary>
        /// <param name="showerCode">Three-letter shower code</param>
        /// <param name="date">Date to get expected ZHR for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Expected ZHR value, with peak ZHR on peak date declining with distance from peak. Returns 0 if shower is not active.</returns>
        Task<double> GetExpectedZHRAsync(string showerCode, DateTime date, CancellationToken cancellationToken = default);
    }
}