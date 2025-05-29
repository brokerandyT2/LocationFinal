using Microsoft.Extensions.Logging;

namespace Location.Photography.Maui;

public partial class SimpleMainPage : ContentPage
{
    public SimpleMainPage(ILogger<SimpleMainPage> logger)
    {
        try
        {
            logger.LogInformation("SimpleMainPage constructor starting");

            Title = "PixMap";
            BackgroundColor = Colors.White;

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                    {
                        new Label
                        {
                            Text = "PixMap is Working!",
                            FontSize = 24,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Colors.Black
                        },
                        new Label
                        {
                            Text = "MainPage loaded successfully",
                            FontSize = 16,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Colors.Gray,
                            Margin = new Thickness(0, 10, 0, 0)
                        }
                    }
            };

            logger.LogInformation("SimpleMainPage constructor completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SimpleMainPage constructor");
            throw;
        }
    }
}
