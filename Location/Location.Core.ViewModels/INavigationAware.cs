namespace Location.Core.ViewModels
{
    public interface INavigationAware
    {
        void OnNavigatedToAsync();
        void OnNavigatedFromAsync();
    }
}
