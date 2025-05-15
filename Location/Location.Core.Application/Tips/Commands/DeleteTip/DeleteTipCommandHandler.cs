using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tips.Commands.DeleteTip
{
    public class DeleteTipCommandHandler : IRequestHandler<DeleteTipCommand, Result<bool>>
    {
        private readonly ITipRepository _tipRepository;

        public DeleteTipCommandHandler(ITipRepository tipRepository)
        {
            _tipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        }

        public async Task<Result<bool>> Handle(DeleteTipCommand request, CancellationToken cancellationToken)
        {
            var result = await _tipRepository.DeleteAsync(request.Id, cancellationToken);

            return result;
        }
    }
}