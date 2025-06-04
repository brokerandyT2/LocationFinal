// Location.Photography.Domain/Entities/PhoneCameraProfile.cs
namespace Location.Photography.Domain.Entities
{
    public class PhoneCameraProfile
    {
        public string PhoneModel { get; private set; }
        public double MainLensFocalLength { get; private set; }
        public double MainLensFOV { get; private set; }
        public double? UltraWideFocalLength { get; private set; }
        public double? TelephotoFocalLength { get; private set; }
        public DateTime DateCalibrated { get; private set; }
        public bool IsActive { get; private set; }

        public int Id { get; set; } // Primary key for ORM

        // Parameterless constructor for ORM
        private PhoneCameraProfile() { }

        public PhoneCameraProfile(
            string phoneModel,
            double mainLensFocalLength,
            double mainLensFOV,
            double? ultraWideFocalLength = null,
            double? telephotoFocalLength = null)
        {
            if (string.IsNullOrWhiteSpace(phoneModel))
                throw new ArgumentException("Phone model cannot be null or empty", nameof(phoneModel));

            if (mainLensFocalLength <= 0)
                throw new ArgumentException("Main lens focal length must be positive", nameof(mainLensFocalLength));

            if (mainLensFOV <= 0 || mainLensFOV >= 180)
                throw new ArgumentException("Main lens FOV must be between 0 and 180 degrees", nameof(mainLensFOV));

            PhoneModel = phoneModel;
            MainLensFocalLength = mainLensFocalLength;
            MainLensFOV = mainLensFOV;
            UltraWideFocalLength = ultraWideFocalLength;
            TelephotoFocalLength = telephotoFocalLength;
            DateCalibrated = DateTime.UtcNow;
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public bool IsCalibrationStale(TimeSpan maxAge)
        {
            return DateTime.UtcNow - DateCalibrated > maxAge;
        }
    }
}