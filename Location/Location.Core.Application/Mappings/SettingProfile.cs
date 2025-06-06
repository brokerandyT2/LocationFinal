using AutoMapper;
using Location.Core.Application.Settings.Commands.CreateSetting;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.DTOs;
using Location.Core.Application.Settings.Queries.GetAllSettings;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Domain.Entities;

namespace Location.Core.Application.Mappings
{
    /// <summary>
    /// Provides mapping configurations for the <see cref="Setting"/> entity and related data transfer objects (DTOs)
    /// and responses.
    /// </summary>
    /// <remarks>This profile defines mappings between the <see cref="Setting"/> entity and its corresponding
    /// DTOs, query responses, and command responses. It is used by AutoMapper to facilitate object-to-object mapping in
    /// the application.</remarks>
    public class SettingProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingProfile"/> class, configuring mappings between  domain
        /// entities, data transfer objects (DTOs), and query/command response types.
        /// </summary>
        /// <remarks>This profile defines the AutoMapper mappings for the <c>Setting</c> entity, enabling
        /// seamless  transformations between the following types: <list type="bullet">
        /// <item><description><c>Setting</c> to <c>SettingDto</c> and vice versa.</description></item>
        /// <item><description><c>Setting</c> to <c>GetSettingByKeyQueryResponse</c> and
        /// <c>GetAllSettingsQueryResponse</c>.</description></item> <item><description><c>Setting</c> to
        /// <c>CreateSettingCommandResponse</c> and <c>UpdateSettingCommandResponse</c>.</description></item> </list>
        /// This configuration is typically used in conjunction with AutoMapper to facilitate object mapping  in
        /// application layers such as queries, commands, and API responses.</remarks>
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