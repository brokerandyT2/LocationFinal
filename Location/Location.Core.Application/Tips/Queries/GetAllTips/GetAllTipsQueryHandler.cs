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
    /// <summary>
    /// Handles the retrieval of all tips as a query operation.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetAllTipsQuery"/> request and retrieves all tips from
    /// the data source. The result is returned as a list of <see cref="TipDto"/> objects wrapped in a <see
    /// cref="Result{T}"/>.</remarks>
    public class GetAllTipsQueryHandler : IRequestHandler<GetAllTipsQuery, Result<List<TipDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Handles the query to retrieve all tips from the data source.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to access the data source.  This parameter cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public GetAllTipsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        /// <summary>
        /// Handles the retrieval of all tips and maps them to a list of <see cref="TipDto"/> objects.
        /// </summary>
        /// <param name="request">The query request containing the parameters for retrieving tips.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="TipDto"/> objects if the operation is successful;
        /// otherwise, a failure result with an error message.</returns>
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