using System;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Commands.UpdateTip
{
    /// <summary>
    /// Handles the update operation for a tip by processing the provided command and updating the corresponding data.
    /// </summary>
    /// <remarks>This handler retrieves the tip by its identifier, updates its content, photography settings,
    /// and localization, and then persists the changes to the repository. Note that the <c>TipTypeId</c> cannot be
    /// updated once the tip is created.</remarks>
    public class UpdateTipCommandHandler : IRequest<Result<List<TipDto>>>
    {
        private readonly ITipRepository _tipRepository;
        /// <summary>
        /// Handles the command to update an existing tip.
        /// </summary>
        /// <param name="tipRepository">The repository used to access and update tip data. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tipRepository"/> is <see langword="null"/>.</exception>
        public UpdateTipCommandHandler(ITipRepository tipRepository)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        }
        /// <summary>
        /// Handles the update operation for a tip, applying the specified changes and returning the updated tip data.
        /// </summary>
        /// <remarks>This method retrieves the tip by its ID, applies the updates specified in the
        /// command, and saves the changes to the repository. If the operation fails at any stage, a failure result is
        /// returned with an appropriate error message.</remarks>
        /// <param name="request">The command containing the details of the tip to update, including its ID and updated properties.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing the updated <see cref="TipDto"/> if the operation succeeds; otherwise,
        /// a failure result with an error message.</returns>
        public async Task<Result<TipDto>> Handle(UpdateTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tipResult = await _tipRepository.GetByIdAsync(request.Id, cancellationToken);

                if (!tipResult.IsSuccess)
                {
                    return Result<TipDto>.Failure(tipResult.ErrorMessage);
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
                    return Result<TipDto>.Failure(updateResult.ErrorMessage);
                }

                var updatedTip = updateResult.Data;

                // Create DTO with the correct ID
                var tipDto = new TipDto
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

                return Result<TipDto>.Success(tipDto);
            }
            catch (Exception ex)
            {
                return Result<TipDto>.Failure($"Failed to update tip: {ex.Message}");
            }
        }
    }
}