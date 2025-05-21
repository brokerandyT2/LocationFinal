// SettingTestModel.cs
using System;

namespace Location.Core.BDD.Tests.Models
{
    /// <summary>
    /// Model class for setting data in tests
    /// </summary>
    public class SettingTestModel
    {
        /// <summary>
        /// Gets or sets the setting ID
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the setting key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the setting value
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the setting description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the setting timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a domain entity from this test model
        /// </summary>
        public Domain.Entities.Setting ToDomainEntity()
        {
            var setting = new Domain.Entities.Setting(Key, Value, Description);

            // Set ID if provided
            if (Id.HasValue && Id.Value > 0)
            {
                SetPrivateProperty(setting, "Id", Id.Value);
            }

            // Set timestamp
            SetPrivateProperty(setting, "Timestamp", Timestamp);

            return setting;
        }

        /// <summary>
        /// Sets a private property on an object using reflection
        /// </summary>
        private static void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
        }

        /// <summary>
        /// Creates a test model from a domain entity
        /// </summary>
        public static SettingTestModel FromDomainEntity(Domain.Entities.Setting setting)
        {
            return new SettingTestModel
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Timestamp = setting.Timestamp
            };
        }

        /// <summary>
        /// Creates a test model from a DTO
        /// </summary>
        public static SettingTestModel FromDto(Application.Settings.DTOs.SettingDto dto)
        {
            return new SettingTestModel
            {
                Id = dto.Id,
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description,
                Timestamp = dto.Timestamp
            };
        }
    }
}