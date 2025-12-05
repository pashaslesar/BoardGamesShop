using System.Windows;
using System.Windows.Input;
using BoardGamesShop.Auth;

namespace BoardGamesShop.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _userOrEmail = "";
        public string UserOrEmail
        {
            get => _userOrEmail;
            set
            {
                if (_userOrEmail != value)
                {
                    _userOrEmail = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login, CanLogin);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool CanLogin(object? _)
        {
            return !string.IsNullOrWhiteSpace(UserOrEmail)
                   && !string.IsNullOrWhiteSpace(Password);
        }

        private void Login(object? parameter)
        {
            var window = parameter as Window;

            if (AuthService.Instance.Login(UserOrEmail, Password, out var err))
            {
                if (window != null)
                    window.DialogResult = true;  
            } 
            else
            {
                ErrorMessage = err;
            }
        }

        private void Cancel(object? parameter)
        {
            if (parameter is Window w)
                w.DialogResult = false;      
        }
    }
}
