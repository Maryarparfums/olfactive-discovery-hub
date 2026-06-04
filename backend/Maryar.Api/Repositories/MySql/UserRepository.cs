using System;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class UserRepository : IUserRepository
    {
        private readonly IConnectionFactory _factory;
        public UserRepository(IConnectionFactory factory) { _factory = factory; }
        public UserRepository() : this(new MySqlConnectionFactory()) { }

        private static User Map(System.Data.IDataReader r) => new User
        {
            Id = Guid.Parse(r["id"].ToString()),
            Email = r["email"].ToString(),
            Name = r["name"].ToString(),
            PasswordHash = r["password_hash"].ToString(),
            Role = r["role"].ToString(),
            CreatedAt = Convert.ToDateTime(r["created_at"])
        };

        public User GetByEmail(string email)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, email, name, password_hash, role, created_at FROM users WHERE email = @e LIMIT 1";
                cmd.Parameters.AddWithValue("@e", email.ToLowerInvariant());
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Map(r) : null;
            }
        }

        public User GetById(Guid id)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, email, name, password_hash, role, created_at FROM users WHERE id = @id LIMIT 1";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Map(r) : null;
            }
        }

        public Guid Create(User u)
        {
            var id = Guid.NewGuid();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO users (id, email, name, password_hash, role)
                                    VALUES (@id, @e, @n, @h, @r)";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                cmd.Parameters.AddWithValue("@e", u.Email.ToLowerInvariant());
                cmd.Parameters.AddWithValue("@n", u.Name);
                cmd.Parameters.AddWithValue("@h", u.PasswordHash);
                cmd.Parameters.AddWithValue("@r", string.IsNullOrEmpty(u.Role) ? "customer" : u.Role);
                cmd.ExecuteNonQuery();
            }
            return id;
        }
    }
}
