using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
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
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteTipCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database transactions and operations.  This parameter cannot be
        /// <see langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public DeleteTipCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the deletion of a tip identified by the specified ID.
        /// </summary>
        /// <param name="request">The command containing the ID of the tip to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation before completion.</param>
        /// <returns>A <see cref="Result{T}"/> containing a boolean value indicating whether the deletion was successful.</returns>
        public async Task<Result<bool>> Handle(DeleteTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tipResult = await _unitOfWork.Tips.GetByIdAsync(request.Id, cancellationToken);

                if (!tipResult.IsSuccess || tipResult.Data == null)
                {
                    await _mediator.Publish(new TipValidationErrorEvent(request.Id, 0, new[] { Error.NotFound("Tip not found") }, "DeleteTipCommandHandler"), cancellationToken);
                    return Result<bool>.Failure("Tip not found");
                }

                var tip = tipResult.Data;
                var result = await _unitOfWork.Tips.DeleteAsync(request.Id, cancellationToken);

                if (!result.IsSuccess)
                {
                    await _mediator.Publish(new TipValidationErrorEvent(request.Id, tip.TipTypeId, new[] { Error.Database(result.ErrorMessage ?? "Failed to delete tip") }, "DeleteTipCommandHandler"), cancellationToken);
                    return result;
                }

                return result;
            }
            catch (Domain.Exceptions.TipDomainException ex) when (ex.Code == "TIP_IN_USE")
            {
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, 0, new[] { Error.Validation("Id", "Tip is currently in use and cannot be deleted") }, "DeleteTipCommandHandler"), cancellationToken);
                return Result<bool>.Failure("Cannot delete tip that is currently in use");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, 0, new[] { Error.Domain(ex.Message) }, "DeleteTipCommandHandler"), cancellationToken);
                return Result<bool>.Failure($"Failed to delete tip: {ex.Message}");
            }
        }
    }
}