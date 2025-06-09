using Android.Runtime;

namespace Location.Core.Maui
{

    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => Location.Core.Maui.MauiProgram.CreateMauiApp();
    }
}