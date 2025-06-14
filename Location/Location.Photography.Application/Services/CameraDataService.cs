using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Queries.CameraEvaluation;
using Location.Photography.Application.Resources;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Application.Services
{
    public class CameraDataService : ICameraDataService
    {
        private readonly IMediator _mediator;
        private readonly ICameraBodyRepository _cameraBodyRepository;
        private readonly ILensRepository _lensRepository;
        private readonly ILensCameraCompatibilityRepository _compatibilityRepository;
        private readonly ILogger<CameraDataService> _logger;
        private readonly IUserCameraBodyRepository _userCameraBodyRepository;

        public CameraDataService(
            IMediator mediator,
            ICameraBodyRepository cameraBodyRepository,
            ILensRepository lensRepository,
            ILensCameraCompatibilityRepository compatibilityRepository,
            ILogger<CameraDataService> logger,
            IUserCameraBodyRepository userCameraBodyRepository)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
            _compatibilityRepository = compatibilityRepository ?? throw new ArgumentNullException(nameof(compatibilityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userCameraBodyRepository = userCameraBodyRepository ?? throw new ArgumentNullException(nameof(userCameraBodyRepository));
        }

        public async Task<Result<GetCameraBodiesResultDto>> GetCameraBodiesAsync(
            int skip = 0,
            int take = 20,
            bool userCamerasOnly = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetCameraBodiesQuery
                {
                    Skip = skip,
                    Take = take,
                    UserCamerasOnly = userCamerasOnly
                };

                return await _mediator.Send(query, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting camera bodies");
                return Result<GetCameraBodiesResultDto>.Failure(AppResources.CameraEvaluation_Error_RetrievingCameras);
            }
        }

        public async Task<Result<GetLensesResultDto>> GetLensesAsync(
            int skip = 0,
            int take = 20,
            bool userLensesOnly = false,
            int? compatibleWithCameraId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetLensesQuery
                {
                    Skip = skip,
                    Take = take,
                    UserLensesOnly = userLensesOnly,
                    CompatibleWithCameraId = compatibleWithCameraId
                };

                return await _mediator.Send(query, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lenses");
                return Result<GetLensesResultDto>.Failure(AppResources.CameraEvaluation_Error_RetrievingLenses);
            }
        }

        public async Task<Result<CameraBodyDto>> CreateCameraBodyAsync(
            string name,
            string sensorType,
            double sensorWidth,
            double sensorHeight,
            MountType mountType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var command = new CreateCameraBodyCommand
                {
                    Name = name,
                    SensorType = sensorType,
                    SensorWidth = sensorWidth,
                    SensorHeight = sensorHeight,
                    MountType = mountType,
                    IsUserCreated = true
                };

                return await _mediator.Send(command, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating camera body: {Name}", name);
                return Result<CameraBodyDto>.Failure(AppResources.CameraEvaluation_Error_CreatingCamera);
            }
        }

        public async Task<Result<CreateLensResultDto>> CreateLensAsync(
            double minMM,
            double? maxMM,
            double? minFStop,
            double? maxFStop,
            List<int> compatibleCameraIds,
            string lensName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var command = new CreateLensCommand
                {
                    MinMM = minMM,
                    MaxMM = maxMM,
                    MinFStop = minFStop,
                    MaxFStop = maxFStop,
                    IsUserCreated = true,
                    CompatibleCameraIds = compatibleCameraIds,
                    LensName = lensName
                };

                return await _mediator.Send(command, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lens");
                return Result<CreateLensResultDto>.Failure(AppResources.CameraEvaluation_Error_CreatingLens);
            }
        }

        public async Task<Result<List<CameraBodyDto>>> CheckDuplicateCameraAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var searchResult = await _cameraBodyRepository.SearchByNameAsync(name, cancellationToken);
                if (!searchResult.IsSuccess)
                {
                    return Result<List<CameraBodyDto>>.Failure(searchResult.ErrorMessage ?? AppResources.CameraEvaluation_Error_RetrievingCameras);
                }

                var dtos = searchResult.Data.Select(c => new CameraBodyDto
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

                return Result<List<CameraBodyDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate camera: {Name}", name);
                return Result<List<CameraBodyDto>>.Failure(AppResources.CameraEvaluation_Error_CheckingDuplicates);
            }
        }

        public async Task<Result<List<LensDto>>> CheckDuplicateLensAsync(
            double focalLength,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var searchResult = await _lensRepository.SearchByFocalLengthAsync(focalLength, cancellationToken);
                if (!searchResult.IsSuccess)
                {
                    return Result<List<LensDto>>.Failure(searchResult.ErrorMessage ?? AppResources.CameraEvaluation_Error_RetrievingLenses);
                }

                var dtos = searchResult.Data.Select(l => new LensDto
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

                return Result<List<LensDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate lens: {FocalLength}", focalLength);
                return Result<List<LensDto>>.Failure(AppResources.CameraEvaluation_Error_CheckingDuplicates);
            }
        }

        public async Task<Result<GetCameraBodiesResultDto>> GetUserCameraBodiesAsync(
            string userId,
            int skip = 0,
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get user's saved camera IDs
                var userCamerasResult = await _userCameraBodyRepository.GetByUserIdAsync(userId, cancellationToken);
                if (!userCamerasResult.IsSuccess)
                {
                    return Result<GetCameraBodiesResultDto>.Failure(userCamerasResult.ErrorMessage ?? AppResources.CameraEvaluation_Error_GettingUserCameras);
                }

                var userCameraIds = userCamerasResult.Data.Select(uc => uc.CameraBodyId).ToList();

                if (!userCameraIds.Any())
                {
                    return Result<GetCameraBodiesResultDto>.Success(new GetCameraBodiesResultDto
                    {
                        CameraBodies = new List<CameraBodyDto>(),
                        TotalCount = 0,
                        HasMore = false
                    });
                }

                // Get all cameras and filter by user's saved ones
                var allCamerasResult = await GetCameraBodiesAsync(0, int.MaxValue, false, cancellationToken);
                if (!allCamerasResult.IsSuccess)
                {
                    return Result<GetCameraBodiesResultDto>.Failure(allCamerasResult.ErrorMessage ?? AppResources.CameraEvaluation_Error_RetrievingCameras);
                }

                // Filter to only user's cameras
                var userCameras = allCamerasResult.Data.CameraBodies
                    .Where(c => userCameraIds.Contains(c.Id))
                    .OrderBy(c => c.DisplayName)
                    .ToList();

                // Apply paging
                var totalCount = userCameras.Count;
                var pagedCameras = userCameras
                    .Skip(skip)
                    .Take(take)
                    .ToList();

                var result = new GetCameraBodiesResultDto
                {
                    CameraBodies = pagedCameras,
                    TotalCount = totalCount,
                    HasMore = (skip + take) < totalCount
                };

                return Result<GetCameraBodiesResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user camera bodies for user: {UserId}", userId);
                return Result<GetCameraBodiesResultDto>.Failure(AppResources.CameraEvaluation_Error_GettingUserCameras);
            }
        }

        public async Task<Result<List<MountTypeDto>>> GetMountTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.CompletedTask; // Make async for consistency

                var mountTypes = new List<MountTypeDto>
                {
                    new MountTypeDto { Value = MountType.CanonEF, DisplayName = "Canon EF", Brand = "Canon" },
                    new MountTypeDto { Value = MountType.CanonEFS, DisplayName = "Canon EF-S", Brand = "Canon" },
                    new MountTypeDto { Value = MountType.CanonRF, DisplayName = "Canon RF", Brand = "Canon" },
                    new MountTypeDto { Value = MountType.CanonEFM, DisplayName = "Canon EF-M", Brand = "Canon" },
                    new MountTypeDto { Value = MountType.NikonF, DisplayName = "Nikon F", Brand = "Nikon" },
                    new MountTypeDto { Value = MountType.NikonZ, DisplayName = "Nikon Z", Brand = "Nikon" },
                    new MountTypeDto { Value = MountType.SonyE, DisplayName = "Sony E", Brand = "Sony" },
                    new MountTypeDto { Value = MountType.SonyFE, DisplayName = "Sony FE", Brand = "Sony" },
                    new MountTypeDto { Value = MountType.FujifilmX, DisplayName = "Fujifilm X", Brand = "Fujifilm" },
                    new MountTypeDto { Value = MountType.FujifilmGFX, DisplayName = "Fujifilm GFX", Brand = "Fujifilm" },
                    new MountTypeDto { Value = MountType.MicroFourThirds, DisplayName = "Micro Four Thirds", Brand = "Olympus/Panasonic" },
                    new MountTypeDto { Value = MountType.PentaxK, DisplayName = "Pentax K", Brand = "Pentax" },
                    new MountTypeDto { Value = MountType.LeicaM, DisplayName = "Leica M", Brand = "Leica" },
                    new MountTypeDto { Value = MountType.LeicaL, DisplayName = "Leica L", Brand = "Leica" },
                    new MountTypeDto { Value = MountType.LeicaSL, DisplayName = "Leica SL", Brand = "Leica" },
                    new MountTypeDto { Value = MountType.Other, DisplayName = "Other", Brand = "Various" }
                };

                return Result<List<MountTypeDto>>.Success(mountTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mount types");
                return Result<List<MountTypeDto>>.Failure(AppResources.CameraEvaluation_Error_GettingMountTypes);
            }
        }

        public Task<Result<bool>> UpdateLensCompatibilityAsync(int lensId, List<int> compatibleCameraIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}