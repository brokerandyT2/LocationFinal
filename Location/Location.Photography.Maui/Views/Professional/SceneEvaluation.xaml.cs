// Location.Photography.Maui/Views/Professional/SceneEvaluation.xaml.cs
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.Maui.Views.Professional;

public partial class SceneEvaluation : ContentPage
{
    private readonly SceneEvaluationViewModel _viewModel;

    public SceneEvaluation(SceneEvaluationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        // Subscribe to error events
        _viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
    }
    [Obsolete("This constructor is for tooling or serialization purposes only. Use the constructor with dependencies instead.")]
    public SceneEvaluation() { throw new NotImplementedException(); }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Re-subscribe to events in case they were unsubscribed
        _viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
        _viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

        // Set default visibility
        _viewModel.IsRedHistogramVisible = true;
        _viewModel.IsGreenHistogramVisible = false;
        _viewModel.IsBlueHistogramVisible = false;
        _viewModel.IsContrastHistogramVisible = false;

        // Set default radio button
        RedRadioButton.IsChecked = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe from events
        if (_viewModel != null)
        {
            _viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
        }
    }

    private async void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
    {
        // Display error alert if not already displayed in the UI
        await MainThread.InvokeOnMainThreadAsync(async () => {
            await DisplayAlert(
                "Error",
                e.Message,
                "OK");
        });
    }

    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radioButton)
        {
            string value = radioButton.Value?.ToString();

            if (string.IsNullOrEmpty(value))
                return;

            // Reset all visibilities
            _viewModel.IsRedHistogramVisible = false;
            _viewModel.IsGreenHistogramVisible = false;
            _viewModel.IsBlueHistogramVisible = false;
            _viewModel.IsContrastHistogramVisible = false;

            // Set visibility based on selected value
            switch (value)
            {
                case "R":
                    _viewModel.IsRedHistogramVisible = true;
                    break;
                case "G":
                    _viewModel.IsGreenHistogramVisible = true;
                    break;
                case "B":
                    _viewModel.IsBlueHistogramVisible = true;
                    break;
                case "C":
                    _viewModel.IsContrastHistogramVisible = true;
                    break;
            }
        }
    }
}

// Helper extension method to find visual children
