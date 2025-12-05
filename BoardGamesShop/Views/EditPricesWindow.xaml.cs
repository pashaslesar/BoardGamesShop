using System.Windows;
using BoardGamesShop.ViewModels;

namespace BoardGamesShop.Views
{
    public partial class EditPricesWindow : Window
    {
        public EditPricesWindow()
        {
            InitializeComponent();
            DataContext = new EditPricesViewModel();
        }
    }
}
