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
    public class GetCameraBodiesQuery : IRequest<Result<GetCameraBodiesResultDto>>
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
        public bool UserCamerasOnly { get; set; } = false;
    }

    public class GetCameraBodiesResultDto
    {
        public List<CameraBodyDto> CameraBodies { get; set; } = new List<CameraBodyDto>();
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }

    public class GetCameraBodiesQueryHandler : IRequestHandler<GetCameraBodiesQuery, Result<GetCameraBodiesResultDto>>
    {
        private readonly ICameraBodyRepository _cameraBodyRepository;
        private readonly ILogger<GetCameraBodiesQueryHandler> _logger;

        public GetCameraBodiesQueryHandler(
            ICameraBodyRepository cameraBodyRepository,
            ILogger<GetCameraBodiesQueryHandler> logger)
        {
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetCameraBodiesResultDto>> Handle(GetCameraBodiesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<Domain.Entities.CameraBody> cameraBodies;
                int totalCount;

                if (request.UserCamerasOnly)
                {
                    var userCamerasResult = await _cameraBodyRepository.GetUserCamerasAsync(cancellationToken);
                    if (!userCamerasResult.IsSuccess)
                    {
                        return Result<GetCameraBodiesResultDto>.Failure(userCamerasResult.ErrorMessage ?? "Failed to retrieve user cameras");
                    }

                    cameraBodies = userCamerasResult.Data.Skip(request.Skip).Take(request.Take).ToList();
                    totalCount = userCamerasResult.Data.Count;
                }
                else
                {
                    var pagedResult = await _cameraBodyRepository.GetPagedAsync(request.Skip, request.Take, cancellationToken);
                    if (!pagedResult.IsSuccess)
                    {
                        return Result<GetCameraBodiesResultDto>.Failure(pagedResult.ErrorMessage ?? "Failed to retrieve cameras");
                    }

                    var countResult = await _cameraBodyRepository.GetTotalCountAsync(cancellationToken);
                    if (!countResult.IsSuccess)
                    {
                        return Result<GetCameraBodiesResultDto>.Failure(countResult.ErrorMessage ?? "Failed to get total count");
                    }

                    cameraBodies = pagedResult.Data;
                    totalCount = countResult.Data;
                }

                var cameraDtos = cameraBodies.Select(c => new CameraBodyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    SensorType = c.SensorType,
                    SensorWidth = c.SensorWidth,
                    SensorHeight = c.SensorHeight,
                    MountType = c.MountType,
                    IsUserCreated = c.IsUserCreated,
                    DateAdded = c.DateAdded,
                    DisplayName = c.GetDisplayName()
                }).ToList();

                var result = new GetCameraBodiesResultDto
                {
                    CameraBodies = cameraDtos,
                    TotalCount = totalCount,
                    HasMore = (request.Skip + request.Take) < totalCount
                };

                return Result<GetCameraBodiesResultDto>.Success(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving camera bodies");
                return Result<GetCameraBodiesResultDto>.Failure($"Error retrieving cameras: {ex.Message}");
            }
        }
    }
}