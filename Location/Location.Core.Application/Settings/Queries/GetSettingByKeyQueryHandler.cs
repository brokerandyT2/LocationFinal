using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Settings.Queries.GetSettingByKey
{
    /// <summary>
    /// Handles the query to retrieve a setting by its key.
    /// </summary>
    /// <remarks>This handler processes a <see cref="GetSettingByKeyQuery"/> and retrieves the corresponding
    /// setting from the data store using the provided key. If the setting is found, it returns a successful result
    /// containing the setting details. If the setting is not found or an error occurs, it returns a failure result with
    /// an appropriate error message.</remarks>
    public class GetSettingByKeyQueryHandler : IRequestHandler<GetSettingByKeyQuery, Result<GetSettingByKeyQueryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetSettingByKeyQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance used to interact with the data store.  This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="unitOfWork"/> is <see langword="null"/>.</exception>
        public GetSettingByKeyQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Handles the query to retrieve a setting by its key.
        /// </summary>
        /// <param name="request">The query containing the key of the setting to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Result{T}"/> containing a <see cref="GetSettingByKeyQueryResponse"/> if the setting is found;
        /// otherwise, a failure result with an appropriate error message.</returns>
        public async Task<Result<GetSettingByKeyQueryResponse>> Handle(GetSettingByKeyQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.Settings.GetByKeyAsync(request.Key, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Result<GetSettingByKeyQueryResponse>.Failure(result.ErrorMessage ?? string.Format(AppResources.Setting_Error_KeyNotFoundSpecific, request.Key));
            }

            var setting = result.Data;

            var response = new GetSettingByKeyQueryResponse
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Timestamp = setting.Timestamp
            };

            return Result<GetSettingByKeyQueryResponse>.Success(response);
        }
    }
}