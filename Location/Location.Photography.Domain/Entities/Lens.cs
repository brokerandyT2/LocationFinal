using SQLite;
using System;

namespace Location.Photography.Domain.Entities
{
    [Table("Lenses")]
    public class Lens
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public double MinMM { get; set; }

        public double? MaxMM { get; set; }

        public double? MinFStop { get; set; }

        public double? MaxFStop { get; set; }

        public bool IsPrime { get; set; }

        public bool IsUserCreated { get; set; }

        public DateTime DateAdded { get; set; }

        public string NameForLens { get; set; }

        public Lens()
        {
            DateAdded = DateTime.UtcNow;
        }

        public Lens(
            double minMM,
            double? maxMM = null,
            double? minFStop = null,
            double? maxFStop = null,
            bool isUserCreated = false)
        {
            if (minMM <= 0)
                throw new ArgumentException("Minimum focal length must be positive", nameof(minMM));
            if (maxMM.HasValue && maxMM.Value <= minMM)
                throw new ArgumentException("Maximum focal length must be greater than minimum", nameof(maxMM));
            if (minFStop.HasValue && minFStop.Value <= 0)
                throw new ArgumentException("Minimum f-stop must be positive", nameof(minFStop));
            if (maxFStop.HasValue && maxFStop.Value <= 0)
                throw new ArgumentException("Maximum f-stop must be positive", nameof(maxFStop));
            if (minFStop.HasValue && maxFStop.HasValue && maxFStop.Value < minFStop.Value)
                throw new ArgumentException("Maximum f-stop must be greater than or equal to minimum f-stop", nameof(maxFStop));

            MinMM = minMM;
            MaxMM = maxMM;
            MinFStop = minFStop;
            MaxFStop = maxFStop;
            IsPrime = !maxMM.HasValue || Math.Abs(maxMM.Value - minMM) < 0.1;
            IsUserCreated = isUserCreated;
            DateAdded = DateTime.UtcNow;
        }

        public void UpdateDetails(
            double minMM,
            double? maxMM = null,
            double? minFStop = null,
            double? maxFStop = null)
        {
            if (minMM <= 0)
                throw new ArgumentException("Minimum focal length must be positive", nameof(minMM));
            if (maxMM.HasValue && maxMM.Value <= minMM)
                throw new ArgumentException("Maximum focal length must be greater than minimum", nameof(maxMM));
            if (minFStop.HasValue && minFStop.Value <= 0)
                throw new ArgumentException("Minimum f-stop must be positive", nameof(minFStop));
            if (maxFStop.HasValue && maxFStop.Value <= 0)
                throw new ArgumentException("Maximum f-stop must be positive", nameof(maxFStop));
            if (minFStop.HasValue && maxFStop.HasValue && maxFStop.Value < minFStop.Value)
                throw new ArgumentException("Maximum f-stop must be greater than or equal to minimum f-stop", nameof(maxFStop));

            MinMM = minMM;
            MaxMM = maxMM;
            MinFStop = minFStop;
            MaxFStop = maxFStop;
            IsPrime = !maxMM.HasValue || Math.Abs(maxMM.Value - minMM) < 0.1;
        }

        public string GetDisplayName()
        {
            var displayName = IsPrime ? $"{MinMM}mm {GetApertureDisplay()}".Trim() : $"{MinMM}-{MaxMM}mm {GetApertureDisplay()}".Trim();
            return IsUserCreated ? $"{displayName}*" : displayName;
        }

        private string GetApertureDisplay()
        {
            if (!MinFStop.HasValue) return string.Empty;

            if (!MaxFStop.HasValue || Math.Abs(MaxFStop.Value - MinFStop.Value) < 0.1)
            {
                return $"f/{MinFStop.Value:F1}";
            }
            else
            {
                return $"f/{MinFStop.Value:F1}-{MaxFStop.Value:F1}";
            }
        }
    }
}