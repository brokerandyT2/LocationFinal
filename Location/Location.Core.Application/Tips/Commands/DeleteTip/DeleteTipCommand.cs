using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Tips.Commands.DeleteTip
{
    /// <summary>
    /// Represents a command to delete a tip identified by its unique ID.
    /// </summary>
    /// <remarks>This command is used in a request-response pattern to initiate the deletion of a tip. The
    /// result of the operation is encapsulated in a <see cref="Result{T}"/> object,  indicating whether the deletion
    /// was successful.</remarks>
    public class DeleteTipCommand : IRequest<Result<bool>>
    {
        public int Id { get; set; }
    }
}