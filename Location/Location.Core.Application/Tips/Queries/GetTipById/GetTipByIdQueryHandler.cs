using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Tips.Queries.GetTipById
{
    public class GetTipByIdQueryHandler : IRequestHandler<GetTipByIdQuery, Result<GetTipByIdQueryResponse>>
    {
        private readonly ITipRepository _tipRepository;

        public GetTipByIdQueryHandler(ITipRepository tipRepository)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        }

        public async Task<Result<GetTipByIdQueryResponse>> Handle(GetTipByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _tipRepository.GetByIdAsync(request.Id, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<GetTipByIdQueryResponse>.Failure(result.ErrorMessage ?? "Tip not found");
            }

            var tip = result.Data;

            var response = new GetTipByIdQueryResponse
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

            return Result<GetTipByIdQueryResponse>.Success(response);
        }
    }
}