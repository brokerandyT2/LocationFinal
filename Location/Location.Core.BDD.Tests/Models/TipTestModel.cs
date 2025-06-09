// TipTestModel.cs
namespace Location.Core.BDD.Tests.Models
{
    /// <summary>
    /// Model class for tip data in tests
    /// </summary>
    public class TipTestModel
    {
        /// <summary>
        /// Gets or sets the tip ID
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the tip type ID
        /// </summary>
        public int TipTypeId { get; set; }

        /// <summary>
        /// Gets or sets the tip title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tip content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the f-stop setting
        /// </summary>
        public string Fstop { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the shutter speed setting
        /// </summary>
        public string ShutterSpeed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ISO setting
        /// </summary>
        public string Iso { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the localization code
        /// </summary>
        public string I8n { get; set; } = "en-US";

        /// <summary>
        /// Creates a domain entity from this test model
        /// </summary>
        public Domain.Entities.Tip ToDomainEntity()
        {
            var tip = new Domain.Entities.Tip(TipTypeId, Title, Content);

            // Set additional properties
            tip.UpdatePhotographySettings(Fstop, ShutterSpeed, Iso);
            tip.SetLocalization(I8n);

            // Set ID if provided
            if (Id.HasValue && Id.Value > 0)
            {
                SetPrivateProperty(tip, "Id", Id.Value);
            }

            return tip;
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
        public static TipTestModel FromDomainEntity(Domain.Entities.Tip tip)
        {
            return new TipTestModel
            {
                Id = tip.Id,
                TipTypeId = tip.TipTypeId,
                Title = tip.Title,
                Content = tip.Content,
                Fstop = tip.Fstop,
                ShutterSpeed = tip.ShutterSpeed,
                Iso = tip.Iso,
                I8n = tip.I8n
            };
        }

        /// <summary>
        /// Creates a test model from an application DTO
        /// </summary>
        public static TipTestModel FromDto(Application.Tips.DTOs.TipDto dto)
        {
            return new TipTestModel
            {
                Id = dto.Id,
                TipTypeId = dto.TipTypeId,
                Title = dto.Title,
                Content = dto.Content,
                Fstop = dto.Fstop,
                ShutterSpeed = dto.ShutterSpeed,
                Iso = dto.Iso,
                I8n = dto.I8n
            };
        }
    }
}