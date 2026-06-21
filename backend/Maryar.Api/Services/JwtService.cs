using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Maryar.Api.Services
{
    public class JwtService
    {
        public string Issue(User u, out DateTime expiresAt)
        {
            var secret = AppConfig.Get("Maryar:JwtSecret");
            var issuer = AppConfig.Get("Maryar:JwtIssuer");
            var audience = AppConfig.Get("Maryar:JwtAudience");
            var mins = AppConfig.GetInt("Maryar:JwtExpirationMinutes", 1440);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            expiresAt = DateTime.UtcNow.AddMinutes(mins);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
                    new Claim(ClaimTypes.Email, u.Email),
                    new Claim(ClaimTypes.Name, u.Name ?? string.Empty),
                    new Claim(ClaimTypes.Role, u.Role ?? "customer"),
                },
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var secret = AppConfig.Get("Maryar:JwtSecret");
            var issuer = AppConfig.Get("Maryar:JwtIssuer");
            var audience = AppConfig.Get("Maryar:JwtAudience");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            SecurityToken _;
            return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
        }
    }
}
