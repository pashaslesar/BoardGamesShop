using System;
using BoardGamesShop.Data;

namespace BoardGamesShop.Auth
{
    public sealed class AuthService
    {
        public sealed class AuthUser
        {
            public int Id { get; init; }
            public string UserName { get; init; } = "";
            public int RoleId { get; init; }
            public bool IsAdmin => RoleId == 1;
        }

        public static AuthService Instance { get; } = new();

        private AuthService() { }

        public AuthUser? CurrentUser { get; private set; }
        public event Action? CurrentUserChanged;

        public bool Login(string userOrEmail, string password, out string error)
        {
            error = "";
            var user = UserRepository.FindByUserNameOrEmail(userOrEmail);
            if (user == null) { error = "Uživatel nenalezen."; return false; }

            if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            {
                error = "Špatné heslo."; return false;
            }

            CurrentUser = new AuthUser { Id = user.Id, UserName = user.UserName, RoleId = user.RoleId };
            CurrentUserChanged?.Invoke();
            return true;
        }

        public bool Register(string userName, string? email, string password, bool asAdmin, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(userName)) { error = "Zadejte uživatelské jméno."; return false; }
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6) { error = "Heslo musí mít alespoň 6 znaků."; return false; }
            if (UserRepository.UserNameExists(userName)) { error = "Uživatelské jméno je již obsazené."; return false; }

            var (hash, salt) = PasswordHasher.Hash(password);
            int roleId = asAdmin ? 1 : 2;
            int id = UserRepository.Create(userName, email, hash, salt, roleId);

            CurrentUser = new AuthUser { Id = id, UserName = userName, RoleId = roleId };
            CurrentUserChanged?.Invoke();
            return true;
        }

        public void Logout()
        {
            CurrentUser = null;
            CurrentUserChanged?.Invoke();
        }
    }
}
