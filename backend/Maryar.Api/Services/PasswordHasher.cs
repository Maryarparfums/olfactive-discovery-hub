using System;
using System.Security.Cryptography;

namespace Maryar.Api.Services
{
    // PBKDF2 (Rfc2898) — formato: {iter}.{saltBase64}.{hashBase64}
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var salt = new byte[SaltSize];
                rng.GetBytes(salt);
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                {
                    var hash = pbkdf2.GetBytes(HashSize);
                    return string.Format("{0}.{1}.{2}", Iterations,
                        Convert.ToBase64String(salt), Convert.ToBase64String(hash));
                }
            }
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return false;
            var parts = stored.Split('.');
            if (parts.Length != 3) return false;
            int iter; if (!int.TryParse(parts[0], out iter)) return false;
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter))
            {
                var test = pbkdf2.GetBytes(hash.Length);
                return ConstantTimeEquals(hash, test);
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
