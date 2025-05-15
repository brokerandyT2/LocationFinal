using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.CreateSetting
{
    public class CreateSettingCommand : IRequest<Result<CreateSettingCommandResponse>>
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CreateSettingCommandResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}