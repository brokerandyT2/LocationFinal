// Location.Photography.Maui/Views/Professional/SceneEvaluation.xaml.cs
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using Location.Core.Application.Services;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class SceneEvaluation : ContentPage
    {
        private SceneEvaluationViewModel _viewModel;
        private readonly IErrorDisplayService _errorDisplayService;

        public SceneEvaluation()
        {
            InitializeComponent();
            _viewModel = new SceneEvaluationViewModel();
            
            BindingContext = _viewModel;
            RedRadioButton.IsChecked = true;
        }

        public SceneEvaluation(IErrorDisplayService errorDisplayService)
        {
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            InitializeComponent();
            _viewModel = new SceneEvaluationViewModel(_errorDisplayService);
            BindingContext = _viewModel;
            RedRadioButton.IsChecked = true;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel == null)
            {
                _viewModel = BindingContext as SceneEvaluationViewModel;
                if (_viewModel == null)
                {
                    _viewModel = _errorDisplayService != null
                        ? new SceneEvaluationViewModel(_errorDisplayService)
                        : new SceneEvaluationViewModel();
                    BindingContext = _viewModel;
                }
            }

            _viewModel.ErrorOccurred -= OnSystemError;
            _viewModel.ErrorOccurred += OnSystemError;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

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