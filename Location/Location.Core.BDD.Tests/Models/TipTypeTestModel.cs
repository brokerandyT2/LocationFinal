using System;
using System.Collections.Generic;

namespace Location.Core.BDD.Tests.Models
{
    /// <summary>
    /// Model class for tip type data in tests
    /// </summary>
    public class TipTypeTestModel
    {
        /// <summary>
        /// Gets or sets the tip type ID
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the tip type name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the localization code
        /// </summary>
        public string I8n { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets the tips associated with this type
        /// </summary>
        public List<TipTestModel> Tips { get; set; } = new List<TipTestModel>();

        /// <summary>
        /// Creates a domain entity from this test model
        /// </summary>
        public Domain.Entities.TipType ToDomainEntity()
        {
            var tipType = new Domain.Entities.TipType(Name);
            tipType.SetLocalization(I8n);

            // Set ID if provided
            if (Id.HasValue && Id.Value > 0)
            {
                SetPrivateProperty(tipType, "Id", Id.Value);
            }

            // Add tips if any
            foreach (var tip in Tips)
            {
                tipType.AddTip(tip.ToDomainEntity());
            }

            return tipType;
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
        public static TipTypeTestModel FromDomainEntity(Domain.Entities.TipType tipType)
        {
            var model = new TipTypeTestModel
            {
                Id = tipType.Id,
                Name = tipType.Name,
                I8n = tipType.I8n
            };

            // Add tips if any
            foreach (var tip in tipType.Tips)
            {
                model.Tips.Add(TipTestModel.FromDomainEntity(tip));
            }

            return model;
        }

        /// <summary>
        /// Creates a test model from an application DTO
        /// </summary>
        public static TipTypeTestModel FromDto(Application.Tips.DTOs.TipTypeDto dto)
        {
            return new TipTypeTestModel
            {
                Id = dto.Id,
                Name = dto.Name,
                I8n = dto.I8n
            };
        }
    }
}