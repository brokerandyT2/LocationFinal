using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.UpdateSetting
{
    public class UpdateSettingCommandHandler : IRequestHandler<UpdateSettingCommand, Result<UpdateSettingCommandResponse>>
    {
        private readonly ISettingRepository _settingRepository;

        public UpdateSettingCommandHandler(ISettingRepository settingRepository)
        {
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
        }

        public async Task<Result<UpdateSettingCommandResponse>> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
        {
            var settingResult = await _settingRepository.GetByKeyAsync(request.Key, cancellationToken);

            if (!settingResult.IsSuccess || settingResult.Data == null)
            {
                return Result<UpdateSettingCommandResponse>.Failure($"Setting with key '{request.Key}' not found");
            }

            var setting = settingResult.Data;
            setting.UpdateValue(request.Value);

            var updateResult = await _settingRepository.UpdateAsync(setting, cancellationToken);

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