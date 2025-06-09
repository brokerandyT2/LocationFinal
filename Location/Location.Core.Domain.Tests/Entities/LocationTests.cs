using FluentAssertions;
using Location.Core.Domain.Events;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Entities
{
    [TestFixture]
    public class LocationTests
    {
        private Coordinate _validCoordinate;
        private Address _validAddress;

        [SetUp]
        public void Setup()
        {
            _validCoordinate = new Coordinate(47.6062, -122.3321);
            _validAddress = new Address("Seattle", "WA");
        }

        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange & Act
            var location = new Location.Core.Domain.Entities.Location("Space Needle", "Iconic Seattle landmark", _validCoordinate, _validAddress);

            // Assert
            location.Title.Should().Be("Space Needle");
            location.Description.Should().Be("Iconic Seattle landmark");
            location.Coordinate.Should().Be(_validCoordinate);
            location.Address.Should().Be(_validAddress);
            location.IsDeleted.Should().BeFalse();
            location.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_ShouldAddLocationSavedEvent()
        {
            // Arrange & Act
            var location = new Location.Core.Domain.Entities.Location("Space Needle", "Iconic Seattle landmark", _validCoordinate, _validAddress);

            // Assert
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.Should().ContainItemsAssignableTo<LocationSavedEvent>();
            var domainEvent = location.DomainEvents.First() as LocationSavedEvent;
            domainEvent?.Location.Should().Be(location);
        }

        [Test]
        public void Constructor_WithEmptyTitle_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Location.Core.Domain.Entities.Location("", "Description", _validCoordinate, _validAddress);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value")
                .WithMessage("Title cannot be empty*");
        }

        [Test]
        public void Constructor_WithNullTitle_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Location.Core.Domain.Entities.Location(null, "Description", _validCoordinate, _validAddress);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value");
        }

        [Test]
        public void Constructor_WithNullCoordinate_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Location.Core.Domain.Entities.Location("Title", "Description", null, _validAddress);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("value");
        }

        [Test]
        public void Constructor_WithNullAddress_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("value");
        }

        [Test]
        public void UpdateDetails_WithValidValues_ShouldUpdateProperties()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Original Title", "Original Description", _validCoordinate, _validAddress);
            location.ClearDomainEvents();

            // Act
            location.UpdateDetails("New Title", "New Description");

            // Assert
            location.Title.Should().Be("New Title");
            location.Description.Should().Be("New Description");
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.Should().ContainItemsAssignableTo<LocationSavedEvent>();
        }

        [Test]
        public void UpdateDetails_WithEmptyTitle_ShouldThrowException()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Original Title", "Original Description", _validCoordinate, _validAddress);

            // Act
            Action act = () => location.UpdateDetails("", "New Description");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value");
        }

        [Test]
        public void UpdateCoordinate_WithValidValue_ShouldUpdateProperty()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);
            var newCoordinate = new Coordinate(48.0, -123.0);
            location.ClearDomainEvents();

            // Act
            location.UpdateCoordinate(newCoordinate);

            // Assert
            location.Coordinate.Should().Be(newCoordinate);
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.Should().ContainItemsAssignableTo<LocationSavedEvent>();
        }

        [Test]
        public void UpdateCoordinate_WithNull_ShouldThrowException()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);

            // Act
            Action act = () => location.UpdateCoordinate(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("value");
        }

        [Test]
        public void AttachPhoto_WithValidPath_ShouldUpdateProperty()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);
            location.ClearDomainEvents();

            // Act
            location.AttachPhoto("/path/to/photo.jpg");

            // Assert
            location.PhotoPath.Should().Be("/path/to/photo.jpg");
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.Should().ContainItemsAssignableTo<PhotoAttachedEvent>();
            var domainEvent = location.DomainEvents.First() as PhotoAttachedEvent;
            domainEvent?.LocationId.Should().Be(location.Id);
            domainEvent?.PhotoPath.Should().Be("/path/to/photo.jpg");
        }

        [Test]
        public void AttachPhoto_WithEmptyPath_ShouldThrowException()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);

            // Act
            Action act = () => location.AttachPhoto("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("photoPath");
        }

        [Test]
        public void RemovePhoto_ShouldClearPhotoPath()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);
            location.AttachPhoto("/path/to/photo.jpg");

            // Act
            location.RemovePhoto();

            // Assert
            location.PhotoPath.Should().BeNull();
        }

        [Test]
        public void Delete_ShouldSetIsDeletedAndAddEvent()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);
            location.ClearDomainEvents();

            // Act
            location.Delete();

            // Assert
            location.IsDeleted.Should().BeTrue();
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.Should().ContainItemsAssignableTo<LocationDeletedEvent>();
            var domainEvent = location.DomainEvents.First() as LocationDeletedEvent;
            domainEvent?.LocationId.Should().Be(location.Id);
        }

        [Test]
        public void Restore_ShouldClearIsDeleted()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);
            location.Delete();

            // Act
            location.Restore();

            // Assert
            location.IsDeleted.Should().BeFalse();
        }
    }
}