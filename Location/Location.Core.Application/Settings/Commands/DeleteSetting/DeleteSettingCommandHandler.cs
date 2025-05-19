using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using MediatR;

namespace Location.Core.Application.Settings.Commands.DeleteSetting
{
    /// <summary>
    /// Handles the deletion of a setting identified by its key.
    /// </summary>
    /// <remarks>This handler processes a <see cref="DeleteSettingCommand"/> to delete a setting from the
    /// underlying data store. It uses the provided <see cref="IUnitOfWork"/> to perform the operation.</remarks>
    public class DeleteSettingCommandHandler : IRequestHandler<DeleteSettingCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Handles the deletion of a setting by executing the associated command.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database transactions and operations.  This parameter cannot be
        /// <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public DeleteSettingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        /// <summary>
        /// Handles the deletion of a setting identified by its key.
        /// </summary>
        /// <param name="request">The command containing the key of the setting to delete.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> indicating whether the deletion was successful. The result contains <see
        /// langword="true"/> if the setting was deleted successfully; otherwise, <see langword="false"/>.</returns>
        public async Task<Result<bool>> Handle(DeleteSettingCommand request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Settings.DeleteAsync(request.Key, cancellationToken);

            return result;
        }
    }
}