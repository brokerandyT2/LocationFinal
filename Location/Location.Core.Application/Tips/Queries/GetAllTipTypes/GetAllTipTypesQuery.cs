// Location.Core.Application/Tips/Queries/GetAllTipTypes/GetAllTipTypesQuery.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Tips.Queries.GetAllTipTypes
{
    public class GetAllTipTypesQuery : IRequest<Result<List<TipTypeDto>>>
    {
    }

    public class GetAllTipTypesQueryHandler : IRequestHandler<GetAllTipTypesQuery, Result<List<TipTypeDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllTipTypesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<TipTypeDto>>> Handle(GetAllTipTypesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _unitOfWork.TipTypes.GetAllAsync(cancellationToken);

                if (result == null)
                {
                    return Result<List<TipTypeDto>>.Failure("Failed to retrieve tip types");
                }

                var tipTypeDtos = result.Select(t => new TipTypeDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    I8n = t.I8n
                }).ToList();

                return Result<List<TipTypeDto>>.Success(tipTypeDtos);
            }
            catch (Exception ex)
            {
                return Result<List<TipTypeDto>>.Failure($"Failed to retrieve tip types: {ex.Message}");
            }
        }
    }
}