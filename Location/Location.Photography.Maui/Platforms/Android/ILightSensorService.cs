namespace Location.Photography.Maui.Platforms.Android
{
    public interface ILightSensorService
    {
        void StartListening();
        void StopListening();
        float GetCurrentLux();
    }
}
