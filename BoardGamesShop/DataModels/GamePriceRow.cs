using System;
using System.ComponentModel;

namespace BoardGamesShop.ViewModels
{
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
