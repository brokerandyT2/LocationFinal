using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Handlers;

namespace Location.Photography.Maui.Platforms.Android.Handlers
{
    public class CustomEntryHandler : EntryHandler
    {
        protected override void ConnectHandler(AppCompatEditText platformView)
        {
            base.ConnectHandler(platformView);

            // Remove the underline completely
            platformView.Background = null;
            platformView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
        }
    }
}