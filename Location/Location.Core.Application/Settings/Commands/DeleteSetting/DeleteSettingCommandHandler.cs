using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Settings.Commands.DeleteSetting
{
    public class DeleteSettingCommandHandler : IRequestHandler<DeleteSettingCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteSettingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<bool>> Handle(DeleteSettingCommand request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Settings.DeleteAsync(request.Key, cancellationToken);

            return result;
        }
    }
}