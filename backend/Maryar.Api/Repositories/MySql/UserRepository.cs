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
            Id            = Guid.Parse(r["id"].ToString()),
            Email         = r["email"].ToString(),
            Name          = r["name"].ToString(),
            PasswordHash  = r["password_hash"].ToString(),
            Role          = r["role"].ToString(),
            CreatedAt     = Convert.ToDateTime(r["created_at"]),
            EmailVerified = Convert.ToBoolean(r["email_verified"]),
            Phone         = Str(r, "phone"),
            Cpf           = Str(r, "cpf"),
            Cep           = Str(r, "cep"),
            Logradouro    = Str(r, "logradouro"),
            Numero        = Str(r, "numero"),
            Complemento   = Str(r, "complemento"),
            Bairro        = Str(r, "bairro"),
            Cidade        = Str(r, "cidade"),
            Estado        = Str(r, "estado")
        };

        private const string SelectCols = @"
            SELECT id, email, name, password_hash, role, created_at, email_verified,
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
                    INSERT INTO users (id, email, name, password_hash, role, email_verified)
                    VALUES (@id, @e, @n, @h, @r, @ev)";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                cmd.Parameters.AddWithValue("@e",  u.Email.ToLowerInvariant());
                cmd.Parameters.AddWithValue("@n",  u.Name);
                cmd.Parameters.AddWithValue("@h",  u.PasswordHash);
                cmd.Parameters.AddWithValue("@r",  string.IsNullOrEmpty(u.Role) ? "customer" : u.Role);
                cmd.Parameters.AddWithValue("@ev", u.EmailVerified ? 1 : 0);
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

        public void MarkEmailVerified(Guid id)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET email_verified = 1 WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // Atualiza apenas o e-mail — usado após confirmação de troca
        public void UpdateEmail(Guid id, string newEmail)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET email = @email WHERE id = @id";
                cmd.Parameters.AddWithValue("@id",    id.ToString());
                cmd.Parameters.AddWithValue("@email", newEmail.ToLowerInvariant());
                cmd.ExecuteNonQuery();
            }
        }

        // Grava o novo e-mail como pendente (sem sobrescrever o atual)
        public void SetPendingEmail(Guid id, string pendingEmail)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET pending_email = @pe WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                cmd.Parameters.AddWithValue("@pe", (object)pendingEmail ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        // Retorna o e-mail pendente de confirmação, ou null se não houver
        public string GetPendingEmail(Guid id)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT pending_email FROM users WHERE id = @id LIMIT 1";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result as string;
            }
        }

        // Atualiza perfil — NÃO altera o e-mail (troca de e-mail vai pelo fluxo RequestEmailChange)
        public void UpdateProfile(Guid id, UserProfileDto dto)
        {
            using (var cn = _factory.Create())
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
                cmd.Parameters.AddWithValue("@id",          id.ToString());
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

        public Guid UpsertByEmail(UserProfileDto dto)
        {
            using (var cn = _factory.Create())
            {
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
                        return existingId.Value;
                    }
                }
                else
                {
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO users
                                (id, email, name, password_hash, role, email_verified,
                                 phone, cpf, cep, logradouro, numero,
                                 complemento, bairro, cidade, estado)
                            VALUES
                                (@id, @email, @name, '', 'customer', 1,
                                 @phone, @cpf, @cep, @logradouro, @numero,
                                 @complemento, @bairro, @cidade, @estado)";
                        var newId = Guid.NewGuid();
                        cmd.Parameters.AddWithValue("@id",          newId.ToString());
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
                        return newId;
                    }
                }
            }
        }
    }
}
