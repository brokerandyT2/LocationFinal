using Location.Photography.Application.Services;
using Location.Photography.Infrastructure;
using Microsoft.Extensions.Logging;
using core = Location.Core.Maui.Views;

namespace Location.Photography.Maui
{
    public partial class MainPage : TabbedPage
    {
        bool isLoggedIn = false;

        private readonly IServiceProvider _serviceProvider;
        private readonly ISubscriptionStatusService _subscriptionStatusService;
        private readonly ILogger<MainPage> _logger;

        public MainPage(
            IServiceProvider serviceProvider,
            ISubscriptionStatusService subscriptionStatusService,
            ILogger<MainPage> logger)
        {
            _serviceProvider = serviceProvider;
            _subscriptionStatusService = subscriptionStatusService;
            _logger = logger;

            InitializeComponent();
            try
            {
                isLoggedIn = !string.IsNullOrEmpty(SecureStorage.GetAsync(MagicStrings.Email).Result);
            }
            catch { }


            if (isLoggedIn)
            {
                _ = InitializeTabsAsync();
            }
        }

        private async Task InitializeTabsAsync()
        {
            try
            {
                // Get subscription status
                var statusResult = await _subscriptionStatusService.CheckSubscriptionStatusAsync();
                var canAccessPremium = await _subscriptionStatusService.CanAccessPremiumFeaturesAsync();
                var canAccessPro = await _subscriptionStatusService.CanAccessProFeaturesAsync();

                // Core features - always available
                this.Children.Add(_serviceProvider.GetRequiredService<core.AddLocation>());
                this.Children.Add(_serviceProvider.GetRequiredService<core.LocationsPage>());
                this.Children.Add(_serviceProvider.GetRequiredService<core.TipsPage>());

                // Professional features
                var sceneEvaluation = _serviceProvider.GetRequiredService<Views.Professional.SceneEvaluation>();
                if (canAccessPro.IsSuccess && canAccessPro.Data)
                {
                    this.Children.Add(sceneEvaluation);
                }
                else
                {
                    sceneEvaluation.IsEnabled = false;

                    this.Children.Add(sceneEvaluation);
                }

                var sunCalculator = _serviceProvider.GetRequiredService<Views.Professional.SunCalculator>();
                if (canAccessPro.IsSuccess && canAccessPro.Data)
                {
                    this.Children.Add(sunCalculator);
                }
                else
                {
                    sunCalculator.IsEnabled = false;
                    this.Children.Add(sunCalculator);
                }

                // Premium features
                var sunLocation = _serviceProvider.GetRequiredService<Views.Premium.SunLocation>();
                if (canAccessPremium.IsSuccess && canAccessPremium.Data)
                {
                    this.Children.Add(sunLocation);
                }
                else
                {
                    sunLocation.IsEnabled = false;
                    this.Children.Add(sunLocation);
                }

                var exposureCalculator = _serviceProvider.GetRequiredService<Views.Premium.ExposureCalculator>();
                if (canAccessPremium.IsSuccess && canAccessPremium.Data)
                {
                    this.Children.Add(exposureCalculator);
                }
                else
                {
                    exposureCalculator.IsEnabled = false;
                    exposureCalculator.Title = "Exposure Calculator (Premium)";
                    this.Children.Add(exposureCalculator);
                }

                // Settings - always available
                this.Children.Add(_serviceProvider.GetRequiredService<Views.Settings>());

                this.MinimumWidthRequest = 1000;

                // Log subscription status for debugging
                if (statusResult.IsSuccess)
                {
                    _logger.LogInformation("Subscription Status: {Type}, Expires: {Expiration}, Grace: {Grace}",
                        statusResult.Data.SubscriptionType,
                        statusResult.Data.ExpirationDate,
                        statusResult.Data.IsInGracePeriod);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing tabs with subscription status");

                // Fallback - add all tabs but mark premium/pro as disabled
                this.Children.Add(_serviceProvider.GetRequiredService<core.AddLocation>());
                this.Children.Add(_serviceProvider.GetRequiredService<core.LocationsPage>());
                this.Children.Add(_serviceProvider.GetRequiredService<core.TipsPage>());

                var sceneEval = _serviceProvider.GetRequiredService<Views.Professional.SceneEvaluation>();
                sceneEval.IsEnabled = false;
                sceneEval.Title = "Scene Evaluation (Pro)";
                this.Children.Add(sceneEval);

                var sunCalc = _serviceProvider.GetRequiredService<Views.Professional.SunCalculator>();
                sunCalc.IsEnabled = false;
                sunCalc.Title = "Sun Calculator (Pro)";
                this.Children.Add(sunCalc);

                var sunLoc = _serviceProvider.GetRequiredService<Views.Premium.SunLocation>();
                sunLoc.IsEnabled = false;
                sunLoc.Title = "Sun Location (Premium)";
                this.Children.Add(sunLoc);

                var expCalc = _serviceProvider.GetRequiredService<Views.Premium.ExposureCalculator>();
                expCalc.IsEnabled = false;
                expCalc.Title = "Exposure Calculator (Premium)";
                this.Children.Add(expCalc);

                this.Children.Add(_serviceProvider.GetRequiredService<Views.Settings>());
                this.MinimumWidthRequest = 1000;
            }
        }
    }
}