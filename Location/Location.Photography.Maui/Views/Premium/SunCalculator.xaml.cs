// Location.Photography.Maui.Views.Premium/SunCalculator.xaml.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Premium;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class SunCalculator : ContentPage
    {
        private readonly SunCalculatorViewModel _viewModel;
        private readonly IAlertService _alertService;

        public SunCalculator()
        {
            InitializeComponent();
        }

        public SunCalculator(SunCalculatorViewModel viewModel, IAlertService alertService)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));

            BindingContext = _viewModel;

            // Subscribe to ViewModel events
            _viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Load locations when page appears
                await _viewModel.LoadLocationsAsync();

                // Initial calculation will happen automatically after location is selected
                // due to the property changed handler in the view model
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error initializing view");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
            }
        }

        private void LocationPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            // The binding will handle updating the ViewModel's SelectedLocation property
            // which will trigger the calculation of sun times through the property changed handler
        }

        private void DatePicker_DateSelected(object sender, DateChangedEventArgs e)
        {
            // The binding will handle updating the ViewModel's Date property
            // which will trigger the calculation of sun times through the property changed handler
        }

        private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
        {
            Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert("Error", e.Message, "OK");
            });
        }

        private async Task HandleErrorAsync(Exception ex, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{message}: {ex}");
            await _alertService.ShowErrorAlertAsync(ex.Message, "Error");
        }
    }
}