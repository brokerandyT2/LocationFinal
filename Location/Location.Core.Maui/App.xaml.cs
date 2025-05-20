namespace Location.Core.Maui
{
    /// <summary>
    /// Base application class that can be inherited by vertical-specific apps
    /// </summary>
    public abstract class LocationAppBase : Microsoft.Maui.Controls.Application
    {
        protected LocationAppBase()
        {
            // Any shared initialization can go here
            // Note: InitializeComponent is removed as it's specific to each implementing app
            var x = string.Empty;
        }

        /// <summary>
        /// Provides a default implementation for creating the main window
        /// Consuming apps can override this if needed
        /// </summary>
        protected virtual Window CreateLocationWindow(IActivationState? activationState)
        {
            // This is just a base implementation
            // Your vertical apps should override this with their specific MainPage
            return new Window(new MainPage());
        }
    }

    /// <summary>
    /// Optional helper class if you want to provide a default shell configuration
    /// </summary>
    public static class LocationAppHelper
    {
        /// <summary>
        /// Creates a standard location shell configuration that can be used by vertical apps
        /// </summary>
        public static Shell CreateLocationShell()
        {
            // Example of a helper method that could create a standard shell 
            // with tabs for common location features
            var shell = new Shell();

            // Set up shell items, routes, etc.
            // shell.Items.Add(new ShellContent { Content = new LocationsPage() });

            return shell;
        }
    }
}