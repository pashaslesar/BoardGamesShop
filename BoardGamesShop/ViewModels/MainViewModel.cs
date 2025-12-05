using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BoardGamesShop.Auth;
using DataControll;
using DataModels;

namespace BoardGamesShop.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public event Action? LoginRequested;
        public event Action? RegisterRequested;
        public event Action? CartRequested;
        public event Action? AddGameRequested;
        public event Action? EditPricesRequested;

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenCartCommand { get; }
        public ICommand OpenAddGameCommand { get; }
        public ICommand OpenEditPricesCommand { get; }

        public ICommand AddToCartCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }

        private List<Game> _allGames = new();
        public List<Game> AllGames
        {
            get => _allGames;
            private set => SetField(ref _allGames, value);
        }

        private List<Game> _filteredGames = new();
        public List<Game> FilteredGames
        {
            get => _filteredGames;
            private set => SetField(ref _filteredGames, value);
        }

        private List<Game> _favoriteGames = new();
        public List<Game> FavoriteGames
        {
            get => _favoriteGames;
            private set => SetField(ref _favoriteGames, value);
        }

        public ObservableCollection<CartItem> Cart { get; } = new();

        public int CartCount => Cart.Sum(ci => ci.Quantity);

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                    ApplyAllFilters();
            }
        }

        private IList<string> _selectedGenres = new List<string>();
        public IList<string> SelectedGenres
        {
            get => _selectedGenres;
            set
            {
                if (SetField(ref _selectedGenres, value.ToList()))
                    ApplyAllFilters();
            }
        }

        private int _minPrice = 0;
        public int MinPrice
        {
            get => _minPrice;
            set
            {
                if (SetField(ref _minPrice, value))
                    ApplyAllFilters();
            }
        }

        private int _maxPrice = 99999;
        public int MaxPrice
        {
            get => _maxPrice;
            set
            {
                if (SetField(ref _maxPrice, value))
                    ApplyAllFilters();
            }
        }

        private int _selectedPlayers = 0;
        public int SelectedPlayers
        {
            get => _selectedPlayers;
            set
            {
                if (SetField(ref _selectedPlayers, value))
                    ApplyAllFilters();
            }
        }

        private bool _filterPlayTime30;
        public bool FilterPlayTime30
        {
            get => _filterPlayTime30;
            set
            {
                if (SetField(ref _filterPlayTime30, value))
                    ApplyAllFilters();
            }
        }

        private bool _filterPlayTime60;
        public bool FilterPlayTime60
        {
            get => _filterPlayTime60;
            set
            {
                if (SetField(ref _filterPlayTime60, value))
                    ApplyAllFilters();
            }
        }

        private bool _filterPlayTime120;
        public bool FilterPlayTime120
        {
            get => _filterPlayTime120;
            set
            {
                if (SetField(ref _filterPlayTime120, value))
                    ApplyAllFilters();
            }
        }

        private bool _filterPlayTimeMore;
        public bool FilterPlayTimeMore
        {
            get => _filterPlayTimeMore;
            set
            {
                if (SetField(ref _filterPlayTimeMore, value))
                    ApplyAllFilters();
            }
        }

        private int _selectedAge = -1;
        public int SelectedAge
        {
            get => _selectedAge;
            set
            {
                if (SetField(ref _selectedAge, value))
                    ApplyAllFilters();
            }
        }

        public bool IsLoggedIn => AuthService.Instance.CurrentUser != null;
        public string CurrentUserName => AuthService.Instance.CurrentUser?.UserName ?? string.Empty;
        public bool IsAdmin => AuthService.Instance.CurrentUser?.IsAdmin == true;

        public MainViewModel()
        {
            AuthService.Instance.CurrentUserChanged += () =>
            {
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(IsAdmin));
            };

            LoginCommand = new RelayCommand(_ => LoginRequested?.Invoke());
            RegisterCommand = new RelayCommand(_ => RegisterRequested?.Invoke());
            LogoutCommand = new RelayCommand(_ => AuthService.Instance.Logout(), _ => IsLoggedIn);
            OpenCartCommand = new RelayCommand(_ => CartRequested?.Invoke());
            OpenAddGameCommand = new RelayCommand(_ => AddGameRequested?.Invoke(), _ => IsAdmin);
            OpenEditPricesCommand = new RelayCommand(_ => EditPricesRequested?.Invoke(), _ => IsAdmin);

            AddToCartCommand = new RelayCommand(
                g => { if (g is Game game) AddToCart(game); });

            ToggleFavoriteCommand = new RelayCommand(
                g => { if (g is Game game) ToggleFavorite(game); });
        }

        public async Task RefreshGamesAsync()
        {
            var fresh = await Task.Run(() => DBController.GetGames());
            AllGames = fresh;
            ApplyAllFilters();
        }

        public void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedGenres = new List<string>();
            MinPrice = 0;
            MaxPrice = 99999;
            SelectedPlayers = 0;
            FilterPlayTime30 = false;
            FilterPlayTime60 = false;
            FilterPlayTime120 = false;
            FilterPlayTimeMore = false;
            SelectedAge = -1;

            ApplyAllFilters();
        }

        public void ToggleFavorite(Game game)
        {
            if (game == null) return;

            var list = FavoriteGames.ToList();

            if (list.Any(g => g.Id == game.Id))
            {
                list.RemoveAll(g => g.Id == game.Id);
                game.IsFavorite = false;
            }
            else
            {
                list.Add(game);
                game.IsFavorite = true;
            }

            FavoriteGames = list;
        }


        public void AddToCart(Game game)
        {
            if (game == null) return;

            var existing = Cart.FirstOrDefault(ci => ci.Game.Id == game.Id);
            if (existing != null)
                existing.Quantity++;
            else
                Cart.Add(new CartItem { Game = game, UnitPrice = game.Price, Quantity = 1 });

            OnPropertyChanged(nameof(CartCount));
        }

        public void ApplyAllFilters()
        {
            var filteredGames = new List<Game>(AllGames);

            var query = (SearchText ?? string.Empty).Trim().ToLower();
            if (!string.IsNullOrEmpty(query))
            {
                filteredGames = filteredGames
                    .Where(g => g.Name != null && g.Name.ToLower().Contains(query))
                    .ToList();
            }

            if (SelectedGenres != null && SelectedGenres.Count > 0)
            {
                List<int> selectedGenreIds = SelectedGenres
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

            int minPrice = MinPrice < 0 ? 0 : MinPrice;
            int maxPrice = MaxPrice <= 0 ? int.MaxValue : MaxPrice;

            filteredGames = filteredGames
                .Where(game => game.Price >= minPrice && game.Price <= maxPrice)
                .ToList();

            if (SelectedPlayers > 0)
            {
                filteredGames = filteredGames
                    .Where(game => game.MinPlayers <= SelectedPlayers && game.MaxPlayers >= SelectedPlayers)
                    .ToList();
            }

            List<Func<int, bool>> timeFilters = new();

            if (FilterPlayTime30) timeFilters.Add(time => time <= 30);
            if (FilterPlayTime60) timeFilters.Add(time => time <= 60);
            if (FilterPlayTime120) timeFilters.Add(time => time <= 120);
            if (FilterPlayTimeMore) timeFilters.Add(time => time > 120);

            if (timeFilters.Count > 0)
            {
                filteredGames = filteredGames
                    .Where(game => timeFilters.Any(f => f(game.PlayTime)))
                    .ToList();
            }

            if (SelectedAge != -1)
            {
                List<int> ageGroups = new() { 0, 4, 10, 14, 16, 18 };
                int index = ageGroups.IndexOf(SelectedAge);

                if (index != -1 && index < ageGroups.Count - 1)
                {
                    int nextAge = ageGroups[index + 1];
                    filteredGames = filteredGames
                        .Where(game => game.Age >= SelectedAge && game.Age < nextAge)
                        .ToList();
                }
                else
                {
                    filteredGames = filteredGames
                        .Where(game => game.Age >= SelectedAge)
                        .ToList();
                }
            }

            FilteredGames = filteredGames;
        }

        public void NotifyCartChanged()
        {
            OnPropertyChanged(nameof(CartCount));
        }
    }
}
