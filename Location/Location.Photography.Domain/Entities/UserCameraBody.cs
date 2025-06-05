// Location.Photography.Domain/Entities/UserCameraBody.cs
namespace Location.Photography.Domain.Entities
{
    public class UserCameraBody 
    {
        public int CameraBodyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime DateSaved { get; set; }
        public bool IsFavorite { get; set; }
        public string? Notes { get; set; }

        // Navigation property
        public CameraBody? CameraBody { get; set; }

        public UserCameraBody()
        {
            DateSaved = DateTime.UtcNow;
        }

        public UserCameraBody(int cameraBodyId, string userId, bool isFavorite = false, string? notes = null)
        {
            CameraBodyId = cameraBodyId;
            UserId = userId;
            IsFavorite = isFavorite;
            Notes = notes;
            DateSaved = DateTime.UtcNow;
        }

        public void SetAsFavorite()
        {
            IsFavorite = true;
        }

        public void RemoveFromFavorites()
        {
            IsFavorite = false;
        }

        public void UpdateNotes(string? notes)
        {
            Notes = notes;
        }
    }
}