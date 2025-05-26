// Location.Photography.Maui/Views/Professional/SunCalculator.xaml.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class SunCalculator : ContentPage
    {
        private readonly SunCalculatorViewModel _viewModel;
        private readonly IAlertService _alertService;

        public SunCalculator()
        {
            InitializeComponent();
            _viewModel = new SunCalculatorViewModel(null, null);
            BindingContext = _viewModel;
        }

        public SunCalculator(SunCalculatorViewModel viewModel, IAlertService alertService)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));

            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.ErrorOccurred += OnSystemError;

                    await _viewModel.LoadLocationsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error initializing view");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
            }
        }

        private void LocationPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Binding handles the SelectedLocation update which triggers calculation
        }

        private void DatePicker_DateSelected(object sender, DateChangedEventArgs e)
        {
            // Binding handles the Date update which triggers calculation
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is SunCalculatorViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private async Task HandleErrorAsync(Exception ex, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message}: {ex}");

            if (_alertService != null)
            {
                await _alertService.ShowErrorAlertAsync(ex.Message, "Error");
            }
            else
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}