using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;

namespace Location.Core.Application.Queries.TipTypes
{
    /// <summary>
    /// Represents a query to retrieve all available tip types.
    /// </summary>
    /// <remarks>This query is used to request a list of all tip types in the system.  The result contains a
    /// collection of <see cref="TipTypeDto"/> objects,  encapsulated in a <see cref="Result{T}"/> wrapper to indicate
    /// success or failure.</remarks>
    public class GetAllTipTypesQuery : IRequest<Result<List<TipTypeDto>>>
    {
    }
    /// <summary>
    /// Handles the retrieval of all tip types.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetAllTipTypesQuery"/> request and retrieves all
    /// available tip types  from the underlying data source. The result is returned as a list of <see
    /// cref="TipTypeDto"/> objects.</remarks>
    public class GetAllTipTypesQueryHandler : IRequestHandler<GetAllTipTypesQuery, Result<List<TipTypeDto>>>
    {
        private readonly ITipTypeRepository _tipTypeRepository;
        /// <summary>
        /// Handles the query to retrieve all available tip types.
        /// </summary>
        /// <param name="tipTypeRepository">The repository used to access tip type data. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tipTypeRepository"/> is <see langword="null"/>.</exception>
        public GetAllTipTypesQueryHandler(ITipTypeRepository tipTypeRepository)
        {
            _tipTypeRepository = tipTypeRepository ?? throw new ArgumentNullException(nameof(tipTypeRepository));
        }
        /// <summary>
        /// Handles the retrieval of all tip types.
        /// </summary>
        /// <remarks>This method retrieves all tip types from the repository, maps them to data transfer
        /// objects (DTOs), and returns them as a successful result. If an error occurs during the operation, a failure
        /// result is returned with an appropriate error message.</remarks>
        /// <param name="request">The query request to retrieve all tip types.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="TipTypeDto"/> objects if the operation succeeds,
        /// or an error message if the operation fails.</returns>
        public async Task<Result<List<TipTypeDto>>> Handle(GetAllTipTypesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var tipTypes = await _tipTypeRepository.GetAllAsync(cancellationToken);

                var tipTypeDtos = tipTypes.Select(tt => new TipTypeDto
                {
                    Id = tt.Id,
                    Name = tt.Name,
                    I8n = tt.I8n
                }).ToList();

                return Result<List<TipTypeDto>>.Success(tipTypeDtos);
            }
            catch (Exception ex)
            {
                return Result<List<TipTypeDto>>.Failure($"Failed to retrieve tip types: {ex.Message}");
            }
        }
    }
}