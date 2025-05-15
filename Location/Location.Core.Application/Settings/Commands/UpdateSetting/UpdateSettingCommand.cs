using System;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.UpdateSetting
{
    public class UpdateSettingCommand : IRequest<Result<UpdateSettingCommandResponse>>
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateSettingCommandResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}