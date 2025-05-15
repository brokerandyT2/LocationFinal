using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Settings.Queries.GetSettingByKey
{
    public class GetSettingByKeyQueryHandler : IRequestHandler<GetSettingByKeyQuery, Result<GetSettingByKeyQueryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSettingByKeyQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<GetSettingByKeyQueryResponse>> Handle(GetSettingByKeyQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Settings.GetByKeyAsync(request.Key, cancellationToken);

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