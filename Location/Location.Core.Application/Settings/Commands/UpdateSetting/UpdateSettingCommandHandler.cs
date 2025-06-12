using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Settings.Commands.UpdateSetting
{
    /// <summary>
    /// Handles the updating of an existing setting by processing an <see cref="UpdateSettingCommand"/> request.
    /// </summary>
    /// <remarks>This handler retrieves the setting by its key, updates its value, and persists the changes
    /// to the data store. If the setting is not found or the update operation fails, an appropriate error result is
    /// returned.</remarks>
    public class UpdateSettingCommandHandler : IRequestHandler<UpdateSettingCommand, Result<UpdateSettingCommandResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSettingCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database operations and transactions. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public UpdateSettingCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the update of an existing setting based on the provided command.
        /// </summary>
        /// <remarks>This method retrieves the setting by its key, updates its value, and persists the changes.
        /// If the update operation fails, the result will indicate failure with the
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
                    return Result<UpdateSettingCommandResponse>.Failure(string.Format(AppResources.Setting_Error_KeyNotFoundSpecific, request.Key));
                }

                var setting = settingResult.Data;
                setting.UpdateValue(request.Value);

                var updateResult = await _unitOfWork.Settings.UpdateAsync(setting, cancellationToken);

                if (!updateResult.IsSuccess || updateResult.Data == null)
                {
                    await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.DatabaseError, updateResult.ErrorMessage), cancellationToken);
                    return Result<UpdateSettingCommandResponse>.Failure(updateResult.ErrorMessage ?? AppResources.Setting_Error_UpdateFailed);
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
                return Result<UpdateSettingCommandResponse>.Failure(AppResources.Setting_Error_CannotUpdateReadOnly);
            }
            catch (Domain.Exceptions.SettingDomainException ex) when (ex.Code == "INVALID_VALUE")
            {
                await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.InvalidValue, ex.Message), cancellationToken);
                return Result<UpdateSettingCommandResponse>.Failure(AppResources.Setting_Error_InvalidValueProvided);
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new SettingErrorEvent(request.Key, SettingErrorType.DatabaseError, ex.Message), cancellationToken);
                return Result<UpdateSettingCommandResponse>.Failure(string.Format(AppResources.Setting_Error_UpdateFailedWithException, ex.Message));
            }
        }
    }
}