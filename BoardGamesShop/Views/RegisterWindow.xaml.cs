using System.Windows;
using BoardGamesShop.Auth;

namespace BoardGamesShop.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow() => InitializeComponent();

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var userName = UserNameBox.Text?.Trim();
            var email = string.IsNullOrWhiteSpace(EmailBox.Text) ? null : EmailBox.Text.Trim();
            var password = PasswordBox.Password;

            if (AuthService.Instance.Register(userName, email, password, false, out var err))
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
