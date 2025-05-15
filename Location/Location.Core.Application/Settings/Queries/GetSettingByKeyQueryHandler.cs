using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using MediatR;
namespace Location.Core.Application.Settings.Queries.GetSettingByKey
{
    public class GetSettingByKeyQueryHandler : IRequestHandler<GetSettingByKeyQuery, Result<GetSettingByKeyQueryResponse>>
    {
        private readonly ISettingRepository _settingRepository;

        public GetSettingByKeyQueryHandler(ISettingRepository settingRepository)
        {
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
        }

        public async Task<Result<GetSettingByKeyQueryResponse>> Handle(GetSettingByKeyQuery request, CancellationToken cancellationToken)
        {
            var result = await _settingRepository.GetByKeyAsync(request.Key, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<GetSettingByKeyQueryResponse>.Failure(result.ErrorMessage ?? $"Setting with key '{request.Key}' not found");
            }

            var setting = result.Data;

            var response = new GetSettingByKeyQueryResponse
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Timestamp = setting.Timestamp
            };

            return Result<GetSettingByKeyQueryResponse>.Success(response);
        }
    }
}