using System.ComponentModel;

namespace DataModels
{
    public class Game : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? AuthorId { get; set; }
        public int? GenreId { get; set; }
        public int Price { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int Age { get; set; }
        public int PlayTime { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(IsFavorite)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
