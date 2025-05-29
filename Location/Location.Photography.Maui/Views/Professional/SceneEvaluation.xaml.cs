using Location.Photography.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using Location.Core.Application.Services;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class SceneEvaluation : ContentPage
    {
        private SceneEvaluationViewModel _viewModel;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly IImageAnalysisService _imageAnalysisService;

        public SceneEvaluation()
        {
            InitializeComponent();
            _imageAnalysisService = new ImageAnalysisService();
            _viewModel = new SceneEvaluationViewModel(_imageAnalysisService, null);

            BindingContext = _viewModel;
            RedRadioButton.IsChecked = true;

            // Subscribe to events immediately
            SubscribeToViewModelEvents();

            // Initialize view model state
            _viewModel.OnNavigatedToAsync();
        }

        public SceneEvaluation(IImageAnalysisService imageAnalysisService, IErrorDisplayService errorDisplayService)
        {
            _imageAnalysisService = imageAnalysisService ?? throw new ArgumentNullException(nameof(imageAnalysisService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            InitializeComponent();
            _viewModel = new SceneEvaluationViewModel(_imageAnalysisService, _errorDisplayService);
            BindingContext = _viewModel;
            RedRadioButton.IsChecked = true;

            // Subscribe to events immediately
            SubscribeToViewModelEvents();

            // Initialize view model state
            _viewModel.OnNavigatedToAsync();
        }

        private void SubscribeToViewModelEvents()
        {
            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
                _viewModel.ErrorOccurred += OnSystemError;
            }
        }

        private void UnsubscribeFromViewModelEvents()
        {
            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is SceneEvaluationViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_viewModel == null)
            {
                _viewModel = BindingContext as SceneEvaluationViewModel;
                if (_viewModel == null) return;
            }

            if (!e.Value) return;

            if (sender == RedRadioButton)
            {
                _viewModel.SetHistogramMode(HistogramDisplayMode.Red);
            }
            else if (sender == GreenRadioButton)
            {
                _viewModel.SetHistogramMode(HistogramDisplayMode.Green);
            }
            else if (sender == BlueRadioButton)
            {
                _viewModel.SetHistogramMode(HistogramDisplayMode.Blue);
            }
            else if (sender == LuminanceRadioButton)
            {
                _viewModel.SetHistogramMode(HistogramDisplayMode.Luminance);
            }
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            // Unsubscribe from old view model
            UnsubscribeFromViewModelEvents();

            if (BindingContext is SceneEvaluationViewModel viewModel)
            {
                _viewModel = viewModel;
                SubscribeToViewModelEvents();
                _viewModel.OnNavigatedToAsync();
            }
        }

        ~SceneEvaluation()
        {
            UnsubscribeFromViewModelEvents();
            _viewModel?.OnNavigatedFromAsync();
            _viewModel?.Dispose();
        }
    }
}