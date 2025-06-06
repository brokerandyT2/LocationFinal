using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Commands.Tips
{
    /// <summary>
    /// Represents a command to retrieve a random tip of a specified type.
    /// </summary>
    /// <remarks>This command is used to request a random tip based on the provided tip type identifier. The
    /// result of the command is encapsulated in a <see cref="Result{T}"/> object containing a <see
    /// cref="TipDto"/>.</remarks>
    public class GetRandomTipCommand : IRequest<Result<TipDto>>
    {
        public int TipTypeId { get; set; }
    }
    /// <summary>
    /// Handles the execution of the <see cref="GetRandomTipCommand"/> to retrieve a random tip of a specified type.
    /// </summary>
    /// <remarks>This handler interacts with the data layer to fetch a random tip based on the provided tip
    /// type identifier. If no tips are found or an error occurs during retrieval, an appropriate failure result is
    /// returned.</remarks>
    public class GetRandomTipCommandHandler : IRequestHandler<GetRandomTipCommand, Result<TipDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetRandomTipCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to interact with the data layer.  This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public GetRandomTipCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        /// <summary>
        /// Handles the retrieval of a random tip based on the specified tip type.
        /// </summary>
        /// <param name="request">The command containing the tip type identifier for which a random tip is requested.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="TipDto"/> if a tip is successfully retrieved; otherwise, a
        /// failure result with an appropriate error message.</returns>
        public async Task<Result<TipDto>> Handle(GetRandomTipCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _unitOfWork.Tips.GetRandomByTypeAsync(request.TipTypeId, cancellationToken);

                if (!result.IsSuccess || result.Data == null)
                {
                    return Result<TipDto>.Failure(result.ErrorMessage ?? "No tips found for the specified type");
                }

                var tip = result.Data;
                var tipDto = new TipDto
                {
                    Id = tip.Id,
                    TipTypeId = tip.TipTypeId,
                    Title = tip.Title,
                    Content = tip.Content,
                    Fstop = tip.Fstop,
                    ShutterSpeed = tip.ShutterSpeed,
                    Iso = tip.Iso,
                    I8n = tip.I8n
                };

                return Result<TipDto>.Success(tipDto);
            }
            catch (Exception ex)
            {
                return Result<TipDto>.Failure($"Failed to retrieve random tip: {ex.Message}");
            }
        }
    }
}