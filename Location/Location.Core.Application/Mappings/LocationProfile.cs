using AutoMapper;
using Location.Core.Domain.Entities;
using Location.Core.Application.Locations.DTOs;

namespace Location.Core.Application.Mappings
{
    public class LocationProfile : Profile
    {
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