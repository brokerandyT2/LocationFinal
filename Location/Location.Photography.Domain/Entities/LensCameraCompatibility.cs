using Location.Core.Helpers.CodeGenerationAttributes;
using SQLite;

namespace Location.Photography.Domain.Entities
{
    [Table("LensCameraCompatibility")]
    [ExportToSQL]
    public class LensCameraCompatibility
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int LensId { get; set; }

        [Indexed]
        public int CameraBodyId { get; set; }

        public DateTime DateAdded { get; set; }

        public LensCameraCompatibility()
        {
            DateAdded = DateTime.UtcNow;
        }

        public LensCameraCompatibility(int lensId, int cameraBodyId)
        {
            LensId = lensId;
            CameraBodyId = cameraBodyId;
            DateAdded = DateTime.UtcNow;
        }
    }
}