using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tips.Queries.GetTipById
{
    /// <summary>
    /// Represents a query to retrieve a tip by its unique identifier.
    /// </summary>
    /// <remarks>This query is used to request a tip from the system by specifying its unique ID.  The result
    /// contains the details of the tip if found, or an appropriate error if not.</remarks>
    public class GetTipByIdQuery : IRequest<Result<GetTipByIdQueryResponse>>
    {
        public int Id { get; set; }
    }
    /// <summary>
    /// Represents the response data for a query to retrieve a tip by its unique identifier.
    /// </summary>
    /// <remarks>This class contains detailed information about a specific tip, including its type, title,
    /// content,  and associated photography settings such as f-stop, shutter speed, and ISO. It also includes 
    /// localization information.</remarks>
    public class GetTipByIdQueryResponse
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