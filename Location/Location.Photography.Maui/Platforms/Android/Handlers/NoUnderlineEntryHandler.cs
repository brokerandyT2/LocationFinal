using Android.Graphics.Drawables;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using Microsoft.Maui.Handlers;

namespace Location.Photography.Maui.Platforms.Android.Handlers
{
    public class NoUnderlineEntryHandler : EntryHandler
    {
        protected override void ConnectHandler(AppCompatEditText platformView)
        {
            base.ConnectHandler(platformView);

            // Remove underline/focus indicator
            platformView.Background = new ColorDrawable(global::Android.Graphics.Color.Transparent);

            // Set cursor color to white
            try
            {
                var cursorDrawableRes = platformView.Resources?.GetIdentifier("android:attr/editTextCursorDrawable", null, null);
                if (cursorDrawableRes != null && cursorDrawableRes != 0)
                {
                    var cursorDrawable = ContextCompat.GetDrawable(platformView.Context, cursorDrawableRes.Value);
                    cursorDrawable?.SetColorFilter(global::Android.Graphics.Color.White, global::Android.Graphics.PorterDuff.Mode.SrcIn);
                }
            }
            catch
            {
                // Fallback - cursor color might not be changeable on all Android versions
            }
        }
    }
}