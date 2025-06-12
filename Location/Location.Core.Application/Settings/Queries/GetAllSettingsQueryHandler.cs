using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Settings.Queries.GetAllSettings
{
    /// <summary>
    /// Handles the query to retrieve all settings from the data source.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetAllSettingsQuery"/> and returns a result containing a
    /// list of  <see cref="GetAllSettingsQueryResponse"/> objects. If the retrieval operation fails, the result will 
    /// indicate failure with an appropriate error message.</remarks>
    public class GetAllSettingsQueryHandler : IRequestHandler<GetAllSettingsQuery, Result<List<GetAllSettingsQueryResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Handles the query to retrieve all application settings.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to access the data store.  This parameter cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public GetAllSettingsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Handles the query to retrieve all settings from the data source.
        /// </summary>
        /// <remarks>This method retrieves all settings from the underlying data source and maps them to
        /// <see cref="GetAllSettingsQueryResponse"/> objects. If the retrieval fails or no data is available, the
        /// method returns a failure result with an appropriate error message.</remarks>
        /// <param name="request">The query request containing the parameters for retrieving settings.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="GetAllSettingsQueryResponse"/> objects if the
        /// operation is successful; otherwise, a failure result with an error message.</returns>
        public async Task<Result<List<GetAllSettingsQueryResponse>>> Handle(GetAllSettingsQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Settings.GetAllAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<List<GetAllSettingsQueryResponse>>.Failure(result.ErrorMessage ?? AppResources.Setting_Error_RetrieveFailed);
            }

            var settings = result.Data;

            var response = settings.Select(setting => new GetAllSettingsQueryResponse
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Timestamp = setting.Timestamp
            }).ToList();

            return Result<List<GetAllSettingsQueryResponse>>.Success(response);
        }
    }
}