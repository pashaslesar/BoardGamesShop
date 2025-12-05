using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BoardGamesShop.Auth;
using BoardGamesShop.ViewModels;
using BoardGamesShop.Views;
using DataControll;
using DataModels;

namespace BoardGamesShop
{
    public partial class MainWindow : Window
    {
        private RadioButton? lastCheckedAgeButton = null;
        private RadioButton? lastCheckedPlayTimeButton = null;

        private MainViewModel Vm => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            DataContext = vm;

            vm.LoginRequested += OnLoginRequested;
            vm.RegisterRequested += OnRegisterRequested;
            vm.CartRequested += OnCartRequested;
            vm.AddGameRequested += OnAddGameRequested;
            vm.EditPricesRequested += OnEditPricesRequested;

            LoadGenres();
            _ = RefreshGamesAsync();
        }


        private void OnLoginRequested()
        {
            var dlg = new LoginWindow { Owner = this };
            dlg.ShowDialog();
        }

        private void OnRegisterRequested()
        {
            var dlg = new RegisterWindow { Owner = this };
            dlg.ShowDialog();
        }

        private void OnCartRequested()
        {
            var dlg = new CartWindow(Vm.Cart)
            {
                Owner = this,
                WhenChanged = Vm.NotifyCartChanged
            };
            dlg.ShowDialog();
        }

        private async void OnAddGameRequested()
        {
            if (AuthService.Instance.CurrentUser?.IsAdmin != true)
            {
                MessageBox.Show("Tuto akci může provést pouze administrátor.");
                return;
            }

            var dlg = new AddGameWindow { Owner = this };
            if (dlg.ShowDialog() == true)
                await RefreshGamesAsync();
        }

        private async void OnEditPricesRequested()
        {
            if (AuthService.Instance.CurrentUser?.IsAdmin != true)
            {
                MessageBox.Show("Tuto акци může provést pouze administrátor.");
                return;
            }

            var dlg = new EditPricesWindow { Owner = this };
            if (dlg.ShowDialog() == true)
                await RefreshGamesAsync();
        }


        private async Task RefreshGamesAsync()
        {
            try
            {
                await Vm.RefreshGamesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Chyba při načítání her",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGenres()
        {
            var genres = DBController.GetGenres();
            GenresList.ItemsSource = genres;
        }


        private async void GameImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Image img) return;
            if (img.DataContext is not Game game) return;

            string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "Ahoy.png");
            string fullPath = string.IsNullOrWhiteSpace(game.ImagePath)
                ? fallback
                : (Path.IsPathRooted(game.ImagePath)
                    ? game.ImagePath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.ImagePath));

            if (!File.Exists(fullPath))
                fullPath = fallback;

            byte[] bytes = await Task.Run(() => File.ReadAllBytes(fullPath));
            using var ms = new MemoryStream(bytes);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            img.Source = bi;
        }

        private void ApplyAllFilters() => Vm.ApplyAllFilters();

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Vm.SearchText = SearchBox.Text.Trim();
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

            Vm.SelectedGenres = GenresList.SelectedItems.Cast<string>().ToList();
            ApplyAllFilters();
        }

        private void GenresList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(GenresList, e.OriginalSource as DependencyObject) as ListBoxItem;

            if (item != null && item.IsSelected)
            {
                GenresList.SelectedItem = null;
                Vm.SelectedGenres = Array.Empty<string>();
                e.Handled = true;
                ApplyAllFilters();
            }
        }

        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(PriceFromTextBox.Text, out int minPrice)) minPrice = 0;
            if (!int.TryParse(PriceToTextBox.Text, out int maxPrice)) maxPrice = (int)PriceSlider.Maximum;

            Vm.MinPrice = minPrice;
            Vm.MaxPrice = maxPrice;
            ApplyAllFilters();
        }

        private void PriceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PriceToTextBox != null)
                PriceToTextBox.Text = ((int)PriceSlider.Value).ToString();

            Vm.MaxPrice = (int)PriceSlider.Value;
            ApplyAllFilters();
        }

        private void PlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Vm.SelectedPlayers = (int)PlayerSlider.Value;
            ApplyAllFilters();
        }

        private void PlayTime_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
                lastCheckedPlayTimeButton = rb;

            Vm.FilterPlayTime30 = PlayTime30CheckBox.IsChecked == true;
            Vm.FilterPlayTime60 = PlayTime60CheckBox.IsChecked == true;
            Vm.FilterPlayTime120 = PlayTime120CheckBox.IsChecked == true;
            Vm.FilterPlayTimeMore = PlayTimeMoreCheckBox.IsChecked == true;

            ApplyAllFilters();
        }

        private void PlayTimeRadioButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is RadioButton clicked &&
                clicked == lastCheckedPlayTimeButton)
            {
                clicked.IsChecked = false;
                lastCheckedPlayTimeButton = null;

                Vm.FilterPlayTime30 = PlayTime30CheckBox.IsChecked == true;
                Vm.FilterPlayTime60 = PlayTime60CheckBox.IsChecked == true;
                Vm.FilterPlayTime120 = PlayTime120CheckBox.IsChecked == true;
                Vm.FilterPlayTimeMore = PlayTimeMoreCheckBox.IsChecked == true;

                ApplyAllFilters();
                e.Handled = true;
            }
        }

        private void AgeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag.ToString(), out int age))
            {
                Vm.SelectedAge = age;
                lastCheckedAgeButton = rb;
                ApplyAllFilters();
            }
        }

        private void AgeRadioButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is RadioButton clicked &&
                clicked == lastCheckedAgeButton)
            {
                clicked.IsChecked = false;
                Vm.SelectedAge = -1;
                lastCheckedAgeButton = null;
                ApplyAllFilters();
                e.Handled = true;
            }
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

            Age0RadioButton.IsChecked = false;
            Age4RadioButton.IsChecked = false;
            Age10RadioButton.IsChecked = false;
            Age14RadioButton.IsChecked = false;
            Age16RadioButton.IsChecked = false;
            Age18RadioButton.IsChecked = false;

            Vm.ResetFilters();
        }

        private void AuthorBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBlock tb) return;
            if (tb.DataContext is not Game game) return;

            string authorName = game.AuthorId.HasValue
                ? DBController.GetAuthorNameById(game.AuthorId.Value)
                : "Neznámý autor";

            tb.Text = $"Autor: {authorName}";
        }

        private void GenresItems_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ItemsControl ic) return;
            if (ic.DataContext is not Game game) return;

            var genres = DBController.GetGenreNamesByGameId(game.Id);
            ic.ItemsSource = genres;
        }
    }
}
