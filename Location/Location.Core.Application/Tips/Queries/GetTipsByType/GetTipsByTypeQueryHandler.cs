using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Tips.Queries.GetTipsByType
{
    public class GetTipsByTypeQueryHandler : IRequestHandler<GetTipsByTypeQuery, Result<List<GetTipsByTypeQueryResponse>>>
    {
        private readonly ITipRepository _tipRepository;

        public GetTipsByTypeQueryHandler(ITipRepository tipRepository)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        }

        public async Task<Result<List<GetTipsByTypeQueryResponse>>> Handle(GetTipsByTypeQuery request, CancellationToken cancellationToken)
        {
            var result = await _tipRepository.GetByTypeAsync(request.TipTypeId, cancellationToken);

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