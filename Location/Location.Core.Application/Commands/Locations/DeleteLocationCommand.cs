using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Commands.Locations
{
    /// <summary>
    /// Represents a command to delete a location identified by its unique ID.
    /// </summary>
    /// <remarks>This command is used in a request-response pattern to delete a location.  The result of the
    /// operation indicates whether the deletion was successful.</remarks>
    public class DeleteLocationCommand : IRequest<Result<bool>>
    {
        public int Id { get; set; }
    }
}