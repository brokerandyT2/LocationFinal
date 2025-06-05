using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Notifications;
using Location.Photography.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Application.Commands.CameraEvaluation
{
    public class CreateLensCommand : IRequest<Result<CreateLensResultDto>>
    {
        public double MinMM { get; set; }
        public double? MaxMM { get; set; }
        public double? MinFStop { get; set; }
        public double? MaxFStop { get; set; }
        public bool IsUserCreated { get; set; } = true;
        public List<int> CompatibleCameraIds { get; set; } = new List<int>();
    }

    public class CreateLensResultDto
    {
        public LensDto Lens { get; set; } = new LensDto();
        public List<int> CompatibleCameraIds { get; set; } = new List<int>();
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class LensDto
    {
        public int Id { get; set; }
        public double MinMM { get; set; }
        public double? MaxMM { get; set; }
        public double? MinFStop { get; set; }
        public double? MaxFStop { get; set; }
        public bool IsPrime { get; set; }
        public bool IsUserCreated { get; set; }
        public DateTime DateAdded { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class CreateLensCommandHandler : IRequestHandler<CreateLensCommand, Result<CreateLensResultDto>>
    {
        private readonly ILensRepository _lensRepository;
        private readonly ILensCameraCompatibilityRepository _compatibilityRepository;
        private readonly ILogger<CreateLensCommandHandler> _logger;
        private readonly IMediator _mediator;
        public CreateLensCommandHandler(
            ILensRepository lensRepository,
            ILensCameraCompatibilityRepository compatibilityRepository,
            ILogger<CreateLensCommandHandler> logger,
            IMediator mediator)
        {
            _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
            _compatibilityRepository = compatibilityRepository ?? throw new ArgumentNullException(nameof(compatibilityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator;
        }

        public async Task<Result<CreateLensResultDto>> Handle(CreateLensCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Validate that at least one camera is selected
                if (!request.CompatibleCameraIds.Any())
                {
                    return Result<CreateLensResultDto>.Failure("At least one compatible camera must be selected");
                }

                // Check for similar lens (fuzzy search by focal length)
                var searchFocalLength = request.MaxMM ?? request.MinMM;
                var existingResult = await _lensRepository.SearchByFocalLengthAsync(searchFocalLength, cancellationToken);
                if (existingResult.IsSuccess && existingResult.Data.Count > 0)
                {
                    var similarLens = existingResult.Data.FirstOrDefault();
                    return Result<CreateLensResultDto>.Failure($"A similar lens '{similarLens?.GetDisplayName()}' already exists. Continue anyway?");
                }

                // Create the lens
                var lens = new Lens(
                    request.MinMM,
                    request.MaxMM,
                    request.MinFStop,
                    request.MaxFStop,
                    request.IsUserCreated);

                var createResult = await _lensRepository.CreateAsync(lens, cancellationToken);

                if (!createResult.IsSuccess)
                {
                    return Result<CreateLensResultDto>.Failure(createResult.ErrorMessage ?? "Failed to create lens");
                }

                // Create compatibility relationships
                var compatibilities = request.CompatibleCameraIds
                    .Select(cameraId => new LensCameraCompatibility(createResult.Data.Id, cameraId))
                    .ToList();

                var compatibilityResult = await _compatibilityRepository.CreateBatchAsync(compatibilities, cancellationToken);

                if (!compatibilityResult.IsSuccess)
                {
                    // Lens was created but compatibility failed - log warning but don't fail
                    _logger.LogWarning("Lens created successfully but failed to create compatibility relationships: {Error}",
                        compatibilityResult.ErrorMessage);
                }

                var lensDto = new LensDto
                {
                    Id = createResult.Data.Id,
                    MinMM = createResult.Data.MinMM,
                    MaxMM = createResult.Data.MaxMM,
                    MinFStop = createResult.Data.MinFStop,
                    MaxFStop = createResult.Data.MaxFStop,
                    IsPrime = createResult.Data.IsPrime,
                    IsUserCreated = createResult.Data.IsUserCreated,
                    DateAdded = createResult.Data.DateAdded,
                    DisplayName = createResult.Data.GetDisplayName()
                };

                var resultDto = new CreateLensResultDto
                {
                    Lens = lensDto,
                    CompatibleCameraIds = request.CompatibleCameraIds,
                    IsSuccessful = true
                };

                // Publish notification
                var currentUserId = await SecureStorage.GetAsync("Email") ?? "default_user";
                await _mediator.Publish(new LensCreatedNotification(lensDto, currentUserId), cancellationToken);

                _logger.LogInformation("Successfully created lens: {DisplayName} with {CameraCount} compatible cameras",
                    createResult.Data.GetDisplayName(), request.CompatibleCameraIds.Count);

                return Result<CreateLensResultDto>.Success(resultDto);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lens");
                return Result<CreateLensResultDto>.Failure($"Error creating lens: {ex.Message}");
            }
        }
    }
}