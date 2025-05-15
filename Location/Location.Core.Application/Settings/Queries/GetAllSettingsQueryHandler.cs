using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Settings.Queries.GetAllSettings
{
    public class GetAllSettingsQueryHandler : IRequestHandler<GetAllSettingsQuery, Result<List<GetAllSettingsQueryResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllSettingsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<List<GetAllSettingsQueryResponse>>> Handle(GetAllSettingsQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Settings.GetAllAsync(cancellationToken);

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