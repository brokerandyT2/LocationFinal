using Location.Core.Application.Services;
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
        private readonly IAlertService _alertService;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        [Obsolete("This constructor is for tooling or serialization purposes only. Use the constructor with dependencies instead.")]
        public TipsPage() { throw new NotImplementedException(); }
        public TipsPage(
            IMediator mediator,
            IAlertService alertService)
        {
            InitializeComponent();

            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));

            // Initialize the view model
            var viewModel = new TipsViewModel(_mediator, _alertService);
            viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Load tip types when the page appears
            if (BindingContext is TipsViewModel viewModel)
            {
                await viewModel.LoadTipTypesCommand.ExecuteAsync(_cts.Token);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from ViewModel events
            if (BindingContext is TipsViewModel viewModel)
            {
                viewModel.ErrorOccurred -= ViewModel_ErrorOccurred;
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
            await _alertService.ShowInfoAlertAsync(
                "Adding new tips will be implemented in a future update.",
                "Coming Soon");
        }

        private void ViewModel_ErrorOccurred(object sender, OperationErrorEventArgs e)
        {
            // Display error to user if it's not already displayed in the UI
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _alertService.ShowErrorAlertAsync(e.Message, "Error");
            });
        }
    }
}