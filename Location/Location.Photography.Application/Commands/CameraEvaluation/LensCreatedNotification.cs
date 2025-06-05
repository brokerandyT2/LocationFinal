using Location.Photography.Application.Commands.CameraEvaluation;
using MediatR;

namespace Location.Photography.Application.Notifications
{
    public class LensCreatedNotification : INotification
    {
        public LensDto CreatedLens { get; }
        public string UserId { get; }

        public LensCreatedNotification(LensDto createdLens, string userId)
        {
            CreatedLens = createdLens ?? throw new ArgumentNullException(nameof(createdLens));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }
    }
}