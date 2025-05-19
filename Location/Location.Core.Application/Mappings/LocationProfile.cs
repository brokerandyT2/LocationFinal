using AutoMapper;
using Location.Core.Domain.Entities;
using Location.Core.Application.Locations.DTOs;

namespace Location.Core.Application.Mappings
{
    /// <summary>
    /// Provides mapping configurations between the <see cref="Domain.Entities.Location"/> entity and its corresponding
    /// Data Transfer Objects (DTOs).
    /// </summary>
    /// <remarks>This profile defines mappings for converting <see cref="Domain.Entities.Location"/> to and
    /// from <see cref="LocationDto"/> and <see cref="LocationListDto"/>. It also includes reverse mappings for creating
    /// <see cref="Domain.Entities.Location"/> instances from DTOs, with custom handling for complex value objects such
    /// as <see cref="Domain.ValueObjects.Coordinate"/> and <see cref="Domain.ValueObjects.Address"/>.</remarks>
    public class LocationProfile : Profile
    {
        /// <summary>
        /// Configures mapping profiles for the <see cref="Domain.Entities.Location"/> entity and its related DTOs.
        /// </summary>
        /// <remarks>This profile defines mappings between the <see cref="Domain.Entities.Location"/>
        /// entity and the following DTOs: <list type="bullet"> <item><description><see cref="LocationDto"/>: Includes
        /// detailed information about a location.</description></item> <item><description><see
        /// cref="LocationListDto"/>: Includes summary information for listing locations.</description></item> </list>
        /// Additionally, reverse mappings are configured to map from DTOs back to the <see
        /// cref="Domain.Entities.Location"/> entity.</remarks>
        public LocationProfile()
        {
            CreateMap<Domain.Entities.Location, LocationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Coordinate.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Coordinate.Longitude))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address.State))
                .ForMember(dest => dest.PhotoPath, opt => opt.MapFrom(src => src.PhotoPath))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted));

            CreateMap<Domain.Entities.Location, LocationListDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Coordinate.Latitude)) // Add this mapping
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Coordinate.Longitude)) // Add this mapping
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address.State))
                .ForMember(dest => dest.PhotoPath, opt => opt.MapFrom(src => src.PhotoPath))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted));

            // Reverse mapping for creating entities from DTOs
            CreateMap<LocationDto, Domain.Entities.Location>()
                .ForMember(dest => dest.Coordinate, opt => opt.MapFrom(src => new Domain.ValueObjects.Coordinate(src.Latitude, src.Longitude)))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new Domain.ValueObjects.Address(src.City, src.State)))
                .ForMember(dest => dest.DomainEvents, opt => opt.Ignore());
        }
    }
}