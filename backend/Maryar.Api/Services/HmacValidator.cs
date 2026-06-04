using System;
using System.Security.Cryptography;
using System.Text;

namespace Maryar.Api.Services
{
    public static class HmacValidator
    {
        // Compara em tempo constante a assinatura recebida com HMAC-SHA256 do corpo bruto.
        public static bool IsValid(string rawBody, string receivedSignatureHex, string secret)
        {
            if (string.IsNullOrEmpty(receivedSignatureHex) || string.IsNullOrEmpty(secret))
                return false;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
                var receivedBytes = FromHex(receivedSignatureHex);
                if (receivedBytes == null || receivedBytes.Length != computed.Length) return false;
                var diff = 0;
                for (var i = 0; i < computed.Length; i++) diff |= computed[i] ^ receivedBytes[i];
                return diff == 0;
            }
        }

        private static byte[] FromHex(string hex)
        {
            try
            {
                if (hex.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                    hex = hex.Substring(7);
                if (hex.Length % 2 != 0) return null;
                var bytes = new byte[hex.Length / 2];
                for (var i = 0; i < bytes.Length; i++)
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                return bytes;
            }
            catch { return null; }
        }
    }
}
