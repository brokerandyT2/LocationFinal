// Location.Core.Application/Tips/Queries/GetAllTipTypes/GetAllTipTypesQuery.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Tips.Queries.GetAllTipTypes
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
    /// <remarks>This query handler processes a <see cref="GetAllTipTypesQuery"/> and returns a result
    /// containing a list of  <see cref="TipTypeDto"/> objects. If the retrieval fails, the result will indicate failure
    /// with an appropriate error message.</remarks>
    public class GetAllTipTypesQueryHandler : IRequestHandler<GetAllTipTypesQuery, Result<List<TipTypeDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// Handles the query to retrieve all tip types.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to access the data store.  This parameter cannot be null.</param>
        public GetAllTipTypesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        /// <summary>
        /// Handles the retrieval of all tip types.
        /// </summary>
        /// <remarks>This method retrieves all tip types from the data source and maps them to <see
        /// cref="TipTypeDto"/> objects. If no tip types are found, or if an error occurs during retrieval, a failure
        /// result is returned.</remarks>
        /// <param name="request">The query request to retrieve all tip types.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="TipTypeDto"/> objects if the operation succeeds;
        /// otherwise, a failure result with an error message.</returns>
        public async Task<Result<List<TipTypeDto>>> Handle(GetAllTipTypesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _unitOfWork.TipTypes.GetAllAsync(cancellationToken);

                if (result == null)
                {
                    return Result<List<TipTypeDto>>.Failure("Failed to retrieve tip types");
                }

                var tipTypeDtos = result.Select(t => new TipTypeDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    I8n = t.I8n
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