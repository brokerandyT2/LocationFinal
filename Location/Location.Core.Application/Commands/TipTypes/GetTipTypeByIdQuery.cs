using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Queries.TipTypes
{
    public class GetTipTypeByIdQuery : IRequest<Result<TipTypeDto>>
    {
        public int Id { get; set; }
    }

    public class GetTipTypeByIdQueryHandler : IRequestHandler<GetTipTypeByIdQuery, Result<TipTypeDto>>
    {
        private readonly ITipTypeRepository _tipTypeRepository;

        public GetTipTypeByIdQueryHandler(ITipTypeRepository tipTypeRepository)
        {
            _tipTypeRepository = tipTypeRepository;
        }

        public async Task<Result<TipTypeDto>> Handle(GetTipTypeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var tipType = await _tipTypeRepository.GetByIdAsync(request.Id, cancellationToken);

                if (tipType == null)
                {
                    return Result<TipTypeDto>.Failure($"Tip type with ID {request.Id} not found");
                }

                var tipTypeDto = new TipTypeDto
                {
                    Id = tipType.Id,
                    Name = tipType.Name,
                    I8n = tipType.I8n
                };

                return Result<TipTypeDto>.Success(tipTypeDto);
            }
            catch (Exception ex)
            {
                return Result<TipTypeDto>.Failure($"Failed to retrieve tip type: {ex.Message}");
            }
        }
    }
}