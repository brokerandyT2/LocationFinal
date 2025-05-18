using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Tips.DTOs;

namespace Location.Core.Application.Tips.Queries.GetTipsByType
{
    public class GetTipsByTypeQueryHandler : IRequestHandler<GetTipsByTypeQuery, Result<List<TipDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTipsByTypeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<List<TipDto>>> Handle(GetTipsByTypeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _unitOfWork.Tips.GetByTypeAsync(request.TipTypeId, cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<List<TipDto>>.Failure("Failed to retrieve tips by type");
                }

                var tips = result.Data;
                var tipDtos = new List<TipDto>();

                foreach (var tip in tips)
                {
                    tipDtos.Add(new TipDto
                    {
                        Id = tip.Id,
                        TipTypeId = tip.TipTypeId,
                        Title = tip.Title,
                        Content = tip.Content,
                        Fstop = tip.Fstop,
                        ShutterSpeed = tip.ShutterSpeed,
                        Iso = tip.Iso,
                        I8n = tip.I8n
                    });
                }

                return Result<List<TipDto>>.Success(tipDtos);
            }
            catch (Exception ex)
            {
                return Result<List<TipDto>>.Failure($"Failed to retrieve tips by type: {ex.Message}");
            }
        }
    }
}