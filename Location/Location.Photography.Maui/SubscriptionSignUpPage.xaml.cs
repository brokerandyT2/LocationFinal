// Location.Photography.Maui/SubscriptionSignUpPage.xaml.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using Microsoft.Extensions.Logging;

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
            _viewModel = new SubscriptionSignUpViewModel();
            BindingContext = _viewModel;
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
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            try
            {
                if (_viewModel != null)
                {
                    _viewModel.SubscriptionCompleted -= OnSubscriptionCompleted;
                    _viewModel.NotNowSelected -= OnNotNowSelected;
                    _viewModel.ErrorOccurred -= OnSystemError;

                    _viewModel.SubscriptionCompleted += OnSubscriptionCompleted;
                    _viewModel.NotNowSelected += OnNotNowSelected;
                    _viewModel.ErrorOccurred += OnSystemError;

                    if (!_viewModel.IsInitialized)
                    {
                        await _viewModel.InitializeCommand.ExecuteAsync(null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during page navigation");
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
                }
                else
                {
                    await DisplayAlert("Error", "There was an error processing your request, please try again", "OK");
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel != null)
            {
                _viewModel.SubscriptionCompleted -= OnSubscriptionCompleted;
                _viewModel.NotNowSelected -= OnNotNowSelected;
                _viewModel.ErrorOccurred -= OnSystemError;
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
                _logger?.LogError(ex, "Error navigating after subscription completion");
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
                }
                else
                {
                    await DisplayAlert("Error", "There was an error processing your request, please try again", "OK");
                }
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
                _logger?.LogError(ex, "Error navigating after 'Not Now' selection");
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
                }
                else
                {
                    await DisplayAlert("Error", "There was an error processing your request, please try again", "OK");
                }
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            _logger?.LogWarning("Subscription error occurred: {Message}", e.Message);

            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is SubscriptionSignUpViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private async Task NavigateToMainPageAsync()
        {
            try
            {
                if (_serviceProvider != null)
                {
                    var mainPage = _serviceProvider.GetService(typeof(App)) as Page;
                    if (mainPage != null)
                    {
                        await Navigation.PushAsync(mainPage);
                        return;
                    }
                }
                var redirect = _serviceProvider.GetRequiredService<AppShell>();
                Microsoft.Maui.Controls.Application.Current.MainPage = redirect;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to navigate to main page");
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
                }
                else
                {
                    await DisplayAlert("Error", "There was an error processing your request, please try again", "OK");
                }
            }
        }
    }
}