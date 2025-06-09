using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Notifications;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Application.Commands.CameraEvaluation
{
    public class CreateCameraBodyCommand : IRequest<Result<CameraBodyDto>>
    {
        public string Name { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public double SensorWidth { get; set; }
        public double SensorHeight { get; set; }
        public MountType MountType { get; set; }
        public bool IsUserCreated { get; set; } = true;
    }

    public class CameraBodyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public double SensorWidth { get; set; }
        public double SensorHeight { get; set; }
        public MountType MountType { get; set; }
        public bool IsUserCreated { get; set; }
        public DateTime DateAdded { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class CreateCameraBodyCommandHandler : IRequestHandler<CreateCameraBodyCommand, Result<CameraBodyDto>>
    {
        private readonly ICameraBodyRepository _cameraBodyRepository;
        private readonly ILogger<CreateCameraBodyCommandHandler> _logger;
        private readonly IMediator _mediator;

        public CreateCameraBodyCommandHandler(
            ICameraBodyRepository cameraBodyRepository,
            ILogger<CreateCameraBodyCommandHandler> logger, IMediator mediator)
        {
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator;
        }

        public async Task<Result<CameraBodyDto>> Handle(CreateCameraBodyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check for fuzzy duplicate
                var existingResult = await _cameraBodyRepository.SearchByNameAsync(request.Name, cancellationToken);
                if (existingResult.IsSuccess && existingResult.Data.Count > 0)
                {
                    return Result<CameraBodyDto>.Failure($"A similar camera '{existingResult.Data[0].Name}' already exists. Continue anyway?");
                }

                // Create the camera body
                var cameraBody = new CameraBody(
                    request.Name,
                    request.SensorType,
                    request.SensorWidth,
                    request.SensorHeight,
                    request.MountType,
                    request.IsUserCreated);

                var createResult = await _cameraBodyRepository.CreateAsync(cameraBody, cancellationToken);

                if (!createResult.IsSuccess)
                {
                    return Result<CameraBodyDto>.Failure(createResult.ErrorMessage ?? "Failed to create camera body");
                }

                var dto = new CameraBodyDto
                {
                    Id = createResult.Data.Id,
                    Name = createResult.Data.Name,
                    SensorType = createResult.Data.SensorType,
                    SensorWidth = createResult.Data.SensorWidth,
                    SensorHeight = createResult.Data.SensorHeight,
                    MountType = createResult.Data.MountType,
                    IsUserCreated = createResult.Data.IsUserCreated,
                    DateAdded = createResult.Data.DateAdded,
                    DisplayName = createResult.Data.GetDisplayName()
                };

                // Publish notification
                var currentUserId = await SecureStorage.GetAsync("Email") ?? "default_user";
                await _mediator.Publish(new CameraCreatedNotification(dto, currentUserId), cancellationToken);

                _logger.LogInformation("Successfully created camera body: {Name}", request.Name);
                return Result<CameraBodyDto>.Success(dto);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating camera body: {Name}", request.Name);
                return Result<CameraBodyDto>.Failure($"Error creating camera body: {ex.Message}");
            }
        }
    }
}