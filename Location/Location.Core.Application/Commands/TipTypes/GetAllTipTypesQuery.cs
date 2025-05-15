using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Queries.TipTypes
{
    public class GetAllTipTypesQuery : IRequest<Result<List<TipTypeDto>>>
    {
    }

    public class GetAllTipTypesQueryHandler : IRequestHandler<GetAllTipTypesQuery, Result<List<TipTypeDto>>>
    {
        private readonly ITipTypeRepository _tipTypeRepository;

        public GetAllTipTypesQueryHandler(ITipTypeRepository tipTypeRepository)
        {
            _tipTypeRepository = tipTypeRepository;
        }

        public async Task<Result<List<TipTypeDto>>> Handle(GetAllTipTypesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var tipTypes = await _tipTypeRepository.GetAllAsync(cancellationToken);

                var tipTypeDtos = tipTypes.Select(tt => new TipTypeDto
                {
                    Id = tt.Id,
                    Name = tt.Name,
                    I8n = tt.I8n
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