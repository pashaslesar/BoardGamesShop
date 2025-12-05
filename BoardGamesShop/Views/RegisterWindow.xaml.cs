using System.Windows;
using System.Windows.Controls;
using BoardGamesShop.ViewModels;

namespace BoardGamesShop.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            DataContext = new RegisterViewModel();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm && sender is PasswordBox pb)
                vm.Password = pb.Password;
        }
    }
}
