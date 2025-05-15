using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Settings.Commands.UpdateSetting
{
    public class UpdateSettingCommandHandler : IRequestHandler<UpdateSettingCommand, Result<UpdateSettingCommandResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSettingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<UpdateSettingCommandResponse>> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
        {
            var settingResult = await _unitOfWork.Settings.GetByKeyAsync(request.Key, cancellationToken);

            if (!settingResult.IsSuccess || settingResult.Data == null)
            {
                return Result<UpdateSettingCommandResponse>.Failure($"Setting with key '{request.Key}' not found");
            }

            var setting = settingResult.Data;
            setting.UpdateValue(request.Value);

            var updateResult = await _unitOfWork.Settings.UpdateAsync(setting, cancellationToken);

            if (!updateResult.IsSuccess || updateResult.Data == null)
            {
                return Result<UpdateSettingCommandResponse>.Failure(updateResult.ErrorMessage ?? "Failed to update setting");
            }

            var updatedSetting = updateResult.Data;

            var response = new UpdateSettingCommandResponse
            {
                Id = updatedSetting.Id,
                Key = updatedSetting.Key,
                Value = updatedSetting.Value,
                Description = updatedSetting.Description,
                Timestamp = updatedSetting.Timestamp
            };

            return Result<UpdateSettingCommandResponse>.Success(response);
        }
    }
}