// Location.Photography.Application/Common/Interfaces/IPhoneCameraProfileRepository.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Entities;

namespace Location.Photography.Application.Common.Interfaces
{
    public interface IPhoneCameraProfileRepository
    {
        /// <summary>
        /// Creates a new phone camera profile
        /// </summary>
        Task<Result<PhoneCameraProfile>> CreateAsync(PhoneCameraProfile profile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the active phone camera profile
        /// </summary>
        Task<Result<PhoneCameraProfile>> GetActiveProfileAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a phone camera profile by ID
        /// </summary>
        Task<Result<PhoneCameraProfile>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all phone camera profiles
        /// </summary>
        Task<Result<List<PhoneCameraProfile>>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing phone camera profile
        /// </summary>
        Task<Result<PhoneCameraProfile>> UpdateAsync(PhoneCameraProfile profile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a phone camera profile
        /// </summary>
        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a profile as the active one (deactivates others)
        /// </summary>
        Task<Result<bool>> SetActiveProfileAsync(int profileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets profiles by phone model
        /// </summary>
        Task<Result<List<PhoneCameraProfile>>> GetByPhoneModelAsync(string phoneModel, CancellationToken cancellationToken = default);
    }
}