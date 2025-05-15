using AutoMapper;
using Location.Core.Domain.Entities;
using Location.Core.Application.Settings.DTOs;
using Location.Core.Application.Settings.Commands.CreateSetting;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Application.Settings.Queries.GetAllSettings;

namespace Location.Core.Application.Mappings
{
    public class SettingProfile : Profile
    {
        public SettingProfile()
        {
            // Entity to DTO mappings
            CreateMap<Setting, SettingDto>();

            // DTO to Entity mappings
            CreateMap<SettingDto, Setting>();

            // Entity to query response mappings
            CreateMap<Setting, GetSettingByKeyQueryResponse>();
            CreateMap<Setting, GetAllSettingsQueryResponse>();

            // Entity to command response mappings
            CreateMap<Setting, CreateSettingCommandResponse>();
            CreateMap<Setting, UpdateSettingCommandResponse>();
        }
    }
}