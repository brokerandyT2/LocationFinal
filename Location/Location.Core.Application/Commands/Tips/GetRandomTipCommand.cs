using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Commands.Tips
{
    public class GetRandomTipCommand : IRequest<Result<TipDto>>
    {
        public int TipTypeId { get; set; }
    }

    public class GetRandomTipCommandHandler : IRequestHandler<GetRandomTipCommand, Result<TipDto>>
    {
        private readonly ITipRepository _tipRepository;

        public GetRandomTipCommandHandler(ITipRepository tipRepository)
        {
            _tipRepository = tipRepository;
        }

        public async Task<Result<TipDto>> Handle(GetRandomTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _tipRepository.GetRandomByTypeAsync(request.TipTypeId, cancellationToken);

                if (!result.IsSuccess || result.Data == null)
                {
                    return Result<TipDto>.Failure(result.ErrorMessage ?? "No tips found for the specified type");
                }

                var tip = result.Data;
                var tipDto = new TipDto
                {
                    Id = tip.Id,
                    TipTypeId = tip.TipTypeId,
                    Title = tip.Title,
                    Content = tip.Content,
                    Fstop = tip.Fstop,
                    ShutterSpeed = tip.ShutterSpeed,
                    Iso = tip.Iso,
                    I8n = tip.I8n
                };

                return Result<TipDto>.Success(tipDto);
            }
            catch (Exception ex)
            {
                return Result<TipDto>.Failure($"Failed to retrieve random tip: {ex.Message}");
            }
        }
    }
}