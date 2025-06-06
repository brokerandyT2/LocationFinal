// AppShell.xaml.cs - Simplified Version
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Controls;
using Location.Photography.Maui.Views.Premium;
using Microsoft.Extensions.Logging;

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

                RegisterModalRoutes();
                InitializeTabs();

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

        private void RegisterModalRoutes()
        {
            try
            {
                Routing.RegisterRoute("AddCameraModal", typeof(AddCameraModal));
                Routing.RegisterRoute("AddLensModal", typeof(AddLensModal));

                _logger.LogInformation("Modal routes registered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering modal routes");
            }
        }

        private void InitializeTabs()
        {
            try
            {
                _logger.LogInformation("Initializing tabs");

                // Core tabs
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

                // Premium tab with sub-tabs
                var premiumTab = new TabItem { Title = "Premium", IsEnabled = false };
                premiumTab.SubTabs = new List<TabItem>
                {
                    new TabItem
                    {
                        Title = "Exposure Calc",
                        PageType = typeof(Views.Premium.ExposureCalculator),
                        IsEnabled = false
                    },
                    new TabItem
                    {
                        Title = "Sun Location",
                        PageType = typeof(Views.Premium.SunLocation),
                        IsEnabled = false
                    },
                    new TabItem
                    {
                        Title = "Field of View",
                        PageType = typeof(Views.Premium.FieldOfView),
                        IsEnabled = false
                    }
                };
                TopTabs.Tabs.Add(premiumTab);

                // Professional tab with sub-tabs
                var professionalTab = new TabItem { Title = "Professional", IsEnabled = false };
                professionalTab.SubTabs = new List<TabItem>
                {
                    new TabItem
                    {
                        Title = "Scene Eval",
                        PageType = typeof(Views.Professional.SceneEvaluation),
                        IsEnabled = false
                    },
                    new TabItem
                    {
                        Title = "Sun Calc",
                        PageType = typeof(Views.Professional.SunCalculator),
                        IsEnabled = false
                    },
                    new TabItem
                    {
                        Title = "Light Meter",
                        PageType = typeof(Views.Professional.LightMeter),
                        IsEnabled = false
                    },
                    new TabItem
                    {
                        Title = "Astro Calc",
                        PageType = typeof(Views.Professional.AstroPhotographyCalculator),
                        IsEnabled = false
                    }

                };
                TopTabs.Tabs.Add(professionalTab);

                // Settings tab
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

                _logger.LogInformation("Tabs initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing tabs");
            }
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
                        ShowUpgradeMessage(selectedTab.Title);
                    }
                }
                else
                {
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

                var page = _serviceProvider.GetService(tab.PageType) as ContentPage;

                if (page == null)
                {
                    page = CreatePageInstance(tab.PageType);
                }

                if (page != null)
                {
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
                    return (ContentPage)_serviceProvider.GetRequiredService<Location.Core.Maui.Views.LocationsPage>();
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
                    var instance = _serviceProvider.GetService(pageType) as ContentPage;
                    if (instance != null)
                        return instance;

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

                var subscriptionData = await Task.Run(async () =>
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                        var email = await SecureStorage.GetAsync(MagicStrings.Email);
                        bool hasSpecialAccess = !string.IsNullOrEmpty(email) &&
                            email.ToLower() == "brokerandy25@gmail.com";

#if DEBUG
                        return new
                        {
                            CanAccessPremium = true,
                            CanAccessPro = true,
                            Source = "Debug"
                        };
#else
                        if (hasSpecialAccess)
                        {
                            return new { 
                                CanAccessPremium = true, 
                                CanAccessPro = true,
                                Source = "SpecialAccess"
                            };
                        }

                        var premiumTask = _subscriptionStatusService.CanAccessPremiumFeaturesAsync();
                        var proTask = _subscriptionStatusService.CanAccessProFeaturesAsync();
                        
                        var completedTask = await Task.WhenAny(
                            Task.WhenAll(premiumTask, proTask),
                            Task.Delay(TimeSpan.FromSeconds(5), cts.Token)
                        );

                        if (completedTask == Task.WhenAll(premiumTask, proTask))
                        {
                            var premiumResult = await premiumTask;
                            var proResult = await proTask;
                            
                            return new { 
                                CanAccessPremium = premiumResult.IsSuccess && premiumResult.Data,
                                CanAccessPro = proResult.IsSuccess && proResult.Data,
                                Source = "Subscription"
                            };
                        }
                        else
                        {
                            _logger.LogWarning("Subscription check timed out, defaulting to enabled");
                            return new { 
                                CanAccessPremium = true, 
                                CanAccessPro = true,
                                Source = "Timeout"
                            };
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Subscription check failed, defaulting to enabled");
                        return new
                        {
                            CanAccessPremium = true,
                            CanAccessPro = true,
                            Source = "Error"
                        };
                    }
                }).ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        _logger.LogInformation($"Configuring subscription-based tab visibility (Source: {subscriptionData.Source})");

                        var premiumTab = TopTabs.Tabs.FirstOrDefault(t => t.Title == "Premium");
                        var professionalTab = TopTabs.Tabs.FirstOrDefault(t => t.Title == "Professional");

                        if (premiumTab != null)
                        {
                            premiumTab.IsEnabled = subscriptionData.CanAccessPremium;
                            foreach (var subTab in premiumTab.SubTabs)
                            {
                                subTab.IsEnabled = premiumTab.IsEnabled;
                            }
                            _logger.LogInformation($"Premium tab enabled: {premiumTab.IsEnabled}");
                        }

                        if (professionalTab != null)
                        {
                            professionalTab.IsEnabled = subscriptionData.CanAccessPro;
                            foreach (var subTab in professionalTab.SubTabs)
                            {
                                subTab.IsEnabled = professionalTab.IsEnabled;
                            }
                            _logger.LogInformation($"Professional tab enabled: {professionalTab.IsEnabled}");
                        }

                        if (subscriptionData.CanAccessPremium && professionalTab != null)
                        {
                            professionalTab.IsEnabled = true;
                            foreach (var subTab in professionalTab.SubTabs)
                            {
                                subTab.IsEnabled = true;
                            }
                            _logger.LogInformation("Professional tab enabled via Premium access");
                        }

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

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        var premiumTab = TopTabs.Tabs.FirstOrDefault(t => t.Title == "Premium");
                        var professionalTab = TopTabs.Tabs.FirstOrDefault(t => t.Title == "Professional");

                        if (premiumTab != null)
                        {
                            premiumTab.IsEnabled = true;
                            foreach (var subTab in premiumTab.SubTabs)
                                subTab.IsEnabled = true;
                        }

                        if (professionalTab != null)
                        {
                            professionalTab.IsEnabled = true;
                            foreach (var subTab in professionalTab.SubTabs)
                                subTab.IsEnabled = true;
                        }

                        TopTabs.RefreshTabs();
                        _logger.LogInformation("Fallback: All tabs enabled due to configuration error");
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Critical: Fallback tab configuration failed");
                    }
                });
            }
        }
    }
}