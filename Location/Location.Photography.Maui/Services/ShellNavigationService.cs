public class ShellNavigationService
{
    public void Initialize()
    {
        Shell.Current.Navigated += OnShellNavigated;
    }

    private async void OnShellNavigated(object sender, ShellNavigatedEventArgs e)
    {
        try
        {
            var page = GetCurrentPage();
            if (page?.BindingContext is Location.Photography.ViewModels.Interfaces.INavigationAware aware)
            {
                aware.OnNavigatedToAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigationService error: {ex}");
        }
    }

    private static Page? GetCurrentPage()
    {
        var shell = Shell.Current;
        var page = shell?.CurrentPage;

        while (page is NavigationPage nav)
            page = nav.CurrentPage;

        if (page is TabbedPage tabbedPage)
            page = tabbedPage.CurrentPage;

        return page;
    }
}
