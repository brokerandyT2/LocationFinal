using System;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Commands.CreateTip
{
    public class CreateTipCommandHandler : IRequest<Result<List<TipDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateTipCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<TipDto>> Handle(CreateTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tip = new Domain.Entities.Tip(
                    request.TipTypeId,
                    request.Title,
                    request.Content);

                if (!string.IsNullOrEmpty(request.Fstop) ||
                    !string.IsNullOrEmpty(request.ShutterSpeed) ||
                    !string.IsNullOrEmpty(request.Iso))
                {
                    tip.UpdatePhotographySettings(
                        request.Fstop ?? string.Empty,
                        request.ShutterSpeed ?? string.Empty,
                        request.Iso ?? string.Empty);
                }

                if (!string.IsNullOrEmpty(request.I8n))
                {
                    tip.SetLocalization(request.I8n);
                }

                var result = await _unitOfWork.Tips.CreateAsync(tip, cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<TipDto>.Failure(result.ErrorMessage);
                }

                var createdTip = result.Data;

                // Important: Return the correct ID from the created entity
                var tipDto = new TipDto
                {
                    Id = createdTip.Id, // Ensure ID is copied correctly
                    TipTypeId = createdTip.TipTypeId,
                    Title = createdTip.Title,
                    Content = createdTip.Content,
                    Fstop = createdTip.Fstop,
                    ShutterSpeed = createdTip.ShutterSpeed,
                    Iso = createdTip.Iso,
                    I8n = createdTip.I8n
                };

                return Result<TipDto>.Success(tipDto);
            }
            catch (Exception ex)
            {
                return Result<TipDto>.Failure($"Failed to create tip: {ex.Message}");
            }
        }
    }
}