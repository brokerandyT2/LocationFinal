using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Tests.Helpers;
using AutoMapper;
using MediatR;
using Location.Core.Application.Common.Interfaces.Persistence;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocationById
{
    [TestFixture]
    public class GetLocationByIdQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private GetLocationByIdQueryHandler _handler;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();

            // Setup mock repository
            var locationRepoMock = new Mock<ILocationRepository>();
            _unitOfWorkMock.Setup(x => x.Locations).Returns(locationRepoMock.Object);

            _handler = new GetLocationByIdQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object
            );
        }

        [Test]
        public async Task Handle_WithExistingLocation_ShouldReturnLocation()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation();
            var locationDto = TestDataBuilder.CreateValidLocationDto(id: 1);

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(locationDto);
        }

        [Test]
        public async Task Handle_WithNonExistentLocation_ShouldReturnNotFound()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 999 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(x => x.Code == "NOT_FOUND");
            result.Errors.Should().Contain(x => x.Message.Contains("999"));
        }

        [Test]
        public async Task Handle_WithDeletedLocation_ShouldStillReturn()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation();
            location.Delete();
            var locationDto = TestDataBuilder.CreateValidLocationDto(id: 1, isDeleted: true);

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(locationDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.IsDeleted.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 0 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(x => x.Code == "NOT_FOUND");
        }

        [Test]
        public async Task Handle_WithDatabaseException_ShouldReturnDatabaseError()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 1 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(x => x.Code == "DATABASE_ERROR");
            result.Errors.Should().Contain(x => x.Message.Contains("Database connection failed"));
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var query = new GetLocationByIdQuery { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation();

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, token))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(TestDataBuilder.CreateValidLocationDto());

            // Act
            await _handler.Handle(query, token);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.GetByIdAsync(1, token), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldMapAllLocationProperties()
        {
            // Arrange
            var query = new GetLocationByIdQuery { Id = 1 };
            var location = TestDataBuilder.CreateValidLocation(
                title: "Test Location",
                description: "Test Description",
                latitude: 47.6062,
                longitude: -122.3321,
                city: "Seattle",
                state: "WA"
            );
            location.AttachPhoto("/photos/test.jpg");

            var expectedDto = new LocationDto
            {
                Id = 1,
                Title = "Test Location",
                Description = "Test Description",
                Latitude = 47.6062,
                Longitude = -122.3321,
                City = "Seattle",
                State = "WA",
                PhotoPath = "/photos/test.jpg",
                IsDeleted = false,
                Timestamp = location.Timestamp
            };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<LocationDto>(location))
                .Returns(expectedDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Title.Should().Be("Test Location");
            result.Data.Description.Should().Be("Test Description");
            result.Data.Latitude.Should().Be(47.6062);
            result.Data.Longitude.Should().Be(-122.3321);
            result.Data.City.Should().Be("Seattle");
            result.Data.State.Should().Be("WA");
            result.Data.PhotoPath.Should().Be("/photos/test.jpg");
        }
    }

    // Placeholder for the actual implementation
    public class GetLocationByIdQueryHandler : IRequestHandler<GetLocationByIdQuery, Result<LocationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLocationByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<LocationDto>> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);

                if (location == null)
                {
                    return Result<LocationDto>.Failure(Error.NotFound($"Location with ID {request.Id} not found"));
                }

                var dto = _mapper.Map<LocationDto>(location);
                return Result<LocationDto>.Success(dto);
            }
            catch (System.Exception ex)
            {
                return Result<LocationDto>.Failure(Error.Database($"Failed to retrieve location: {ex.Message}"));
            }
        }
    }
}