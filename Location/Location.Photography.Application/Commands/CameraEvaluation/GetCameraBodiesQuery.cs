// Location.Photography.Application/Queries/CameraEvaluation/GetCameraBodiesQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
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
        private readonly ICameraSensorProfileService _cameraSensorProfileService;
        private readonly ILogger<GetCameraBodiesQueryHandler> _logger;

        public GetCameraBodiesQueryHandler(
            ICameraBodyRepository cameraBodyRepository,
            ICameraSensorProfileService cameraSensorProfileService,
            ILogger<GetCameraBodiesQueryHandler> logger)
        {
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _cameraSensorProfileService = cameraSensorProfileService ?? throw new ArgumentNullException(nameof(cameraSensorProfileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetCameraBodiesResultDto>> Handle(GetCameraBodiesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var allCameras = new List<CameraBodyDto>();

                // Step 1: Load user-created cameras from database
                if (request.UserCamerasOnly)
                {
                    var userCamerasResult = await _cameraBodyRepository.GetUserCamerasAsync(cancellationToken);
                    if (!userCamerasResult.IsSuccess)
                    {
                        return Result<GetCameraBodiesResultDto>.Failure(userCamerasResult.ErrorMessage ?? "Failed to retrieve user cameras");
                    }

                    var userCameraDtos = userCamerasResult.Data.Select(c => new CameraBodyDto
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

                    allCameras.AddRange(userCameraDtos);
                }
                else
                {
                    // Load all database cameras (user + system)
                    var allDbCamerasResult = await _cameraBodyRepository.GetPagedAsync(0, int.MaxValue, cancellationToken);
                    if (allDbCamerasResult.IsSuccess)
                    {
                        var dbCameraDtos = allDbCamerasResult.Data.Select(c => new CameraBodyDto
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

                        allCameras.AddRange(dbCameraDtos);
                    }

                    // Step 2: Load cameras from JSON sensor profiles
                    var jsonCamerasResult = await _cameraSensorProfileService.LoadCameraSensorProfilesAsync(new List<string>(), cancellationToken);
                    if (jsonCamerasResult.IsSuccess)
                    {
                        allCameras.AddRange(jsonCamerasResult.Data);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to load JSON camera profiles: {Error}", jsonCamerasResult.ErrorMessage);
                    }
                }

                // Step 3: Sort cameras (user cameras first, then JSON cameras, then alphabetically)
                var sortedCameras = allCameras
                    .OrderBy(c => c.IsUserCreated ? 0 : 1) // User cameras first
                    .ThenBy(c => c.DisplayName)
                    .ToList();

                // Step 4: Apply paging
                var totalCount = sortedCameras.Count;
                var pagedCameras = sortedCameras
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToList();

                var result = new GetCameraBodiesResultDto
                {
                    CameraBodies = pagedCameras,
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