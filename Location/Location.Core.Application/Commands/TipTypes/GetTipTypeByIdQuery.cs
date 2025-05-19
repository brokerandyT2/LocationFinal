using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;
namespace Location.Core.Application.Queries.TipTypes
{
/// <summary>
/// Represents a query to retrieve a specific tip type by its unique identifier.
/// </summary>
/// <remarks>This query is used to request a <see cref="TipTypeDto"/> object corresponding to the specified ID.
/// The result will indicate whether the operation was successful and, if so, will contain the requested data.</remarks>
    public class GetTipTypeByIdQuery : IRequest<Result<TipTypeDto>>
    {
        public int Id { get; set; }
    }
    /// <summary>
    /// Handles queries to retrieve a tip type by its unique identifier.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetTipTypeByIdQuery"/> and retrieves the corresponding
    /// tip type from the repository. If the tip type is not found, a failure result is returned. If an error occurs
    /// during processing, a failure result with the error message is returned.</remarks>
    public class GetTipTypeByIdQueryHandler : IRequestHandler<GetTipTypeByIdQuery, Result<TipTypeDto>>
    {
        private readonly ITipTypeRepository _tipTypeRepository;
        /// <summary>
        /// Handles queries to retrieve a tip type by its unique identifier.
        /// </summary>
        /// <param name="tipTypeRepository">The repository used to access tip type data. This parameter cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tipTypeRepository"/> is <see langword="null"/>.</exception>
        public GetTipTypeByIdQueryHandler(ITipTypeRepository tipTypeRepository)
        {
            _tipTypeRepository = tipTypeRepository ?? throw new ArgumentNullException(nameof(tipTypeRepository));
        }
        /// <summary>
        /// Handles the retrieval of a tip type by its unique identifier.
        /// </summary>
        /// <remarks>If the tip type with the specified ID does not exist, the method returns a failure
        /// result with a message indicating that the tip type was not found. In case of an unexpected error, the method
        /// returns a failure result with the error details.</remarks>
        /// <param name="request">The query containing the ID of the tip type to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="TipTypeDto"/> if the tip type is found; otherwise, a
        /// failure result with an appropriate error message.</returns>
        public async Task<Result<TipTypeDto>> Handle(GetTipTypeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var tipType = await _tipTypeRepository.GetByIdAsync(request.Id, cancellationToken);

                if (tipType == null)
                {
                    return Result<TipTypeDto>.Failure($"Tip type with ID {request.Id} not found");
                }

                var tipTypeDto = new TipTypeDto
                {
                    Id = tipType.Id,
                    Name = tipType.Name,
                    I8n = tipType.I8n
                };

                return Result<TipTypeDto>.Success(tipTypeDto);
            }
            catch (Exception ex)
            {
                return Result<TipTypeDto>.Failure($"Failed to retrieve tip type: {ex.Message}");
            }
        }
    }
}