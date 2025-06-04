using Location.Photography.Domain.Enums;
using SQLite;
using System;

namespace Location.Photography.Domain.Entities
{
    [Table("CameraBodies")]
    public class CameraBody
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), NotNull]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50), NotNull]
        public string SensorType { get; set; } = string.Empty;

        [NotNull]
        public double SensorWidth { get; set; }

        [NotNull]
        public double SensorHeight { get; set; }

        [NotNull]
        public MountType MountType { get; set; }

        public bool IsUserCreated { get; set; }

        public DateTime DateAdded { get; set; }

        public CameraBody()
        {
            DateAdded = DateTime.UtcNow;
        }

        public CameraBody(
            string name,
            string sensorType,
            double sensorWidth,
            double sensorHeight,
            MountType mountType,
            bool isUserCreated = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Camera name cannot be null or empty", nameof(name));
            if (string.IsNullOrWhiteSpace(sensorType))
                throw new ArgumentException("Sensor type cannot be null or empty", nameof(sensorType));
            if (sensorWidth <= 0)
                throw new ArgumentException("Sensor width must be positive", nameof(sensorWidth));
            if (sensorHeight <= 0)
                throw new ArgumentException("Sensor height must be positive", nameof(sensorHeight));

            Name = name.Trim();
            SensorType = sensorType.Trim();
            SensorWidth = sensorWidth;
            SensorHeight = sensorHeight;
            MountType = mountType;
            IsUserCreated = isUserCreated;
            DateAdded = DateTime.UtcNow;
        }

        public void UpdateDetails(
            string name,
            string sensorType,
            double sensorWidth,
            double sensorHeight,
            MountType mountType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Camera name cannot be null or empty", nameof(name));
            if (string.IsNullOrWhiteSpace(sensorType))
                throw new ArgumentException("Sensor type cannot be null or empty", nameof(sensorType));
            if (sensorWidth <= 0)
                throw new ArgumentException("Sensor width must be positive", nameof(sensorWidth));
            if (sensorHeight <= 0)
                throw new ArgumentException("Sensor height must be positive", nameof(sensorHeight));

            Name = name.Trim();
            SensorType = sensorType.Trim();
            SensorWidth = sensorWidth;
            SensorHeight = sensorHeight;
            MountType = mountType;
        }

        public string GetDisplayName()
        {
            return IsUserCreated ? $"{Name}*" : Name;
        }
    }
}