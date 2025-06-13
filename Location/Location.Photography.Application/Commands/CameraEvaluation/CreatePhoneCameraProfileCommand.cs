using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Application.Resources;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Application.Commands.CameraEvaluation
{
    public class CreatePhoneCameraProfileCommand : IRequest<Result<PhoneCameraProfileDto>>
    {
        public string ImagePath { get; set; } = string.Empty;
        public bool DeleteImageAfterProcessing { get; set; } = true;
    }

    public class PhoneCameraProfileDto
    {
        public int Id { get; set; }
        public string PhoneModel { get; set; } = string.Empty;
        public double MainLensFocalLength { get; set; }
        public double MainLensFOV { get; set; }
        public double? UltraWideFocalLength { get; set; }
        public double? TelephotoFocalLength { get; set; }
        public DateTime DateCalibrated { get; set; }
        public bool IsActive { get; set; }
        public bool IsCalibrationSuccessful { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CreatePhoneCameraProfileCommandHandler : IRequestHandler<CreatePhoneCameraProfileCommand, Result<PhoneCameraProfileDto>>
    {
        private readonly IExifService _exifService;
        private readonly IFOVCalculationService _fovCalculationService;
        private readonly IPhoneCameraProfileRepository _repository;
        private readonly ILogger<CreatePhoneCameraProfileCommandHandler> _logger;

        public CreatePhoneCameraProfileCommandHandler(
            IExifService exifService,
            IFOVCalculationService fovCalculationService,
            IPhoneCameraProfileRepository repository,
            ILogger<CreatePhoneCameraProfileCommandHandler> logger)
        {
            _exifService = exifService ?? throw new ArgumentNullException(nameof(exifService));
            _fovCalculationService = fovCalculationService ?? throw new ArgumentNullException(nameof(fovCalculationService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PhoneCameraProfileDto>> Handle(CreatePhoneCameraProfileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.ImagePath))
                {
                    return Result<PhoneCameraProfileDto>.Failure(AppResources.CameraEvaluation_ValidationError_ImagePathRequired);
                }

                if (!File.Exists(request.ImagePath))
                {
                    return Result<PhoneCameraProfileDto>.Failure(AppResources.CameraEvaluation_ValidationError_ImageNotExists);
                }

                _logger.LogInformation("Starting phone camera profile creation from image: {ImagePath}", request.ImagePath);

                // Step 1: Extract EXIF data
                var exifResult = await _exifService.ExtractExifDataAsync(request.ImagePath, cancellationToken);
                if (!exifResult.IsSuccess)
                {
                    return CreateFailureDto($"{AppResources.CameraEvaluation_Error_ExtractingEXIF}: {exifResult.ErrorMessage}");
                }

                var exifData = exifResult.Data;

                // Step 2: Validate required EXIF data
                if (!exifData.HasValidFocalLength)
                {
                    return CreateFailureDto(AppResources.CameraEvaluation_Error_InvalidFocalLength);
                }

                if (string.IsNullOrEmpty(exifData.FullCameraModel))
                {
                    return CreateFailureDto(AppResources.CameraEvaluation_Error_MissingCameraModel);
                }

                // Step 3: Create phone camera profile
                var profileResult = await _fovCalculationService.CreatePhoneCameraProfileAsync(
                    exifData.FullCameraModel,
                    exifData.FocalLength.Value,
                    cancellationToken);

                if (!profileResult.IsSuccess)
                {
                    return CreateFailureDto($"{AppResources.CameraEvaluation_Error_CameraProfileFailed}: {profileResult.ErrorMessage}");
                }

                var profile = profileResult.Data;

                // Step 4: Deactivate existing profiles and save new one
                await DeactivateExistingProfilesAsync(cancellationToken);

                var saveResult = await _repository.CreateAsync(profile, cancellationToken);
                if (!saveResult.IsSuccess)
                {
                    return CreateFailureDto($"{AppResources.CameraEvaluation_Error_SavingProfile}: {saveResult.ErrorMessage}");
                }

                // Step 5: Cleanup temporary image file if requested
                if (request.DeleteImageAfterProcessing)
                {
                    try
                    {
                        File.Delete(request.ImagePath);
                        _logger.LogDebug("Deleted temporary image file: {ImagePath}", request.ImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary image file: {ImagePath}", request.ImagePath);
                    }
                }

                // Step 6: Create success response
                var dto = new PhoneCameraProfileDto
                {
                    Id = saveResult.Data.Id,
                    PhoneModel = saveResult.Data.PhoneModel,
                    MainLensFocalLength = saveResult.Data.MainLensFocalLength,
                    MainLensFOV = saveResult.Data.MainLensFOV,
                    UltraWideFocalLength = saveResult.Data.UltraWideFocalLength,
                    TelephotoFocalLength = saveResult.Data.TelephotoFocalLength,
                    DateCalibrated = saveResult.Data.DateCalibrated,
                    IsActive = saveResult.Data.IsActive,
                    IsCalibrationSuccessful = true
                };

                _logger.LogInformation(AppResources.Camera_Calibration_Successful,
                    dto.PhoneModel, dto.MainLensFocalLength, dto.MainLensFOV);

                return Result<PhoneCameraProfileDto>.Success(dto);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating phone camera profile from image: {ImagePath}", request.ImagePath);
                return CreateFailureDto($"{AppResources.CameraEvaluation_Error_CalibrationFailed}: {ex.Message}");
            }
        }

        private async Task DeactivateExistingProfilesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var existingProfilesResult = await _repository.GetAllAsync(cancellationToken);
                if (existingProfilesResult.IsSuccess)
                {
                    foreach (var existingProfile in existingProfilesResult.Data)
                    {
                        if (existingProfile.IsActive)
                        {
                            existingProfile.Deactivate();
                            await _repository.UpdateAsync(existingProfile, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deactivate existing profiles");
            }
        }

        private Result<PhoneCameraProfileDto> CreateFailureDto(string errorMessage)
        {
            var dto = new PhoneCameraProfileDto
            {
                IsCalibrationSuccessful = false,
                ErrorMessage = errorMessage
            };

            return Result<PhoneCameraProfileDto>.Success(dto);
        }
    }
}