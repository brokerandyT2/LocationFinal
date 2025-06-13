// Location.Photography.Infrastructure/Services/CameraSensorProfileService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Services;
using Location.Photography.Application.Resources;
using Location.Photography.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Location.Photography.Infrastructure.Services
{
    public class CameraSensorProfileService : ICameraSensorProfileService
    {
        private readonly ILogger<CameraSensorProfileService> _logger;

        public CameraSensorProfileService(ILogger<CameraSensorProfileService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<CameraBodyDto>>> LoadCameraSensorProfilesAsync(List<string> jsonContents, CancellationToken cancellationToken = default)
        {
            try
            {
                var cameras = new List<CameraBodyDto>();

                foreach (var jsonContent in jsonContents)
                {
                    var fileCameras = await ParseCameraJsonAsync(jsonContent, cancellationToken);
                    if (fileCameras.IsSuccess)
                    {
                        cameras.AddRange(fileCameras.Data);
                    }
                }

                _logger.LogInformation("Loaded {Count} cameras from {FileCount} JSON contents", cameras.Count, jsonContents.Count);
                return Result<List<CameraBodyDto>>.Success(cameras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading camera sensor profiles from JSON contents");
                return Result<List<CameraBodyDto>>.Failure(AppResources.CameraEvaluation_Error_RetrievingCameras);
            }
        }

        private async Task<Result<List<CameraBodyDto>>> ParseCameraJsonAsync(string jsonContent, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cameras = new List<CameraBodyDto>();
                var jsonDocument = JsonDocument.Parse(jsonContent);

                if (jsonDocument.RootElement.TryGetProperty("Cameras", out var camerasElement))
                {
                    foreach (var camera in camerasElement.EnumerateObject())
                    {
                        var cameraName = camera.Name; // e.g., "Canon EOS 7D (2010 - 2010)"
                        var cameraData = camera.Value;

                        if (cameraData.TryGetProperty("Brand", out var brandElement) &&
                            cameraData.TryGetProperty("SensorType", out var sensorTypeElement) &&
                            cameraData.TryGetProperty("Sensor", out var sensorElement))
                        {
                            if (sensorElement.TryGetProperty("SensorWidthInMM", out var widthElement) &&
                                sensorElement.TryGetProperty("SensorHeightInMM", out var heightElement))
                            {
                                var mountType = DetermineMountType(brandElement.GetString(), cameraName);

                                var cameraDto = new CameraBodyDto
                                {
                                    Id = 0, // JSON cameras don't have database IDs
                                    Name = cameraName,
                                    SensorType = sensorTypeElement.GetString() ?? "Unknown",
                                    SensorWidth = widthElement.GetDouble(),
                                    SensorHeight = heightElement.GetDouble(),
                                    MountType = mountType,
                                    IsUserCreated = false,
                                    DateAdded = DateTime.UtcNow,
                                    DisplayName = cameraName // Use the full JSON key as display name
                                };

                                cameras.Add(cameraDto);
                            }
                        }
                    }
                }

                return Result<List<CameraBodyDto>>.Success(cameras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing camera JSON content");
                // TODO: Add new string entry to AppResources.resx:
                // <data name="CameraEvaluation_Error_ParsingCameraData">
                //   <value>Unable to load camera data</value>
                //   <comment>Error message when camera data parsing fails</comment>
                // </data>
                return Result<List<CameraBodyDto>>.Failure(AppResources.CameraEvaluation_Error_ParsingCameraData);
            }
        }

        private MountType DetermineMountType(string brand, string cameraName)
        {
            var brandLower = brand?.ToLowerInvariant() ?? "";
            var cameraNameLower = cameraName.ToLowerInvariant();

            return brandLower switch
            {
                "canon" when cameraNameLower.Contains("eos r") => MountType.CanonRF,
                "canon" when cameraNameLower.Contains("eos m") => MountType.CanonEFM,
                "canon" => MountType.CanonEF,
                "nikon" when cameraNameLower.Contains(" z") => MountType.NikonZ,
                "nikon" => MountType.NikonF,
                "sony" when cameraNameLower.Contains("fx") || cameraNameLower.Contains("a7") => MountType.SonyFE,
                "sony" => MountType.SonyE,
                "fujifilm" when cameraNameLower.Contains("gfx") => MountType.FujifilmGFX,
                "fujifilm" => MountType.FujifilmX,
                "pentax" => MountType.PentaxK,
                "olympus" => MountType.MicroFourThirds,
                "panasonic" => MountType.MicroFourThirds,
                "leica" when cameraNameLower.Contains(" sl") => MountType.LeicaSL,
                "leica" when cameraNameLower.Contains(" m") => MountType.LeicaM,
                "leica" => MountType.LeicaL,
                _ => MountType.Other
            };
        }
    }
}