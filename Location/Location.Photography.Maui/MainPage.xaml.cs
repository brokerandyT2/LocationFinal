using core = Location.Core.Maui.Views;

namespace Location.Photography.Maui
{
    public partial class MainPage : TabbedPage
    {
        bool isLoggedIn = false;
        
        public MainPage()
        {
            InitializeComponent();


            if (isLoggedIn)
            {
                this.Children.Add(new core.AddLocation());
                this.Children.Add(new core.LocationsPage());
                this.Children.Add(new core.TipsPage());
            }
        }

       
    }
}
