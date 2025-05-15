using MediatR;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.DeleteSetting
{
    public class DeleteSettingCommand : IRequest<Result<bool>>
    {
        public string Key { get; set; } = string.Empty;
    }
}