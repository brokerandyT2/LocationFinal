// Location.Photography.Infrastructure/Services/ExposureCalculatorService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Errors;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class ExposureCalculatorService : IExposureCalculatorService
    {
        private readonly ILogger<ExposureCalculatorService> _logger;
        private readonly IExposureTriangleService _exposureTriangleService;

        public ExposureCalculatorService(ILogger<ExposureCalculatorService> logger, IExposureTriangleService exposureTriangleService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exposureTriangleService = exposureTriangleService ?? throw new ArgumentNullException(nameof(exposureTriangleService));
        }

        public async Task<Result<ExposureSettingsDto>> CalculateShutterSpeedAsync(
            ExposureTriangleDto baseExposure,
            string targetAperture,
            string targetIso,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default,
            double evCompensation = 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var scale = GetIncrementScale(increments);
                var shutterSpeed = _exposureTriangleService.CalculateShutterSpeed(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetAperture,
                    targetIso,
                    scale,
                    evCompensation);

                return Result<ExposureSettingsDto>.Success(new ExposureSettingsDto
                {
                    ShutterSpeed = shutterSpeed,
                    Aperture = targetAperture,
                    Iso = targetIso
                });
            }
            catch (ExposureError ex)
            {
                _logger.LogWarning(ex, "Exposure error calculating shutter speed");
                return Result<ExposureSettingsDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shutter speed");
                return Result<ExposureSettingsDto>.Failure($"Error calculating shutter speed: {ex.Message}");
            }
        }

        public async Task<Result<ExposureSettingsDto>> CalculateApertureAsync(
            ExposureTriangleDto baseExposure,
            string targetShutterSpeed,
            string targetIso,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default,
            double evCompensation = 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var scale = GetIncrementScale(increments);
                var aperture = _exposureTriangleService.CalculateAperture(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetIso,
                    scale,
                    evCompensation);

                return Result<ExposureSettingsDto>.Success(new ExposureSettingsDto
                {
                    ShutterSpeed = targetShutterSpeed,
                    Aperture = aperture,
                    Iso = targetIso
                });
            }
            catch (ExposureError ex)
            {
                _logger.LogWarning(ex, "Exposure error calculating aperture");
                return Result<ExposureSettingsDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating aperture");
                return Result<ExposureSettingsDto>.Failure($"Error calculating aperture: {ex.Message}");
            }
        }

        public async Task<Result<ExposureSettingsDto>> CalculateIsoAsync(
            ExposureTriangleDto baseExposure,
            string targetShutterSpeed,
            string targetAperture,
            ExposureIncrements increments,
            CancellationToken cancellationToken = default,
            double evCompensation = 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var scale = GetIncrementScale(increments);
                var iso = _exposureTriangleService.CalculateIso(
                    baseExposure.ShutterSpeed,
                    baseExposure.Aperture,
                    baseExposure.Iso,
                    targetShutterSpeed,
                    targetAperture,
                    scale,
                    evCompensation);

                return Result<ExposureSettingsDto>.Success(new ExposureSettingsDto
                {
                    ShutterSpeed = targetShutterSpeed,
                    Aperture = targetAperture,
                    Iso = iso
                });
            }
            catch (ExposureError ex)
            {
                _logger.LogWarning(ex, "Exposure error calculating ISO");
                return Result<ExposureSettingsDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ISO");
                return Result<ExposureSettingsDto>.Failure($"Error calculating ISO: {ex.Message}");
            }
        }

        public async Task<Result<string[]>> GetShutterSpeedsAsync(ExposureIncrements increments, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string step = increments.ToString();
                return Result<string[]>.Success(ShutterSpeeds.GetScale(step));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shutter speeds");
                return Result<string[]>.Failure($"Error retrieving shutter speeds: {ex.Message}");
            }
        }

        public async Task<Result<string[]>> GetAperturesAsync(ExposureIncrements increments, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string step = increments.ToString();
                return Result<string[]>.Success(Apetures.GetScale(step));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving apertures");
                return Result<string[]>.Failure($"Error retrieving apertures: {ex.Message}");
            }
        }

        public async Task<Result<string[]>> GetIsosAsync(ExposureIncrements increments, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string step = increments.ToString();
                return Result<string[]>.Success(ISOs.GetScale(step));
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ISOs");
                return Result<string[]>.Failure($"Error retrieving ISOs: {ex.Message}");
            }
        }

        private int GetIncrementScale(ExposureIncrements increments)
        {
            return increments switch
            {
                ExposureIncrements.Full => 1,
                ExposureIncrements.Half => 2,
                ExposureIncrements.Third => 3,
                _ => 1
            };
        }
    }
}