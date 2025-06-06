// Location.Core.Infrastructure/Data/Entities/PhoneCameraProfileEntity.cs
using SQLite;

namespace Location.Core.Infrastructure.Data.Entities
{
    [Table("PhoneCameraProfiles")]
    public class PhoneCameraProfileEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string PhoneModel { get; set; } = string.Empty;

        [NotNull]
        public double MainLensFocalLength { get; set; }

        [NotNull]
        public double MainLensFOV { get; set; }

        public double? UltraWideFocalLength { get; set; }

        public double? TelephotoFocalLength { get; set; }

        [NotNull]
        public DateTime DateCalibrated { get; set; }

        [NotNull]
        public bool IsActive { get; set; } = true;

        public PhoneCameraProfileEntity()
        {
            DateCalibrated = DateTime.UtcNow;
        }
    }
}