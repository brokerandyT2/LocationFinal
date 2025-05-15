using AutoMapper;
using Location.Core.Domain.Entities;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tips.Commands.CreateTip;
using Location.Core.Application.Tips.Commands.UpdateTip;
using Location.Core.Application.Tips.Queries.GetTipById;
using Location.Core.Application.Tips.Queries.GetAllTips;
using Location.Core.Application.Tips.Queries.GetTipsByType;

namespace Location.Core.Application.Mappings
{
    public class TipProfile : Profile
    {
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