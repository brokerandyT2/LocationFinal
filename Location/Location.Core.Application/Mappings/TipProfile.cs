using AutoMapper;
using Location.Core.Application.Tips.Commands.CreateTip;
using Location.Core.Application.Tips.Commands.UpdateTip;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tips.Queries.GetAllTips;
using Location.Core.Application.Tips.Queries.GetTipById;
using Location.Core.Application.Tips.Queries.GetTipsByType;
using Location.Core.Domain.Entities;

namespace Location.Core.Application.Mappings
{
    /// <summary>
    /// Configures object-object mapping profiles for Tip-related entities and data transfer objects (DTOs).
    /// </summary>
    /// <remarks>This class defines mappings between Tip domain entities, their corresponding DTOs, and
    /// query/command response types. It is used by AutoMapper to facilitate transformations between these types during
    /// application execution.</remarks>
    public class TipProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TipProfile"/> class, defining mappings between  domain
        /// entities, data transfer objects (DTOs), and query/command response types.
        /// </summary>
        /// <remarks>This profile is used to configure AutoMapper mappings for the Tip-related domain
        /// models and  their corresponding DTOs and response types. It includes mappings for both directions: - Entity
        /// to DTO and response types. - DTO to entity types.  These mappings are essential for transforming data
        /// between layers in the application.</remarks>
        public TipProfile()
        {
            // Entity to DTO mappings
            CreateMap<Tip, TipDto>();
            CreateMap<TipType, TipTypeDto>();

            // DTO to Entity mappings
            CreateMap<TipDto, Tip>();
            CreateMap<TipTypeDto, TipType>();

            // Entity to query response mappings
            CreateMap<Tip, GetTipByIdQueryResponse>();
            CreateMap<Tip, GetAllTipsQueryResponse>();
            CreateMap<Tip, GetTipsByTypeQueryResponse>();

            // Entity to command response mappings
            CreateMap<Tip, CreateTipCommandResponse>();
            CreateMap<Tip, UpdateTipCommandResponse>();
        }
    }
}