using System.Windows;
using BoardGamesShop.Auth;

namespace BoardGamesShop.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow() => InitializeComponent();

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.Instance.Login(UserOrEmailBox.Text, PasswordBox.Password, out var err))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = err;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
