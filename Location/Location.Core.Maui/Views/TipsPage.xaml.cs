// Location.Core.Maui/Views/TipsPage.xaml.cs
using Location.Core.Application.Services;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using MediatR;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Location.Core.Maui.Views
{
    public partial class TipsPage : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly INavigationService _navigationService;

        public TipsPage(
            IMediator mediator,
            IAlertService alertService,
            INavigationService navigationService)
        {
            InitializeComponent();

            _mediator = mediator;
            _alertService = alertService;
            _navigationService = navigationService;

            // Initialize the view model
            BindingContext = new TipsViewModel(_mediator);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Load tip types when the page appears
            if (BindingContext is TipsViewModel viewModel)
            {
                await viewModel.LoadTipTypesCommand.ExecuteAsync(null);
            }
        }

        private async void OnAddTipClicked(object sender, EventArgs e)
        {
            // We'll implement this later to add new tips
            await _alertService.DisplayAlert(
                "Coming Soon",
                "Adding new tips will be implemented in a future update.",
                "OK");
        }
    }
}