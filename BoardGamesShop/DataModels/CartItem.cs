using System;

namespace DataModels
{
    public sealed class CartItem
    {
        public Game Game { get; set; } = null!;
        public int UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int LineTotal => UnitPrice * (Quantity > 0 ? Quantity : 1);
    }
}

