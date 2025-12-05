using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DataModels;

namespace BoardGamesShop.ViewModels
{
    public class CartViewModel : BaseViewModel
    {
        public ObservableCollection<CartItem> Items { get; }

        public Action? OnChanged { get; set; }

        public CartViewModel(IList<CartItem> cart)
        {
            Items = cart as ObservableCollection<CartItem>
                    ?? new ObservableCollection<CartItem>(cart);

            IncCommand = new RelayCommand(p => Inc((CartItem)p!));
            DecCommand = new RelayCommand(p => Dec((CartItem)p!));
            RemoveCommand = new RelayCommand(p => Remove((CartItem)p!));
            BuyCommand = new RelayCommand(Buy, _ => HasItems);
            CloseCommand = new RelayCommand(Close);
        }

        public double Total => Items.Sum(ci => ci.LineTotal);
        public bool HasItems => Items.Count > 0;

        public ICommand IncCommand { get; }
        public ICommand DecCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand BuyCommand { get; }
        public ICommand CloseCommand { get; }

        private void Inc(CartItem item)
        {
            item.Quantity++;
            RaiseTotalsChanged();
        }

        private void Dec(CartItem item)
        {
            if (item.Quantity > 1)
                item.Quantity--;
            else
                Items.Remove(item);

            RaiseTotalsChanged();
        }

        private void Remove(CartItem item)
        {
            Items.Remove(item);
            RaiseTotalsChanged();
        }

        private void Buy(object? parameter)
        {
            if (!HasItems) return;

            double total = Total;
            if (total <= 0) return;

            MessageBox.Show("Děkujeme za nákup!", "Košík",
                MessageBoxButton.OK, MessageBoxImage.Information);

            Items.Clear();
            RaiseTotalsChanged();

            if (parameter is Window w)
                w.Close();
        }

        private void Close(object? parameter)
        {
            if (parameter is Window w)
                w.Close();
        }

        private void RaiseTotalsChanged()
        {
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(HasItems));
            OnChanged?.Invoke();
        }
    }
}
