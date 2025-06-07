using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels
{
    public class UserEquipmentRecommendation
    {
        public AstroTarget Target { get; set; }
        public List<CameraLensCombination> RecommendedCombinations { get; set; } = new();
        public List<CameraLensCombination> AlternativeCombinations { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public bool HasOptimalEquipment { get; set; }
        public OptimalEquipmentSpecs TargetSpecs { get; set; } = new();
    }

    public class CameraLensCombination
    {
        public CameraBody Camera { get; set; } = new();
        public Lens Lens { get; set; } = new();
        public double MatchScore { get; set; } // 0-100 how well this matches target requirements
        public string RecommendationReason { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Limitations { get; set; } = new();
        public bool IsOptimal { get; set; }
        public string DisplayText => $"Use {Camera.Name} with {Lens.NameForLens}";
        public string DetailedRecommendation { get; set; } = string.Empty;
    }

    public class HourlyEquipmentRecommendation
    {
        public DateTime PredictionTime { get; set; }
        public AstroTarget Target { get; set; }
        public CameraLensCombination? RecommendedCombination { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public bool HasUserEquipment { get; set; }
        public string GenericRecommendation { get; set; } = string.Empty;
    }

    public class GenericEquipmentRecommendation
    {
        public AstroTarget Target { get; set; }
        public string LensRecommendation { get; set; } = string.Empty;
        public string CameraRecommendation { get; set; } = string.Empty;
        public OptimalEquipmentSpecs Specs { get; set; } = new();
        public List<string> ShoppingList { get; set; } = new();
    }

    public class OptimalEquipmentSpecs
    {
        public double MinFocalLength { get; set; }
        public double MaxFocalLength { get; set; }
        public double OptimalFocalLength { get; set; }
        public double MaxAperture { get; set; }
        public int MinISO { get; set; }
        public int MaxISO { get; set; }
        public string RecommendedSettings { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
