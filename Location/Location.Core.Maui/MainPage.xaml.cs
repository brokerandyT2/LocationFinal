namespace Location.Core.Maui
{
    public partial class MainPage : TabbedPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            this.Children.Add(new Views.AddLocation());
            this.Children.Add(new Views.LocationsPage());
            this.Children.Add(new Views.TipsPage());
        }

       
    }
}
