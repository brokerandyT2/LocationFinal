using NUnit.Framework;
using FluentAssertions;
using AutoMapper;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Application.Tests.Mappings
{
    [TestFixture]
    public class LocationProfileTests
    {
        private IMapper _mapper;

        [SetUp]
        public void Setup()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<LocationProfile>();
            });

            _mapper = config.CreateMapper();
        }

        [Test]
        public void Configuration_ShouldBeValid()
        {
            // Arrange
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<LocationProfile>();
            });

            // Act & Assert
            config.AssertConfigurationIsValid();
        }

        [Test]
        public void Map_LocationToLocationDto_ShouldMapCorrectly()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation(
                title: "Space Needle",
                description: "Iconic Seattle landmark",
                latitude: 47.6205,
                longitude: -122.3493,
                city: "Seattle",
                state: "WA"
            );
            location.AttachPhoto("/photos/space-needle.jpg");

            // Act
            var dto = _mapper.Map<LocationDto>(location);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(location.Id);
            dto.Title.Should().Be("Space Needle");
            dto.Description.Should().Be("Iconic Seattle landmark");
            dto.Latitude.Should().Be(47.6205);
            dto.Longitude.Should().Be(-122.3493);
            dto.City.Should().Be("Seattle");
            dto.State.Should().Be("WA");
            dto.PhotoPath.Should().Be("/photos/space-needle.jpg");
            dto.IsDeleted.Should().Be(location.IsDeleted);
            dto.Timestamp.Should().Be(location.Timestamp);
        }

        [Test]
        public void Map_LocationToLocationListDto_ShouldMapCorrectly()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation(
                title: "Pike Place Market",
                city: "Seattle",
                state: "WA"
            );
            location.AttachPhoto("/photos/pike-place.jpg");

            // Act
            var dto = _mapper.Map<LocationListDto>(location);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(location.Id);
            dto.Title.Should().Be("Pike Place Market");
            dto.City.Should().Be("Seattle");
            dto.State.Should().Be("WA");
            dto.PhotoPath.Should().Be("/photos/pike-place.jpg");
            dto.IsDeleted.Should().Be(location.IsDeleted);
            dto.Timestamp.Should().Be(location.Timestamp);
        }

        [Test]
        public void Map_LocationWithNoPhoto_ShouldMapNullPhotoPath()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();

            // Act
            var dto = _mapper.Map<LocationDto>(location);

            // Assert
            dto.PhotoPath.Should().BeNull();
        }

        [Test]
        public void Map_DeletedLocation_ShouldMapCorrectly()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            location.Delete();

            // Act
            var dto = _mapper.Map<LocationDto>(location);

            // Assert
            dto.IsDeleted.Should().BeTrue();
        }

        [Test]
        public void Map_LocationWithComplexCoordinate_ShouldFlattenCorrectly()
        {
            // Arrange
            var coordinate = new Coordinate(37.7749, -122.4194);
            var address = new Address("San Francisco", "CA");
            var location = new Domain.Entities.Location("Golden Gate Bridge", "Historic bridge", coordinate, address);

            // Act
            var dto = _mapper.Map<LocationDto>(location);

            // Assert
            dto.Latitude.Should().Be(37.7749);
            dto.Longitude.Should().Be(-122.4194);
        }

        [Test]
        public void Map_LocationWithComplexAddress_ShouldFlattenCorrectly()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var address = new Address("Bellevue", "WA");
            var location = new Domain.Entities.Location("Bellevue Downtown", "City center", coordinate, address);

            // Act
            var dto = _mapper.Map<LocationDto>(location);

            // Assert
            dto.City.Should().Be("Bellevue");
            dto.State.Should().Be("WA");
        }

        [Test]
        public void Map_CollectionOfLocations_ShouldMapAllItems()
        {
            // Arrange
            var locations = new[]
            {
                TestDataBuilder.CreateValidLocation(title: "Location 1"),
                TestDataBuilder.CreateValidLocation(title: "Location 2"),
                TestDataBuilder.CreateValidLocation(title: "Location 3")
            };

            // Act
            var dtos = _mapper.Map<LocationDto[]>(locations);

            // Assert
            dtos.Should().HaveCount(3);
            dtos[0].Title.Should().Be("Location 1");
            dtos[1].Title.Should().Be("Location 2");
            dtos[2].Title.Should().Be("Location 3");
        }

        [Test]
        public void Map_LocationDtoToLocation_ShouldNotBeDefined()
        {
            // Arrange
            var dto = TestDataBuilder.CreateValidLocationDto();

            // Act
            Action act = () => _mapper.Map<Domain.Entities.Location>(dto);

            // Assert
            act.Should().Throw<AutoMapperMappingException>()
                .WithMessage("*Missing type map configuration*");
        }
    }

    // Placeholder for the actual implementation
    public class LocationProfile : Profile
    {
        public LocationProfile()
        {
            CreateMap<Domain.Entities.Location, LocationDto>()
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Coordinate.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Coordinate.Longitude))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address.State));

            CreateMap<Domain.Entities.Location, LocationListDto>()
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address.State));
        }
    }
}