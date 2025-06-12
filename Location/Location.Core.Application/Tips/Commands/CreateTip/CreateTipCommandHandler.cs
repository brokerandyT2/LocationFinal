using AutoMapper;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Resources;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Commands.CreateTip
{
    /// <summary>
    /// Handles the creation of a new tip by processing a <see cref="CreateTipCommand"/> request.
    /// </summary>
    /// <remarks>This handler creates a new tip entity, sets its photography parameters and localization data,
    /// and persists it to the repository. If the operation is successful, a <see cref="TipDto"/> representing the
    /// created tip is returned. In case of failure, an error message is included in the result.</remarks>
    public class CreateTipCommandHandler : IRequestHandler<CreateTipCommand, Result<List<TipDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        /// <summary>
        /// Handles the creation of new tips by processing the associated command.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database operations. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="mapper">The mapper used to convert between domain entities and DTOs.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the required parameters are <see langword="null"/>.</exception>
        public CreateTipCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IMediator mediator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the creation of a new tip and returns the result.
        /// </summary>
        /// <remarks>This method creates a new tip entity, sets its photography metadata, persists it to the
        /// repository, and maps it to a data transfer object (DTO). If an error occurs during the process, the method
        /// returns a failure result with the error message.</remarks>
        /// <param name="request">The command containing the details of the tip to create, including title, content, and photography settings.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list with the created <see cref="TipDto"/> if the operation succeeds,
        /// or an error message if the operation fails.</returns>
        public async Task<Result<List<TipDto>>> Handle(CreateTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tip = new Domain.Entities.Tip(request.TipTypeId, request.Title, request.Content);

                // Set photography parameters if provided
                if (!string.IsNullOrEmpty(request.Fstop) || !string.IsNullOrEmpty(request.ShutterSpeed) || !string.IsNullOrEmpty(request.Iso))
                {
                    tip.UpdatePhotographySettings(
                        request.Fstop ?? string.Empty,
                        request.ShutterSpeed ?? string.Empty,
                        request.Iso ?? string.Empty);
                }

                if (!string.IsNullOrEmpty(request.I8n))
                {
                    tip.SetLocalization(request.I8n);
                }

                var result = await _unitOfWork.Tips.CreateAsync(tip, cancellationToken);

                if (!result.IsSuccess)
                {
                    await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Database(result.ErrorMessage ?? AppResources.Tip_Error_CreateFailed) }, "CreateTipCommandHandler"), cancellationToken);
                    return Result<List<TipDto>>.Failure(result.ErrorMessage);
                }

                var createdTip = result.Data;

                // Important: Return the correct ID from the created entity
                var tipDto = new TipDto
                {
                    Id = createdTip.Id, // Ensure ID is copied correctly
                    TipTypeId = createdTip.TipTypeId,
                    Title = createdTip.Title,
                    Content = createdTip.Content,
                    Fstop = createdTip.Fstop,
                    ShutterSpeed = createdTip.ShutterSpeed,
                    Iso = createdTip.Iso,
                    I8n = createdTip.I8n
                };

                return Result<List<TipDto>>.Success(new List<TipDto> { tipDto });
            }
            catch (Domain.Exceptions.TipDomainException ex) when (ex.Code == "DUPLICATE_TITLE")
            {
                await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Validation("Title", AppResources.Tip_Error_DuplicateTitle) }, "CreateTipCommandHandler"), cancellationToken);
                return Result<List<TipDto>>.Failure(string.Format(AppResources.Tip_Error_DuplicateTitle, request.Title));
            }
            catch (Domain.Exceptions.TipDomainException ex) when (ex.Code == "INVALID_TIP_TYPE")
            {
                await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Validation("TipTypeId", AppResources.Tip_Error_InvalidTipType) }, "CreateTipCommandHandler"), cancellationToken);
                return Result<List<TipDto>>.Failure(AppResources.Tip_Error_InvalidTipType);
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Domain(ex.Message) }, "CreateTipCommandHandler"), cancellationToken);
                return Result<List<TipDto>>.Failure(string.Format(AppResources.Tip_Error_CreateFailed, ex.Message));
            }
        }
    }
}