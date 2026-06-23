using System;
using System.Data;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly IConnectionFactory _factory;
        public PasswordResetRepository(IConnectionFactory factory) { _factory = factory; }
        public PasswordResetRepository() : this(new MySqlConnectionFactory()) { }

        public void Create(PasswordResetToken token)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO password_reset_tokens (user_id, token, expires_at, used, type)
                    VALUES (@uid, @tok, @exp, 0, @type)";
                cmd.Parameters.AddWithValue("@uid",  token.UserId);
                cmd.Parameters.AddWithValue("@tok",  token.Token);
                cmd.Parameters.AddWithValue("@exp",  token.ExpiresAt);
                cmd.Parameters.AddWithValue("@type", token.Type ?? "password_reset");
                cmd.ExecuteNonQuery();
            }
        }

        public PasswordResetToken GetByToken(string token)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, user_id, token, expires_at, used, type
                    FROM password_reset_tokens
                    WHERE token = @tok LIMIT 1";
                cmd.Parameters.AddWithValue("@tok", token);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new PasswordResetToken
                    {
                        Id        = Convert.ToInt32(r["id"]),
                        UserId    = r["user_id"].ToString(),
                        Token     = r["token"].ToString(),
                        ExpiresAt = Convert.ToDateTime(r["expires_at"]),
                        Used      = Convert.ToBoolean(r["used"]),
                        Type      = r["type"].ToString()
                    };
                }
            }
        }

        public void MarcarComoUsado(int id)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE password_reset_tokens SET used = 1 WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void InvalidarAnteriores(string userId, string type)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE password_reset_tokens
                    SET used = 1
                    WHERE user_id = @uid AND type = @type AND used = 0";
                cmd.Parameters.AddWithValue("@uid",  userId);
                cmd.Parameters.AddWithValue("@type", type);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
