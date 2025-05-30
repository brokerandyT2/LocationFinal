// AppShell.xaml.cs - Fixed implementation
using Location.Core.Maui.Services;
using Location.Photography.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Location.Photography.Maui
{
    public partial class AppShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubscriptionStatusService _subscriptionStatusService;
        private readonly ILogger<AppShell> _logger;
        private bool isLoggedIn = false;

        public AppShell(
            IServiceProvider serviceProvider,
            ISubscriptionStatusService subscriptionStatusService,
            ILogger<AppShell> logger)
        {
            try
            {
                _serviceProvider = serviceProvider;
                _subscriptionStatusService = subscriptionStatusService;
                _logger = logger;

                _logger.LogInformation("AppShell constructor starting");
                InitializeComponent();
                _logger.LogInformation("AppShell InitializeComponent completed");

                // Initialize tabs with nested structure
                InitializeNestedTabs();

                // Background subscription checking
                Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Starting background AppShell setup");

                        var email = await SecureStorage.GetAsync(MagicStrings.Email);
                        isLoggedIn = !string.IsNullOrEmpty(email);
                        _logger.LogInformation($"Login status checked: {isLoggedIn}");

                        if (isLoggedIn)
                        {
                            await ConfigureSubscriptionBasedTabsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in AppShell background setup");
                    }
                });

                _logger.LogInformation("AppShell constructor completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AppShell constructor");
                throw;
            }
        }

        private void InitializeNestedTabs()
        {
            try
            {
                _logger.LogInformation("Initializing nested tabs");

                // Core tabs (no sub-tabs) - Create instances properly
                TopTabs.Tabs.Add(new TabItem 
                { 
                    Title = "Add Location", 
                    PageType = typeof(Location.Core.Maui.Views.AddLocation),
                    IsEnabled = true
                });
                
                TopTabs.Tabs.Add(new TabItem 
                { 
                    Title = "List Locations", 
                    PageType = typeof(Location.Core.Maui.Views.LocationsPage),
                    IsEnabled = true
                });
                
                TopTabs.Tabs.Add(new TabItem 
                { 
                    Title = "Tips", 
                    PageType = typeof(Location.Core.Maui.Views.TipsPage),
                    IsEnabled = true
                });

                // Premium tab with dynamically discovered sub-tabs
                var premiumTab = new TabItem { Title = "Premium", IsEnabled = false };
                premiumTab.SubTabs = DiscoverPremiumSubTabs();
                TopTabs.Tabs.Add(premiumTab);

                // Professional tab with dynamically discovered sub-tabs  
                var professionalTab = new TabItem { Title = "Professional", IsEnabled = false };
                professionalTab.SubTabs = DiscoverProfessionalSubTabs();
                TopTabs.Tabs.Add(professionalTab);

                // Settings tab (no sub-tabs)
                TopTabs.Tabs.Add(new TabItem 
                { 
                    Title = "Settings", 
                    PageType = typeof(Views.Settings),
                    IsEnabled = true
                });

                // Set first tab as selected and load it
                if (TopTabs.Tabs.Count > 0)
                {
                    TopTabs.SelectedTab = TopTabs.Tabs[0];
                    LoadPage(TopTabs.Tabs[0]);
                }

                // Wire up events
                TopTabs.TabSelected += OnMainTabSelected;
                TopTabs.SubTabSelected += OnSubTabSelected;

                _logger.LogInformation("Nested tabs initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing nested tabs");
            }
        }

        private List<TabItem> DiscoverPremiumSubTabs()
        {
            var subTabs = new List<TabItem>();

            try
            {
                // Use the TabDiscoveryService to find Premium tabs
                var assembly = Assembly.GetExecutingAssembly();
                subTabs = TabDiscoveryService.DiscoverPremiumTabs(assembly);
                
                // If discovery fails, add manually
                if (subTabs.Count == 0)
                {
                    subTabs.Add(new TabItem
                    {
                        Title = "Exposure Calc",
                        PageType = typeof(Views.Premium.ExposureCalculator),
                        IsEnabled = false
                    });

                    subTabs.Add(new TabItem
                    {
                        Title = "Sun Location",
                        PageType = typeof(Views.Premium.SunLocation),
                        IsEnabled = false
                    });

                   
                }

                _logger.LogInformation($"Discovered {subTabs.Count} Premium sub-tabs: {string.Join(", ", subTabs.Select(s => s.Title))}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering Premium sub-tabs");
            }

            return subTabs;
        }

        private List<TabItem> DiscoverProfessionalSubTabs()
        {
            var subTabs = new List<TabItem>();

            try
            {
                // Use the TabDiscoveryService to find Professional tabs
                var assembly = Assembly.GetExecutingAssembly();
                subTabs = TabDiscoveryService.DiscoverProfessionalTabs(assembly);
                
                // If discovery fails, add manually
                if (subTabs.Count == 0)
                {
                    subTabs.Add(new TabItem
                    {
                        Title = "Scene Eval",
                        PageType = typeof(Views.Professional.SceneEvaluation),
                        IsEnabled = false
                    });

                    subTabs.Add(new TabItem
                    {
                        Title = "Sun Calc",
                        PageType = typeof(Views.Professional.SunCalculator),
                        IsEnabled = false
                    });
                    subTabs.Add(new TabItem
                    {
                        Title = "Light Meter",
                        PageType = typeof(Views.Professional.LightMeter),
                        IsEnabled = false
                    });
                }

                _logger.LogInformation($"Discovered {subTabs.Count} Professional sub-tabs: {string.Join(", ", subTabs.Select(s => s.Title))}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering Professional sub-tabs");
            }

            return subTabs;
        }

        private void OnMainTabSelected(object sender, TabItem selectedTab)
        {
            try
            {
                _logger.LogInformation($"Main tab selected: {selectedTab.Title}");

                if (!selectedTab.IsEnabled)
                {
                    _logger.LogInformation($"Main tab {selectedTab.Title} is disabled");
                    ShowUpgradeMessage(selectedTab.Title);
                    return;
                }

                // If tab has sub-tabs, load the first enabled sub-tab
                if (selectedTab.HasSubTabs)
                {
                    var firstEnabledSubTab = selectedTab.SubTabs.FirstOrDefault(st => st.IsEnabled);
                    if (firstEnabledSubTab != null)
                    {
                        TopTabs.SelectedSubTab = firstEnabledSubTab;
                        LoadPage(firstEnabledSubTab);
                    }
                    else
                    {
                        // No enabled sub-tabs, show upgrade message
                        ShowUpgradeMessage(selectedTab.Title);
                    }
                }
                else
                {
                    // Regular tab without sub-tabs
                    LoadPage(selectedTab);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting main tab: {selectedTab?.Title}");
            }
        }

        private void OnSubTabSelected(object sender, TabItem selectedSubTab)
        {
            try
            {
                _logger.LogInformation($"Sub-tab selected: {selectedSubTab.Title}");

                if (selectedSubTab.IsEnabled)
                {
                    LoadPage(selectedSubTab);
                }
                else
                {
                    _logger.LogInformation($"Sub-tab {selectedSubTab.Title} is disabled");
                    ShowUpgradeMessage(selectedSubTab.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting sub-tab: {selectedSubTab?.Title}");
            }
        }

        private void LoadPage(TabItem tab)
        {
            try
            {
                if (tab?.PageType == null)
                {
                    _logger.LogWarning($"No PageType specified for tab: {tab?.Title}");
                    return;
                }

                // Get the page instance from the service provider
                var page = _serviceProvider.GetService(tab.PageType) as ContentPage;
                
                if (page == null)
                {
                    // If not registered in DI, try to create manually
                    page = CreatePageInstance(tab.PageType);
                }

                if (page != null)
                {
                    // Update the content area with the page content
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            ContentArea.Content = page.Content;
                            _logger.LogInformation($"Loaded page for tab: {tab.Title}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error setting content for tab: {tab.Title}");
                        }
                    });
                }
                else
                {
                    _logger.LogError($"Failed to create page instance for tab: {tab.Title}, Type: {tab.PageType.Name}");
                    ShowErrorMessage($"Failed to load {tab.Title}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading page for tab: {tab?.Title}");
                ShowErrorMessage($"Error loading {tab?.Title}: {ex.Message}");
            }
        }

        private ContentPage CreatePageInstance(Type pageType)
        {
            try
            {
                // Handle special cases that need specific constructor parameters
                if (pageType == typeof(Location.Core.Maui.Views.AddLocation))
                {
                    var mediator = _serviceProvider.GetRequiredService<MediatR.IMediator>();
                    var mediaService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IMediaService>();
                    var geoService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IGeolocationService>();
                    var errorService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IErrorDisplayService>();
                    
                    return new Location.Core.Maui.Views.AddLocation(mediator, mediaService, geoService, errorService);
                }
                else if (pageType == typeof(Location.Core.Maui.Views.LocationsPage))
                {
                    var mediator = _serviceProvider.GetRequiredService<MediatR.IMediator>();
                    var navService = _serviceProvider.GetRequiredService<INavigationService>();
                    var mediaService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IMediaService>();
                    var geoService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IGeolocationService>();
                    var errorService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IErrorDisplayService>();
                    var weatherService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IWeatherService>();

                    return (ContentPage)_serviceProvider.GetRequiredService<Location.Core.Maui.Views.LocationsPage>(); // Location.Core.Maui.Views.LocationsPage(mediator, navService, mediaService, geoService, errorService, weatherService);
                }
                else if (pageType == typeof(Location.Core.Maui.Views.TipsPage))
                {
                    var mediator = _serviceProvider.GetRequiredService<MediatR.IMediator>();
                    var errorService = _serviceProvider.GetRequiredService<Location.Core.Application.Services.IErrorDisplayService>();
                    var tipRepo = _serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository>();
                    var tipTypeRepo = _serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository>();
                    
                    return new Location.Core.Maui.Views.TipsPage(mediator, errorService, tipRepo, tipTypeRepo);
                }
                else
                {
                    // Try to get from DI container first
                    var instance = _serviceProvider.GetService(pageType) as ContentPage;
                    if (instance != null)
                        return instance;

                    // Fallback: try parameterless constructor
                    return (ContentPage)Activator.CreateInstance(pageType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating instance of {pageType.Name}");
                return null;
            }
        }

        private void ShowUpgradeMessage(string featureName)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ContentArea.Content = new StackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label
                            {
                                Text = $"{featureName} Feature",
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold,
                                HorizontalOptions = LayoutOptions.Center
                            },
                            new Label
                            {
                                Text = "This feature requires a subscription upgrade.",
                                FontSize = 16,
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 10, 0, 0)
                            },
                            new Button
                            {
                                Text = "Upgrade Now",
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 20, 0, 0),
                                Command = new Command(async () => await HandleUpgradeRequest())
                            }
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing upgrade message");
            }
        }

        private void ShowErrorMessage(string message)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ContentArea.Content = new StackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label
                            {
                                Text = "Error",
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold,
                                HorizontalOptions = LayoutOptions.Center,
                                TextColor = Colors.Red
                            },
                            new Label
                            {
                                Text = message,
                                FontSize = 16,
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 10, 0, 0)
                            }
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing error message");
            }
        }

        private async Task HandleUpgradeRequest()
        {
            try
            {
                var subscriptionPage = _serviceProvider.GetService<SubscriptionSignUpPage>();
                if (subscriptionPage != null)
                {
                    await Shell.Current.Navigation.PushModalAsync(subscriptionPage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling upgrade request");
            }
        }

        private async Task ConfigureSubscriptionBasedTabsAsync()
        {
            try
            {
                _logger.LogInformation("ConfigureSubscriptionBasedTabsAsync starting");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var statusResult = await _subscriptionStatusService.CheckSubscriptionStatusAsync();

#if DEBUG
                var canAccessPremium = Result<bool>.Success(true);
                var canAccessPro = Result<bool>.Success(true);
#else
                var canAccessPremium = await _subscriptionStatusService.CanAccessPremiumFeaturesAsync();
                var canAccessPro = await _subscriptionStatusService.CanAccessProFeaturesAsync();
#endif
                var email = SecureStorage.GetAsync(MagicStrings.Email);
                if(email.ToString().ToLower() == "brokerandy25@gmail.com")
                {

                }
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        _logger.LogInformation("Configuring subscription-based tab visibility");

                        var premiumTab = TopTabs.Tabs.FirstOrDefault(t => t.Title == "Premium");
                        var professionalTab = TopTabs.Tabs.FirstOrDefault(t => t.Title == "Professional");

                        // Enable Premium tab and its sub-tabs
                        if (premiumTab != null)
                        {
                            premiumTab.IsEnabled = canAccessPremium.IsSuccess && canAccessPremium.Data;
                            foreach (var subTab in premiumTab.SubTabs)
                            {
                                subTab.IsEnabled = premiumTab.IsEnabled;
                            }
                            _logger.LogInformation($"Premium tab enabled: {premiumTab.IsEnabled}");
                        }

                        // Enable Professional tab and its sub-tabs
                        if (professionalTab != null)
                        {
                            professionalTab.IsEnabled = canAccessPro.IsSuccess && canAccessPro.Data;
                            foreach (var subTab in professionalTab.SubTabs)
                            {
                                subTab.IsEnabled = professionalTab.IsEnabled;
                            }
                            _logger.LogInformation($"Professional tab enabled: {professionalTab.IsEnabled}");
                        }

                        // If premium is enabled, professional is also enabled
                        if (canAccessPremium.IsSuccess && canAccessPremium.Data && professionalTab != null)
                        {
                            professionalTab.IsEnabled = true;
                            foreach (var subTab in professionalTab.SubTabs)
                            {
                                subTab.IsEnabled = true;
                            }
                        }

                        // Refresh the UI
                        TopTabs.RefreshTabs();

                        _logger.LogInformation("Subscription-based tabs configured successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error configuring subscription tabs");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfigureSubscriptionBasedTabsAsync");
            }
        }
    }
}