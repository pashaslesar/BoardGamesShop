using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BoardGamesShop.Data;

namespace BoardGamesShop.ViewModels
{
    public class AddGameViewModel : BaseViewModel
    {
        public string Name { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string AuthorCountry { get; set; } = "";
        public string MinPlayersText { get; set; } = "";
        public string MaxPlayersText { get; set; } = "";
        public string PlayTimeText { get; set; } = "";
        public string AgeText { get; set; } = "";
        public string PriceText { get; set; } = "";
        public string ImagePath { get; set; } = "";

        public ObservableCollection<string> AllGenres { get; } = new();
        public ObservableCollection<string> SelectedGenres { get; } = new();

        private string? _selectedGenre;
        public string? SelectedGenre
        {
            get => _selectedGenre;
            set
            {
                if (_selectedGenre != value)
                {
                    _selectedGenre = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand AddGenreCommand { get; }
        public ICommand RemoveGenreCommand { get; }
        public ICommand AddGameCommand { get; }
        public ICommand CancelCommand { get; }

        public AddGameViewModel()
        {
            AddGenreCommand = new RelayCommand(_ => AddGenre());
            RemoveGenreCommand = new RelayCommand(g => RemoveGenre(g as string));
            AddGameCommand = new RelayCommand(AddGame);
            CancelCommand = new RelayCommand(Cancel);

            LoadAllGenresIntoCombo();
        }

        private void LoadAllGenresIntoCombo()
        {
            try
            {
                using var con = new SQLiteConnection(Db.ConnectionString);
                con.Open();
                using var cmd = new SQLiteCommand("SELECT Name FROM Genres ORDER BY Name;", con);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    AllGenres.Add(r.GetString(0));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nelze načíst žánry: " + ex.Message,
                    "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddGenre()
        {
            if (SelectedGenre is not string name)
            {
                MessageBox.Show("Vyberte žánr.", "Kontrola",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedGenres.Count >= 5)
            {
                MessageBox.Show("Lze vybrat maximálně 5 žánrů.",
                    "Kontrola", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!SelectedGenres.Contains(name))
                SelectedGenres.Add(name);
        }

        private void RemoveGenre(string? g)
        {
            if (g == null) return;
            SelectedGenres.Remove(g);
        }

        private void AddGame(object? parameter)
        {
            var window = parameter as Window;

            string name = Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Warn("Zadejte název hry.");
                return;
            }

            string authorName = AuthorName.Trim();
            string? authorCountry = string.IsNullOrWhiteSpace(AuthorCountry) ? null : AuthorCountry.Trim();

            if (!int.TryParse(MinPlayersText, out int minP) || minP < 1)
            {
                Warn("Min hráčů musí být celé číslo ≥ 1.");
                return;
            }
            if (!int.TryParse(MaxPlayersText, out int maxP) || maxP < minP)
            {
                Warn("Max hráčů musí být celé číslo ≥ Min.");
                return;
            }
            if (!int.TryParse(PlayTimeText, out int duration) || duration <= 0)
            {
                Warn("Doba hraní musí být celé číslo > 0.");
                return;
            }
            if (!int.TryParse(AgeText, out int age) || age < 0)
            {
                Warn("Věk musí být nezáporné celé číslo.");
                return;
            }
            if (!double.TryParse(PriceText, out double price) || price < 0)
            {
                Warn("Cena musí být číslo ≥ 0.");
                return;
            }

            string? imagePath = string.IsNullOrWhiteSpace(ImagePath)
                ? null
                : ImagePath.Trim();

            try
            {
                using var con = new SQLiteConnection(Db.ConnectionString);
                con.Open();
                using var tx = con.BeginTransaction();

                int? authorId = null;
                if (!string.IsNullOrWhiteSpace(authorName))
                {
                    using (var cmdInsA = new SQLiteCommand(
                               "INSERT OR IGNORE INTO Authors(Name, Country) VALUES(@n,@c);", con, tx))
                    {
                        cmdInsA.Parameters.AddWithValue("@n", authorName);
                        cmdInsA.Parameters.AddWithValue("@c", (object?)authorCountry ?? DBNull.Value);
                        cmdInsA.ExecuteNonQuery();
                    }

                    using (var cmdGetA = new SQLiteCommand(
                               "SELECT Id FROM Authors WHERE Name=@n;", con, tx))
                    {
                        cmdGetA.Parameters.AddWithValue("@n", authorName);
                        var v = cmdGetA.ExecuteScalar();
                        authorId = (v == null || v == DBNull.Value)
                            ? null
                            : Convert.ToInt32(v);
                    }
                }

                var genreIds = SelectedGenres
                    .Select(g => EnsureGenre(con, tx, g))
                    .ToList();
                int? mainGenreId = genreIds.FirstOrDefault();

                int gameId;
                using (var cmdInsG = new SQLiteCommand(@"
                    INSERT INTO Games (Name, AuthorId, GenreId, Price, MinPlayers, MaxPlayers, Age, Duration, Stock, IsActive, ImagePath)
                    VALUES (@name, @auth, @genre, @price, @minP, @maxP, @age, @dur, 0, 1, @img);
                    SELECT last_insert_rowid();", con, tx))
                {
                    cmdInsG.Parameters.AddWithValue("@name", name);
                    cmdInsG.Parameters.AddWithValue("@auth", (object?)authorId ?? DBNull.Value);
                    cmdInsG.Parameters.AddWithValue("@genre", (object?)mainGenreId ?? DBNull.Value);
                    cmdInsG.Parameters.AddWithValue("@price", price);
                    cmdInsG.Parameters.AddWithValue("@minP", minP);
                    cmdInsG.Parameters.AddWithValue("@maxP", maxP);
                    cmdInsG.Parameters.AddWithValue("@age", age);
                    cmdInsG.Parameters.AddWithValue("@dur", duration);
                    cmdInsG.Parameters.AddWithValue("@img", (object?)imagePath ?? DBNull.Value);

                    gameId = Convert.ToInt32(cmdInsG.ExecuteScalar());
                }

                foreach (var gid in genreIds.Distinct())
                {
                    using var cmdGc = new SQLiteCommand(
                        "INSERT OR IGNORE INTO GameCategories(GameId, GenreId) VALUES(@g,@ge);",
                        con, tx);
                    cmdGc.Parameters.AddWithValue("@g", gameId);
                    cmdGc.Parameters.AddWithValue("@ge", gid);
                    cmdGc.ExecuteNonQuery();
                }

                tx.Commit();

                MessageBox.Show("Hra byla úspěšně přidána.",
                    "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);

                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Chyba SQLite: " + ex.Message,
                    "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Neočekávaná chyba: " + ex.Message,
                    "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object? parameter)
        {
            if (parameter is Window w)
                w.Close();
        }

        private static void Warn(string msg)
            => MessageBox.Show(msg, "Kontrola",
                MessageBoxButton.OK, MessageBoxImage.Warning);

        private static int EnsureGenre(SQLiteConnection con, SQLiteTransaction tx, string name)
        {
            using (var ins = new SQLiteCommand(
                       "INSERT OR IGNORE INTO Genres(Name) VALUES(@n);", con, tx))
            {
                ins.Parameters.AddWithValue("@n", name);
                ins.ExecuteNonQuery();
            }

            using (var sel = new SQLiteCommand(
                       "SELECT Id FROM Genres WHERE Name=@n;", con, tx))
            {
                sel.Parameters.AddWithValue("@n", name);
                return Convert.ToInt32(sel.ExecuteScalar());
            }
        }
    }
}
