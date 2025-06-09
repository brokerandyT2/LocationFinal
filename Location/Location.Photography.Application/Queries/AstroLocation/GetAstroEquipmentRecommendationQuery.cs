// Location.Photography.Application/Queries/AstroLocation/GetAstroEquipmentRecommendationQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using MediatR;

namespace Location.Photography.Application.Queries.AstroLocation
{
    public class GetAstroEquipmentRecommendationQuery : IRequest<Result<AstroEquipmentRecommendationResult>>
    {
        public AstroTarget Target { get; set; }
        public DateTime DateTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double TargetAltitude { get; set; }
        public double TargetAzimuth { get; set; }
        public double? TargetMagnitude { get; set; }
        public double? TargetAngularSize { get; set; }

        public class GetAstroEquipmentRecommendationQueryHandler : IRequestHandler<GetAstroEquipmentRecommendationQuery, Result<AstroEquipmentRecommendationResult>>
        {
            private readonly IExposureCalculatorService _exposureCalculatorService;
            private readonly IUserCameraBodyRepository _userCameraBodyRepository;
            private readonly ILensRepository _lensRepository;

            public GetAstroEquipmentRecommendationQueryHandler(
                IExposureCalculatorService exposureCalculatorService,
                IUserCameraBodyRepository userCameraBodyRepository,
                ILensRepository lensRepository)
            {
                _exposureCalculatorService = exposureCalculatorService ?? throw new ArgumentNullException(nameof(exposureCalculatorService));
                _userCameraBodyRepository = userCameraBodyRepository ?? throw new ArgumentNullException(nameof(userCameraBodyRepository));
                _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
            }

            public async Task<Result<AstroEquipmentRecommendationResult>> Handle(GetAstroEquipmentRecommendationQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    var result = new AstroEquipmentRecommendationResult();

                    // Simple equipment recommendations without the complex service
                    result.RecommendedCamera = GetGenericCameraRecommendation(request.Target);
                    result.RecommendedLens = GetGenericLensRecommendation(request.Target);
                    result.FieldOfViewWidth = 0; // Will be calculated later
                    result.FieldOfViewHeight = 0;
                    result.TargetFitsInFrame = true; // Assume it fits for now
                    result.TargetCoveragePercentage = 75; // Default

                    // Calculate exposure recommendations
                    var exposureRecommendation = await CalculateExposureRecommendations(request);
                    result.ExposureSettings = exposureRecommendation.ExposureSettings;
                    result.FocusDistance = exposureRecommendation.FocusDistance;
                    result.TrackingRequired = exposureRecommendation.TrackingRequired;

                    // Calculate stacking recommendations
                    var stackingRecommendation = CalculateStackingRecommendations(request.Target, request.TargetMagnitude, request.TargetAltitude);
                    result.StackingRecommendations = stackingRecommendation;

                    return Result<AstroEquipmentRecommendationResult>.Success(result);
                }
                catch (Exception ex)
                {
                    return Result<AstroEquipmentRecommendationResult>.Failure($"Error calculating equipment recommendations: {ex.Message}");
                }
            }

            private async Task<ExposureRecommendationResult> CalculateExposureRecommendations(GetAstroEquipmentRecommendationQuery request)
            {
                var result = new ExposureRecommendationResult();

                try
                {
                    // Get user's primary camera for calculations - use simplified approach
                    var userCameras = await GetUserCamerasAsync();
                    var primaryCamera = userCameras?.FirstOrDefault()?.CameraBody;

                    // Calculate based on target type and conditions
                    var exposureCalc = request.Target switch
                    {
                        AstroTarget.Moon => CalculateLunarExposure(request.TargetAltitude),
                        AstroTarget.Planets => CalculatePlanetaryExposure(request.TargetMagnitude, request.TargetAngularSize),
                        AstroTarget.MilkyWayCore => CalculateMilkyWayExposure(request.TargetAltitude),
                        AstroTarget.DeepSkyObjects => CalculateDeepSkyExposure(request.TargetMagnitude, request.TargetAngularSize),
                        AstroTarget.MeteorShowers => CalculateMeteorExposure(),
                        AstroTarget.StarTrails => CalculateStarTrailExposure(),
                        _ => CalculateGenericExposure(request.Target)
                    };

                    result.ExposureSettings = exposureCalc.Settings;
                    result.FocusDistance = exposureCalc.FocusDistance;
                    result.TrackingRequired = exposureCalc.TrackingRequired;
                }
                catch
                {
                    // Fallback to safe defaults
                    result.ExposureSettings = "f/2.8, 30s, ISO 3200";
                    result.FocusDistance = "Infinity";
                    result.TrackingRequired = false;
                }

                return result;
            }

