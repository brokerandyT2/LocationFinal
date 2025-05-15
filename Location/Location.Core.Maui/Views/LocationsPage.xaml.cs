using Location.Core.Application.Services;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using MediatR;

namespace Location.Core.Maui.Views
{
    public partial class LocationsPage : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly INavigationService _navigationService;
        private readonly IMediaService _mediaService;
        private readonly IGeolocationService _geolocationService;

        public LocationsPage(
            IMediator mediator,
            IAlertService alertService,
            INavigationService navigationService,
            IMediaService mediaService,
            IGeolocationService geolocationService)
        {
            InitializeComponent();

            _mediator = mediator;
            _alertService = alertService;
            _navigationService = navigationService;
            _mediaService = mediaService;
            _geolocationService = geolocationService;

            // Initialize the view model
            BindingContext = new LocationsViewModel(_mediator);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Refresh locations whenever the page appears
            if (BindingContext is LocationsViewModel viewModel)
            {
                await viewModel.LoadLocationsCommand.ExecuteAsync(null);
            }
        }

        private async void OnAddLocationClicked(object sender, EventArgs e)
        {
            var page = new AddLocation(
                _mediator,
                _mediaService,
                _geolocationService,
                _alertService);

            await _navigationService.NavigateToModalAsync(page);
        }

        private async void OnLocationSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is LocationListItemViewModel selectedItem)
            {
                // Clear selection
                ((CollectionView)sender).SelectedItem = null;

                // Navigate to detail page
                var page = new AddLocation(
                    _mediator,
                    _mediaService,
                    _geolocationService,
                    _alertService,
                    selectedItem.Id,
                    true);

                await _navigationService.NavigateToModalAsync(page);
            }
        }
    }
}