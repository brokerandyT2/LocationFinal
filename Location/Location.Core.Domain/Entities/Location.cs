using System;
using Location.Core.Domain.Common;
using Location.Core.Domain.Events;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Entities
{
    /// <summary>
    /// Location aggregate root
    /// </summary>
    public class Location : AggregateRoot
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private Coordinate _coordinate = null!;
        private Address _address = null!;
        private string? _photoPath;
        private bool _isDeleted;
        private DateTime _timestamp;
        private int _id;
        public int Id
        {
            get => _id;
            private set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Id must be greater than zero");
                _id = value;
            }
        }
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

        public string Description
        {
            get => _description;
            private set => _description = value ?? string.Empty;
        }

        public Coordinate Coordinate
        {
            get => _coordinate;
            private set => _coordinate = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Address Address
        {
            get => _address;
            private set => _address = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string? PhotoPath
        {
            get => _photoPath;
            private set => _photoPath = value;
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            private set => _isDeleted = value;
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            private set => _timestamp = value;
        }

        protected Location() { } // For ORM

        public Location(string title, string description, Coordinate coordinate, Address address)
        {
            Title = title;
            Description = description;
            Coordinate = coordinate;
            Address = address;
            Timestamp = DateTime.UtcNow;

            AddDomainEvent(new LocationSavedEvent(this));
        }

        public void UpdateDetails(string title, string description)
        {
            Title = title;
            Description = description;
            AddDomainEvent(new LocationSavedEvent(this));
        }

        public void UpdateCoordinate(Coordinate coordinate)
        {
            Coordinate = coordinate;
            AddDomainEvent(new LocationSavedEvent(this));
        }

        public void AttachPhoto(string photoPath)
        {
            if (string.IsNullOrWhiteSpace(photoPath))
                throw new ArgumentException("Photo path cannot be empty", nameof(photoPath));

            PhotoPath = photoPath;
            AddDomainEvent(new PhotoAttachedEvent(Id, photoPath));
        }

        public void RemovePhoto()
        {
            PhotoPath = null;
        }

        public void Delete()
        {
            IsDeleted = true;
            AddDomainEvent(new LocationDeletedEvent(Id));
        }

        public void Restore()
        {
            IsDeleted = false;
        }
    }
}
