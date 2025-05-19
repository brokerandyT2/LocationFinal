using System;
using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.UpdateSetting
{
    /// <summary>
    /// Represents a command to update a setting with a specified key, value, and optional description.
    /// </summary>
    /// <remarks>This command is used to modify the value of a setting identified by its key.  The optional
    /// description can provide additional context about the setting being updated.</remarks>
    public class UpdateSettingCommand : IRequest<Result<UpdateSettingCommandResponse>>
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    /// <summary>
    /// Represents the response returned after updating a setting in the system.
    /// </summary>
    /// <remarks>This class contains details about the updated setting, including its identifier, key, value, 
    /// description, and the timestamp of the update. It is typically used to convey the result of  an update operation
    /// in a settings management context.</remarks>
    public class UpdateSettingCommandResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}