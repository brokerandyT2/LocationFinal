
namespace Location.Photography.Maui;

public partial class SubscriptionSignUpPage : ContentPage
{
    private IServiceProvider serviceProvider;

    public SubscriptionSignUpPage()
	{
		InitializeComponent();
	}

    public SubscriptionSignUpPage(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
}