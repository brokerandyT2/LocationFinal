using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Tips.Queries.GetAllTips
{
    public class GetAllTipsQueryHandler : IRequestHandler<GetAllTipsQuery, Result<List<GetAllTipsQueryResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllTipsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<List<GetAllTipsQueryResponse>>> Handle(GetAllTipsQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Tips.GetAllAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<List<GetAllTipsQueryResponse>>.Failure(result.ErrorMessage ?? "Failed to retrieve tips");
            }

            var tips = result.Data;

            var response = tips.Select(tip => new GetAllTipsQueryResponse
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

            return Result<List<GetAllTipsQueryResponse>>.Success(response);
        }
    }
}