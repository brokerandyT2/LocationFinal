using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Enums;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for application integration scenarios
    /// </summary>
    public class ApplicationIntegrationTestModel
    {
        public int? Id { get; set; }
        public string UserId { get; set; } = "user123";
        public DateTime SessionStartTime { get; set; } = DateTime.UtcNow;
        public DateTime TargetDate { get; set; } = DateTime.Today;

        // Location data
        public double Latitude { get; set; } = 40.7128;
        public double Longitude { get; set; } = -74.0060;
        public string LocationName { get; set; } = "Test Location";
        public int LocationId { get; set; } = 1;

        // Equipment data
        public CameraBodyTestModel CameraBody { get; set; } = new();
        public LensTestModel Lens { get; set; } = new();
        public List<UserCameraBodyTestModel> UserCameraBodies { get; set; } = new();
        public bool EquipmentCompatible { get; set; } = true;

        // Exposure data
        public ExposureTestModel BaseExposure { get; set; } = new();
        public ExposureTestModel CalculatedExposure { get; set; } = new();
        public bool ExposureCalculationSuccessful { get; set; }

        // Sun calculation data
        public SunCalculationTestModel SunData { get; set; } = new();
        public EnhancedSunTimes SunTimes { get; set; } = new();
        public List<OptimalShootingTime> OptimalTimes { get; set; } = new();

        // Weather data
        public WeatherConditions WeatherConditions { get; set; } = new();
        public WeatherImpactAnalysis WeatherImpact { get; set; } = new();
        public bool WeatherDataAvailable { get; set; } = true;

        // Light prediction data
        public PredictiveLightTestModel LightPrediction { get; set; } = new();
        public List<HourlyLightPrediction> HourlyPredictions { get; set; } = new();
        public bool LightPredictionSuccessful { get; set; }

        // Alert data
        public List<ShootingAlertTestModel> Alerts { get; set; } = new();
        public bool AlertsGenerated { get; set; }

        // Image analysis data
        public List<SceneEvaluationTestModel> CapturedImages { get; set; } = new();
        public bool ImageAnalysisCompleted { get; set; }

        // Performance metrics
        public TimeSpan TotalProcessingTime { get; set; }
        public int MemoryUsageMB { get; set; }
        public Dictionary<string, TimeSpan> FeatureProcessingTimes { get; set; } = new();

        // Error handling and validation
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();

        // Workflow state
        public WorkflowState CurrentState { get; set; } = WorkflowState.NotStarted;
        public List<WorkflowStep> CompletedSteps { get; set; } = new();
        public WorkflowStep? CurrentStep { get; set; }
        public bool WorkflowCompleted { get; set; }

        // Integration results
        public bool DataConsistencyValid { get; set; } = true;
        public double OverallSuccessRate { get; set; } = 1.0;
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// Validates data consistency across all features
        /// </summary>
        public bool ValidateDataConsistency(out List<string> inconsistencies)
        {
            inconsistencies = new List<string>();

            // Location consistency
            if (Math.Abs(SunData.Latitude - Latitude) > 0.001)
                inconsistencies.Add("Sun calculation latitude doesn't match session latitude");

            if (Math.Abs(SunData.Longitude - Longitude) > 0.001)
                inconsistencies.Add("Sun calculation longitude doesn't match session longitude");

            // Date consistency
            if (SunData.Date.Date != TargetDate.Date)
                inconsistencies.Add("Sun calculation date doesn't match target date");

            // Equipment consistency
            if (EquipmentCompatible && CameraBody.MountType != Lens.MountType)
            {
                // Check for adapter compatibility
                if (!IsAdapterCompatible(CameraBody.MountType, Lens.MountType))
                    inconsistencies.Add("Camera and lens mount types are incompatible");
            }

            // Exposure consistency
            if (ExposureCalculationSuccessful && BaseExposure.IsValid)
            {
                // Check if calculated exposure is within reasonable bounds
                if (!IsExposureReasonable())
                    inconsistencies.Add("Calculated exposure settings are outside reasonable bounds");
            }

            DataConsistencyValid = inconsistencies.Count == 0;
            return DataConsistencyValid;
        }

        /// <summary>
        /// Calculates overall workflow success rate
        /// </summary>
        public void CalculateSuccessRate()
        {
            var totalSteps = Enum.GetValues<WorkflowStep>().Length;
            var successfulSteps = CompletedSteps.Count;
            OverallSuccessRate = (double)successfulSteps / totalSteps;
        }

        /// <summary>
        /// Starts a new workflow step
        /// </summary>
        public void StartWorkflowStep(WorkflowStep step)
        {
            CurrentStep = step;
            CurrentState = WorkflowState.InProgress;

            if (!FeatureProcessingTimes.ContainsKey(step.ToString()))
                FeatureProcessingTimes[step.ToString()] = TimeSpan.Zero;
        }

        /// <summary>
        /// Completes a workflow step
        /// </summary>
        public void CompleteWorkflowStep(WorkflowStep step, bool successful = true)
        {
            if (successful && !CompletedSteps.Contains(step))
                CompletedSteps.Add(step);

            CurrentStep = null;

            // Check if all steps completed
            var allSteps = Enum.GetValues<WorkflowStep>();
            WorkflowCompleted = allSteps.All(s => CompletedSteps.Contains(s));

            if (WorkflowCompleted)
                CurrentState = WorkflowState.Completed;
        }

        /// <summary>
        /// Adds an error to the workflow
        /// </summary>
        public void AddError(string error, WorkflowStep? step = null)
        {
            var errorMessage = step.HasValue ? $"[{step}] {error}" : error;
            Errors.Add(errorMessage);

            if (step.HasValue)
                CurrentState = WorkflowState.Failed;
        }

        /// <summary>
        /// Adds a warning to the workflow
        /// </summary>
        public void AddWarning(string warning, WorkflowStep? step = null)
        {
            var warningMessage = step.HasValue ? $"[{step}] {warning}" : warning;
            Warnings.Add(warningMessage);
        }

        /// <summary>
        /// Generates integration recommendations
        /// </summary>
        public void GenerateRecommendations()
        {
            Recommendations.Clear();

            // Equipment recommendations
            if (!EquipmentCompatible)
                Recommendations.Add("Consider using compatible equipment or adapters");

            // Weather-based recommendations
            if (WeatherConditions.CloudCover > 70)
                Recommendations.Add("Overcast conditions - consider indoor photography or diffused lighting");

            if (WeatherConditions.Precipitation > 0)
                Recommendations.Add("Protect equipment from moisture");

            // Light prediction recommendations
            if (LightPredictionSuccessful && HourlyPredictions.Any())
            {
                var bestHour = HourlyPredictions.OrderByDescending(h => h.ConfidenceLevel).First();
                Recommendations.Add($"Best shooting time: {bestHour.DateTime:HH:mm}");
            }

            // Performance recommendations
            if (TotalProcessingTime.TotalSeconds > 30)
                Recommendations.Add("Consider optimizing workflow for better performance");

            // Error-based recommendations
            if (HasErrors)
                Recommendations.Add("Review and resolve errors before proceeding");
        }

        /// <summary>
        /// Exports session summary
        /// </summary>
        public SessionSummary ExportSessionSummary()
        {
            return new SessionSummary
            {
                SessionId = Id ?? 0,
                UserId = UserId,
                StartTime = SessionStartTime,
                EndTime = DateTime.UtcNow,
                LocationName = LocationName,
                TotalProcessingTime = TotalProcessingTime,
                SuccessRate = OverallSuccessRate,
                StepsCompleted = CompletedSteps.Count,
                TotalSteps = Enum.GetValues<WorkflowStep>().Length,
                ErrorCount = Errors.Count,
                WarningCount = Warnings.Count,
                Recommendations = Recommendations,
                DataConsistency = DataConsistencyValid
            };
        }

        private bool IsAdapterCompatible(MountType cameraMount, MountType lensMount)
        {
            return (cameraMount, lensMount) switch
            {
                (MountType.CanonRF, MountType.CanonEF) => true,
                (MountType.CanonRF, MountType.CanonEFS) => true,
                (MountType.NikonZ, MountType.NikonF) => true,
                (MountType.SonyFE, MountType.SonyE) => true,
                _ => false
            };
        }

        private bool IsExposureReasonable()
        {
            // Basic sanity checks for exposure settings
            if (string.IsNullOrEmpty(CalculatedExposure.ResultShutterSpeed) ||
                string.IsNullOrEmpty(CalculatedExposure.ResultAperture) ||
                string.IsNullOrEmpty(CalculatedExposure.ResultIso))
                return false;

            // Check if ISO is within camera capabilities
            if (CameraBody.IsValidSpecifications)
            {
                // This would need proper ISO parsing and validation
                return true; // Simplified for now
            }

            return true;
        }

        /// <summary>
        /// Creates a test model with complete valid workflow
        /// </summary>
        public static ApplicationIntegrationTestModel CreateValidWorkflow()
        {
            var model = new ApplicationIntegrationTestModel
            {
                Id = 1,
                UserId = "user123",
                TargetDate = DateTime.Today,
                Latitude = 40.7128,
                Longitude = -74.0060,
                LocationName = "Central Park, NYC",
                LocationId = 1,
                CameraBody = CameraBodyTestModel.CreateValid(),
                Lens = LensTestModel.CreateValid(),
                BaseExposure = ExposureTestModel.CreateValid(),
                SunData = SunCalculationTestModel.CreateValid(),
                WeatherConditions = new WeatherConditions
                {
                    CloudCover = 30,
                    Precipitation = 0,
                    Humidity = 65,
                    Visibility = 15,
                    WindSpeed = 8,
                    Description = "Partly cloudy"
                },
                LightPrediction = PredictiveLightTestModel.CreateValid(),
                CurrentState = WorkflowState.NotStarted
            };

            // Set up compatible equipment
            model.Lens.MountType = model.CameraBody.MountType;
            model.EquipmentCompatible = true;

            return model;
        }

        /// <summary>
        /// Creates a test model with integration errors
        /// </summary>
        public static ApplicationIntegrationTestModel CreateWithErrors()
        {
            var model = CreateValidWorkflow();

            // Introduce incompatibilities
            model.Lens.MountType = MountType.NikonF; // Incompatible with Canon RF
            model.EquipmentCompatible = false;

            // Add some errors
            model.AddError("Equipment compatibility check failed", WorkflowStep.EquipmentValidation);
            model.AddError("Weather service unavailable", WorkflowStep.WeatherAnalysis);

            model.CurrentState = WorkflowState.Failed;

            return model;
        }
    }

    public enum WorkflowState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    public enum WorkflowStep
    {
        LocationSetup,
        EquipmentValidation,
        SunCalculation,
        WeatherAnalysis,
        LightPrediction,
        ExposureCalculation,
        AlertGeneration,
        ImageAnalysis,
        DataConsistencyCheck,
        RecommendationGeneration
    }

    public class SessionSummary
    {
        public int SessionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public TimeSpan TotalProcessingTime { get; set; }
        public double SuccessRate { get; set; }
        public int StepsCompleted { get; set; }
        public int TotalSteps { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public bool DataConsistency { get; set; }
    }
}