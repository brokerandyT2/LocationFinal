// Location.Photography.ViewModels/SubscriptionSignUpViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Queries.Subscription;
using Location.Photography.Domain.Entities;
using Location.Photography.ViewModels.Events;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels
{
    public partial class SubscriptionSignUpViewModel : ViewModelBase
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<SubscriptionProductViewModel> _subscriptionProducts = new();

        [ObservableProperty]
        private SubscriptionProductViewModel _selectedProduct;

        [ObservableProperty]
        private bool _isInitialized;

        [ObservableProperty]
        private bool _hasError;

        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        public event EventHandler SubscriptionCompleted;
        public event EventHandler NotNowSelected;

        public SubscriptionSignUpViewModel()
        {
            // Design-time constructor
        }

        public SubscriptionSignUpViewModel(IMediator mediator, IAlertService alertService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var command = new InitializeSubscriptionCommand();
                var result = await _mediator.Send(command);

                if (!result.IsSuccess)
                {
                    HandleError(result.ErrorMessage ?? "Failed to initialize subscription service");
                    return;
                }

                SubscriptionProducts.Clear();
                foreach (var product in result.Data.Products)
                {
                    SubscriptionProducts.Add(new SubscriptionProductViewModel
                    {
                        ProductId = product.ProductId,
                        Title = product.Title,
                        Description = product.Description,
                        Price = product.Price,
                        Period = product.Period,
                        IsSelected = false
                    });
                }

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                HandleError($"Error initializing subscription: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task PurchaseSubscriptionAsync()
        {
            if (SelectedProduct == null)
            {
                await _alertService.ShowErrorAlertAsync("Please select a subscription plan", "Selection Required");
                return;
            }

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var command = new ProcessSubscriptionCommand
                {
                    ProductId = SelectedProduct.ProductId,
                    Period = SelectedProduct.Period
                };

                var result = await _mediator.Send(command);

                if (!result.IsSuccess)
                {
                    HandleError(result.ErrorMessage ?? "Failed to process subscription");
                    return;
                }

                if (result.Data.IsSuccessful)
                {
                    await _alertService.ShowSuccessAlertAsync("Subscription activated successfully!", "Success");
                    OnSubscriptionCompleted();
                }
                else
                {
                    HandleError("There was an error processing your request, please try again");
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error processing subscription: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void SelectProduct(SubscriptionProductViewModel product)
        {
            if (product == null) return;

            // Clear previous selection
            foreach (var p in SubscriptionProducts)
            {
                p.IsSelected = false;
            }

            // Select new product
            product.IsSelected = true;
            SelectedProduct = product;
        }

        [RelayCommand]
        public void NotNow()
        {
            OnNotNowSelected();
        }

        private void HandleError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            OnErrorOccurred(new OperationErrorEventArgs(OperationErrorSource.Unknown, message));

            // Show generic error dialog for user-facing errors
            if (message.Contains("Network connectivity") || message.Contains("There was an error processing"))
            {
                _alertService?.ShowErrorAlertAsync(message, "Error");
            }
            else
            {
                _alertService?.ShowErrorAlertAsync("There was an error processing your request, please try again", "Error");
            }
        }

        protected virtual void OnErrorOccurred(OperationErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual void OnSubscriptionCompleted()
        {
            SubscriptionCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnNotNowSelected()
        {
            NotNowSelected?.Invoke(this, EventArgs.Empty);
        }
    }

    public partial class SubscriptionProductViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _productId = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _price = string.Empty;

        [ObservableProperty]
        private SubscriptionPeriod _period;

        [ObservableProperty]
        private bool _isSelected;

        public string PeriodText => Period == SubscriptionPeriod.Monthly ? "Monthly" : "Yearly";
    }
}