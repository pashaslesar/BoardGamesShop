using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using BoardGamesShop.Data;

namespace BoardGamesShop.Views
{
    public partial class AddGameWindow : Window
    {
        private readonly ObservableCollection<string> _selectedGenres = new();

        public AddGameWindow()
        {
            InitializeComponent();
            SelectedGenresPanel.ItemsSource = _selectedGenres;
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
                    GenreComboBox.Items.Add(r.GetString(0));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nelze načíst žánry: " + ex.Message, "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddGenre_Click(object sender, RoutedEventArgs e)
        {
            if (GenreComboBox.SelectedItem is not string name) { MessageBox.Show("Vyberte žánr."); return; }
            if (_selectedGenres.Count >= 5) { MessageBox.Show("Lze vybrat maximálně 5 žánrů."); return; }
            if (_selectedGenres.Contains(name)) return;
            _selectedGenres.Add(name);
        }

        private void RemoveGenre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is string g)
                _selectedGenres.Remove(g);
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Obrázky|*.png;*.jpg;*.jpeg|Všechny soubory|*.*",
                Title = "Vybrat obrázek"
            };
            if (dlg.ShowDialog(this) == true)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string p = dlg.FileName;
                if (p.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    p = p.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar);

                ImagePathBox.Text = p.Replace('\\', '/');
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { Warn("Zadejte název hry."); return; }

            string authorName = AuthorNameBox.Text.Trim();
            string authorCountry = string.IsNullOrWhiteSpace(AuthorCountryBox.Text) ? null : AuthorCountryBox.Text.Trim();

            if (!int.TryParse(MinPlayersBox.Text, out int minP) || minP < 1) { Warn("Min hráčů musí být celé číslo ≥ 1."); return; }
            if (!int.TryParse(MaxPlayersBox.Text, out int maxP) || maxP < minP) { Warn("Max hráčů musí být celé číslo ≥ Min."); return; }
            if (!int.TryParse(PlayTimeBox.Text, out int duration) || duration <= 0) { Warn("Doba hraní musí být celé číslo > 0."); return; }
            if (!int.TryParse(AgeBox.Text, out int age) || age < 0) { Warn("Věk musí být nezáporné celé číslo."); return; }

            if (!double.TryParse(PriceBox.Text, out double price) || price < 0) { Warn("Cena musí být číslo ≥ 0."); return; }

            string imagePath = string.IsNullOrWhiteSpace(ImagePathBox.Text) ? null : ImagePathBox.Text.Trim();

            try
            {
                using var con = new SQLiteConnection(Db.ConnectionString);
                con.Open();
                using var tx = con.BeginTransaction();

                int? authorId = null;
                if (!string.IsNullOrWhiteSpace(authorName))
                {
                    using (var cmdInsA = new SQLiteCommand("INSERT OR IGNORE INTO Authors(Name, Country) VALUES(@n,@c);", con, tx))
                    {
                        cmdInsA.Parameters.AddWithValue("@n", authorName);
                        cmdInsA.Parameters.AddWithValue("@c", (object?)authorCountry ?? DBNull.Value);
                        cmdInsA.ExecuteNonQuery();
                    }
                    using (var cmdGetA = new SQLiteCommand("SELECT Id FROM Authors WHERE Name=@n;", con, tx))
                    {
                        cmdGetA.Parameters.AddWithValue("@n", authorName);
                        var v = cmdGetA.ExecuteScalar();
                        authorId = (v == null || v == DBNull.Value) ? null : Convert.ToInt32(v);
                    }
                }

                var genreIds = _selectedGenres.Select(g => EnsureGenre(con, tx, g)).ToList();
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
                        "INSERT OR IGNORE INTO GameCategories(GameId, GenreId) VALUES(@g,@ge);", con, tx);
                    cmdGc.Parameters.AddWithValue("@g", gameId);
                    cmdGc.Parameters.AddWithValue("@ge", gid);
                    cmdGc.ExecuteNonQuery();
                }

                tx.Commit();

                MessageBox.Show("Hra byla úspěšně přidána.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Chyba SQLite: " + ex.Message, "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Neočekávaná chyba: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void Warn(string msg)
            => MessageBox.Show(msg, "Kontrola", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static int EnsureGenre(SQLiteConnection con, SQLiteTransaction tx, string name)
        {
            using (var ins = new SQLiteCommand("INSERT OR IGNORE INTO Genres(Name) VALUES(@n);", con, tx))
            {
                ins.Parameters.AddWithValue("@n", name);
                ins.ExecuteNonQuery();
            }
            using (var sel = new SQLiteCommand("SELECT Id FROM Genres WHERE Name=@n;", con, tx))
            {
                sel.Parameters.AddWithValue("@n", name);
                return Convert.ToInt32(sel.ExecuteScalar());
            }
        }
    }
}
