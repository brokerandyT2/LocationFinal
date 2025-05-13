using System;
using System.Collections.Generic;
using Location.Core.Domain.Common;

namespace Location.Core.Domain.Entities
{
    /// <summary>
    /// Tip category entity
    /// </summary>
    public class TipType : Entity
    {
        private string _name = string.Empty;
        private readonly List<Tip> _tips = new();

        public string Name
        {
            get => _name;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name cannot be empty", nameof(value));
                _name = value;
            }
        }

        public string I8n { get; private set; } = "en-US";
        public IReadOnlyCollection<Tip> Tips => _tips.AsReadOnly();

        protected TipType() { } // For ORM

        public TipType(string name)
        {
            Name = name;
        }

        public void SetLocalization(string i8n)
        {
            I8n = i8n ?? "en-US";
        }

        public void AddTip(Tip tip)
        {
            if (tip == null)
                throw new ArgumentNullException(nameof(tip));

            if (tip.TipTypeId != Id && Id > 0)
                throw new InvalidOperationException("Tip type ID mismatch");

            _tips.Add(tip);
        }

        public void RemoveTip(Tip tip)
        {
            _tips.Remove(tip);
        }
    }
}