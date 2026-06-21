using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json.Linq;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Maryar.Api.Services
{
    public class JwtService
    {
        public string Issue(User u, out DateTime expiresAt)
        {
            var secret   = AppConfig.Get("Maryar:JwtSecret");
            var issuer   = AppConfig.Get("Maryar:JwtIssuer");
            var audience = AppConfig.Get("Maryar:JwtAudience");
            var mins     = AppConfig.GetInt("Maryar:JwtExpirationMinutes", 1440);

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            expiresAt = DateTime.UtcNow.AddMinutes(mins);

            var token = new JwtSecurityToken(
                issuer:            issuer,
                audience:          audience,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
                    new Claim(ClaimTypes.Email,          u.Email ?? ""),
                    new Claim(ClaimTypes.Name,           u.Name ?? ""),
                    new Claim(ClaimTypes.Role,           u.Role ?? "customer")
                },
                notBefore:         DateTime.UtcNow,
                expires:           expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var secret = AppConfig.Get("Maryar:JwtSecret");

            var parts = token.Split('.');
            if (parts.Length != 3)
                throw new Exception("JWT inválido.");

            var header    = parts[0];
            var payload   = parts[1];
            var signature = parts[2];

            // Valida assinatura HS256
            var data = Encoding.UTF8.GetBytes(header + "." + payload);
            using (var hmac = new System.Security.Cryptography.HMACSHA256(
                Encoding.UTF8.GetBytes(secret)))
            {
                var expected = Base64UrlEncode(hmac.ComputeHash(data));
                if (expected != signature)
                    throw new Exception("Assinatura inválida.");
            }

            // Lê payload
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            var obj  = JObject.Parse(json);

            // Valida expiração
            var exp = obj["exp"];
            if (exp != null)
            {
                var expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds((long)exp);
                if (expires < DateTime.UtcNow)
                    throw new Exception("Token expirado.");
            }

            // Valida issuer
            var issuer         = obj["iss"];
            var expectedIssuer = AppConfig.Get("Maryar:JwtIssuer");
            if (issuer != null && issuer.ToString() != expectedIssuer)
                throw new Exception("Issuer inválido.");

            // Valida audience
            var audience         = obj["aud"];
            var expectedAudience = AppConfig.Get("Maryar:JwtAudience");
            if (audience != null && audience.ToString() != expectedAudience)
                throw new Exception("Audience inválida.");
           
            // ✅ CORRETO — usa ClaimTypes que são iguais às URIs longas no payload
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, (string)obj[ClaimTypes.NameIdentifier] ?? ""),
                new Claim(ClaimTypes.Email,          (string)obj[ClaimTypes.Email]          ?? ""),
                new Claim(ClaimTypes.Name,           (string)obj[ClaimTypes.Name]           ?? ""),
                new Claim(ClaimTypes.Role,           (string)obj[ClaimTypes.Role]           ?? "customer")
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "JWT"));
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "=";  break;
            }
            return Convert.FromBase64String(s);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
