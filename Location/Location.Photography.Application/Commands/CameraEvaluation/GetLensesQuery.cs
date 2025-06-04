using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Queries.CameraEvaluation
{
    public class GetLensesQuery : IRequest<Result<GetLensesResultDto>>
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
        public bool UserLensesOnly { get; set; } = false;
        public int? CompatibleWithCameraId { get; set; }
    }

    public class GetLensesResultDto
    {
        public List<LensDto> Lenses { get; set; } = new List<LensDto>();
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }

    public class GetLensesQueryHandler : IRequestHandler<GetLensesQuery, Result<GetLensesResultDto>>
    {
        private readonly ILensRepository _lensRepository;
        private readonly ILogger<GetLensesQueryHandler> _logger;

        public GetLensesQueryHandler(
            ILensRepository lensRepository,
            ILogger<GetLensesQueryHandler> logger)
        {
            _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetLensesResultDto>> Handle(GetLensesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<Domain.Entities.Lens> lenses;
                int totalCount;

                if (request.CompatibleWithCameraId.HasValue)
                {
                    var compatibleResult = await _lensRepository.GetCompatibleLensesAsync(request.CompatibleWithCameraId.Value, cancellationToken);
                    if (!compatibleResult.IsSuccess)
                    {
                        return Result<GetLensesResultDto>.Failure(compatibleResult.ErrorMessage ?? "Failed to retrieve compatible lenses");
                    }

                    lenses = compatibleResult.Data.Skip(request.Skip).Take(request.Take).ToList();
                    totalCount = compatibleResult.Data.Count;
                }
                else if (request.UserLensesOnly)
                {
                    var userLensesResult = await _lensRepository.GetUserLensesAsync(cancellationToken);
                    if (!userLensesResult.IsSuccess)
                    {
                        return Result<GetLensesResultDto>.Failure(userLensesResult.ErrorMessage ?? "Failed to retrieve user lenses");
                    }

                    lenses = userLensesResult.Data.Skip(request.Skip).Take(request.Take).ToList();
                    totalCount = userLensesResult.Data.Count;
                }
                else
                {
                    var pagedResult = await _lensRepository.GetPagedAsync(request.Skip, request.Take, cancellationToken);
                    if (!pagedResult.IsSuccess)
                    {
                        return Result<GetLensesResultDto>.Failure(pagedResult.ErrorMessage ?? "Failed to retrieve lenses");
                    }

                    var countResult = await _lensRepository.GetTotalCountAsync(cancellationToken);
                    if (!countResult.IsSuccess)
                    {
                        return Result<GetLensesResultDto>.Failure(countResult.ErrorMessage ?? "Failed to get total count");
                    }

                    lenses = pagedResult.Data;
                    totalCount = countResult.Data;
                }

                var lensDtos = lenses.Select(l => new LensDto
                {
                    Id = l.Id,
                    MinMM = l.MinMM,
                    MaxMM = l.MaxMM,
                    MinFStop = l.MinFStop,
                    MaxFStop = l.MaxFStop,
                    IsPrime = l.IsPrime,
                    IsUserCreated = l.IsUserCreated,
                    DateAdded = l.DateAdded,
                    DisplayName = l.GetDisplayName()
                }).ToList();

                var result = new GetLensesResultDto
                {
                    Lenses = lensDtos,
                    TotalCount = totalCount,
                    HasMore = (request.Skip + request.Take) < totalCount
                };

                return Result<GetLensesResultDto>.Success(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lenses");
                return Result<GetLensesResultDto>.Failure($"Error retrieving lenses: {ex.Message}");
            }
        }
    }
}