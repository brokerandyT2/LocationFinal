using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Tips.Queries.GetTipById
{
    /// <summary>
    /// Handles the query to retrieve a tip by its unique identifier.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetTipByIdQuery"/> and returns a result containing the
    /// tip details if found, or an error message if the tip does not exist. The query is executed using the provided
    /// <see cref="IUnitOfWork"/> to access the data store.</remarks>
    public class GetTipByIdQueryHandler : IRequestHandler<GetTipByIdQuery, Result<GetTipByIdQueryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTipByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to access the data layer. This parameter cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public GetTipByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Handles the retrieval of a tip by its unique identifier.
        /// </summary>
        /// <param name="request">The query containing the unique identifier of the tip to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="GetTipByIdQueryResponse"/> if the operation is successful;
        /// otherwise, a failure result with an error message.</returns>
        public async Task<Result<GetTipByIdQueryResponse>> Handle(GetTipByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Tips.GetByIdAsync(request.Id, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<GetTipByIdQueryResponse>.Failure(result.ErrorMessage ?? AppResources.Tip_Error_NotFound);
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