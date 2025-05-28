namespace Location.Photography.Maui.Views.Premium;

public partial class DummyPage : ContentPage
{
    private IServiceProvider _serviceProvider;
    private bool _modalAppeared = false;
    public DummyPage()
    {
        _modalAppeared = false;
    }
    public DummyPage(IServiceProvider service) : this()
    {
        _serviceProvider = service;
        InitializeComponent();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_modalAppeared)
        {
            using var scope = _serviceProvider.CreateScope();

            var modal = scope.ServiceProvider.GetRequiredService<MainPage>();
            Navigation.PushAsync(modal);
            _modalAppeared = false;
        }
    }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        using var scope = _serviceProvider.CreateScope();

        var modal = scope.ServiceProvider.GetRequiredService<LightMeter>();
        if (_modalAppeared == false)
        {
            Navigation.PushModalAsync(modal);
            _modalAppeared = true;
        }
        else { 
        //Navigation.PushAsync(scope.ServiceProvider.GetRequiredService<MainPage>());
        }

        //Navigation.PushModalAsync(new LightMeter(), true);
    }
}