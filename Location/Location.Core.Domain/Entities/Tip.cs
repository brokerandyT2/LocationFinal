using Location.Core.Domain.Common;

namespace Location.Core.Domain.Entities
{
    /// <summary>
    /// Photography tip entity
    /// </summary>
    public class Tip : Entity
    {
        private string _title = string.Empty;
        private string _content = string.Empty;
        private string _fstop = string.Empty;
        private string _shutterSpeed = string.Empty;
        private string _iso = string.Empty;

        public int TipTypeId { get; private set; }

        public string Title
        {
            get => _title;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Title cannot be empty", nameof(value));
                _title = value;
            }
        }

        public string Content
        {
            get => _content;
            private set => _content = value ?? string.Empty;
        }

        public string Fstop
        {
            get => _fstop;
            private set => _fstop = value ?? string.Empty;
        }

        public string ShutterSpeed
        {
            get => _shutterSpeed;
            private set => _shutterSpeed = value ?? string.Empty;
        }

        public string Iso
        {
            get => _iso;
            private set => _iso = value ?? string.Empty;
        }

        public string I8n { get; private set; } = "en-US";

        protected Tip() { } // For ORM

        public Tip(int tipTypeId, string title, string content)
        {
            TipTypeId = tipTypeId;
            Title = title;
            Content = content;
        }

        public void UpdatePhotographySettings(string fstop, string shutterSpeed, string iso)
        {
            Fstop = fstop;
            ShutterSpeed = shutterSpeed;
            Iso = iso;
        }

        public void UpdateContent(string title, string content)
        {
            Title = title;
            Content = content;
        }

        public void SetLocalization(string i8n)
        {
            I8n = i8n ?? "en-US";
        }
    }
}