using Location.Core.Application.Services;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Maui.Views
{
    public partial class LocationsPage : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly INavigationService _navigationService;
        private readonly IMediaService _mediaService;
        private readonly IGeolocationService _geolocationService;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public LocationsPage(
            IMediator mediator,
            IAlertService alertService,
            INavigationService navigationService,
            IMediaService mediaService,
            IGeolocationService geolocationService)
        {
            InitializeComponent();

            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));

            // Initialize the view model
            var viewModel = new LocationsViewModel(_mediator, _alertService);
            viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            BindingContext = viewModel;
        }
        // Parameterless constructor marked as obsolete to prevent usage
        

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Refresh locations whenever the page appears
            if (BindingContext is LocationsViewModel viewModel)
            {
                await viewModel.LoadLocationsCommand.ExecuteAsync(_cts.Token);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is LocationsViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
            }

            // Cancel any pending operations
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
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

        private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
        {
            // Display error to user if it's not already displayed in the UI
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _alertService.ShowErrorAlertAsync(e.Message, "Error");
            });
        }

        private void ImageButton_Pressed(object sender, EventArgs e)
        {
            var LocationID = ((LocationViewModel)sender).Id;
            
        }
    }
}