// Location.Photography.Domain/Entities/UserCameraBody.cs
using Location.Core.Helpers.CodeGenerationAttributes;
using SQLite;

namespace Location.Photography.Domain.Entities
{
    [Table("UserCameraBodies")]
    [ExportToSQL]
    public class UserCameraBody
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int CameraBodyId { get; set; }

        [MaxLength(100), NotNull, Indexed]
        public string UserId { get; set; } = string.Empty;

        [NotNull]
        public DateTime DateSaved { get; set; }

        public bool IsFavorite { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation property - not stored in database
        [Ignore]
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