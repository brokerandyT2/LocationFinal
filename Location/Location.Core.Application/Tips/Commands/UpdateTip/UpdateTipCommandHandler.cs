using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tips.Commands.UpdateTip
{
    public class UpdateTipCommandHandler : IRequestHandler<UpdateTipCommand, Result<UpdateTipCommandResponse>>
    {
        private readonly ITipRepository _tipRepository;

        public UpdateTipCommandHandler(ITipRepository tipRepository)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        }

        public async Task<Result<UpdateTipCommandResponse>> Handle(UpdateTipCommand request, CancellationToken cancellationToken)
        {
            var tipResult = await _tipRepository.GetByIdAsync(request.Id, cancellationToken);

            if (!tipResult.IsSuccess || tipResult.Data == null)
            {
                return Result<UpdateTipCommandResponse>.Failure("Tip not found");
            }

            var tip = tipResult.Data;

            tip.UpdateContent(request.Title, request.Content);
            tip.UpdatePhotographySettings(request.Fstop, request.ShutterSpeed, request.Iso);
            tip.SetLocalization(request.I8n);

            var updateResult = await _tipRepository.UpdateAsync(tip, cancellationToken);

            if (!updateResult.IsSuccess || updateResult.Data == null)
            {
                return Result<UpdateTipCommandResponse>.Failure(updateResult.ErrorMessage ?? "Failed to update tip");
            }

            var updatedTip = updateResult.Data;

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
    }
}