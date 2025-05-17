using Location.Photography.Infrastructure;
using core = Location.Core.Maui.Views;

namespace Location.Photography.Maui
{
    public partial class MainPage : TabbedPage
    {
        bool isLoggedIn = !string.IsNullOrEmpty(SecureStorage.GetAsync(MagicStrings.Email).Result);
        
        public MainPage()
        {
            InitializeComponent();


            if (isLoggedIn)
            {
                this.Children.Add(new core.AddLocation());
                this.Children.Add(new core.LocationsPage());
                this.Children.Add(new core.TipsPage());
                this.Children.Add(new Views.Premium.ExposureCalculator());
                this.Children.Add(new Views.Premium.SunCalculator());
                this.Children.Add(new Views.Premium.SunLocation());
                this.Children.Add(new Views.Professional.SceneEvaluation());
               // this.Children.Add(new Views.Professional.LightMeter());
               // this.Children.Add(new core.Settings());
            }
        }

       
    }
}
