using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using DataControll;
using DataModels;
using System.IO;
using BoardGamesShop.Auth;
using BoardGamesShop.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BoardGamesShop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    List<Game> games = new List<Game>();

    private RadioButton? lastCheckedAgeButton = null;
    private RadioButton? lastCheckedPlayTimeButton = null;
    private readonly ObservableCollection<CartItem> _cart = new();
    private List<Game> favoriteGames = new List<Game>();
    private HashSet<int> favoriteGameIds = new();

    private int selectedAge = -1;

    public MainWindow()
    {
        InitializeComponent();
        WireAuthUi();
        LoadGenres();
        UpdateCartBadge();
        _ = RefreshGamesAsync();
    }

    private void WireAuthUi()
    {
        AuthService.Instance.CurrentUserChanged += UpdateAuthUi;

        UpdateAuthUi();
    }

    private void UpdateAuthUi()
    {
        var u = AuthService.Instance.CurrentUser;

        LoginButton.Visibility = (u == null) ? Visibility.Visible : Visibility.Collapsed;
        RegisterButton.Visibility = (u == null) ? Visibility.Visible : Visibility.Collapsed;

        UserBlock.Visibility = (u == null) ? Visibility.Collapsed : Visibility.Visible;
        UserNameText.Text = u?.UserName ?? string.Empty;

        AdminPanel.Visibility = (u?.IsAdmin == true) ? Visibility.Visible : Visibility.Collapsed;
    }
    private async Task RefreshGamesAsync()
    {
        try
        {
            var fresh = await Task.Run(() => DBController.GetGames());
            games = fresh;

            DisplayGames(games, gamePanel);

            ApplyAllFilters();

            FavoritesDisplay();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Chyba při načítání her",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private async Task LoadAndSetImageAsync(Image target, string? relPath)
    {
        string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "Ahoy.png");
        string fullPath = string.IsNullOrWhiteSpace(relPath)
            ? fallback
            : (Path.IsPathRooted(relPath) ? relPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relPath));
        if (!File.Exists(fullPath)) fullPath = fallback;

        byte[] bytes = await Task.Run(() => File.ReadAllBytes(fullPath));
        using var ms = new MemoryStream(bytes);
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.StreamSource = ms;
        bi.EndInit();
        bi.Freeze();
        target.Source = bi;
    }

    private void LoadGenres()
    {
        var genres = DBController.GetGenres();
        GenresList.ItemsSource = genres;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyAllFilters();
    }

    private void GenresList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GenresList.SelectedItems.Count > 5)
        {
            foreach (var item in e.AddedItems)
            {
                GenresList.SelectedItems.Remove(item);
                break;
            }

            MessageBox.Show("Lze vybrat maximálně 5 žánrů.");
        }

        ApplyAllFilters();
    }

    private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyAllFilters();

    private void PriceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PriceToTextBox != null)
            PriceToTextBox.Text = ((int)PriceSlider.Value).ToString();

        ApplyAllFilters();
    }

    private void PlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ApplyAllFilters();

    private void PlayTime_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.IsChecked == true)
        {
            lastCheckedPlayTimeButton = rb;
        }

        ApplyAllFilters();
    }

    private void AgeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && int.TryParse(rb.Tag.ToString(), out int age))
        {
            selectedAge = age;
            lastCheckedAgeButton = rb;
            ApplyAllFilters();
        }
    }

    private void GenresList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var item = ItemsControl.ContainerFromElement(GenresList, e.OriginalSource as DependencyObject) as ListBoxItem;

        if (item != null && item.IsSelected)
        {
            GenresList.SelectedItem = null;
            e.Handled = true;
            ApplyAllFilters();
        }
    }

    private void ApplyAllFilters()
    {
        var filteredGames = new List<Game>(games);

        string query = SearchBox.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(query))
        {
            filteredGames = filteredGames
                .Where(g => g.Name.ToLower().Contains(query))
                .ToList();
        }

        if (GenresList.SelectedItems.Count > 0)
        {
            List<int> selectedGenreIds = GenresList.SelectedItems
                .Cast<string>()
                .Select(g => DBController.GetGenreIdByName(g))
                .Where(id => id != -1)
                .ToList();

            filteredGames = filteredGames
                .Where(game =>
                {
                    List<int> gameGenreIds = DBController.GetGenreIdsByGameId(game.Id);
                    return selectedGenreIds.All(id => gameGenreIds.Contains(id));
                })
                .ToList();
        }

        bool isMinOk = int.TryParse(PriceFromTextBox.Text, out int minPrice);
        bool isMaxOk = int.TryParse(PriceToTextBox.Text, out int maxPrice);

        if (!isMinOk) minPrice = 0;
        if (!isMaxOk) maxPrice = (int)PriceSlider.Maximum;

        filteredGames = filteredGames
            .Where(game => game.Price >= minPrice && game.Price <= maxPrice)
            .ToList();

        int selectedPlayers = (int)PlayerSlider.Value;
        if (selectedPlayers > 0)
        {
            filteredGames = filteredGames
                .Where(game => game.MinPlayers <= selectedPlayers && game.MaxPlayers >= selectedPlayers)
                .ToList();
        }

        List<Func<int, bool>> timeFilters = new();

        if (PlayTime30CheckBox.IsChecked == true) timeFilters.Add(time => time <= 30);
        if (PlayTime60CheckBox.IsChecked == true) timeFilters.Add(time => time <= 60);
        if (PlayTime120CheckBox.IsChecked == true) timeFilters.Add(time => time <= 120);
        if (PlayTimeMoreCheckBox.IsChecked == true) timeFilters.Add(time => time > 120);

        if (timeFilters.Count > 0)
        {
            filteredGames = filteredGames
                .Where(game => timeFilters.Any(f => f(game.PlayTime)))
                .ToList();
        }

        if (selectedAge != -1)
        {
            List<int> ageGroups = new List<int> { 0, 4, 10, 14, 16, 18 };
            int index = ageGroups.IndexOf(selectedAge);

            if (index != -1 && index < ageGroups.Count - 1)
            {
                int nextAge = ageGroups[index + 1];
                filteredGames = filteredGames
                    .Where(game => game.Age >= selectedAge && game.Age < nextAge)
                    .ToList();
            }
            else
            {
                filteredGames = filteredGames
                    .Where(game => game.Age >= selectedAge)
                    .ToList();
            }
        }

        DisplayGames(filteredGames, gamePanel);
    }

    private void AgeRadioButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RadioButton clicked = sender as RadioButton;

        if (clicked != null && clicked == lastCheckedAgeButton)
        {
            clicked.IsChecked = false;
            selectedAge = -1;
            lastCheckedAgeButton = null;
            ApplyAllFilters();
            e.Handled = true;
        }
    }

    private void PlayTimeRadioButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RadioButton clicked = sender as RadioButton;

        if (clicked != null && clicked == lastCheckedPlayTimeButton)
        {
            clicked.IsChecked = false;
            lastCheckedPlayTimeButton = null;

            ApplyAllFilters();

            e.Handled = true;
        }
    }

    private StackPanel CreateInfoBlock(string emoji, string text)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 10, 0),
            Children =
        {
            new TextBlock
            {
                Text = emoji,
                FontSize = 12
            },
            new TextBlock
            {
                Text = text,
                FontSize = 12,
                Margin = new Thickness(3, 0, 0, 0)
            }
        }
        };
    }

    private void DisplayGames(List<Game> gamesToDisplay, WrapPanel targetPanel)
    {
        targetPanel.Children.Clear();

        foreach (var game in gamesToDisplay)
        {
            var addToCartBtn = new Button
            {
                Content = "Do košíku",
                Margin = new Thickness(5, 0, 5, 5),
                Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(79, 192, 206)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(79, 192, 206)),
                Cursor = Cursors.Hand
            };
            addToCartBtn.Click += (s, e) =>
            {
                var existing = _cart.FirstOrDefault(ci => ci.Game.Id == game.Id);
                if (existing != null) existing.Quantity++;
                else _cart.Add(new CartItem { Game = game, UnitPrice = game.Price, Quantity = 1 });

                UpdateCartBadge();
            };

            var cardBorder = new Border
            {
                Width = 220,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(10)
            };

            var mainPanel = new StackPanel();

            var image = new Image
            {
                Height = 185,
                Margin = new Thickness(10),
                Stretch = Stretch.UniformToFill
            };
            _ = LoadAndSetImageAsync(image, game.ImagePath);

            var infoPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 0, 5, 5)
            };

            if (game.MinPlayers != game.MaxPlayers)
                infoPanel.Children.Add(CreateInfoBlock("👥", $"{game.MinPlayers}–{game.MaxPlayers}"));
            else
                infoPanel.Children.Add(CreateInfoBlock("👥", $"{game.MinPlayers}"));

            infoPanel.Children.Add(CreateInfoBlock("⏱", $"{game.PlayTime} min"));

            var ageBlock = new TextBlock
            {
                Text = $"{game.Age}+",
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(5, 0, 0, 5)
            };

            var title = new TextBlock
            {
                Text = game.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap
            };

            var priceBlock = new TextBlock
            {
                Text = $"{game.Price},-",
                FontSize = 16,
                Foreground = Brushes.DarkRed,
                Margin = new Thickness(5, 0, 0, 5),
                FontWeight = FontWeights.SemiBold
            };

            string authorName = game.AuthorId.HasValue
                ? DBController.GetAuthorNameById(game.AuthorId.Value)
                : "Neznámý autor";
            var autorBlock = new TextBlock
            {
                Text = $"Autor: {authorName}",
                FontSize = 12,
                Foreground = Brushes.Black,
                Margin = new Thickness(5, 0, 0, 5)
            };

            var heartButton = new Button
            {
                Content = favoriteGames.Contains(game) ? "❤️" : "🤍",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand,
                FontSize = 16,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            heartButton.MouseEnter += (s, e) => heartButton.Opacity = 0.7;
            heartButton.MouseLeave += (s, e) => heartButton.Opacity = 1.0;
            heartButton.Click += (s, e) =>
            {
                if (favoriteGames.Contains(game)) favoriteGames.Remove(game);
                else favoriteGames.Add(game);

                DisplayGames(this.games, gamePanel);
                DisplayGames(favoriteGames, favoritesPanel);
            };

            var genrePanel = new WrapPanel
            {
                Margin = new Thickness(5, 0, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            foreach (var genre in DBController.GetGenreNamesByGameId(game.Id))
            {
                var tag = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(3, 2, 3, 2),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };
                tag.Child = new TextBlock { Text = genre, FontSize = 11, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center };
                genrePanel.Children.Add(tag);
            }

            mainPanel.Children.Add(image);
            mainPanel.Children.Add(heartButton);
            mainPanel.Children.Add(addToCartBtn);
            mainPanel.Children.Add(infoPanel);
            mainPanel.Children.Add(ageBlock);
            mainPanel.Children.Add(autorBlock);
            mainPanel.Children.Add(title);
            mainPanel.Children.Add(priceBlock);
            mainPanel.Children.Add(genrePanel);

            cardBorder.Child = mainPanel;
            targetPanel.Children.Add(cardBorder);
        }
    }

    //private static ImageSource LoadImageOrFallback(string? relPath)
    //{
    //    string fallback = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "Ahoy.png");

    //    string? fullPath = null;
    //    if (!string.IsNullOrWhiteSpace(relPath))
    //        fullPath = System.IO.Path.IsPathRooted(relPath)
    //            ? relPath
    //            : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relPath);

    //    string toUse = (!string.IsNullOrWhiteSpace(fullPath) && System.IO.File.Exists(fullPath))
    //        ? fullPath
    //        : (System.IO.File.Exists(fallback) ? fallback : string.Empty);

    //    var bi = new BitmapImage();
    //    bi.BeginInit();
    //    bi.CacheOption = BitmapCacheOption.OnLoad;
    //    bi.UriSource = new Uri(toUse, UriKind.Absolute);
    //    bi.EndInit();
    //    bi.Freeze();
    //    return bi;
    //}

    private void FavoritesDisplay()
    {
        favoritesPanel.Children.Clear();
        DisplayGames(favoriteGames, favoritesPanel);
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";

        GenresList.SelectedItem = null;

        PriceFromTextBox.Text = "";
        PriceToTextBox.Text = "";
        PriceSlider.Value = PriceSlider.Maximum;

        PlayerSlider.Value = 0;

        lastCheckedPlayTimeButton = null;
        PlayTime30CheckBox.IsChecked = false;
        PlayTime60CheckBox.IsChecked = false;
        PlayTime120CheckBox.IsChecked = false;
        PlayTimeMoreCheckBox.IsChecked = false;

        lastCheckedAgeButton = null;
        selectedAge = -1;

        Age0RadioButton.IsChecked = false;
        Age4RadioButton.IsChecked = false;
        Age10RadioButton.IsChecked = false;
        Age14RadioButton.IsChecked = false;
        Age16RadioButton.IsChecked = false;
        Age18RadioButton.IsChecked = false;

        DisplayGames(games, gamePanel);
        FavoritesDisplay();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LoginWindow { Owner = this };
        if (dlg.ShowDialog() == true) { }
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RegisterWindow { Owner = this };
        if (dlg.ShowDialog() == true) { }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        AuthService.Instance.Logout();
    }

    private void CartButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Views.CartWindow(_cart) { Owner = this, WhenChanged = UpdateCartBadge };
        dlg.ShowDialog();
    }

    private async void AddGame_Click(object sender, RoutedEventArgs e)
    {
        if (AuthService.Instance.CurrentUser?.IsAdmin != true)
        {
            MessageBox.Show("Tuto akci může provést pouze administrátor.");
            return;
        }

        var dlg = new Views.AddGameWindow { Owner = this };
        if (dlg.ShowDialog() == true)
            await RefreshGamesAsync();
    }

    private async void EditPrices_Click(object sender, RoutedEventArgs e)
    {
        if (AuthService.Instance.CurrentUser?.IsAdmin != true)
        {
            MessageBox.Show("Tuto akci může provést pouze administrátor.");
            return;
        }

        var dlg = new Views.EditPricesWindow { Owner = this };
        if (dlg.ShowDialog() == true)
            await RefreshGamesAsync();
    }

    private void UpdateCartBadge()
    {
        int totalQty = _cart.Sum(ci => ci.Quantity);
        CartCountText.Text = totalQty.ToString();
    }
}