            private async Task<List<UserCameraBody>?> GetUserCamerasAsync()
            {
                try
                {
                    // Remove the faulty user camera logic for now - return empty list
                    // TODO: Implement proper user camera retrieval when the correct repository method is available
                    return new List<UserCameraBody>();
                }
                catch
                {
                    return null;
                }
            }

            private string GetGenericCameraRecommendation(AstroTarget target)
            {
                return target switch
                {
                    AstroTarget.Moon => "Full-frame or APS-C camera",
                    AstroTarget.Planets => "High-resolution camera with good sensor",
                    AstroTarget.MilkyWayCore => "Full-frame camera with good high-ISO performance",
                    AstroTarget.DeepSkyObjects => "Cooled camera or modified DSLR",
                    AstroTarget.MeteorShowers => "Any camera with manual controls",
                    _ => "DSLR or mirrorless camera"
                };
            }

            private string GetGenericLensRecommendation(AstroTarget target)
            {
                return target switch
                {
                    AstroTarget.Moon => "300-600mm telephoto lens",
                    AstroTarget.Planets => "500mm+ telephoto or telescope",
                    AstroTarget.MilkyWayCore => "14-24mm wide-angle lens f/2.8",
                    AstroTarget.DeepSkyObjects => "50-200mm telephoto lens",
                    AstroTarget.MeteorShowers => "14-35mm wide-angle lens",
                    _ => "24-70mm standard lens"
                };
            }

            private StackingRecommendationResult CalculateStackingRecommendations(AstroTarget target, double? magnitude, double altitude)
            {
                var result = new StackingRecommendationResult();

                var (frames, exposureTime, totalMinutes) = target switch
                {
                    AstroTarget.Moon => (20, 1.0 / 250, 0.1), // Fast shutter for moon
                    AstroTarget.Planets => (100, 1.0 / 60, 1.7), // Video capture equivalent
                    AstroTarget.MilkyWayCore => (15, 30, 7.5), // Wide field shots
                    AstroTarget.DeepSkyObjects => DSOStackingCalc(magnitude, altitude),
                    AstroTarget.MeteorShowers => (1, 30, 0.5), // Single long exposures
                    AstroTarget.StarTrails => (60, 240, 240), // 4-minute intervals
                    _ => (10, 30, 5) // Default
                };

                result.RecommendedFrames = frames;
                result.ExposureTimePerFrame = exposureTime;
                result.TotalExposureMinutes = totalMinutes;
                result.CalibrationFrames = target == AstroTarget.DeepSkyObjects ? "20 darks, 20 flats, 20 bias" : "10 darks recommended";
                result.ExpectedQuality = CalculateExpectedQuality(target, frames, totalMinutes, altitude);

                return result;
            }

            private (int frames, double exposureTime, double totalMinutes) DSOStackingCalc(double? magnitude, double altitude)
            {
                // More frames for fainter objects and lower altitudes
                var baseFrames = 30;
                var baseExposure = 120.0; // 2 minutes

                if (magnitude.HasValue && magnitude > 8)
                    baseFrames += 20; // Faint object

                if (altitude < 30)
                    baseFrames += 10; // Low altitude, more atmospheric extinction

                var totalMinutes = (baseFrames * baseExposure) / 60.0;
                return (baseFrames, baseExposure, totalMinutes);
            }

