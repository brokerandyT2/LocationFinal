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
    public partial class SubscriptionSignUpViewModel : ViewModelBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;

        // PERFORMANCE: Threading and state management
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private CancellationTokenSource _cancellationTokenSource = new();

        // Core properties
        private ObservableCollection<SubscriptionProductViewModel> _subscriptionProducts = new();
        private SubscriptionProductViewModel _selectedProduct;
        private bool _isInitialized;
        private bool _hasError;
        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<SubscriptionProductViewModel> _subscriptionProductsProp = new();

        [ObservableProperty]
        private SubscriptionProductViewModel _selectedProductProp;

        [ObservableProperty]
        private bool _isInitializedProp;

        [ObservableProperty]
        private bool _hasErrorProp;

        // Legacy property mappings for compatibility
        public ObservableCollection<SubscriptionProductViewModel> SubscriptionProducts
        {
            get => _subscriptionProducts;
            set => SetProperty(ref _subscriptionProducts, value);
        }

        public SubscriptionProductViewModel SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public bool IsInitialized
        {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }
        #endregion

        #region Events
        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        public event EventHandler SubscriptionCompleted;
        public event EventHandler NotNowSelected;
        #endregion

        #region Constructors
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
        #endregion

        #region PERFORMANCE OPTIMIZED COMMANDS

        [RelayCommand]
        public async Task InitializeAsync()
        {
            if (!await _operationLock.WaitAsync(100))
            {
                return; // Skip if another operation is in progress
            }

            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HasError = false;
                        ClearErrors();
                    });

                    // Perform initialization on background thread
                    var initResult = await Task.Run(async () =>
                    {
                        try
                        {
                            var initCommand = new InitializeSubscriptionCommand();
                            return await _mediator.Send(initCommand, _cancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Subscription initialization failed: {ex.Message}", ex);
                        }
                    }, _cancellationTokenSource.Token);

                    if (!initResult.IsSuccess)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            OnSystemError(initResult.ErrorMessage ?? "Failed to initialize subscription service");
                        });
                        return;
                    }

                    // Update UI on main thread with batch operations
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        BeginPropertyChangeBatch();

                        SubscriptionProducts.Clear();
                        foreach (var product in initResult.Data.Products)
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

                        _ = EndPropertyChangeBatchAsync();
                    });
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation occurs
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OnSystemError($"Error initializing subscription: {ex.Message}");
                    });
                }
            });

            try
            {
                await ExecuteAndTrackAsync(command);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        [RelayCommand]
        public async Task PurchaseSubscriptionAsync()
        {
            if (SelectedProduct == null)
            {
                SetValidationError("Please select a subscription plan");
                return;
            }

            if (!await _operationLock.WaitAsync(100))
            {
                return; // Skip if another operation is in progress
            }

            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HasError = false;
                        ClearErrors();
                    });

                    // Perform purchase on background thread
                    var purchaseResult = await Task.Run(async () =>
                    {
                        try
                        {
                            var purchaseCommand = new ProcessSubscriptionCommand
                            {
                                ProductId = SelectedProduct.ProductId,
                                Period = SelectedProduct.Period
                            };

                            return await _mediator.Send(purchaseCommand, _cancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Subscription purchase failed: {ex.Message}", ex);
                        }
                    }, _cancellationTokenSource.Token);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (!purchaseResult.IsSuccess)
                        {
                            OnSystemError(purchaseResult.ErrorMessage ?? "Failed to process subscription");
                            return;
                        }

                        if (purchaseResult.Data.IsSuccessful)
                        {
                            OnSubscriptionCompleted();
                        }
                        else
                        {
                            OnSystemError("There was an error processing your request, please try again");
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation occurs
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        OnSystemError($"Error processing subscription: {ex.Message}");
                    });
                }
            });

            try
            {
                await ExecuteAndTrackAsync(command);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        [RelayCommand]
        public void SelectProduct(SubscriptionProductViewModel product)
        {
            if (product == null) return;

            try
            {
                BeginPropertyChangeBatch();

                // Clear previous selection
                foreach (var p in SubscriptionProducts)
                {
                    p.IsSelected = false;
                }

                // Select new product
                product.IsSelected = true;
                SelectedProduct = product;

                _ = EndPropertyChangeBatchAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error selecting product: {ex.Message}");
                _ = EndPropertyChangeBatchAsync();
            }
        }

        [RelayCommand]
        public void NotNow()
        {
            OnNotNowSelected();
        }

        #endregion

        #region Methods

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
            // Reset state when navigating to the page
            IsInitialized = false;
            HasError = false;
            ClearErrors();
        }

        public void OnNavigatedFromAsync()
        {
            _cancellationTokenSource?.Cancel();
        }

        public override void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _operationLock?.Dispose();
            base.Dispose();
        }

        #endregion
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