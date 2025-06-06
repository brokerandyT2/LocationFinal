using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using MediatR;

namespace Location.Core.Application.Settings.Commands.UpdateSetting
{
    /// <summary>
    /// Handles the execution of the <see cref="UpdateSettingCommand"/> to update a setting's value in the system.
    /// </summary>
    /// <remarks>This handler retrieves the setting by its key, updates its value, and persists the changes to
    /// the data store. If the setting is not found or the update operation fails, an appropriate failure result is
    /// returned.</remarks>
    public class UpdateSettingCommandHandler : IRequestHandler<UpdateSettingCommand, Result<UpdateSettingCommandResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        /// <summary>
        /// Handles the execution of commands to update application settings.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database transactions and ensure consistency. This parameter cannot
        /// be <see langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public UpdateSettingCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the update of a setting by its key and value.
        /// </summary>
        /// <remarks>If the setting with the specified key is not found, the operation will fail with an
        /// appropriate error message. If the update operation fails, the result will indicate failure with the
        /// corresponding error message.</remarks>
        /// <param name="request">The command containing the key of the setting to update and the new value.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains a <see
        /// cref="Result{T}"/> object that indicates success or failure. On success, the result includes an <see
        /// cref="UpdateSettingCommandResponse"/> with details of the updated setting.</returns>
        public async Task<Result<UpdateSettingCommandResponse>> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var settingResult = await _unitOfWork.Settings.GetByKeyAsync(request.Key, cancellationToken);

                if (!settingResult.IsSuccess || settingResult.Data == null)
                {
                    await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.KeyNotFound), cancellationToken);
                    return Result<UpdateSettingCommandResponse>.Failure($"Setting with key '{request.Key}' not found");
                }

                var setting = settingResult.Data;
                setting.UpdateValue(request.Value);

                var updateResult = await _unitOfWork.Settings.UpdateAsync(setting, cancellationToken);

                if (!updateResult.IsSuccess || updateResult.Data == null)
                {
                    await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.DatabaseError, updateResult.ErrorMessage), cancellationToken);
                    return Result<UpdateSettingCommandResponse>.Failure(updateResult.ErrorMessage ?? "Failed to update setting");
                }

                var updatedSetting = updateResult.Data;

                var response = new UpdateSettingCommandResponse
                {
                    Id = updatedSetting.Id,
                    Key = updatedSetting.Key,
                    Value = updatedSetting.Value,
                    Description = updatedSetting.Description,
                    Timestamp = updatedSetting.Timestamp
                };

                return Result<UpdateSettingCommandResponse>.Success(response);
            }
            catch (Domain.Exceptions.SettingDomainException ex) when (ex.Code == "READ_ONLY_SETTING")
            {
                await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.ReadOnlySetting, ex.Message), cancellationToken);
                return Result<UpdateSettingCommandResponse>.Failure("Cannot update read-only setting");
            }
            catch (Domain.Exceptions.SettingDomainException ex) when (ex.Code == "INVALID_VALUE")
            {
                await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.InvalidValue, ex.Message), cancellationToken);
                return Result<UpdateSettingCommandResponse>.Failure("Invalid setting value provided");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.DatabaseError, ex.Message), cancellationToken);
                return Result<UpdateSettingCommandResponse>.Failure($"Failed to update setting: {ex.Message}");
            }
        }
    }
}