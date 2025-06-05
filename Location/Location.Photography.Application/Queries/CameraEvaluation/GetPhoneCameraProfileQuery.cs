// Location.Photography.Application/Queries/CameraEvaluation/GetPhoneCameraProfileQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Queries.CameraEvaluation
{
    public class GetPhoneCameraProfileQuery : IRequest<Result<PhoneCameraProfileDto>>
    {
        // No parameters needed - gets the active phone camera profile
    }

    public class GetPhoneCameraProfileQueryHandler : IRequestHandler<GetPhoneCameraProfileQuery, Result<PhoneCameraProfileDto>>
    {
        private readonly IPhoneCameraProfileRepository _repository;
        private readonly ILogger<GetPhoneCameraProfileQueryHandler> _logger;

        public GetPhoneCameraProfileQueryHandler(
            IPhoneCameraProfileRepository repository,
            ILogger<GetPhoneCameraProfileQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PhoneCameraProfileDto>> Handle(GetPhoneCameraProfileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _repository.GetActiveProfileAsync(cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<PhoneCameraProfileDto>.Failure(result.ErrorMessage ?? "Failed to retrieve active phone camera profile");
                }

                if (result.Data == null)
                {
                    return Result<PhoneCameraProfileDto>.Failure("No active phone camera profile found");
                }

                var dto = new PhoneCameraProfileDto
                {
                    Id = result.Data.Id,
                    PhoneModel = result.Data.PhoneModel,
                    MainLensFocalLength = result.Data.MainLensFocalLength,
                    MainLensFOV = result.Data.MainLensFOV,
                    UltraWideFocalLength = result.Data.UltraWideFocalLength,
                    TelephotoFocalLength = result.Data.TelephotoFocalLength,
                    DateCalibrated = result.Data.DateCalibrated,
                    IsActive = result.Data.IsActive,
                    IsCalibrationSuccessful = true
                };

                return Result<PhoneCameraProfileDto>.Success(dto);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving phone camera profile");
                return Result<PhoneCameraProfileDto>.Failure($"Error retrieving phone camera profile: {ex.Message}");
            }
        }
    }
}