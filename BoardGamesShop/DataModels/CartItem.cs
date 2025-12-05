using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataModels
{
    public class CartItem : INotifyPropertyChanged
    {
        public Game Game { get; set; } = null!;

        public int UnitPrice { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineTotal)); 
                }
            }
        }

        public int LineTotal => UnitPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
