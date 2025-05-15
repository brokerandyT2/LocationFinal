using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Queries.Locations;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Queries.Locations.GetLocationByTitle
{
    [TestFixture]
    public class GetLocationByTitleQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILocationRepository> _locationRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private GetLocationByTitleQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);

            _handler = new GetLocationByTitleQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object);
        }

        [Test]
        public async Task Handle_WithExistingTitle_ShouldReturnLocation()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "Space Needle" };
            var location = TestDataBuilder.CreateValidLocation(
                title: "Space Needle",
                description: "Iconic Seattle landmark");

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Title = location.Title,
                Description = location.Description,
                Latitude = location.Coordinate.Latitude,
                Longitude = location.Coordinate.Longitude,
                City = location.Address.City,
                State = location.Address.State
            };

            _mapperMock
                .Setup(x => x.Map<LocationDto>(location))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Title.Should().Be("Space Needle");

            _locationRepositoryMock.Verify(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentTitle_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "Non-existent Location" };

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be($"Location with title '{query.Title}' not found");

            _mapperMock.Verify(x => x.Map<LocationDto>(It.IsAny<Domain.Entities.Location>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithEmptyTitle_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = string.Empty };

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Invalid title"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be($"Location with title '{query.Title}' not found");
        }

        [Test]
        public async Task Handle_WithWhitespacePaddedTitle_ShouldWork()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "  Space Needle  " };
            var location = TestDataBuilder.CreateValidLocation(title: "Space Needle");

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Title = location.Title
            };

            _mapperMock
                .Setup(x => x.Map<LocationDto>(location))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithCaseInsensitiveTitle_DependsOnRepository()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "space needle" };

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Not found"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            // Note: Case sensitivity depends on repository implementation
        }

        [Test]
        public async Task Handle_WithLongTitle_ShouldWork()
        {
            // Arrange
            var longTitle = "This is an extremely long location title that might test the limits of the system";
            var query = new GetLocationByTitleQuery { Title = longTitle };
            var location = TestDataBuilder.CreateValidLocation(title: longTitle);

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Title = location.Title
            };

            _mapperMock
                .Setup(x => x.Map<LocationDto>(location))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Title.Should().Be(longTitle);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "Test Location" };
            var cancellationToken = new CancellationToken();

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Not found"));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _locationRepositoryMock.Verify(x => x.GetByTitleAsync(query.Title, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WithSpecialCharactersInTitle_ShouldWork()
        {
            // Arrange
            var specialTitle = "Pike's Place Market #1";
            var query = new GetLocationByTitleQuery { Title = specialTitle };
            var location = TestDataBuilder.CreateValidLocation(title: specialTitle);

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto { Title = specialTitle };

            _mapperMock
                .Setup(x => x.Map<LocationDto>(location))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Title.Should().Be(specialTitle);
        }

        [Test]
        public async Task Handle_WithException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "Test Location" };

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve location");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_WithSuccessfulMapping_ShouldReturnCompleteDto()
        {
            // Arrange
            var query = new GetLocationByTitleQuery { Title = "Golden Gate Bridge" };
            var location = TestDataBuilder.CreateValidLocation(
                title: "Golden Gate Bridge",
                description: "Famous SF bridge",
                latitude: 37.8199,
                longitude: -122.4783,
                city: "San Francisco",
                state: "CA");

            location.AttachPhoto("/photos/ggb.jpg");

            _locationRepositoryMock
                .Setup(x => x.GetByTitleAsync(query.Title, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Title = location.Title,
                Description = location.Description,
                Latitude = location.Coordinate.Latitude,
                Longitude = location.Coordinate.Longitude,
                City = location.Address.City,
                State = location.Address.State,
                PhotoPath = location.PhotoPath,
                Timestamp = location.Timestamp,
                IsDeleted = location.IsDeleted
            };

            _mapperMock
                .Setup(x => x.Map<LocationDto>(location))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Title.Should().Be("Golden Gate Bridge");
            result.Data.Description.Should().Be("Famous SF bridge");
            result.Data.PhotoPath.Should().Be("/photos/ggb.jpg");
            result.Data.City.Should().Be("San Francisco");
            result.Data.State.Should().Be("CA");
        }
    }
}