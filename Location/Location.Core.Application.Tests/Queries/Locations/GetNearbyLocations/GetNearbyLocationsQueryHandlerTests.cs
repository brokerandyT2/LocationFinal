using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Queries.Locations;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Queries.Locations.GetNearbyLocations
{
    [Category("Locations")]
    [Category("Query")]

    [TestFixture]
    public class GetNearbyLocationsQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILocationRepository> _locationRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private GetNearbyLocationsQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);

            _handler = new GetNearbyLocationsQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object);
        }

        [Test]
        public async Task Handle_WithValidCoordinates_ShouldReturnNearbyLocations()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 10.0
            };

            var nearbyLocations = TestDataBuilder.CreateValidLocationList(3);

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DistanceKm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(nearbyLocations));

            var locationDtos = nearbyLocations.Select(l => new LocationListDto
            {
                Id = l.Id,
                Title = l.Title,
                City = l.Address.City,
                State = l.Address.State
            }).ToList();

            _mapperMock
                .Setup(x => x.Map<List<LocationListDto>>(nearbyLocations))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);

            _locationRepositoryMock.Verify(x => x.GetNearbyAsync(
                query.Latitude,
                query.Longitude,
                query.DistanceKm,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithDefaultDistance_ShouldUse10Km()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321
                // DistanceKm defaults to 10.0
            };

            var nearbyLocations = new List<Domain.Entities.Location>();

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    query.Latitude,
                    query.Longitude,
                    10.0, // Default distance
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(nearbyLocations));

            _mapperMock
                .Setup(x => x.Map<List<LocationListDto>>(nearbyLocations))
                .Returns(new List<LocationListDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationRepositoryMock.Verify(x => x.GetNearbyAsync(
                query.Latitude,
                query.Longitude,
                10.0,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoNearbyLocations_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 0.0, // Middle of ocean
                Longitude = 0.0,
                DistanceKm = 1.0
            };

            var emptyList = new List<Domain.Entities.Location>();

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DistanceKm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(emptyList));

            _mapperMock
                .Setup(x => x.Map<List<LocationListDto>>(emptyList))
                .Returns(new List<LocationListDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WithLargeRadius_ShouldReturnMatchingLocations()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 100.0 // Large radius
            };

            var manyLocations = TestDataBuilder.CreateValidLocationList(20);

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DistanceKm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(manyLocations));

            var locationDtos = manyLocations.Select(l => new LocationListDto
            {
                Id = l.Id,
                Title = l.Title
            }).ToList();

            _mapperMock
                .Setup(x => x.Map<List<LocationListDto>>(manyLocations))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(20);
        }

        [Test]
        public async Task Handle_WhenRepositoryFails_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 10.0
            };

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DistanceKm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Failure("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Database error");

            _mapperMock.Verify(x => x.Map<List<LocationListDto>>(It.IsAny<IEnumerable<Domain.Entities.Location>>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithExtremeCoordinates_ShouldWork()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 89.999, // Near North Pole
                Longitude = 179.999, // Near date line
                DistanceKm = 50.0
            };

            var locations = new List<Domain.Entities.Location>();

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DistanceKm,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(locations));

            _mapperMock
                .Setup(x => x.Map<List<LocationListDto>>(locations))
                .Returns(new List<LocationListDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 10.0
            };

            var cancellationToken = new CancellationToken();
            var locations = new List<Domain.Entities.Location>();

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    query.Latitude,
                    query.Longitude,
                    query.DistanceKm,
                    cancellationToken))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(locations));

            _mapperMock
                .Setup(x => x.Map<List<LocationListDto>>(locations))
                .Returns(new List<LocationListDto>());

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _locationRepositoryMock.Verify(x => x.GetNearbyAsync(
                query.Latitude,
                query.Longitude,
                query.DistanceKm,
                cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WithException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 10.0
            };

            _locationRepositoryMock
                .Setup(x => x.GetNearbyAsync(
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve nearby locations");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }
    }
}