using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.DeleteSetting
{
    public class DeleteSettingCommandHandler : IRequestHandler<DeleteSettingCommand, Result<bool>>
    {
        private readonly ISettingRepository _settingRepository;

        public DeleteSettingCommandHandler(ISettingRepository settingRepository)
        {
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
        }

        public async Task<Result<bool>> Handle(DeleteSettingCommand request, CancellationToken cancellationToken)
        {
            var result = await _settingRepository.DeleteAsync(request.Key, cancellationToken);

            return result;
        }
    }
}