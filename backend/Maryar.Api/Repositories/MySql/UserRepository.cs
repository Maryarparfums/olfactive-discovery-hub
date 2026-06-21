using System;
using System.Data;
using Maryar.Api.Dtos;
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

        private static string Str(IDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static User Map(IDataReader r) => new User
        {
            Id           = Guid.Parse(r["id"].ToString()),
            Email        = r["email"].ToString(),
            Name         = r["name"].ToString(),
            PasswordHash = r["password_hash"].ToString(),
            Role         = r["role"].ToString(),
            CreatedAt    = Convert.ToDateTime(r["created_at"]),
            Phone        = Str(r, "phone"),
            Cpf          = Str(r, "cpf"),
            Cep          = Str(r, "cep"),
            Logradouro   = Str(r, "logradouro"),
            Numero       = Str(r, "numero"),
            Complemento  = Str(r, "complemento"),
            Bairro       = Str(r, "bairro"),
            Cidade       = Str(r, "cidade"),
            Estado       = Str(r, "estado")
        };

        private const string SelectCols = @"
            SELECT id, email, name, password_hash, role, created_at,
                   phone, cpf, cep, logradouro, numero, complemento,
                   bairro, cidade, estado
            FROM users";

        public User GetByEmail(string email)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = SelectCols + " WHERE email = @e LIMIT 1";
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
                cmd.CommandText = SelectCols + " WHERE id = @id LIMIT 1";
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
                cmd.CommandText = @"
                    INSERT INTO users (id, email, name, password_hash, role)
                    VALUES (@id, @e, @n, @h, @r)";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                cmd.Parameters.AddWithValue("@e",  u.Email.ToLowerInvariant());
                cmd.Parameters.AddWithValue("@n",  u.Name);
                cmd.Parameters.AddWithValue("@h",  u.PasswordHash);
                cmd.Parameters.AddWithValue("@r",  string.IsNullOrEmpty(u.Role) ? "customer" : u.Role);
                cmd.ExecuteNonQuery();
            }
            return id;
        }

        public void UpdatePassword(Guid id, string passwordHash)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET password_hash = @h WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                cmd.Parameters.AddWithValue("@h",  passwordHash);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateProfile(Guid id, UserProfileDto dto)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE users SET
                        name        = @name,
                        email       = @email,
                        phone       = @phone,
                        cpf         = @cpf,
                        cep         = @cep,
                        logradouro  = @logradouro,
                        numero      = @numero,
                        complemento = @complemento,
                        bairro      = @bairro,
                        cidade      = @cidade,
                        estado      = @estado
                    WHERE id = @id";
                cmd.Parameters.AddWithValue("@id",          id.ToString());
                cmd.Parameters.AddWithValue("@name",        dto.Name);
                cmd.Parameters.AddWithValue("@email",       dto.Email);
                cmd.Parameters.AddWithValue("@phone",       (object)dto.Phone       ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cpf",         (object)dto.Cpf         ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cep",         (object)dto.Cep         ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@logradouro",  (object)dto.Logradouro  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numero",      (object)dto.Numero      ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@complemento", (object)dto.Complemento ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bairro",      (object)dto.Bairro      ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cidade",      (object)dto.Cidade      ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado",      (object)dto.Estado      ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public Guid UpsertByEmail(UserProfileDto dto)
        {
            using (var cn = _factory.Create())
            {
                // Verifica se já existe conta com esse e-mail
                Guid? existingId = null;
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT id FROM users WHERE email = @e LIMIT 1";
                    cmd.Parameters.AddWithValue("@e", dto.Email.ToLowerInvariant());
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) existingId = Guid.Parse(r["id"].ToString());
                }

                if (existingId.HasValue)
                {
                    // Conta existe → atualiza dados (não sobrescreve e-mail)
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            UPDATE users SET
                                name        = @name,
                                phone       = @phone,
                                cpf         = @cpf,
                                cep         = @cep,
                                logradouro  = @logradouro,
                                numero      = @numero,
                                complemento = @complemento,
                                bairro      = @bairro,
                                cidade      = @cidade,
                                estado      = @estado
                            WHERE id = @id";
                        cmd.Parameters.AddWithValue("@id",          existingId.Value.ToString());
                        cmd.Parameters.AddWithValue("@name",        dto.Name);
                        cmd.Parameters.AddWithValue("@phone",       (object)dto.Phone       ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cpf",         (object)dto.Cpf         ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cep",         (object)dto.Cep         ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@logradouro",  (object)dto.Logradouro  ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@numero",      (object)dto.Numero      ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@complemento", (object)dto.Complemento ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@bairro",      (object)dto.Bairro      ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cidade",      (object)dto.Cidade      ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@estado",      (object)dto.Estado      ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Conta não existe → cria sem senha (visitante que comprou)
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO users
                                (id, email, name, password_hash, role,
                                 phone, cpf, cep, logradouro, numero,
                                 complemento, bairro, cidade, estado)
                            VALUES
                                (@id, @email, @name, '', 'customer',
                                 @phone, @cpf, @cep, @logradouro, @numero,
                                 @complemento, @bairro, @cidade, @estado)";
                        cmd.Parameters.AddWithValue("@id",          Guid.NewGuid().ToString());
                        cmd.Parameters.AddWithValue("@email",       dto.Email.ToLowerInvariant());
                        cmd.Parameters.AddWithValue("@name",        dto.Name);
                        cmd.Parameters.AddWithValue("@phone",       (object)dto.Phone       ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cpf",         (object)dto.Cpf         ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cep",         (object)dto.Cep         ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@logradouro",  (object)dto.Logradouro  ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@numero",      (object)dto.Numero      ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@complemento", (object)dto.Complemento ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@bairro",      (object)dto.Bairro      ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cidade",      (object)dto.Cidade      ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@estado",      (object)dto.Estado      ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
