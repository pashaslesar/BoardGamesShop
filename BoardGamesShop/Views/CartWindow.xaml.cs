using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DataModels;

namespace BoardGamesShop.Views
{
    public partial class CartWindow : Window
    {
        private readonly IList<CartItem> _cart;
        public Action? WhenChanged { get; set; }

        public CartWindow(IList<CartItem> cart)
        {
            InitializeComponent();
            _cart = cart;
            Grid.ItemsSource = _cart;
            RefreshTotal();
        }

        private void RefreshTotal()
        {
            double total = _cart.Sum(ci => ci.LineTotal);
            TotalText.Text = $"Celkem: {total},-";
            BuyButton.IsEnabled = _cart.Count > 0;
            Grid.Items.Refresh();
        }

        private void Inc_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CartItem item)
            {
                item.Quantity++;
                RefreshTotal();
                WhenChanged?.Invoke();
            }
        }

        private void Dec_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CartItem item)
            {
                if (item.Quantity > 1) item.Quantity--;
                else _cart.Remove(item);
                RefreshTotal();
                WhenChanged?.Invoke();
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CartItem item)
            {
                _cart.Remove(item);
                RefreshTotal();
                WhenChanged?.Invoke();
            }
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            double total = _cart.Sum(ci => ci.LineTotal);
            if (total <= 0) return;

            MessageBox.Show($"Děkujeme za nákup!", "Košík",
                MessageBoxButton.OK, MessageBoxImage.Information);

            _cart.Clear();
            RefreshTotal();
            WhenChanged?.Invoke();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
