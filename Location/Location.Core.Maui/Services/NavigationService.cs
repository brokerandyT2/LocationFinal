using Microsoft.Maui.Controls;

namespace Location.Core.Maui.Services
{

    public interface INavigationService
    {
        Task NavigateToAsync(string route, IDictionary<string, object> parameters = null);
        Task NavigateBackAsync();
        Task NavigateToModalAsync(Page page);
        Task NavigateBackModalAsync();
    }

    public class NavigationService : INavigationService
    {
        public async Task NavigateToAsync(string route, IDictionary<string, object> parameters = null)
        {
            await Shell.Current.GoToAsync(route, parameters ?? new Dictionary<string, object>());
        }

        public async Task NavigateBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        public async Task NavigateToModalAsync(Page page)
        {
            await Microsoft.Maui.Controls.Application.Current.MainPage.Navigation.PushModalAsync(page);
        }

        public async Task NavigateBackModalAsync()
        {
            await Microsoft.Maui.Controls.Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}