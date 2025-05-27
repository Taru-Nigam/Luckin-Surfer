using System.Security.Cryptography;
namespace GameCraft.Helpers
{
    public static class PasswordHelper
    {
        public static (string Hash, string Salt) HashPassword(string password)
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000);
            byte[] hash = pbkdf2.GetBytes(32);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(saltBytes));
        }

        public static bool VerifyPassword(string inputPassword, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            var pbkdf2 = new Rfc2898DeriveBytes(inputPassword, saltBytes, 10000);
            byte[] inputHash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(inputHash) == storedHash;
        }
    }
}