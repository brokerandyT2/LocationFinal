using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Queries.GetAllTips
{
    public class GetAllTipsQueryHandler : IRequestHandler<GetAllTipsQuery, Result<List<TipDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllTipsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<List<TipDto>>> Handle(GetAllTipsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _unitOfWork.Tips.GetAllAsync(cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<List<TipDto>>.Failure("Failed to retrieve tips");
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
                return Result<List<TipDto>>.Failure($"Failed to retrieve tips: {ex.Message}");
            }
        }
    }
}