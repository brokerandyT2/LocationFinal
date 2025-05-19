using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Settings.Commands.CreateSetting
{
    /// <summary>
    /// Handles the creation of a new setting by processing a <see cref="CreateSettingCommand"/> request.
    /// </summary>
    /// <remarks>This handler ensures that a setting with the specified key does not already exist before
    /// creating a new one. If a setting with the same key exists, the operation fails with an appropriate error
    /// message.</remarks>
    public class CreateSettingCommandHandler : IRequestHandler<CreateSettingCommand, Result<CreateSettingCommandResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSettingCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database transactions and operations.  This parameter cannot be
        /// <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public CreateSettingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        /// <summary>
        /// Handles the creation of a new setting based on the provided command.
        /// </summary>
        /// <remarks>If a setting with the specified key already exists, the operation will fail and
        /// return an error message.</remarks>
        /// <param name="request">The command containing the key, value, and description for the new setting.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="CreateSettingCommandResponse"/> if the operation succeeds,
        /// or a failure result with an error message if the operation fails.</returns>
        public async Task<Result<CreateSettingCommandResponse>> Handle(CreateSettingCommand request, CancellationToken cancellationToken)
        {
            var existingSettingResult = await _unitOfWork.Settings.GetByKeyAsync(request.Key, cancellationToken);

            if (existingSettingResult.IsSuccess && existingSettingResult.Data != null)
            {
                return Result<CreateSettingCommandResponse>.Failure($"Setting with key '{request.Key}' already exists");
            }

            var setting = new Domain.Entities.Setting(request.Key, request.Value, request.Description);

            var result = await _unitOfWork.Settings.CreateAsync(setting, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<CreateSettingCommandResponse>.Failure(result.ErrorMessage ?? "Failed to create setting");
            }

            var createdSetting = result.Data;

            var response = new CreateSettingCommandResponse
            {
                Id = createdSetting.Id,
                Key = createdSetting.Key,
                Value = createdSetting.Value,
                Description = createdSetting.Description,
                Timestamp = createdSetting.Timestamp
            };

            return Result<CreateSettingCommandResponse>.Success(response);
        }
    }
}