// Location.Photography.Application/Services/ICameraSensorProfileService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.CameraEvaluation;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public interface ICameraSensorProfileService
    {
        /// <summary>
        /// Loads camera sensor profiles from JSON files in Resources/CameraSensorProfiles
        /// </summary>
        Task<Result<List<CameraBodyDto>>> LoadCameraSensorProfilesAsync(List<string> jsonContents, CancellationToken cancellationToken = default);
    }
}