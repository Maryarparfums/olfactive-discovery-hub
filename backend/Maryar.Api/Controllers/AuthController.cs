using System;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;
using Maryar.Api.Services;

namespace Maryar.Api.Controllers
{
    public class AuthController : ApiController
    {
        private readonly IUserRepository _users;
        private readonly IPasswordResetRepository _resets;
        private readonly JwtService _jwt;
        private readonly EmailService _email;

        public AuthController()
            : this(new UserRepository(), new PasswordResetRepository(), new JwtService(), new EmailService()) { }

        public AuthController(IUserRepository users, IPasswordResetRepository resets, JwtService jwt, EmailService email)
        {
            _users  = users;
            _resets = resets;
            _jwt    = jwt;
            _email  = email;
        }

        [HttpPost]
        public IHttpActionResult SignUp([FromBody] SignUpRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email)
                || string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
                return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos (senha mínima 8 caracteres)." });

            var existing = _users.GetByEmail(req.Email.Trim().ToLowerInvariant());

            if (existing != null && !string.IsNullOrEmpty(existing.PasswordHash) && existing.EmailVerified)
                return Content(HttpStatusCode.Conflict, new { error = "E-mail já cadastrado." });

            Guid userId;

            if (existing != null)
            {
                // Conta existente (criada no checkout ou cadastro pendente de verificação)
                // → atualiza senha e dados, e reenvia verificação
                _users.UpdatePassword(existing.Id, PasswordHasher.Hash(req.Password));

                if (!string.IsNullOrWhiteSpace(req.Name))
                    _users.UpdateProfile(existing.Id, new UserProfileDto
                    {
                        Name        = req.Name.Trim(),
                        Email       = existing.Email,
                        Phone       = existing.Phone,
                        Cpf         = existing.Cpf,
                        Cep         = existing.Cep,
                        Logradouro  = existing.Logradouro,
                        Numero      = existing.Numero,
                        Complemento = existing.Complemento,
                        Bairro      = existing.Bairro,
                        Cidade      = existing.Cidade,
                        Estado      = existing.Estado
                    });

                userId = existing.Id;
            }
            else
            {
                // Cadastro totalmente novo — email_verified = false
                var newUser = new User
                {
                    Email         = req.Email.Trim().ToLowerInvariant(),
                    Name          = req.Name?.Trim(),
                    PasswordHash  = PasswordHasher.Hash(req.Password),
                    Role          = "customer",
                    EmailVerified = false
                };
                userId = _users.Create(newUser);
            }

            // Gera token de verificação de e-mail
            _resets.InvalidarAnteriores(userId.ToString(), "email_verification");

            var bytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            var tokenStr = BitConverter.ToString(bytes).Replace("-", "").ToLower();

            _resets.Create(new PasswordResetToken
            {
                UserId    = userId.ToString(),
                Token     = tokenStr,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Used      = false,
                Type      = "email_verification"
            });

            var urlBase = ConfigurationManager.AppSettings["App.Url"];
            var link    = string.Format("{0}/confirmar-email?token={1}", urlBase, tokenStr);

            try { _email.EnviarConfirmacaoEmail(req.Email.Trim().ToLowerInvariant(), link); }
            catch { /* falha no envio não bloqueia o fluxo */ }

            return Ok(new { message = "Verifique seu e-mail para ativar a conta." });
        }

        [HttpPost]
        public IHttpActionResult SignIn([FromBody] SignInRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos." });

                var user = _users.GetByEmail(req.Email.Trim().ToLowerInvariant());

                if (user == null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
                    return Content(HttpStatusCode.Unauthorized, new { error = "Credenciais inválidas." });

                if (!user.EmailVerified)
                    return Content(HttpStatusCode.Forbidden, new
                    {
                        error = "Confirme seu e-mail antes de fazer login. Verifique sua caixa de entrada.",
                        code  = "EMAIL_NOT_VERIFIED"
                    });

                DateTime exp;
                var token = _jwt.Issue(user, out exp);
                return Ok(new AuthResponse
                {
                    UserId = user.Id, Name = user.Name, Email = user.Email,
                    Role = user.Role, Token = token, ExpiresAt = exp
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        public IHttpActionResult VerifyEmail([FromBody] VerifyEmailRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Token))
                    return Content(HttpStatusCode.BadRequest, new { error = "Token inválido." });

                var tokenEntity = _resets.GetByToken(req.Token);

                if (tokenEntity == null
                    || tokenEntity.Type != "email_verification"
                    || tokenEntity.Used
                    || tokenEntity.ExpiresAt < DateTime.UtcNow)
                    return Content(HttpStatusCode.BadRequest, new { error = "Link inválido ou expirado. Faça o cadastro novamente." });

                _users.MarkEmailVerified(Guid.Parse(tokenEntity.UserId));
                _resets.MarcarComoUsado(tokenEntity.Id);

                return Ok(new { message = "E-mail confirmado com sucesso! Faça login para acessar sua conta." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        public IHttpActionResult ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Email))
                    return Ok();

                var user = _users.GetByEmail(req.Email.Trim().ToLowerInvariant());
                if (user == null) return Ok();

                _resets.InvalidarAnteriores(user.Id.ToString(), "password_reset");

                var bytes = new byte[32];
                using (var rng = new RNGCryptoServiceProvider())
                    rng.GetBytes(bytes);
                var tokenStr = BitConverter.ToString(bytes).Replace("-", "").ToLower();

                _resets.Create(new PasswordResetToken
                {
                    UserId    = user.Id.ToString(),
                    Token     = tokenStr,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    Used      = false,
                    Type      = "password_reset"
                });

                var urlBase = ConfigurationManager.AppSettings["App.Url"];
                var link    = string.Format("{0}/redefinir-senha?token={1}", urlBase, tokenStr);

                try { _email.EnviarLinkRedefinicao(user.Email, link); }
                catch { }

                return Ok();
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        public IHttpActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
                    return Content(HttpStatusCode.BadRequest, new { message = "Dados inválidos." });

                var tokenEntity = _resets.GetByToken(req.Token);

                if (tokenEntity == null
                    || tokenEntity.Type != "password_reset"
                    || tokenEntity.Used
                    || tokenEntity.ExpiresAt < DateTime.UtcNow)
                    return Content(HttpStatusCode.BadRequest, new { message = "Link inválido ou expirado. Solicite um novo link." });

                _users.UpdatePassword(Guid.Parse(tokenEntity.UserId), PasswordHasher.Hash(req.NewPassword));
                _resets.MarcarComoUsado(tokenEntity.Id);

                return Ok();
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }

    public class ForgotPasswordRequest { public string Email { get; set; } }
    public class ResetPasswordRequest  { public string Token { get; set; } public string NewPassword { get; set; } }
    public class VerifyEmailRequest    { public string Token { get; set; } }
}
