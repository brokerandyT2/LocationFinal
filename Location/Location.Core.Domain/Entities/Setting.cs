using System;
using Location.Core.Domain.Common;

namespace Location.Core.Domain.Entities
{
    /// <summary>
    /// User setting entity
    /// </summary>
    public class Setting : Entity
    {
        private string _key = string.Empty;
        private string _value = string.Empty;

        public string Key
        {
            get => _key;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Key cannot be empty", nameof(value));
                _key = value;
            }
        }

        public string Value
        {
            get => _value;
            private set => _value = value ?? string.Empty;
        }

        public string Description { get; private set; } = string.Empty;
        public DateTime Timestamp { get; private set; }

        protected Setting() { } // For ORM

        public Setting(string key, string value, string description = "")
        {
            Key = key;
            Value = value;
            Description = description;
            Timestamp = DateTime.UtcNow;
        }

        public void UpdateValue(string value)
        {
            Value = value;
            Timestamp = DateTime.UtcNow;
        }

        public bool GetBooleanValue()
        {
            return bool.TryParse(Value, out var result) && result;
        }

        public int GetIntValue(int defaultValue = 0)
        {
            return int.TryParse(Value, out var result) ? result : defaultValue;
        }

        public DateTime? GetDateTimeValue()
        {
            return DateTime.TryParse(Value, out var result) ? result : null;
        }
    }
}