using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Queries.GetTipsByType
{
    /// <summary>
    /// Represents a query to retrieve a list of tips filtered by a specific tip type.
    /// </summary>
    /// <remarks>This query is used to request tips of a particular type, identified by the <see
    /// cref="TipTypeId"/>. The result contains a list of tips in the form of <see cref="TipDto"/> objects.</remarks>
    public class GetTipsByTypeQuery : IRequest<Result<List<TipDto>>>
    {
        public int TipTypeId { get; set; }
    }
    /// <summary>
    /// Represents the response for a query to retrieve tips by type.
    /// </summary>
    /// <remarks>This class encapsulates the details of a tip, including its identifier, type, title, content,
    /// and associated photographic settings such as f-stop, shutter speed, and ISO.  It also includes localization
    /// information.</remarks>
    public class GetTipsByTypeQueryResponse
    {
        public int Id { get; set; }
        public int TipTypeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Fstop { get; set; } = string.Empty;
        public string ShutterSpeed { get; set; } = string.Empty;
        public string Iso { get; set; } = string.Empty;
        public string I8n { get; set; } = "en-US";
    }
}