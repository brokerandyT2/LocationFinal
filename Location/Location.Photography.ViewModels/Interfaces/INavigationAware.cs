namespace Location.Photography.ViewModels.Interfaces
{
    public interface INavigationAware
    {
        void OnNavigatedToAsync();
        void OnNavigatedFromAsync();
    }
}
