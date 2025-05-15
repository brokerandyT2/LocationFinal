using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Queries.GetAllSettings
{
    public class GetAllSettingsQueryHandler : IRequestHandler<GetAllSettingsQuery, Result<List<GetAllSettingsQueryResponse>>>
    {
        private readonly ISettingRepository _settingRepository;

        public GetAllSettingsQueryHandler(ISettingRepository settingRepository)
        {
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
        }

        public async Task<Result<List<GetAllSettingsQueryResponse>>> Handle(GetAllSettingsQuery request, CancellationToken cancellationToken)
        {
            var result = await _settingRepository.GetAllAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<List<GetAllSettingsQueryResponse>>.Failure(result.ErrorMessage ?? "Failed to retrieve settings");
            }

            var settings = result.Data;

            var response = settings.Select(setting => new GetAllSettingsQueryResponse
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Timestamp = setting.Timestamp
            }).ToList();

            return Result<List<GetAllSettingsQueryResponse>>.Success(response);
        }
    }
}