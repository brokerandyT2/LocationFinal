using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Tips.Queries.GetTipsByType
{
    public class GetTipsByTypeQueryHandler : IRequestHandler<GetTipsByTypeQuery, Result<List<GetTipsByTypeQueryResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTipsByTypeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<List<GetTipsByTypeQueryResponse>>> Handle(GetTipsByTypeQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Tips.GetByTypeAsync(request.TipTypeId, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<List<GetTipsByTypeQueryResponse>>.Failure(result.ErrorMessage ?? "Failed to retrieve tips by type");
            }

            var tips = result.Data;

            var response = tips.Select(tip => new GetTipsByTypeQueryResponse
            {
                Id = tip.Id,
                TipTypeId = tip.TipTypeId,
                Title = tip.Title,
                Content = tip.Content,
                Fstop = tip.Fstop,
                ShutterSpeed = tip.ShutterSpeed,
                Iso = tip.Iso,
                I8n = tip.I8n
            }).ToList();

            return Result<List<GetTipsByTypeQueryResponse>>.Success(response);
        }
    }
}