// Location.Photography.ViewModels/SubscriptionSignUpViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Queries.Subscription;
using Location.Photography.Domain.Entities;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Location.Photography.ViewModels
{
    public partial class SubscriptionSignUpViewModel : ViewModelBase, INavigationAware
    {
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;

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

        public SubscriptionSignUpViewModel() : base(null, null)
        {
            // Design-time constructor
        }

        public SubscriptionSignUpViewModel(IMediator mediator, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    HasError = false;
                    ClearErrors();

                    var initCommand = new InitializeSubscriptionCommand();
                    var result = await _mediator.Send(initCommand);

                    if (!result.IsSuccess)
                    {
                        OnSystemError(result.ErrorMessage ?? "Failed to initialize subscription service");
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
                    OnSystemError($"Error initializing subscription: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
        }

        [RelayCommand]
        public async Task PurchaseSubscriptionAsync()
        {
            if (SelectedProduct == null)
            {
                SetValidationError("Please select a subscription plan");
                return;
            }

            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    HasError = false;
                    ClearErrors();

                    var purchaseCommand = new ProcessSubscriptionCommand
                    {
                        ProductId = SelectedProduct.ProductId,
                        Period = SelectedProduct.Period
                    };

                    var result = await _mediator.Send(purchaseCommand);

                    if (!result.IsSuccess)
                    {
                        OnSystemError(result.ErrorMessage ?? "Failed to process subscription");
                        return;
                    }

                    if (result.Data.IsSuccessful)
                    {
                        OnSubscriptionCompleted();
                    }
                    else
                    {
                        OnSystemError("There was an error processing your request, please try again");
                    }
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error processing subscription: {ex.Message}");
                }
            });

            await ExecuteAndTrackAsync(command);
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

        protected override void OnErrorOccurred(string message)
        {
            HasError = true;
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        protected virtual void OnSubscriptionCompleted()
        {
            SubscriptionCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnNotNowSelected()
        {
            NotNowSelected?.Invoke(this, EventArgs.Empty);
        }

        public void OnNavigatedToAsync()
        {
        }

        public void OnNavigatedFromAsync()
        {

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