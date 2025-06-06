using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Tips.Commands.CreateTip
{
    /// <summary>
    /// Handles the creation of a new tip and returns the result as a <see cref="Result{T}"/> containing a <see
    /// cref="TipDto"/>.
    /// </summary>
    /// <remarks>This handler processes a <see cref="CreateTipCommand"/> to create a new tip entity.  It
    /// validates and applies optional photography settings and localization data if provided in the request. The
    /// created tip is persisted using the provided <see cref="IUnitOfWork"/> and returned as a data transfer object
    /// (DTO).</remarks>
    public class CreateTipCommandHandler : IRequestHandler<CreateTipCommand, Result<List<TipDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTipCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to manage database transactions and operations.  This parameter cannot be
        /// <see langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public CreateTipCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator;
        }
        /// <summary>
        /// Handles the creation of a new tip based on the provided command and returns the result.
        /// </summary>
        /// <remarks>This method creates a new tip entity, optionally updates its photography settings and
        /// localization, and persists it to the data store. If the creation is successful, the method returns a DTO
        /// representing the created tip.</remarks>
        /// <param name="request">The command containing the details of the tip to be created, including its type, title, content, and
        /// optional settings.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="TipDto"/> representing the created tip if the operation is
        /// successful; otherwise, a failure result with an error message.</returns>
        public async Task<Result<List<TipDto>>> Handle(CreateTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tip = new Domain.Entities.Tip(
                    request.TipTypeId,
                    request.Title,
                    request.Content);

                if (!string.IsNullOrEmpty(request.Fstop) ||
                    !string.IsNullOrEmpty(request.ShutterSpeed) ||
                    !string.IsNullOrEmpty(request.Iso))
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
                    await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Database(result.ErrorMessage ?? "Failed to create tip") }, "CreateTipCommandHandler"), cancellationToken);
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
                await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Validation("Title", "Tip with this title already exists") }, "CreateTipCommandHandler"), cancellationToken);
                return Result<List<TipDto>>.Failure($"Tip with title '{request.Title}' already exists");
            }
            catch (Domain.Exceptions.TipDomainException ex) when (ex.Code == "INVALID_TIP_TYPE")
            {
                await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Validation("TipTypeId", "Invalid tip type") }, "CreateTipCommandHandler"), cancellationToken);
                return Result<List<TipDto>>.Failure("Invalid tip type specified");
            }
            catch (Exception ex)
            {
                await _mediator.Publish(new TipValidationErrorEvent(null, request.TipTypeId, new[] { Error.Domain(ex.Message) }, "CreateTipCommandHandler"), cancellationToken);
                return Result<List<TipDto>>.Failure($"Failed to create tip: {ex.Message}");
            }
        }
    }
}