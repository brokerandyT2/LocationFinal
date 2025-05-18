using System;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Commands.UpdateTip
{
    public class UpdateTipCommandHandler : IRequest<Result<List<TipDto>>>
    {
        private readonly ITipRepository _tipRepository;

        public UpdateTipCommandHandler(ITipRepository tipRepository)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        }

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