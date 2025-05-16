using Foundation;

namespace Location.Core.Maui.Platforms.MacCatalyst
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => Location.Core.Maui.MauiProgram.CreateMauiApp();
    }
}