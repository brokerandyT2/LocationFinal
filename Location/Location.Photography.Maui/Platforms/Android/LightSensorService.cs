#if ANDROID
using aa= Android.App;
using Android.Content;
using Android.Hardware;
using Microsoft.Maui.Controls;
using Location.Photography.Maui.Platforms.Android;
[assembly: Dependency(typeof(Location.Photography.Maui.Platforms.Android.LightSensorService))]
namespace Location.Photography.Maui.Platforms.Android
{
    public class LightSensorService : Java.Lang.Object, ISensorEventListener, ILightSensorService
    {
        private SensorManager sensorManager;
        private Sensor lightSensor;
        private float currentLux;

        private const double CalibrationConstant = 12.5; // Typical value for handheld meters


        public LightSensorService()
        {
            sensorManager = (SensorManager)aa.Application.Context.GetSystemService(Context.SensorService);
            lightSensor = sensorManager.GetDefaultSensor(SensorType.Light);
        }

        public void StartListening()
        {
            if (lightSensor != null)
            {
                sensorManager.RegisterListener(this, lightSensor, SensorDelay.Ui);
            }
        }

        public void StopListening()
        {
            if (lightSensor != null)
            {
                sensorManager.UnregisterListener(this, lightSensor);
            }
        }

        public float GetCurrentLux()
        {
            return currentLux;
        }

        public double GetCurrentEV(double aperture, int iso)
        {
            if (currentLux <= 0 || aperture <= 0 || iso <= 0)
                return 0;

            double ev = Math.Log((currentLux * aperture * aperture) / (CalibrationConstant * iso), 2);
            return ev;
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (e.Sensor.Type == SensorType.Light)
            {
                currentLux = e.Values[0]; // lux value
            }
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) { }
    }
}
#endif