            private string CalculateExpectedQuality(AstroTarget target, int frames, double totalMinutes, double altitude)
            {
                var baseQuality = target switch
                {
                    AstroTarget.Moon => 95,
                    AstroTarget.Planets => 85,
                    AstroTarget.MilkyWayCore => 80,
                    AstroTarget.DeepSkyObjects => 70,
                    _ => 75
                };

                // Adjust for number of frames and altitude
                if (frames > 50) baseQuality += 10;
                if (totalMinutes > 60) baseQuality += 5;
                if (altitude > 60) baseQuality += 10;
                else if (altitude < 30) baseQuality -= 15;

                baseQuality = Math.Max(50, Math.Min(100, baseQuality));
                return $"{baseQuality}% excellent";
            }

            // Exposure calculation methods
            private (string Settings, string FocusDistance, bool TrackingRequired) CalculateLunarExposure(double altitude)
            {
                var iso = altitude > 45 ? "ISO 100" : "ISO 200";
                return ($"f/8, 1/250s, {iso}", "Infinity", false);
            }

            private (string Settings, string FocusDistance, bool TrackingRequired) CalculatePlanetaryExposure(double? magnitude, double? angularSize)
            {
                var iso = magnitude < 0 ? "ISO 400" : "ISO 800";
                return ($"f/8, 1/60s, {iso}", "Infinity", true);
            }

            private (string Settings, string FocusDistance, bool TrackingRequired) CalculateMilkyWayExposure(double altitude)
            {
                return ("f/2.8, 30s, ISO 3200", "Infinity", false);
            }

            private (string Settings, string FocusDistance, bool TrackingRequired) CalculateDeepSkyExposure(double? magnitude, double? angularSize)
            {
                var iso = magnitude > 8 ? "ISO 1600" : "ISO 800";
                return ($"f/4, 120s, {iso}", "Infinity", true);
            }

            private (string Settings, string FocusDistance, bool TrackingRequired) CalculateMeteorExposure()
            {
                return ("f/2.8, 30s, ISO 3200", "Infinity", false);
            }

            private (string Settings, string FocusDistance, bool TrackingRequired) CalculateStarTrailExposure()
            {
                return ("f/8, 240s, ISO 400", "Infinity", false);
            }

            private (string Settings, string FocusDistance, bool TrackingRequired) CalculateGenericExposure(AstroTarget target)
            {
                return ("f/4, 60s, ISO 1600", "Infinity", false);
            }
        }

        private StackingRecommendationResult CalculateStackingRecommendations(AstroTarget target, double? magnitude, double altitude)
        {
            var result = new StackingRecommendationResult();

            var (frames, exposureTime, totalMinutes) = target switch
            {
                AstroTarget.Moon => (20, 1.0 / 250, 0.1), // Fast shutter for moon
                AstroTarget.Planets => (100, 1.0 / 60, 1.7), // Video capture equivalent
                AstroTarget.MilkyWayCore => (15, 30, 7.5), // Wide field shots
                AstroTarget.DeepSkyObjects => DSOStackingCalc(magnitude, altitude),
                AstroTarget.MeteorShowers => (1, 30, 0.5), // Single long exposures
                AstroTarget.StarTrails => (60, 240, 240), // 4-minute intervals
                _ => (10, 30, 5) // Default
            };

            result.RecommendedFrames = frames;
            result.ExposureTimePerFrame = exposureTime;
            result.TotalExposureMinutes = totalMinutes;
            result.CalibrationFrames = target == AstroTarget.DeepSkyObjects ? "20 darks, 20 flats, 20 bias" : "10 darks recommended";
            result.ExpectedQuality = CalculateExpectedQuality(target, frames, totalMinutes, altitude);

            return result;
        }

        private (int frames, double exposureTime, double totalMinutes) DSOStackingCalc(double? magnitude, double altitude)
        {
            // More frames for fainter objects and lower altitudes
            var baseFrames = 30;
            var baseExposure = 120.0; // 2 minutes

            if (magnitude.HasValue && magnitude > 8)
                baseFrames += 20; // Faint object

            if (altitude < 30)
                baseFrames += 10; // Low altitude, more atmospheric extinction

            var totalMinutes = (baseFrames * baseExposure) / 60.0;
            return (baseFrames, baseExposure, totalMinutes);
        }

