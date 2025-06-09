using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Settings.Commands.CreateSetting
{
    /// <summary>
    /// Represents a command to create a new setting with a specified key, value, and description.
    /// </summary>
    /// <remarks>This command is used to encapsulate the data required to create a new setting.  The <see
    /// cref="Key"/> property identifies the setting, the <see cref="Value"/> property specifies its value,  and the
    /// <see cref="Description"/> provides additional context or details about the setting.</remarks>
    public class CreateSettingCommand : IRequest<Result<CreateSettingCommandResponse>>
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    /// <summary>
    /// Represents the response returned after creating a new setting.
    /// </summary>
    /// <remarks>This class contains details about the created setting, including its unique identifier, key,
    /// value,  description, and the timestamp of when it was created.</remarks>
    public class CreateSettingCommandResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}