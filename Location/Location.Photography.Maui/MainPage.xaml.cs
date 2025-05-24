using Location.Photography.Infrastructure;
using core = Location.Core.Maui.Views;

namespace Location.Photography.Maui
{
    public partial class MainPage : TabbedPage
    {
        bool isLoggedIn = !string.IsNullOrEmpty(SecureStorage.GetAsync(MagicStrings.Email).Result);

        private readonly IServiceProvider _serviceProvider;

        public MainPage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            if (isLoggedIn)
            {
                // Use the service provider to resolve the pages
                //TODO: Get subscription info.  you can set IsEnabled which SHOULD disable the tab.  Need to look into it further.  We may disable ever
                this.Children.Add(_serviceProvider.GetRequiredService<core.AddLocation>());
                
                this.Children.Add(_serviceProvider.GetRequiredService<core.LocationsPage>());
                this.Children.Add(_serviceProvider.GetRequiredService<core.TipsPage>());    
                this.Children.Add(_serviceProvider.GetRequiredService<Views.Professional.SceneEvaluation>());
                this.Children.Add(_serviceProvider.GetRequiredService<Views.Professional.SunCalculator>());
                this.Children.Add(_serviceProvider.GetRequiredService<Views.Premium.SunLocation>());
                this.Children.Add(_serviceProvider.GetRequiredService<Views.Premium.ExposureCalculator>());
               // this.Children[6].DisplayPromptAsync("Scene Evaluation", "This feature is only available in the Professional version. Please upgrade to access this feature.", "OK", null, null, 0);
                // Register this too if it has dependencies
                // this.Children.Add(new Views.Professional.LightMeter());
                this.Children.Add(_serviceProvider.GetRequiredService<Views.Settings>());
                this.MinimumWidthRequest = 1000;
            }
        }
    }
}

       
 