using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class SceneEvaluation : ContentPage
    {
        private SceneEvaluationViewModel _viewModel;

        public SceneEvaluation()
        {
            InitializeComponent();

            // Ensure the ViewModel is set
            _viewModel = BindingContext as SceneEvaluationViewModel ?? new SceneEvaluationViewModel();
            BindingContext = _viewModel;

            // Set the initial radio button state
            RedRadioButton.IsChecked = true;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // If the ViewModel wasn't set in the constructor, get it now
            if (_viewModel == null)
            {
                _viewModel = BindingContext as SceneEvaluationViewModel;
                if (_viewModel == null)
                {
                    _viewModel = new SceneEvaluationViewModel();
                    BindingContext = _viewModel;
                }
            }

            // Subscribe to error events
            _viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from events to prevent memory leaks
            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
            }
        }

        private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
        {
            // Handle the error event, perhaps showing an alert
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", e.Message, "OK");
            });
        }

        private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_viewModel == null)
            {
                _viewModel = BindingContext as SceneEvaluationViewModel;
                if (_viewModel == null) return;
            }

            // Only process the event if a radio button is being checked (not unchecked)
            if (!e.Value) return;

            // Determine which radio button was checked
            if (sender == RedRadioButton)
            {
                _viewModel.IsRedHistogramVisible = true;
                _viewModel.IsGreenHistogramVisible = false;
                _viewModel.IsBlueHistogramVisible = false;
                _viewModel.IsContrastHistogramVisible = false;
            }
            else if (sender == GreenRadioButton)
            {
                _viewModel.IsRedHistogramVisible = false;
                _viewModel.IsGreenHistogramVisible = true;
                _viewModel.IsBlueHistogramVisible = false;
                _viewModel.IsContrastHistogramVisible = false;
            }
            else if (sender == BlueRadioButton)
            {
                _viewModel.IsRedHistogramVisible = false;
                _viewModel.IsGreenHistogramVisible = false;
                _viewModel.IsBlueHistogramVisible = true;
                _viewModel.IsContrastHistogramVisible = false;
            }
            else if (sender == ContrastRadioButton)
            {
                _viewModel.IsRedHistogramVisible = false;
                _viewModel.IsGreenHistogramVisible = false;
                _viewModel.IsBlueHistogramVisible = false;
                _viewModel.IsContrastHistogramVisible = true;
            }
        }
    }
}