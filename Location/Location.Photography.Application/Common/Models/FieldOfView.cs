namespace Location.Photography.Application.Common.Models
{
    public class FieldOfView
    {
        public class CameraInfo
        {
            public string Name { get; set; } = string.Empty;
            public SensorInfo Sensor { get; set; } = new SensorInfo();
            public List<LensInfo> Lenses { get; set; } = new List<LensInfo>();
        }

        public class SensorInfo
        {
            public double Width { get; set; }
            public double Height { get; set; }
        }

        public class LensInfo
        {
            public double FocalLength { get; set; }
            public double? MaxAperture { get; set; }
        }

        public class CameraDatabase
        {
            public Dictionary<string, CameraData> Cameras { get; set; } = new Dictionary<string, CameraData>();

            public class CameraData
            {
                public SensorInfo Sensor { get; set; } = new SensorInfo();
                public List<LensInfo> Lenses { get; set; } = new List<LensInfo>();
            }
        }
    }
}
