using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class SunCalculator : ContentPage
    {
        private readonly SunCalculationsViewModel _viewModel;
        private readonly IAlertService _alertService;

        public SunCalculator()
        {
            InitializeComponent();
        }

        public SunCalculator(SunCalculationsViewModel viewModel, IAlertService alertService)
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
                await _viewModel.LoadLocationsAsync();
                _viewModel.CalculateSun();
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
            // which will trigger the calculation of sun times
        }

        private void DatePicker_DateSelected(object sender, DateChangedEventArgs e)
        {
            // The binding will handle updating the ViewModel's Date property
            // which will trigger the calculation of sun times
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