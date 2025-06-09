using Location.Core.Application.Common.Interfaces.Persistence;
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
            IGeolocationService geolocationService, ITipRepository tipRepo, ITipTypeRepository tiptype, IErrorDisplayService errorDisplayService, IWeatherService weatherService)
        {
            this.Children.Add(new Views.AddLocation());
            this.Children.Add(new Views.LocationsPage(mediator, navigationService, mediaService, geolocationService, errorDisplayService, weatherService));
            this.Children.Add(new Views.TipsPage(mediator, errorDisplayService, tipRepo, tiptype));
        }



    }
}
