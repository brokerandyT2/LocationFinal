using System.Collections.Generic;
using MediatR;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;

namespace Location.Core.Application.Tips.Queries.GetAllTips
{
    /// <summary>
    /// Represents a query to retrieve all tips.
    /// </summary>
    /// <remarks>This query is used to request a list of all available tips. The result contains a collection
    /// of  <see cref="TipDto"/> objects wrapped in a <see cref="Result{T}"/> to indicate success or failure.</remarks>
    public class GetAllTipsQuery : IRequest<Result<List<TipDto>>>
    {
    }
    /// <summary>
    /// Represents the response for a query to retrieve all tips.
    /// </summary>
    /// <remarks>This class encapsulates the details of a tip, including its type, title, content, and
    /// associated photographic settings such as F-stop, shutter speed, and ISO. It also includes localization
    /// information.</remarks>
    public class GetAllTipsQueryResponse
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