using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Tests.Utilities;
using AutoMapper;
using MediatR;
using Location.Core.Application.Common.Interfaces.Persistence;

namespace Location.Core.Application.Tests.Locations.Queries.GetLocations
{
    [TestFixture]
    public class GetLocationsQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private GetLocationsQueryHandler _handler;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();

            // Setup mock repository
            var locationRepoMock = new Mock<ILocationRepository>();
            _unitOfWorkMock.Setup(x => x.Locations).Returns(locationRepoMock.Object);

            _handler = new GetLocationsQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object
            );
        }

        [Test]
        public async Task Handle_WithActiveLocations_ShouldReturnPagedList()
        {
            // Arrange
            var query = new GetLocationsQuery { PageNumber = 1, PageSize = 10 };
            var locations = CreateTestLocations(15);
            var locationDtos = CreateTestLocationListDtos(15);

            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(locations);

            _mapperMock.Setup(x => x.Map<IEnumerable<LocationListDto>>(It.IsAny<IEnumerable<Domain.Entities.Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(10);
            result.Data.TotalCount.Should().Be(15);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
            result.Data.TotalPages.Should().Be(2);
        }

        [Test]
        public async Task Handle_WithIncludeDeleted_ShouldReturnAllLocations()
        {
            // Arrange
            var query = new GetLocationsQuery { PageNumber = 1, PageSize = 10, IncludeDeleted = true };
            var locations = CreateTestLocations(20, includeDeleted: true);
            var locationDtos = CreateTestLocationListDtos(20);

            _unitOfWorkMock.Setup(x => x.Locations.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(locations);

            _mapperMock.Setup(x => x.Map<IEnumerable<LocationListDto>>(It.IsAny<IEnumerable<Domain.Entities.Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalCount.Should().Be(20);
            _unitOfWorkMock.Verify(x => x.Locations.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithPageTwo_ShouldReturnCorrectPage()
        {
            // Arrange
            var query = new GetLocationsQuery { PageNumber = 2, PageSize = 5 };
            var locations = CreateTestLocations(12);
            var locationDtos = CreateTestLocationListDtos(12);

            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(locations);

            _mapperMock.Setup(x => x.Map<IEnumerable<LocationListDto>>(It.IsAny<IEnumerable<Domain.Entities.Location>>()))
                .Returns(locationDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(5);
            result.Data.PageNumber.Should().Be(2);
            result.Data.TotalPages.Should().Be(3);
            result.Data.HasPreviousPage.Should().BeTrue();
            result.Data.HasNextPage.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithNoLocations_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetLocationsQuery { PageNumber = 1, PageSize = 10 };

            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.Location>());

            _mapperMock.Setup(x => x.Map<IEnumerable<LocationListDto>>(It.IsAny<IEnumerable<Domain.Entities.Location>>()))
                .Returns(new List<LocationListDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            result.Data.TotalPages.Should().Be(0);
            result.Data.HasPreviousPage.Should().BeFalse();
            result.Data.HasNextPage.Should().BeFalse();
        }

        [Test]
        public async Task Handle_WithDatabaseException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetLocationsQuery();

            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(x => x.Code == "DATABASE_ERROR");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var query = new GetLocationsQuery();

            _unitOfWorkMock.Setup(x => x.Locations.GetActiveAsync(token))
                .ReturnsAsync(new List<Domain.Entities.Location>());

            // Act
            await _handler.Handle(query, token);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.GetActiveAsync(token), Times.Once);
        }

        private List<Domain.Entities.Location> CreateTestLocations(int count, bool includeDeleted = false)
        {
            var locations = new List<Domain.Entities.Location>();
            for (int i = 0; i < count; i++)
            {
                var location = TestDataBuilder.CreateValidLocation(
                    title: $"Location {i + 1}",
                    description: $"Description {i + 1}"
                );

                if (includeDeleted && i % 3 == 0)
                {
                    location.Delete();
                }

                locations.Add(location);
            }
            return locations;
        }

        private List<LocationListDto> CreateTestLocationListDtos(int count)
        {
            var dtos = new List<LocationListDto>();
            for (int i = 0; i < count; i++)
            {
                dtos.Add(new LocationListDto
                {
                    Id = i + 1,
                    Title = $"Location {i + 1}",
                    City = "Seattle",
                    State = "WA",
                    PhotoPath = i % 2 == 0 ? $"/photos/location{i + 1}.jpg" : null,
                    IsDeleted = i % 3 == 0
                });
            }
            return dtos;
        }
    }

    // Placeholder for the actual implementation
    public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, Result<PagedList<LocationListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLocationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PagedList<LocationListDto>>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var locations = request.IncludeDeleted
                    ? await _unitOfWork.Locations.GetAllAsync(cancellationToken)
                    : await _unitOfWork.Locations.GetActiveAsync(cancellationToken);

                var locationDtos = _mapper.Map<IEnumerable<LocationListDto>>(locations);
                var pagedList = PagedList<LocationListDto>.Create(locationDtos.ToList(), request.PageNumber, request.PageSize);

                return Result<PagedList<LocationListDto>>.Success(pagedList);
            }
            catch (System.Exception ex)
            {
                return Result<PagedList<LocationListDto>>.Failure(Error.Database($"Failed to retrieve locations: {ex.Message}"));
            }
        }
    }
}