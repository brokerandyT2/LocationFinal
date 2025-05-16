using Location.Core.Application.Services;
using Location.Core.Maui.Services;
using MediatR;

namespace Location.Core.Maui
{
    public partial class MainPage : TabbedPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
           
        }
        public MainPage(
            IMediator mediator,
            IAlertService alertService,
            INavigationService navigationService,
            IMediaService mediaService,
            IGeolocationService geolocationService)
        {
            this.Children.Add(new Views.AddLocation());
            this.Children.Add(new Views.LocationsPage(mediator, alertService, navigationService, mediaService, geolocationService));
            this.Children.Add(new Views.TipsPage(mediator, alertService));
        }



    }
}
