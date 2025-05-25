using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Location.Photography.Maui
{
    public partial class SubscriptionSignUpPage : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAlertService _alertService;
        private readonly ILogger<SubscriptionSignUpPage> _logger;
        private SubscriptionSignUpViewModel _viewModel;

        public SubscriptionSignUpPage()
        {
            InitializeComponent();
        }

        public SubscriptionSignUpPage(
            IServiceProvider serviceProvider,
            IAlertService alertService,
            ILogger<SubscriptionSignUpPage> logger,
            SubscriptionSignUpViewModel viewModel)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
            BindingContext = _viewModel;

            // Subscribe to view model events
            _viewModel.SubscriptionCompleted += OnSubscriptionCompleted;
            _viewModel.NotNowSelected += OnNotNowSelected;
            _viewModel.ErrorOccurred += OnErrorOccurred;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            try
            {
                if (_viewModel != null && !_viewModel.IsInitialized)
                {
                    await _viewModel.InitializeCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during page navigation");
                await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from events to prevent memory leaks
            if (_viewModel != null)
            {
                _viewModel.SubscriptionCompleted -= OnSubscriptionCompleted;
                _viewModel.NotNowSelected -= OnNotNowSelected;
                _viewModel.ErrorOccurred -= OnErrorOccurred;
            }
        }

        private async void OnSubscriptionCompleted(object sender, EventArgs e)
        {
            try
            {
                await NavigateToMainPageAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating after subscription completion");
                await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
            }
        }

        private async void OnNotNowSelected(object sender, EventArgs e)
        {
            try
            {
                await NavigateToMainPageAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating after 'Not Now' selection");
                await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
            }
        }

        private void OnErrorOccurred(object sender, Location.Photography.ViewModels.Events.OperationErrorEventArgs e)
        {
            _logger.LogWarning("Subscription error occurred: {Message}", e.Message);
        }

        private async Task NavigateToMainPageAsync()
        {
            try
            {
                // Navigate to MainPage - you'll need to create this or specify the correct main page
                var mainPage = _serviceProvider.GetService(typeof(MainPage)) as Page;
                if (mainPage != null)
                {
                    await Navigation.PushAsync(mainPage);
                }
                else
                {
                    // Fallback navigation
                    await Shell.Current.GoToAsync("//MainPage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to navigate to main page");
                await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
            }
        }
    }
}