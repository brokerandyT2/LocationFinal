using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using MediatR;

namespace Location.Core.Application.Tips.Commands.UpdateTip
{
    /// <summary>
    /// Handles the update operation for a tip by processing the provided command and updating the corresponding data.
    /// </summary>
    /// <remarks>This handler retrieves the tip by its identifier, updates its content, photography settings,
    /// and localization, and then persists the changes to the repository. Note that the <c>TipTypeId</c> cannot be
    /// updated once the tip is created.</remarks>
    public class UpdateTipCommandHandler : IRequestHandler<UpdateTipCommand, Result<UpdateTipCommandResponse>>
    {
        private readonly ITipRepository _tipRepository;
        private readonly IMediator _mediator;
        /// <summary>
        /// Handles the command to update an existing tip.
        /// </summary>
        /// <param name="tipRepository">The repository used to access and update tip data. Cannot be <see langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tipRepository"/> is <see langword="null"/>.</exception>
        public UpdateTipCommandHandler(ITipRepository tipRepository, IMediator mediator)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
            _mediator = mediator;
        }
        /// <summary>
        /// Handles the update operation for a tip, applying the specified changes and returning the updated tip data.
        /// </summary>
        /// <remarks>This method retrieves the tip by its ID, applies the updates specified in the
        /// command, and saves the changes to the repository. If the operation fails at any stage, a failure result is
        /// returned with an appropriate error message.</remarks>
        /// <param name="request">The command containing the details of the tip to update, including its ID and updated properties.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing the updated <see cref="UpdateTipCommandResponse"/> if the operation succeeds; otherwise,
        /// a failure result with an error message.</returns>
        public async Task<Result<UpdateTipCommandResponse>> Handle(UpdateTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tipResult = await _tipRepository.GetByIdAsync(request.Id, cancellationToken);

                if (!tipResult.IsSuccess)
                {
                    await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.NotFound("Tip not found") }, "UpdateTipCommandHandler"), cancellationToken);
                    return Result<UpdateTipCommandResponse>.Failure(tipResult.ErrorMessage);
                }

                var tip = tipResult.Data;

                // Update content using the available method
                tip.UpdateContent(request.Title, request.Content);

                // Update photography settings
                tip.UpdatePhotographySettings(
                    request.Fstop ?? string.Empty,
                    request.ShutterSpeed ?? string.Empty,
                    request.Iso ?? string.Empty);

                // Set localization
                tip.SetLocalization(request.I8n ?? "en-US");

                // Unfortunately there's no method to update TipTypeId once the Tip is created
                // We would need to add such a method to the Tip class if this is required
                // For now, we can't update the TipTypeId

                var updateResult = await _tipRepository.UpdateAsync(tip, cancellationToken);

                if (!updateResult.IsSuccess)
                {
                    await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.Database(updateResult.ErrorMessage ?? "Failed to update tip") }, "UpdateTipCommandHandler"), cancellationToken);
                    return Result<UpdateTipCommandResponse>.Failure(updateResult.ErrorMessage);
                }

                var updatedTip = updateResult.Data;

                // Create response with the correct ID
                var response = new UpdateTipCommandResponse
                {
                    Id = updatedTip.Id,
                    TipTypeId = updatedTip.TipTypeId,
                    Title = updatedTip.Title,
                    Content = updatedTip.Content,
                    Fstop = updatedTip.Fstop,
                    ShutterSpeed = updatedTip.ShutterSpeed,
                    Iso = updatedTip.Iso,
                    I8n = updatedTip.I8n
                };

                return Result<UpdateTipCommandResponse>.Success(response);
            }
            catch (Domain.Exceptions.TipDomainException ex) when (ex.Code == "DUPLICATE_TITLE")
            {
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.Validation("Title", "Tip with this title already exists") }, "UpdateTipCommandHandler"), cancellationToken);
                return Result<UpdateTipCommandResponse>.Failure($"Tip with title '{request.Title}' already exists");
            }
            catch (Domain.Exceptions.TipDomainException ex) when (ex.Code == "INVALID_CONTENT")
            {
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.Validation("Content", "Invalid tip content") }, "UpdateTipCommandHandler"), cancellationToken);
                return Result<UpdateTipCommandResponse>.Failure("Invalid tip content provided");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.Domain(ex.Message) }, "UpdateTipCommandHandler"), cancellationToken);
                return Result<UpdateTipCommandResponse>.Failure($"Failed to update tip: {ex.Message}");
            }
        }
    }
}