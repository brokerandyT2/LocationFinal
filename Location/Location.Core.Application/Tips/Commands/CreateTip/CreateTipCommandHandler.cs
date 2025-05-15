using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Tips.Commands.CreateTip
{
    public class CreateTipCommandHandler : IRequestHandler<CreateTipCommand, Result<CreateTipCommandResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateTipCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<CreateTipCommandResponse>> Handle(CreateTipCommand request, CancellationToken cancellationToken)
        {
            var tip = new Domain.Entities.Tip(
                request.TipTypeId,
                request.Title,
                request.Content);

            if (!string.IsNullOrEmpty(request.Fstop) || !string.IsNullOrEmpty(request.ShutterSpeed) || !string.IsNullOrEmpty(request.Iso))
            {
                tip.UpdatePhotographySettings(request.Fstop, request.ShutterSpeed, request.Iso);
            }

            tip.SetLocalization(request.I8n);

            var result = await _unitOfWork.Tips.CreateAsync(tip, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<CreateTipCommandResponse>.Failure(result.ErrorMessage ?? "Failed to create tip");
            }

            var createdTip = result.Data;

            var response = new CreateTipCommandResponse
            {
                Id = createdTip.Id,
                TipTypeId = createdTip.TipTypeId,
                Title = createdTip.Title,
                Content = createdTip.Content,
                Fstop = createdTip.Fstop,
                ShutterSpeed = createdTip.ShutterSpeed,
                Iso = createdTip.Iso,
                I8n = createdTip.I8n
            };

            return Result<CreateTipCommandResponse>.Success(response);
        }
    }
}