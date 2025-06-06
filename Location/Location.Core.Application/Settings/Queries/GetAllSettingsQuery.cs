using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Settings.Queries.GetAllSettings
{
    /// <summary>
    /// Represents a query to retrieve all settings.
    /// </summary>
    /// <remarks>This query is used to request a list of all available settings. The result contains a
    /// collection of  <see cref="GetAllSettingsQueryResponse"/> objects wrapped in a <see cref="Result{T}"/>.</remarks>
    public class GetAllSettingsQuery : IRequest<Result<List<GetAllSettingsQueryResponse>>>
    {
    }
    /// <summary>
    /// Represents the response for a query that retrieves all settings.
    /// </summary>
    /// <remarks>This class encapsulates the details of a single setting, including its identifier, key,
    /// value,  description, and the timestamp indicating when the setting was last updated.</remarks>
    public class GetAllSettingsQueryResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}