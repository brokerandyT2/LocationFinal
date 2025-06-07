
// Location.Photography.Infrastructure/Services/EquipmentRecommendationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Infrastructure.Services
{
    public class EquipmentRecommendationService : IEquipmentRecommendationService
    {
        private readonly ICameraBodyRepository _cameraBodyRepository;
        private readonly ILensRepository _lensRepository;
        private readonly ILensCameraCompatibilityRepository _compatibilityRepository;
        private readonly ILogger<EquipmentRecommendationService> _logger;

        public EquipmentRecommendationService(
            ICameraBodyRepository cameraBodyRepository,
            ILensRepository lensRepository,
            ILensCameraCompatibilityRepository compatibilityRepository,
            ILogger<EquipmentRecommendationService> logger)
        {
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
            _compatibilityRepository = compatibilityRepository ?? throw new ArgumentNullException(nameof(compatibilityRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<UserEquipmentRecommendation>> GetUserEquipmentRecommendationAsync(
            AstroTarget target,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var specs = GetOptimalEquipmentSpecs(target);

                // Get user equipment
                var userCamerasResult = await _cameraBodyRepository.GetUserCamerasAsync(cancellationToken);
                var userLensesResult = await _lensRepository.GetUserLensesAsync(cancellationToken);

                if (!userCamerasResult.IsSuccess || !userLensesResult.IsSuccess)
                {
                    return Result<UserEquipmentRecommendation>.Failure("Failed to load user equipment");
                }

                var userCameras = userCamerasResult.Data ?? new List<CameraBody>();
                var userLenses = userLensesResult.Data ?? new List<Lens>();

                // Find matching lenses for target
                var matchingLenses = await FindMatchingLensesAsync(userLenses, specs, cancellationToken);

                // Find compatible cameras for each matching lens
                var combinations = new List<CameraLensCombination>();

                foreach (var lens in matchingLenses)
                {
                    var compatibleCameras = await GetCompatibleUserCamerasAsync(lens, userCameras, cancellationToken);

                    foreach (var camera in compatibleCameras)
                    {
                        var combination = await CreateCameraLensCombinationAsync(camera, lens, specs);
                        combinations.Add(combination);
                    }
                }

                // Sort by match score (best first)
                var orderedCombinations = combinations.OrderByDescending(c => c.MatchScore).ToList();

                var recommendation = new UserEquipmentRecommendation
                {
                    Target = target,
                    TargetSpecs = specs,
                    RecommendedCombinations = orderedCombinations.Where(c => c.MatchScore >= 70).ToList(),
                    AlternativeCombinations = orderedCombinations.Where(c => c.MatchScore < 70 && c.MatchScore >= 40).ToList(),
                    HasOptimalEquipment = orderedCombinations.Any(c => c.IsOptimal),
                    Summary = GenerateRecommendationSummary(orderedCombinations, specs, target)
                };

                return Result<UserEquipmentRecommendation>.Success(recommendation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user equipment recommendation for {Target}", target);
                return Result<UserEquipmentRecommendation>.Failure($"Error generating recommendation: {ex.Message}");
            }
        }

        public async Task<Result<List<HourlyEquipmentRecommendation>>> GetHourlyEquipmentRecommendationsAsync(
            AstroTarget target,
            List<DateTime> predictionTimes,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userRecommendationResult = await GetUserEquipmentRecommendationAsync(target, cancellationToken);
                var genericRecommendationResult = await GetGenericRecommendationAsync(target, cancellationToken);

                var hourlyRecommendations = new List<HourlyEquipmentRecommendation>();

                foreach (var time in predictionTimes)
                {
                    var hourlyRec = new HourlyEquipmentRecommendation
                    {
                        PredictionTime = time,
                        Target = target,
                        HasUserEquipment = userRecommendationResult.IsSuccess &&
                                         userRecommendationResult.Data.RecommendedCombinations.Any()
                    };

                    if (hourlyRec.HasUserEquipment)
                    {
                        var bestCombination = userRecommendationResult.Data.RecommendedCombinations.First();
                        hourlyRec.RecommendedCombination = bestCombination;
                        hourlyRec.Recommendation = bestCombination.DisplayText;
                    }
                    else if (genericRecommendationResult.IsSuccess)
                    {
                        hourlyRec.GenericRecommendation = genericRecommendationResult.Data.LensRecommendation;
                        hourlyRec.Recommendation = $"Recommended: {genericRecommendationResult.Data.LensRecommendation}";
                    }
                    else
                    {
                        hourlyRec.Recommendation = "No equipment recommendations available";
                    }

                    hourlyRecommendations.Add(hourlyRec);
                }

                return Result<List<HourlyEquipmentRecommendation>>.Success(hourlyRecommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hourly equipment recommendations");
                return Result<List<HourlyEquipmentRecommendation>>.Failure($"Error generating hourly recommendations: {ex.Message}");
            }
        }

        public async Task<Result<GenericEquipmentRecommendation>> GetGenericRecommendationAsync(
            AstroTarget target,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var specs = GetOptimalEquipmentSpecs(target);

                var recommendation = new GenericEquipmentRecommendation
                {
                    Target = target,
                    Specs = specs,
                    LensRecommendation = GenerateGenericLensRecommendation(specs),
                    CameraRecommendation = GenerateGenericCameraRecommendation(specs),
                    ShoppingList = GenerateShoppingList(specs, target)
                };

                return Result<GenericEquipmentRecommendation>.Success(recommendation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating generic recommendation for {Target}", target);
                return Result<GenericEquipmentRecommendation>.Failure($"Error generating generic recommendation: {ex.Message}");
            }
        }

        // Private helper methods
        private async Task<List<Lens>> FindMatchingLensesAsync(
            List<Lens> userLenses,
            ViewModels.OptimalEquipmentSpecs specs,
            CancellationToken cancellationToken)
        {
            var matchingLenses = new List<Lens>();

            foreach (var lens in userLenses)
            {
                if (IsLensMatchingSpecs(lens, specs))
                {
                    matchingLenses.Add(lens);
                }
            }

            return matchingLenses;
        }

        private bool IsLensMatchingSpecs(Lens lens, ViewModels.OptimalEquipmentSpecs specs)
        {
            // Check focal length compatibility
            bool focalLengthMatch = false;

            if (lens.IsPrime)
            {
                // Prime lens: check if focal length is within range
                focalLengthMatch = lens.MinMM >= specs.MinFocalLength && lens.MinMM <= specs.MaxFocalLength;
            }
            else
            {
                // Zoom lens: check if range overlaps with target range
                var lensMaxMM = lens.MaxMM ?? lens.MinMM;
                focalLengthMatch = !(lensMaxMM < specs.MinFocalLength || lens.MinMM > specs.MaxFocalLength);
            }

            // Check aperture capability
            bool apertureMatch = lens.MaxFStop <= specs.MaxAperture;

            return focalLengthMatch && apertureMatch;
        }

        private async Task<List<CameraBody>> GetCompatibleUserCamerasAsync(
            Lens lens,
            List<CameraBody> userCameras,
            CancellationToken cancellationToken)
        {
            try
            {
                var compatibilitiesResult = await _compatibilityRepository.GetByLensIdAsync(lens.Id, cancellationToken);

                if (!compatibilitiesResult.IsSuccess || !compatibilitiesResult.Data.Any())
                {
                    return new List<CameraBody>();
                }

                var compatibleCameraIds = compatibilitiesResult.Data.Select(c => c.CameraBodyId).ToHashSet();
                return userCameras.Where(c => compatibleCameraIds.Contains(c.Id)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatible cameras for lens {LensId}", lens.Id);
                return new List<CameraBody>();
            }
        }

        private async Task<CameraLensCombination> CreateCameraLensCombinationAsync(
            CameraBody camera,
            Lens lens,
            ViewModels.OptimalEquipmentSpecs specs)
        {
            var matchScore = CalculateMatchScore(camera, lens, specs);
            var strengths = IdentifyStrengths(camera, lens, specs);
            var limitations = IdentifyLimitations(camera, lens, specs);

            return new CameraLensCombination
            {
                Camera = camera,
                Lens = lens,
                MatchScore = matchScore,
                IsOptimal = matchScore >= 85,
                RecommendationReason = GenerateRecommendationReason(camera, lens, specs, matchScore),
                Strengths = strengths,
                Limitations = limitations,
                DetailedRecommendation = GenerateDetailedRecommendation(camera, lens, specs, matchScore)
            };
        }

        private double CalculateMatchScore(CameraBody camera, Lens lens, ViewModels.OptimalEquipmentSpecs specs)
        {
            double score = 0;

            // Focal length score (40% of total)
            double focalScore = CalculateFocalLengthScore(lens, specs) * 0.4;

            // Aperture score (30% of total)
            double apertureScore = CalculateApertureScore(lens, specs) * 0.3;

            // Sensor compatibility score (20% of total)
            double sensorScore = CalculateSensorScore(camera, specs) * 0.2;

            // User equipment bonus (10% of total)
            double userBonus = (camera.IsUserCreated ? 0.05 : 0) + (lens.IsUserCreated ? 0.05 : 0);

            score = (focalScore + apertureScore + sensorScore + userBonus) * 100;
            return Math.Min(100, Math.Max(0, score));
        }

        private double CalculateFocalLengthScore(Lens lens, ViewModels.OptimalEquipmentSpecs specs)
        {
            if (lens.IsPrime)
            {
                var distance = Math.Abs(lens.MinMM - specs.OptimalFocalLength);
                var tolerance = (specs.MaxFocalLength - specs.MinFocalLength) / 2;
                return Math.Max(0, 1 - (distance / tolerance));
            }
            else
            {
                var lensMaxMM = lens.MaxMM ?? lens.MinMM;
                var overlap = Math.Min(lensMaxMM, specs.MaxFocalLength) - Math.Max(lens.MinMM, specs.MinFocalLength);
                var targetRange = specs.MaxFocalLength - specs.MinFocalLength;
                return Math.Max(0, overlap / targetRange);
            }
        }

        private double CalculateApertureScore(Lens lens, ViewModels.OptimalEquipmentSpecs specs)
        {
            if (lens.MaxFStop <= specs.MaxAperture)
            {
                return 1.0; // Perfect aperture
            }

            var apertureDeficit = lens.MaxFStop - specs.MaxAperture;
            return Math.Max(0.0, (double)(1 - (apertureDeficit / 2.0))); // Penalty for slower aperture
        }

        private double CalculateSensorScore(CameraBody camera, ViewModels.OptimalEquipmentSpecs specs)
        {
            // Basic sensor score - could be enhanced based on target requirements
            var sensorArea = camera.SensorWidth * camera.SensorHeight;

            // Full frame equivalent area
            var fullFrameArea = 36.0 * 24.0;

            return Math.Min(1.0, sensorArea / fullFrameArea);
        }

        private List<string> IdentifyStrengths(CameraBody camera, Lens lens, ViewModels.OptimalEquipmentSpecs specs)
        {
            var strengths = new List<string>();

            if (lens.MaxFStop <= specs.MaxAperture)
                strengths.Add($"Fast f/{lens.MaxFStop} aperture ideal for {specs.RecommendedSettings}");

            var userFocalLength = lens.IsPrime ? lens.MinMM : (lens.MinMM + (lens.MaxMM ?? lens.MinMM)) / 2;
            if (Math.Abs(userFocalLength - specs.OptimalFocalLength) <= 5)
                strengths.Add($"Perfect {userFocalLength}mm focal length for target");

            if (camera.IsUserCreated)
                strengths.Add("Your personal camera - familiar controls");

            if (lens.IsUserCreated)
                strengths.Add("Your personal lens - familiar handling");

            return strengths;
        }

        private List<string> IdentifyLimitations(CameraBody camera, Lens lens, ViewModels.OptimalEquipmentSpecs specs)
        {
            var limitations = new List<string>();

            if (lens.MaxFStop > specs.MaxAperture)
                limitations.Add($"f/{lens.MaxFStop} aperture slower than ideal f/{specs.MaxAperture}");

            var userFocalLength = lens.IsPrime ? lens.MinMM : (lens.MinMM + (lens.MaxMM ?? lens.MinMM)) / 2;
            if (userFocalLength < specs.MinFocalLength)
                limitations.Add($"{userFocalLength}mm shorter than ideal {specs.MinFocalLength}-{specs.MaxFocalLength}mm range");
            else if (userFocalLength > specs.MaxFocalLength)
                limitations.Add($"{userFocalLength}mm longer than ideal {specs.MinFocalLength}-{specs.MaxFocalLength}mm range");

            return limitations;
        }

        private string GenerateRecommendationReason(CameraBody camera, Lens lens, ViewModels.OptimalEquipmentSpecs specs, double score)
        {
            if (score >= 85)
                return "Excellent match - optimal for this target";
            if (score >= 70)
                return "Very good match - will produce great results";
            if (score >= 50)
                return "Good alternative - workable for this target";

            return "Functional but not ideal for this target";
        }

        private string GenerateDetailedRecommendation(CameraBody camera, Lens lens, ViewModels.OptimalEquipmentSpecs specs, double score)
        {
            var recommendation = $"Use your {camera.Name} with {lens.NameForLens}. ";

            if (score >= 85)
            {
                recommendation += "This combination is ideal for " + GetTargetDescription(specs) + ". ";
            }
            else if (score >= 70)
            {
                recommendation += "This combination will work very well for " + GetTargetDescription(specs) + ". ";
            }
            else
            {
                recommendation += "This combination can work for " + GetTargetDescription(specs) + " with some compromises. ";
            }

            recommendation += specs.RecommendedSettings;

            return recommendation;
        }

        private string GetTargetDescription(ViewModels.OptimalEquipmentSpecs specs)
        {
            return specs.Notes.Split('.').FirstOrDefault() ?? "astrophotography";
        }

        private string GenerateRecommendationSummary(List<CameraLensCombination> combinations, ViewModels.OptimalEquipmentSpecs specs, AstroTarget target)
        {
            if (!combinations.Any())
            {
                return $"No compatible user equipment found for {target}. Consider: {GenerateGenericLensRecommendation(specs)}";
            }

            var bestCombination = combinations.First();
            if (bestCombination.MatchScore >= 85)
            {
                return $"✓ Excellent: {bestCombination.DisplayText} is ideal for {target}";
            }
            if (bestCombination.MatchScore >= 70)
            {
                return $"✓ Very Good: {bestCombination.DisplayText} works very well for {target}";
            }

            return $"⚠ Workable: {bestCombination.DisplayText} can work for {target} with compromises";
        }

        private string GenerateGenericLensRecommendation(ViewModels.OptimalEquipmentSpecs specs)
        {
            var focalLengthDesc = specs.MinFocalLength == specs.MaxFocalLength
                ? $"{specs.OptimalFocalLength}mm"
                : $"{specs.MinFocalLength}-{specs.MaxFocalLength}mm";

            var apertureDesc = specs.MaxAperture <= 2.8 ? "fast" : specs.MaxAperture <= 4.0 ? "moderate" : "standard";

            return $"{focalLengthDesc} f/{specs.MaxAperture} {apertureDesc} lens";
        }

        private string GenerateGenericCameraRecommendation(ViewModels.OptimalEquipmentSpecs specs)
        {
            return "Camera with good high ISO performance (ISO " + specs.MinISO + "-" + specs.MaxISO + ")";
        }

        private List<string> GenerateShoppingList(ViewModels.OptimalEquipmentSpecs specs, AstroTarget target)
        {
            var list = new List<string>
            {
                $"Lens: {GenerateGenericLensRecommendation(specs)}",
                $"Camera: {GenerateGenericCameraRecommendation(specs)}",
                "Sturdy tripod for stability",
                "Remote shutter release or intervalometer"
            };

            if (target == AstroTarget.DeepSkyObjects || target == AstroTarget.StarTrails)
            {
                list.Add("Star tracking mount for long exposures");
            }

            return list;
        }

        private ViewModels.OptimalEquipmentSpecs GetOptimalEquipmentSpecs(AstroTarget target)
        {
            return target switch
            {
                AstroTarget.MilkyWayCore => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    OptimalFocalLength = 24,
                    MaxAperture = 2.8,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/2.8, 20-25 seconds",
                    Notes = "Wide-angle lens essential for capturing galactic arch. Fast aperture critical for light gathering."
                },
                AstroTarget.Moon => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 200,
                    MaxFocalLength = 800,
                    OptimalFocalLength = 400,
                    MaxAperture = 8.0,
                    MinISO = 100,
                    MaxISO = 800,
                    RecommendedSettings = "ISO 200, f/8, 1/125s",
                    Notes = "Telephoto lens for detail. Moon is bright - low ISO and fast shutter prevent overexposure."
                },
                AstroTarget.Planets => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 300,
                    MaxFocalLength = 1000,
                    OptimalFocalLength = 600,
                    MaxAperture = 6.3,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = "ISO 1600, f/5.6, 1/60s",
                    Notes = "Long telephoto essential. Planets are small - maximum focal length recommended."
                },
                AstroTarget.DeepSkyObjects => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 50,
                    MaxFocalLength = 300,
                    OptimalFocalLength = 135,
                    MaxAperture = 4.0,
                    MinISO = 1600,
                    MaxISO = 12800,
                    RecommendedSettings = "ISO 6400, f/4, 4-8 minutes",
                    Notes = "Medium telephoto for framing. Very high ISO capability needed. Tracking mount essential."
                },
                AstroTarget.StarTrails => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 50,
                    OptimalFocalLength = 24,
                    MaxAperture = 4.0,
                    MinISO = 100,
                    MaxISO = 800,
                    RecommendedSettings = "ISO 400, f/4, 30s intervals",
                    Notes = "Wide-angle for interesting compositions. Multiple exposures combined in post-processing."
                },
                AstroTarget.MeteorShowers => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    OptimalFocalLength = 24,
                    MaxAperture = 2.8,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/2.8, 15-30s",
                    Notes = "Wide field to capture meteors. Point 45-60° away from radiant for longer trails."
                },
                AstroTarget.Constellations => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 35,
                    MaxFocalLength = 135,
                    OptimalFocalLength = 85,
                    MaxAperture = 4.0,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = "ISO 1600, f/4, 60s",
                    Notes = "Medium lens for constellation framing. Balance stars with constellation patterns."
                },
                AstroTarget.PolarAlignment => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 50,
                    MaxFocalLength = 200,
                    OptimalFocalLength = 100,
                    MaxAperture = 5.6,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = "ISO 1600, f/5.6, 30s",
                    Notes = "Medium telephoto to see Polaris clearly. Used for mount alignment verification."
                },
                _ => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 24,
                    MaxFocalLength = 200,
                    OptimalFocalLength = 50,
                    MaxAperture = 4.0,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/4, 30s",
                    Notes = "General astrophotography setup."
                }
            };
        }

        Task<Result<UserEquipmentRecommendation>> IEquipmentRecommendationService.GetUserEquipmentRecommendationAsync(AstroTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<Result<List<HourlyEquipmentRecommendation>>> IEquipmentRecommendationService.GetHourlyEquipmentRecommendationsAsync(AstroTarget target, List<DateTime> predictionTimes, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<Result<GenericEquipmentRecommendation>> IEquipmentRecommendationService.GetGenericRecommendationAsync(AstroTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}