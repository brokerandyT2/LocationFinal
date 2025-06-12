using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Tips.Commands.UpdateTip
{
    /// <summary>
    /// Handles the updating of an existing tip by processing an <see cref="UpdateTipCommand"/> request.
    /// </summary>
    /// <remarks>This handler retrieves the tip by its ID, updates its properties with the provided values,
    /// and persists the changes to the repository. If the operation is successful, an <see cref="UpdateTipCommandResponse"/>
    /// representing the updated tip is returned. In case of failure, an error message is included in the result.</remarks>
    public class UpdateTipCommandHandler : IRequestHandler<UpdateTipCommand, Result<UpdateTipCommandResponse>>
    {
        private readonly ITipRepository _tipRepository;
        private readonly IMediator _mediator;

        /// <summary>
        /// Handles the updating of tips by processing the associated command.
        /// </summary>
        /// <param name="tipRepository">The repository used to persist and manage tip data. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tipRepository"/> is <see langword="null"/>.</exception>
        public UpdateTipCommandHandler(ITipRepository tipRepository, IMediator mediator)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the update of an existing tip and returns the result.
        /// </summary>
        /// <remarks>This method retrieves the tip by its ID, updates its properties with the provided values,
        /// and persists the changes to the repository. If an error occurs during the process, the method returns a
        /// failure result with the error message.</remarks>
        /// <param name="request">The command containing the updated details of the tip, including title, content, and photography settings.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing an <see cref="UpdateTipCommandResponse"/> if the operation succeeds,
        /// or an error message if the operation fails.</returns>
        public async Task<Result<UpdateTipCommandResponse>> Handle(UpdateTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tip = await _tipRepository.GetByIdAsync(request.Id, cancellationToken);
                if (tip == null)
                {
                    await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.NotFound(AppResources.Tip_Error_NotFound) }, "UpdateTipCommandHandler"), cancellationToken);
                    return Result<UpdateTipCommandResponse>.Failure(AppResources.Tip_Error_NotFound);
                }

                // Update tip properties
                tip.UpdateContent(request.Title, request.Content);

                // Set photography parameters
                tip.UpdatePhotographySettings(
                    request.Fstop ?? string.Empty,
                    request.ShutterSpeed ?? string.Empty,
                    request.Iso ?? string.Empty);

                // Update localization if provided, otherwise keep existing or default
                tip.SetLocalization(request.I8n ?? AppResources.Default_LocalizationCode);

                // Unfortunately there's no method to update TipTypeId once the Tip is created
                // We would need to add such a method to the Tip class if this is required
                // For now, we can't update the TipTypeId

                await _tipRepository.UpdateAsync(tip, cancellationToken);

                // Since UpdateAsync returns void, we'll use the tip entity directly
                var updatedTip = tip;

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
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.Validation("Title", AppResources.Tip_Error_DuplicateTitle) }, "UpdateTipCommandHandler"), cancellationToken);
                return Result<UpdateTipCommandResponse>.Failure(string.Format(AppResources.Tip_Error_DuplicateTitle, request.Title));
            }
            catch (Domain.Exceptions.TipDomainException ex) when (ex.Code == "INVALID_CONTENT")
            {
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.Validation("Content", AppResources.Tip_Error_InvalidContent) }, "UpdateTipCommandHandler"), cancellationToken);
                return Result<UpdateTipCommandResponse>.Failure(AppResources.Tip_Error_InvalidContent);
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new TipValidationErrorEvent(request.Id, request.TipTypeId, new[] { Error.Domain(ex.Message) }, "UpdateTipCommandHandler"), cancellationToken);
                return Result<UpdateTipCommandResponse>.Failure(string.Format(AppResources.Tip_Error_UpdateFailed, ex.Message));
            }
        }
    }
}