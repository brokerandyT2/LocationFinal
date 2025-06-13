// Location.Photography.Infrastructure/Services/EquipmentRecommendationService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.Infrastructure.Resources;
using Location.Photography.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                    return Result<UserEquipmentRecommendation>.Failure(AppResources.Equipment_Error_FailedToLoadUserEquipment);
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
                return Result<UserEquipmentRecommendation>.Failure(string.Format(AppResources.Equipment_Error_GeneratingRecommendation, ex.Message));
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
                        hourlyRec.Recommendation = string.Format(AppResources.Equipment_Recommended, genericRecommendationResult.Data.LensRecommendation);
                    }
                    else
                    {
                        hourlyRec.Recommendation = AppResources.Equipment_NoRecommendationsAvailable;
                    }

                    hourlyRecommendations.Add(hourlyRec);
                }

                return Result<List<HourlyEquipmentRecommendation>>.Success(hourlyRecommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hourly equipment recommendations");
                return Result<List<HourlyEquipmentRecommendation>>.Failure(string.Format(AppResources.Equipment_Error_GeneratingHourlyRecommendations, ex.Message));
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
                return Result<GenericEquipmentRecommendation>.Failure(string.Format(AppResources.Equipment_Error_GeneratingGenericRecommendation, ex.Message));
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
                RecommendationReason =  GenerateRecommendationText(camera, lens, specs, matchScore),
                Strengths = strengths,
                Limitations = limitations,
                DetailedRecommendation = GenerateRecommendationText(camera, lens, specs, matchScore)
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
                strengths.Add(string.Format(AppResources.Equipment_Strength_FastAperture, lens.MaxFStop, specs.RecommendedSettings));

            var userFocalLength = lens.IsPrime ? lens.MinMM : (lens.MinMM + (lens.MaxMM ?? lens.MinMM)) / 2;
            if (userFocalLength >= specs.MinFocalLength && userFocalLength <= specs.MaxFocalLength)
                strengths.Add(string.Format(AppResources.Equipment_Strength_OptimalFocalLength, userFocalLength));

            if (camera.SensorWidth * camera.SensorHeight >= 800) // Rough full-frame equivalent
                strengths.Add(AppResources.Equipment_Strength_LargeSensor);

            return strengths;
        }

        private List<string> IdentifyLimitations(CameraBody camera, Lens lens, ViewModels.OptimalEquipmentSpecs specs)
        {
            var limitations = new List<string>();

            if (lens.MaxFStop > specs.MaxAperture)
                limitations.Add(string.Format(AppResources.Equipment_Limitation_SlowerAperture, lens.MaxFStop, specs.MaxAperture));

            var userFocalLength = lens.IsPrime ? lens.MinMM : (lens.MinMM + (lens.MaxMM ?? lens.MinMM)) / 2;
            if (userFocalLength < specs.MinFocalLength)
                limitations.Add(string.Format(AppResources.Equipment_Limitation_TooWide, userFocalLength, specs.MinFocalLength));
            else if (userFocalLength > specs.MaxFocalLength)
                limitations.Add(string.Format(AppResources.Equipment_Limitation_TooTelephoto, userFocalLength, specs.MaxFocalLength));

            return limitations;
        }

        private string GenerateRecommendationText(CameraBody camera, Lens lens, ViewModels.OptimalEquipmentSpecs specs, double matchScore)
        {
            string recommendation;

            if (matchScore >= 85)
            {
                recommendation = AppResources.Equipment_RecommendationText_Excellent;
            }
            else if (matchScore >= 70)
            {
                recommendation = AppResources.Equipment_RecommendationText_VeryGood;
            }
            else
            {
                recommendation = string.Format(AppResources.Equipment_CombinationWorksWith, GetTargetDescription(specs));
            }

            recommendation += specs.RecommendedSettings;

            return recommendation;
        }

        private string GetTargetDescription(ViewModels.OptimalEquipmentSpecs specs)
        {
            return specs.Notes.Split('.').FirstOrDefault() ?? AppResources.Equipment_DefaultTargetDescription;
        }

        private string GenerateRecommendationSummary(List<CameraLensCombination> combinations, ViewModels.OptimalEquipmentSpecs specs, AstroTarget target)
        {
            if (!combinations.Any())
            {
                return string.Format(AppResources.Equipment_NoCompatibleEquipmentFound, target, GenerateGenericLensRecommendation(specs));
            }

            var bestCombination = combinations.First();
            if (bestCombination.MatchScore >= 85)
            {
                return string.Format(AppResources.Equipment_ExcellentMatch, bestCombination.DisplayText, target);
            }
            if (bestCombination.MatchScore >= 70)
            {
                return string.Format(AppResources.Equipment_VeryGoodMatch, bestCombination.DisplayText, target);
            }

            return string.Format(AppResources.Equipment_WorkableMatch, bestCombination.DisplayText, target);
        }

        private string GenerateGenericLensRecommendation(ViewModels.OptimalEquipmentSpecs specs)
        {
            var focalLengthDesc = specs.MinFocalLength == specs.MaxFocalLength
                ? $"{specs.OptimalFocalLength}mm"
                : $"{specs.MinFocalLength}-{specs.MaxFocalLength}mm";

            var apertureDesc = specs.MaxAperture <= 2.8 ? AppResources.Equipment_ApertureDescription_Fast :
                              specs.MaxAperture <= 4.0 ? AppResources.Equipment_ApertureDescription_Moderate :
                              AppResources.Equipment_ApertureDescription_Standard;

            return string.Format(AppResources.Equipment_LensRecommendationFormat, focalLengthDesc, specs.MaxAperture, apertureDesc);
        }

        private string GenerateGenericCameraRecommendation(ViewModels.OptimalEquipmentSpecs specs)
        {
            return string.Format(AppResources.Equipment_CameraRecommendationFormat, specs.MinISO, specs.MaxISO);
        }

        private List<string> GenerateShoppingList(ViewModels.OptimalEquipmentSpecs specs, AstroTarget target)
        {
            var list = new List<string>
            {
                string.Format(AppResources.Equipment_ShoppingList_Lens, GenerateGenericLensRecommendation(specs)),
                string.Format(AppResources.Equipment_ShoppingList_Camera, GenerateGenericCameraRecommendation(specs)),
                AppResources.Equipment_ShoppingList_Tripod,
                AppResources.Equipment_ShoppingList_RemoteShutter
            };

            if (target == AstroTarget.DeepSkyObjects || target == AstroTarget.StarTrails)
            {
                list.Add(AppResources.Equipment_ShoppingList_StarTracker);
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
                    RecommendedSettings = AppResources.Equipment_Settings_MilkyWay,
                    Notes = AppResources.Equipment_Notes_MilkyWay
                },
                AstroTarget.Moon => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 200,
                    MaxFocalLength = 800,
                    OptimalFocalLength = 400,
                    MaxAperture = 8.0,
                    MinISO = 100,
                    MaxISO = 800,
                    RecommendedSettings = AppResources.Equipment_Settings_Moon,
                    Notes = AppResources.Equipment_Notes_Moon
                },
                AstroTarget.Planets => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 300,
                    MaxFocalLength = 1000,
                    OptimalFocalLength = 600,
                    MaxAperture = 6.3,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = AppResources.Equipment_Settings_Planets,
                    Notes = AppResources.Equipment_Notes_Planets
                },
                AstroTarget.DeepSkyObjects => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 50,
                    MaxFocalLength = 300,
                    OptimalFocalLength = 135,
                    MaxAperture = 4.0,
                    MinISO = 800,
                    MaxISO = 6400,
                    RecommendedSettings = AppResources.Equipment_Settings_DeepSky,
                    Notes = AppResources.Equipment_Notes_DeepSky
                },
                AstroTarget.StarTrails => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 50,
                    OptimalFocalLength = 24,
                    MaxAperture = 4.0,
                    MinISO = 100,
                    MaxISO = 800,
                    RecommendedSettings = AppResources.Equipment_Settings_StarTrails,
                    Notes = AppResources.Equipment_Notes_StarTrails
                },
                _ => new ViewModels.OptimalEquipmentSpecs
                {
                    MinFocalLength = 24,
                    MaxFocalLength = 70,
                    OptimalFocalLength = 50,
                    MaxAperture = 2.8,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = AppResources.Equipment_Settings_General,
                    Notes = AppResources.Equipment_Notes_General
                }
            };
        }
    }
}