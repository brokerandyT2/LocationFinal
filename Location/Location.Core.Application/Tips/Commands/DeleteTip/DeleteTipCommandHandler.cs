using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Tips.Commands.DeleteTip
{
    /// <summary>
    /// Handles the deletion of a tip by processing a <see cref="DeleteTipCommand"/>.
    /// </summary>
    /// <remarks>This handler interacts with the data layer to delete a tip identified by the command's ID. It
    /// uses the provided <see cref="IUnitOfWork"/> to perform the operation.</remarks>
    public class DeleteTipCommandHandler : IRequestHandler<DeleteTipCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteTipCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database transactions and operations.  This parameter cannot be
        /// <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public DeleteTipCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        /// <summary>
        /// Handles the deletion of a tip identified by the specified ID.
        /// </summary>
        /// <param name="request">The command containing the ID of the tip to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation before completion.</param>
        /// <returns>A <see cref="Result{T}"/> containing a boolean value indicating whether the deletion was successful.</returns>
        public async Task<Result<bool>> Handle(DeleteTipCommand request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Tips.DeleteAsync(request.Id, cancellationToken);

            return result;
        }
    }
}