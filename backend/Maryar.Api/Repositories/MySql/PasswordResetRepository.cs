using System;
using System.Configuration;
using MySql.Data.MySqlClient;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["DefaultConnection"].ConnectionString;

        public void Create(PasswordResetToken token)
        {
            using (var con = new MySqlConnection(_conn))
            {
                con.Open();
                var cmd = new MySqlCommand(
                    "INSERT INTO password_reset_tokens (user_id, token, expires_at, used) " +
                    "VALUES (@uid, @tok, @exp, 0)", con);
                cmd.Parameters.AddWithValue("@uid", token.UserId);
                cmd.Parameters.AddWithValue("@tok", token.Token);
                cmd.Parameters.AddWithValue("@exp", token.ExpiresAt);
                cmd.ExecuteNonQuery();
            }
        }

        public PasswordResetToken GetByToken(string token)
        {
            using (var con = new MySqlConnection(_conn))
            {
                con.Open();
                var cmd = new MySqlCommand(
                    "SELECT id, user_id, token, expires_at, used " +
                    "FROM password_reset_tokens WHERE token = @tok LIMIT 1", con);
                cmd.Parameters.AddWithValue("@tok", token);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new PasswordResetToken
                    {
                        Id        = r.GetInt32("id"),
                        UserId    = r.GetString("user_id"),   // ← string
                        Token     = r.GetString("token"),
                        ExpiresAt = r.GetDateTime("expires_at"),
                        Used      = r.GetBoolean("used")
                    };
                }
            }
        }

        public void InvalidarAnteriores(string userId)   // ← string
        {
            using (var con = new MySqlConnection(_conn))
            {
                con.Open();
                var cmd = new MySqlCommand(
                    "UPDATE password_reset_tokens SET used = 1 " +
                    "WHERE user_id = @uid AND used = 0", con);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public void MarcarComoUsado(int id)
        {
            using (var con = new MySqlConnection(_conn))
            {
                con.Open();
                var cmd = new MySqlCommand(
                    "UPDATE password_reset_tokens SET used = 1 WHERE id = @id", con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
