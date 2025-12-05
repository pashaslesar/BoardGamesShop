using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using BoardGamesShop.Data;
using BoardGamesShop.Views;

namespace BoardGamesShop.ViewModels
{
    public class EditPricesViewModel : BaseViewModel
    {
        public ObservableCollection<GamePriceRow> Rows { get; } = new();

        public ICollectionView RowsView { get; }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    RowsView.Refresh();   // фильтрация по поиску
                }
            }
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }

        public EditPricesViewModel()
        {
            RowsView = CollectionViewSource.GetDefaultView(Rows);
            RowsView.Filter = FilterRow;

            SaveCommand = new RelayCommand(Save);
            CloseCommand = new RelayCommand(Close);

            LoadRows();
        }

        private bool FilterRow(object obj)
        {
            if (obj is not GamePriceRow row) return false;
            var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(q)) return true;

            return row.Name.ToLowerInvariant().Contains(q)
                || row.Author.ToLowerInvariant().Contains(q);
        }

        private void LoadRows()
        {
            Rows.Clear();
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
                    double price = r.IsDBNull(3)
                        ? 0.0
                        : Convert.ToDouble(r.GetValue(3), CultureInfo.InvariantCulture);

                    Rows.Add(new GamePriceRow
                    {
                        Id = r.GetInt32(0),
                        Name = r.GetString(1),
                        Author = r.IsDBNull(2) ? "—" : r.GetString(2),
                        Price = price,
                        OriginalPrice = price
                    });
                }

                Status = $"Načteno: {Rows.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba načítání: " + ex.Message,
                    "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save(object? parameter)
        {
            var changed = Rows
                .Where(r => Math.Abs(r.Price - r.OriginalPrice) > 0.0001)
                .ToList();

            if (changed.Count == 0)
            {
                Status = "Žádné změny.";
                return;
            }

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
                var pId = cmd.Parameters.Add("@id", DbType.Int32);
                var pPrice = cmd.Parameters.Add("@p", DbType.Double);

                int affected = 0;

                foreach (var row in changed)
                {
                    pId.Value = row.Id;
                    pPrice.Value = row.Price;
                    affected += cmd.ExecuteNonQuery();
                    row.OriginalPrice = row.Price;
                }

                tx.Commit();
                Status = $"Uloženo změn: {affected}";

                if (parameter is Window w)
                    w.DialogResult = true;
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("SQLite chyba: " + ex.Message,
                    "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Neočekávaná chyba: " + ex.Message,
                    "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close(object? parameter)
        {
            if (parameter is Window w)
                w.Close();
        }
    }
}
