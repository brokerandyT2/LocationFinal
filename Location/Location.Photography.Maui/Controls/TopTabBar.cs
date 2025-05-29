// Enhanced Controls/TopTabBar.cs with RefreshTabs method
using System.Collections.ObjectModel;
using System.Reflection;

namespace Location.Photography.Maui.Controls
{
    public class TopTabBar : ContentView
    {
        public static readonly BindableProperty TabsProperty =
            BindableProperty.Create(nameof(Tabs), typeof(ObservableCollection<TabItem>), typeof(TopTabBar), new ObservableCollection<TabItem>());

        public static readonly BindableProperty SelectedTabProperty =
            BindableProperty.Create(nameof(SelectedTab), typeof(TabItem), typeof(TopTabBar), null, propertyChanged: OnSelectedTabChanged);

        public static readonly BindableProperty SelectedSubTabProperty =
            BindableProperty.Create(nameof(SelectedSubTab), typeof(TabItem), typeof(TopTabBar), null, propertyChanged: OnSelectedSubTabChanged);

        public ObservableCollection<TabItem> Tabs
        {
            get => (ObservableCollection<TabItem>)GetValue(TabsProperty);
            set => SetValue(TabsProperty, value);
        }

        public TabItem SelectedTab
        {
            get => (TabItem)GetValue(SelectedTabProperty);
            set => SetValue(SelectedTabProperty, value);
        }

        public TabItem SelectedSubTab
        {
            get => (TabItem)GetValue(SelectedSubTabProperty);
            set => SetValue(SelectedSubTabProperty, value);
        }

        public event EventHandler<TabItem> TabSelected;
        public event EventHandler<TabItem> SubTabSelected;

        private ScrollView _mainTabScrollView;
        private HorizontalStackLayout _mainTabContainer;
        private ScrollView _subTabScrollView;
        private HorizontalStackLayout _subTabContainer;
        private StackLayout _containerLayout;

        public TopTabBar()
        {
            CreateTabBar();
            Tabs.CollectionChanged += OnTabsCollectionChanged;
        }

        private void CreateTabBar()
        {
            _containerLayout = new StackLayout
            {
                Spacing = 5,
                Padding = new Thickness(5, 10, 5, 5)
            };

            // Main tabs (horizontal scrollable)
            _mainTabContainer = new HorizontalStackLayout
            {
                Spacing = 8,
                Padding = new Thickness(5, 0)
            };

            _mainTabScrollView = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = _mainTabContainer,
                BackgroundColor = Colors.Transparent,
                HeightRequest = 50
            };

            // Sub tabs (horizontal scrollable, initially hidden)
            _subTabContainer = new HorizontalStackLayout
            {
                Spacing = 6,
                Padding = new Thickness(15, 0)
            };

            _subTabScrollView = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = _subTabContainer,
                BackgroundColor = Colors.Transparent,
                HeightRequest = 40,
                IsVisible = false
            };

            _containerLayout.Children.Add(_mainTabScrollView);
            _containerLayout.Children.Add(_subTabScrollView);

