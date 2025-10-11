using System.Windows;
using BoardGamesShop.Data;
using BoardGamesShop.Auth;

namespace BoardGamesShop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Db.EnsureCreated();

            EnsureAdminSeed();
        }

        private static void EnsureAdminSeed()
        {
            if (UserRepository.FindByUserNameOrEmail("admin") != null) return;

            var (hash, salt) = PasswordHasher.Hash("admin123");
            UserRepository.Create("admin", "admin@example.com", hash, salt, roleId: 1);
        }
    }
}
