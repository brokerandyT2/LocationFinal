using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Queries.GetTipsByType
{
    /// <summary>
    /// Handles the retrieval of tips filtered by a specific type.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetTipsByTypeQuery"/> request to retrieve a list of tips
    /// associated with the specified tip type. The result is returned as a <see cref="Result{T}"/> containing a list of
    /// <see cref="TipDto"/> objects. If the operation fails, the result will indicate failure with an appropriate error
    /// message.</remarks>
    public class GetTipsByTypeQueryHandler : IRequestHandler<GetTipsByTypeQuery, Result<List<TipDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetTipsByTypeQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to interact with the data layer.  This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public GetTipsByTypeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        /// <summary>
        /// Handles the retrieval of tips filtered by a specific type.
        /// </summary>
        /// <param name="request">The query containing the identifier of the tip type to filter by.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="TipDto"/> objects that match the specified tip
        /// type. Returns a failure result if the retrieval operation is unsuccessful.</returns>
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