using Location.Core.Application.Services;
using Location.Core.Maui.Services;
using Location.Core.Maui.Resources;
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
        private readonly INavigationService _navigationService;
        private readonly IMediaService _mediaService;
        private readonly IGeolocationService _geolocationService;
        private readonly IErrorDisplayService _errorDisplayService;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public LocationsPage(
            IMediator mediator,
            INavigationService navigationService,
            IMediaService mediaService,
            IGeolocationService geolocationService,
            IErrorDisplayService errorDisplayService)
        {
            InitializeComponent();

            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            // Initialize the view model
            var viewModel = new LocationsViewModel(_mediator, _errorDisplayService);
            viewModel.ErrorOccurred += OnSystemError;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Re-subscribe to ViewModel events in case the binding context changed
            if (BindingContext is LocationsViewModel viewModel)
            {
                viewModel.ErrorOccurred -= OnSystemError;
                viewModel.ErrorOccurred += OnSystemError;

                // Refresh locations whenever the page appears
                await viewModel.ExecuteAndTrackAsync(viewModel.LoadLocationsCommand, _cts.Token);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is LocationsViewModel viewModel)
            {
                viewModel.ErrorOccurred -= OnSystemError;
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
                _errorDisplayService);

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
                    _errorDisplayService,
                    selectedItem.Id,
                    true);

                await _navigationService.NavigateToModalAsync(page);
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert(
                AppResources.Error,
                $"{e.Message}. Click OK to try again.",
                AppResources.OK,
                AppResources.Cancel);

            if (retry && sender is LocationsViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private void ImageButton_Pressed(object sender, EventArgs e)
        {
            // Handle map button press if needed
        }
    }
}