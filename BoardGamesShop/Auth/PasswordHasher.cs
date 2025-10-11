using System.Security.Cryptography;

namespace BoardGamesShop.Auth
{
    public static class PasswordHasher
    {
        public static (byte[] hash, byte[] salt) Hash(string password, int iterations = 100_000)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return (hash, salt);
        }

        public static bool Verify(string password, byte[] hash, byte[] salt, int iterations = 100_000)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var test = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(test, hash);
        }
    }
}
