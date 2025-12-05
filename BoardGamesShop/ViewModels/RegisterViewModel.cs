using System.Windows;
using System.Windows.Input;
using BoardGamesShop.Auth;

namespace BoardGamesShop.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private string _userName = "";
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _email;
        public string? Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
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

        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(Register, CanRegister);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool CanRegister(object? _)
        {
            return !string.IsNullOrWhiteSpace(UserName)
                   && !string.IsNullOrWhiteSpace(Password);
        }

        private void Register(object? parameter)
        {
            var window = parameter as Window;

            var userName = UserName?.Trim();
            var email = string.IsNullOrWhiteSpace(Email) ? null : Email!.Trim();
            var password = Password;

            if (AuthService.Instance.Register(userName, email, password, false, out var err))
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