            Content = _containerLayout;
        }

        private void OnTabsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshMainTabs();
        }

        // Public method to refresh all tabs - called by AppShell
        public void RefreshTabs()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RefreshMainTabs();
                RefreshSubTabs();
            });
        }

        private void RefreshMainTabs()
        {
            _mainTabContainer.Children.Clear();

            foreach (var tab in Tabs)
            {
                var tabButton = CreateMainTabButton(tab);
                _mainTabContainer.Children.Add(tabButton);
            }
        }

        private void RefreshSubTabs()
        {
            _subTabContainer.Children.Clear();

            if (SelectedTab?.SubTabs != null && SelectedTab.SubTabs.Any())
            {
                _subTabScrollView.IsVisible = true;

                foreach (var subTab in SelectedTab.SubTabs)
                {
                    var subTabButton = CreateSubTabButton(subTab);
                    _subTabContainer.Children.Add(subTabButton);
                }

                // Auto-select first enabled sub-tab if none selected
                if (SelectedSubTab == null || !SelectedTab.SubTabs.Contains(SelectedSubTab))
                {
                    var firstEnabledSubTab = SelectedTab.SubTabs.FirstOrDefault(st => st.IsEnabled);
                    if (firstEnabledSubTab != null)
                    {
                        SelectedSubTab = firstEnabledSubTab;
                    }
                }
            }
            else
            {
                _subTabScrollView.IsVisible = false;
                SelectedSubTab = null;
            }
        }

        private Border CreateMainTabButton(TabItem tab)
        {
            var isSelected = tab == SelectedTab;
            var isEnabled = tab.IsEnabled;

            // Shorter text for main tabs to fit on screen
            var displayText = GetShortTabText(tab.Title);

            var label = new Label
            {
                Text = displayText,
                FontSize = 12,
                FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isSelected ? Colors.White : (isEnabled ? Colors.Black : Colors.Gray),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var border = new Border
            {
                StrokeThickness = 1,
                Stroke = isSelected ? Colors.Blue : Colors.LightGray,
                BackgroundColor = isSelected ? Colors.Blue : (isEnabled ? Colors.White : Colors.LightGray),
                Content = label,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle() { CornerRadius = 20 },

                HeightRequest = 40,
                Padding = new Thickness(12, 0),
                MinimumWidthRequest = 50, // Smaller to fit 6 tabs
                Opacity = isEnabled ? 1.0 : 0.6
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                if (tab.IsEnabled)
                {
                    SelectedTab = tab;
                    TabSelected?.Invoke(this, tab);
                }
                else
                {
                    // Still allow selection of disabled tabs to show upgrade message
                    SelectedTab = tab;
                    TabSelected?.Invoke(this, tab);
                }
            };
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }

        private Border CreateSubTabButton(TabItem subTab)
        {
            var isSelected = subTab == SelectedSubTab;
            var isEnabled = subTab.IsEnabled;

            var label = new Label
            {
                Text = subTab.Title,
                FontSize = 11,
                FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isSelected ? Colors.White : (isEnabled ? Colors.DarkBlue : Colors.Gray),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var border = new Border
            {
                StrokeThickness = 1,
                Stroke = isSelected ? Colors.DarkBlue : Colors.LightBlue,
                BackgroundColor = isSelected ? Colors.DarkBlue : (isEnabled ? Colors.LightBlue.WithAlpha(0.3f) : Colors.LightGray),
                Content = label,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle() { CornerRadius = 20 },
                HeightRequest = 30,
                Padding = new Thickness(10, 0),
                MinimumWidthRequest = 50,
                Opacity = isEnabled ? 1.0 : 0.6
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                SelectedSubTab = subTab;
                SubTabSelected?.Invoke(this, subTab);
            };
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }

        private string GetShortTabText(string title)
        {
            return title switch
            {
                "Add Location" => "Add",
                "List Locations" => "List",
                "Tips" => "Tips",
                "Premium" => "Premium",
                "Professional" => "Pro",
                "Settings" => "Settings",
                _ => title.Length > 8 ? title.Substring(0, 8) : title
            };
        }

        private static void OnSelectedTabChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TopTabBar tabBar)
            {
                tabBar.RefreshMainTabs();
                tabBar.RefreshSubTabs();
            }
        }

        private static void OnSelectedSubTabChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TopTabBar tabBar)
            {
                tabBar.RefreshSubTabs();
            }
        }
    }

    public class TabItem
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public ContentPage Page { get; set; }
        public bool IsEnabled { get; set; } = true;
        public Type PageType { get; set; }
        public List<TabItem> SubTabs { get; set; } = new List<TabItem>();
        public bool HasSubTabs => SubTabs?.Any() == true;
    }

    // Enhanced tab discovery service
    public static class TabDiscoveryService
    {
        public static List<TabItem> DiscoverPremiumTabs(Assembly assembly)
        {
            var premiumTabs = new List<TabItem>();

            try
            {
                var premiumTypes = assembly.GetTypes()
                    .Where(t => t.Namespace?.Contains("Views.Premium") == true &&
                               t.IsSubclassOf(typeof(ContentPage)) &&
                               t.Name != "DummyPage") // Exclude dummy page
                    .OrderBy(t => GetDisplayOrder(t.Name)) // Order for consistent display
                    .ToList();

                foreach (var type in premiumTypes)
                {
                    var displayName = GetDisplayName(type.Name);
                    premiumTabs.Add(new TabItem
                    {
                        Title = displayName,
                        PageType = type,
                        IsEnabled = false // Will be enabled based on subscription
                    });
                }

                System.Diagnostics.Debug.WriteLine($"Discovered Premium tabs: {string.Join(", ", premiumTabs.Select(t => t.Title))}");
            }
            catch (Exception ex)
            {
                // Log error but don't fail
                System.Diagnostics.Debug.WriteLine($"Error discovering premium tabs: {ex.Message}");
            }

            return premiumTabs;
        }

        public static List<TabItem> DiscoverProfessionalTabs(Assembly assembly)
        {
            var professionalTabs = new List<TabItem>();

            try
            {
                var professionalTypes = assembly.GetTypes()
                    .Where(t => t.Namespace?.Contains("Views.Professional") == true &&
                               t.IsSubclassOf(typeof(ContentPage)))
                    .OrderBy(t => GetDisplayOrder(t.Name)) // Order for consistent display
                    .ToList();

                foreach (var type in professionalTypes)
                {
                    var displayName = GetDisplayName(type.Name);
                    professionalTabs.Add(new TabItem
                    {
                        Title = displayName,
                        PageType = type,
                        IsEnabled = false // Will be enabled based on subscription
                    });
                }

                System.Diagnostics.Debug.WriteLine($"Discovered Professional tabs: {string.Join(", ", professionalTabs.Select(t => t.Title))}");
            }
            catch (Exception ex)
            {
                // Log error but don't fail
                System.Diagnostics.Debug.WriteLine($"Error discovering professional tabs: {ex.Message}");
            }

            return professionalTabs;
        }

        private static string GetDisplayName(string typeName)
        {
            // Convert class names to friendly display names
            return typeName switch
            {
                "SunLocation" => "Sun Location",
                "ExposureCalculator" => "Exposure Calc",
                "LightMeter" => "Light Meter",
                "SceneEvaluation" => "Scene Eval",
                "SunCalculator" => "Sun Calc",
                _ => typeName
            };
        }

        private static int GetDisplayOrder(string typeName)
        {
            // Define display order for consistent tab ordering
            return typeName switch
            {
                "ExposureCalculator" => 1,
                "SunLocation" => 2,
                "LightMeter" => 3,
                "SceneEvaluation" => 4,
                "SunCalculator" => 5,
                _ => 999
            };
        }
    }
}