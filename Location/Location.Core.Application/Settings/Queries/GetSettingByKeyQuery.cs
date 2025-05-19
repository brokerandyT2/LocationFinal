using System;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Queries.GetSettingByKey
{
    /// <summary>
    /// Represents a query to retrieve a specific setting by its key.
    /// </summary>
    /// <remarks>This query is used to fetch the value of a setting identified by a unique key.  The result
    /// contains the setting's value if the key exists, or an appropriate error if it does not.</remarks>
    public class GetSettingByKeyQuery : IRequest<Result<GetSettingByKeyQueryResponse>>
    {
        public string Key { get; set; } = string.Empty;
    }
    /// <summary>
    /// Represents the response for a query that retrieves a setting by its key.
    /// </summary>
    /// <remarks>This class encapsulates the details of a setting, including its identifier, key, value,
    /// description,  and the timestamp indicating when the setting was last updated.</remarks>
    public class GetSettingByKeyQueryResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}