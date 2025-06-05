using Location.Photography.Application.Commands.CameraEvaluation;
using MediatR;

namespace Location.Photography.Application.Notifications
{
    public class CameraCreatedNotification : INotification
    {
        public CameraBodyDto CreatedCamera { get; }
        public string UserId { get; }

        public CameraCreatedNotification(CameraBodyDto createdCamera, string userId)
        {
            CreatedCamera = createdCamera ?? throw new ArgumentNullException(nameof(createdCamera));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }
    }
}