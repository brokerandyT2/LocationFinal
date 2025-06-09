using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Settings.Commands.DeleteSetting
{
    /// <summary>
    /// Represents a command to delete a setting identified by its key.
    /// </summary>
    /// <remarks>This command is used to request the deletion of a specific setting in the system. The result
    /// of the operation indicates whether the deletion was successful.</remarks>
    public class DeleteSettingCommand : IRequest<Result<bool>>
    {
        public string Key { get; set; } = string.Empty;
    }
}