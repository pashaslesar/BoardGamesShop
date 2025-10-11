using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BoardGamesShop.Data;

namespace BoardGamesShop.Views
{
    public partial class EditPricesWindow : Window
    {
        private readonly ObservableCollection<GamePriceRow> _rows = new();
        private ICollectionView? _view;

        public EditPricesWindow()
        {
            InitializeComponent();

            GamesGrid ??= FindName("GamesGrid") as DataGrid;
            SearchTextBox ??= FindName("SearchTextBox") as TextBox;
            StatusText ??= FindName("StatusText") as TextBlock;
            SaveButton ??= FindName("SaveButton") as Button;
            CloseButton ??= FindName("CloseButton") as Button;

            if (GamesGrid == null)
            {
                MessageBox.Show("V XAML chybí DataGrid s x:Name=\"GamesGrid\".",
                    "XAML chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            GamesGrid.ItemsSource = _rows;

            _view = CollectionViewSource.GetDefaultView(GamesGrid.ItemsSource);
            if (_view != null) _view.Filter = FilterRow;

            if (SearchTextBox != null) SearchTextBox.TextChanged += (_, __) => _view?.Refresh();
            if (SaveButton != null) SaveButton.Click += Save_Click;
            if (CloseButton != null) CloseButton.Click += (_, __) => Close();

            LoadRows();
        }

        private void LoadRows()
        {
            _rows.Clear();
            try
            {
                using var con = new SQLiteConnection(Db.ConnectionString);
                con.Open();

                const string sql = @"
                    SELECT g.Id, g.Name, COALESCE(a.Name,'—') AS Author, g.Price
                    FROM Games g
                    LEFT JOIN Authors a ON a.Id = g.AuthorId
                    WHERE g.IsActive = 1
                    ORDER BY g.Name;";

                using var cmd = new SQLiteCommand(sql, con);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    double price = r.IsDBNull(3) ? 0.0 : Convert.ToDouble(r.GetValue(3), CultureInfo.InvariantCulture);
                    _rows.Add(new GamePriceRow
                    {
                        Id = r.GetInt32(0),
                        Name = r.GetString(1),
                        Author = r.IsDBNull(2) ? "—" : r.GetString(2),
                        Price = price,
                        OriginalPrice = price
                    });
                }

                Status($"Načteno: {_rows.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba načítání: " + ex.Message, "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Status(string text)
        {
            if (StatusText != null) StatusText.Text = text;
        }

        private bool FilterRow(object obj)
        {
            if (obj is not GamePriceRow row) return false;
            string q = (SearchTextBox?.Text ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(q)) return true;
            return row.Name.ToLowerInvariant().Contains(q) ||
                   row.Author.ToLowerInvariant().Contains(q);
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            var changed = _rows.Where(r => Math.Abs(r.Price - r.OriginalPrice) > 0.0001).ToList();
            if (changed.Count == 0) { Status("Žádné změny."); return; }

            foreach (var row in changed)
            {
                if (row.Price < 0 || double.IsNaN(row.Price) || double.IsInfinity(row.Price))
                {
                    MessageBox.Show($"Cena musí být číslo ≥ 0 (hra: {row.Name}).",
                                    "Kontrola", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                using var con = new SQLiteConnection(Db.ConnectionString);
                con.Open();
                using var tx = con.BeginTransaction();

                using var cmd = new SQLiteCommand("UPDATE Games SET Price=@p WHERE Id=@id;", con, tx);
                var pId = cmd.Parameters.Add("@id", System.Data.DbType.Int32);
                var pPrice = cmd.Parameters.Add("@p", System.Data.DbType.Double);

                int affected = 0;
                foreach (var row in changed)
                {
                    pId.Value = row.Id;
                    pPrice.Value = row.Price;
                    affected += cmd.ExecuteNonQuery();
                    row.OriginalPrice = row.Price;
                }

                tx.Commit();
                Status($"Uloženo změn: {affected}");
                DialogResult = true;
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("SQLite chyba: " + ex.Message, "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Neočekávaná chyba: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public sealed class GamePriceRow : INotifyPropertyChanged
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Author { get; init; } = "";

        private double _price;
        public double Price
        {
            get => _price;
            set
            {
                if (Math.Abs(_price - value) > 0.0001)
                {
                    _price = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Price)));
                }
            }
        }

        public double OriginalPrice { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
