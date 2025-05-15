using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.CreateSetting
{
    public class CreateSettingCommandHandler : IRequestHandler<CreateSettingCommand, Result<CreateSettingCommandResponse>>
    {
        private readonly ISettingRepository _settingRepository;

        public CreateSettingCommandHandler(ISettingRepository settingRepository)
        {
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
        }

        public async Task<Result<CreateSettingCommandResponse>> Handle(CreateSettingCommand request, CancellationToken cancellationToken)
        {
            var existingSettingResult = await _settingRepository.GetByKeyAsync(request.Key, cancellationToken);

            if (existingSettingResult.Id != null)
            {
                return Result<CreateSettingCommandResponse>.Failure($"Setting with key '{request.Key}' already exists");
            }

            var setting = new Domain.Entities.Setting(request.Key, request.Value, request.Description);

            var result = await _settingRepository.CreateAsync(setting, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<CreateSettingCommandResponse>.Failure(result.ErrorMessage ?? "Failed to create setting");
            }

            var createdSetting = result.Data;

            var response = new CreateSettingCommandResponse
            {
                Id = createdSetting.Id,
                Key = createdSetting.Key,
                Value = createdSetting.Value,
                Description = createdSetting.Description,
                Timestamp = createdSetting.Timestamp
            };

            return Result<CreateSettingCommandResponse>.Success(response);
        }
    }
}