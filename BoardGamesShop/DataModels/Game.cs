using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels
{
    public class Game
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
    }

}

