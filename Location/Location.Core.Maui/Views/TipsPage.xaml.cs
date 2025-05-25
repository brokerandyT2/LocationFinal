using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Maui.Resources;
using Location.Core.ViewModels;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Maui.Views
{
    public partial class TipsPage : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly ITipRepository _tipRepository;
        private readonly ITipTypeRepository _tipTypeRepository;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        [Obsolete("This constructor is for tooling or serialization purposes only. Use the constructor with dependencies instead.")]
        public TipsPage()
        {
            throw new NotImplementedException();
        }

        public TipsPage(
            IMediator mediator,
            IErrorDisplayService errorDisplayService,
            ITipRepository tipRepo,
            ITipTypeRepository tipType)
        {
            InitializeComponent();

            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _tipRepository = tipRepo ?? throw new ArgumentNullException(nameof(tipRepo));
            _tipTypeRepository = tipType ?? throw new ArgumentNullException(nameof(tipType));

            // Initialize the view model
            var viewModel = new TipsViewModel(_mediator, _errorDisplayService, _tipTypeRepository, _tipRepository);
            viewModel.ErrorOccurred += OnSystemError;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Re-subscribe to ViewModel events in case BindingContext changed
            if (BindingContext is TipsViewModel viewModel)
            {
                viewModel.ErrorOccurred -= OnSystemError;
                viewModel.ErrorOccurred += OnSystemError;

                // Load tip types when the page appears
                await viewModel.ExecuteAndTrackAsync(viewModel.LoadTipTypesCommand, _cts.Token);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is TipsViewModel viewModel)
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

        private async void OnAddTipClicked(object sender, EventArgs e)
        {
            // Implementation for adding new tips (future feature)
            await DisplayAlert(
                "Coming Soon",
                "Adding new tips will be implemented in a future update.",
                AppResources.OK);
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert(
                AppResources.Error,
                $"{e.Message}. Click OK to try again.",
                AppResources.OK,
                AppResources.Cancel);

            if (retry && sender is TipsViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }
    }
}