        private string CalculateExpectedQuality(AstroTarget target, int frames, double totalMinutes, double altitude)
        {
            var baseQuality = target switch
            {
                AstroTarget.Moon => 95,
                AstroTarget.Planets => 85,
                AstroTarget.MilkyWayCore => 80,
                AstroTarget.DeepSkyObjects => 70,
                _ => 75
            };

            // Adjust for number of frames and altitude
            if (frames > 50) baseQuality += 10;
            if (totalMinutes > 60) baseQuality += 5;
            if (altitude > 60) baseQuality += 10;
            else if (altitude < 30) baseQuality -= 15;

            baseQuality = Math.Max(50, Math.Min(100, baseQuality));
            return $"{baseQuality}% excellent";
        }

        // Exposure calculation methods
        private (string Settings, string FocusDistance, bool TrackingRequired) CalculateLunarExposure(double altitude)
        {
            var iso = altitude > 45 ? "ISO 100" : "ISO 200";
            return ($"f/8, 1/250s, {iso}", "Infinity", false);
        }

        private (string Settings, string FocusDistance, bool TrackingRequired) CalculatePlanetaryExposure(double? magnitude, double? angularSize)
        {
            var iso = magnitude < 0 ? "ISO 400" : "ISO 800";
            return ($"f/8, 1/60s, {iso}", "Infinity", true);
        }

        private (string Settings, string FocusDistance, bool TrackingRequired) CalculateMilkyWayExposure(double altitude)
        {
            return ("f/2.8, 30s, ISO 3200", "Infinity", false);
        }

        private (string Settings, string FocusDistance, bool TrackingRequired) CalculateDeepSkyExposure(double? magnitude, double? angularSize)
        {
            var iso = magnitude > 8 ? "ISO 1600" : "ISO 800";
            return ($"f/4, 120s, {iso}", "Infinity", true);
        }

        private (string Settings, string FocusDistance, bool TrackingRequired) CalculateMeteorExposure()
        {
            return ("f/2.8, 30s, ISO 3200", "Infinity", false);
        }

        private (string Settings, string FocusDistance, bool TrackingRequired) CalculateStarTrailExposure()
        {
            return ("f/8, 240s, ISO 400", "Infinity", false);
        }

        private (string Settings, string FocusDistance, bool TrackingRequired) CalculateGenericExposure(AstroTarget target)
        {
            return ("f/4, 60s, ISO 1600", "Infinity", false);
        }
    }
}


public class AstroEquipmentRecommendationResult
{
    public string RecommendedCamera { get; set; } = string.Empty;
    public string RecommendedLens { get; set; } = string.Empty;
    public double FieldOfViewWidth { get; set; }
    public double FieldOfViewHeight { get; set; }
    public bool TargetFitsInFrame { get; set; }
    public double TargetCoveragePercentage { get; set; }
    public string ExposureSettings { get; set; } = string.Empty;
    public string FocusDistance { get; set; } = string.Empty;
    public bool TrackingRequired { get; set; }
    public StackingRecommendationResult StackingRecommendations { get; set; } = new();
}

public class ExposureRecommendationResult
{
    public string ExposureSettings { get; set; } = string.Empty;
    public string FocusDistance { get; set; } = string.Empty;
    public bool TrackingRequired { get; set; }
}

public class StackingRecommendationResult
{
    public int RecommendedFrames { get; set; }
    public double ExposureTimePerFrame { get; set; }
    public double TotalExposureMinutes { get; set; }
    public string CalibrationFrames { get; set; } = string.Empty;
    public string ExpectedQuality { get; set; } = string.Empty;

    public string GetFormattedRecommendation()
    {
        var exposureText = ExposureTimePerFrame < 1
            ? $"1/{(int)(1 / ExposureTimePerFrame)}s"
            : $"{ExposureTimePerFrame:F0}s";

        return $"{RecommendedFrames} frames @ {exposureText} each";
    }

    public string GetFormattedTotalTime()
    {
        if (TotalExposureMinutes < 60)
            return $"Total: {TotalExposureMinutes:F1} minutes";
        else
            return $"Total: {TotalExposureMinutes / 60:F1} hours";
    }
}
