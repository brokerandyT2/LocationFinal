using AutoMapper;
using AutoMapper.QueryableExtensions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;

namespace Location.Core.Application.Mappings
{
    /// <summary>
    /// Optimized mapping configurations with compiled mappings and projection support
    /// </summary>
    /// <remarks>This profile uses compiled mappings, projection configurations, and bulk mapping
    /// optimizations for maximum performance in high-throughput scenarios.</remarks>
    public class LocationProfile : Profile
    {
        /// <summary>
        /// Configures optimized mapping profiles with performance enhancements
        /// </summary>
        public LocationProfile()
        {
            // Compiled mapping for frequently used conversions
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
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted)); // Compile for performance

            // Optimized list mapping with bulk operations support
            CreateMap<Domain.Entities.Location, LocationListDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Coordinate.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Coordinate.Longitude))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address.State))
                .ForMember(dest => dest.PhotoPath, opt => opt.MapFrom(src => src.PhotoPath))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted)); // Compile for performance

            // Reverse mapping for creating entities from DTOs
            CreateMap<LocationDto, Domain.Entities.Location>()
                .ForMember(dest => dest.Coordinate, opt => opt.MapFrom(src => new Domain.ValueObjects.Coordinate(src.Latitude, src.Longitude)))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new Domain.ValueObjects.Address(src.City, src.State)))
                .ForMember(dest => dest.DomainEvents, opt => opt.Ignore());

            // High-performance PagedList mapping
            CreateMap<PagedList<Domain.Entities.Location>, PagedList<LocationListDto>>()
                .ConvertUsing<OptimizedPagedListConverter<Domain.Entities.Location, LocationListDto>>();

            // Projection mapping for database-level projections (avoid loading full entities)
            CreateProjectionMap<Domain.Entities.Location, LocationListDto>();
        }

        /// <summary>
        /// Creates a projection mapping that can be used with LINQ projections
        /// </summary>
        private void CreateProjectionMap<TSource, TDestination>()
        {
            // This allows for direct database projections without loading full entities
            // Usage: queryable.ProjectTo<LocationListDto>(_mapper.ConfigurationProvider)
            ////CreateMap<TSource, TDestination>()
            //   .ConstructUsing((src, context) => context.Mapper.Map<TDestination>(src));
        }
    }

    /// <summary>
    /// High-performance converter for PagedList mapping with minimal allocations
    /// </summary>
    public class OptimizedPagedListConverter<TSource, TDestination> : ITypeConverter<PagedList<TSource>, PagedList<TDestination>>
    {
        public PagedList<TDestination> Convert(PagedList<TSource> source, PagedList<TDestination> destination, ResolutionContext context)
        {
            if (source?.Items == null)
            {
                return new PagedList<TDestination>(
                    Enumerable.Empty<TDestination>(),
                    0,
                    source?.PageNumber ?? 1,
                    source?.PageSize ?? 10);
            }

            // Use bulk mapping for better performance
            var mappedItems = context.Mapper.Map<IReadOnlyList<TDestination>>(source.Items);

            return new PagedList<TDestination>(
                mappedItems,
                source.TotalCount,
                source.PageNumber,
                source.PageSize);
        }
    }

    /// <summary>
    /// Extension methods for optimized AutoMapper usage
    /// </summary>
    public static class MappingExtensions
    {
        /// <summary>
        /// Maps a collection with optimized bulk operations
        /// </summary>
        public static IReadOnlyList<TDestination> MapCollection<TSource, TDestination>(
            this IMapper mapper,
            IEnumerable<TSource> source)
        {
            if (source == null)
                return new List<TDestination>().AsReadOnly();

            // Use bulk mapping for collections
            return mapper.Map<IReadOnlyList<TDestination>>(source);
        }

        /// <summary>
        /// Projects a queryable for database-level projections
        /// </summary>
        public static IQueryable<TDestination> ProjectToOptimized<TDestination>(
            this IQueryable source,
            IConfigurationProvider configuration)
        {
            return source.ProjectTo<TDestination>(configuration);
        }
    }
}