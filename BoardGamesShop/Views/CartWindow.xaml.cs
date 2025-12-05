using System;
using System.Collections.Generic;
using System.Windows;
using BoardGamesShop.ViewModels;
using DataModels;

namespace BoardGamesShop.Views
{
    public partial class CartWindow : Window
    {
        public Action? WhenChanged { get; set; }

        public CartWindow(IList<CartItem> cart)
        {
            InitializeComponent();

            var vm = new CartViewModel(cart);
            DataContext = vm;

            vm.OnChanged = () => WhenChanged?.Invoke();
        }
    }
}
