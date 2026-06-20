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

            if (_users.GetByEmail(req.Email) != null)
                return Content(HttpStatusCode.Conflict, new { error = "E-mail já cadastrado." });

            var user = new User
            {
                Email        = req.Email.Trim().ToLowerInvariant(),
                Name         = req.Name?.Trim(),
                PasswordHash = PasswordHasher.Hash(req.Password),
                Role         = "customer"
            };
            user.Id = _users.Create(user);

            DateTime exp;
            var token = _jwt.Issue(user, out exp);
            return Ok(new AuthResponse
            {
                UserId = user.Id, Name = user.Name, Email = user.Email,
                Role = user.Role, Token = token, ExpiresAt = exp
            });
        }

        [HttpPost]
        public IHttpActionResult SignIn([FromBody] SignInRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos." });

                var user = _users.GetByEmail(req.Email);
                if (user == null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
                    return Content(HttpStatusCode.Unauthorized, new { error = "Credenciais inválidas." });

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
                return Content(HttpStatusCode.InternalServerError, new
                {
                    error = ex.Message,
                    tipo  = ex.GetType().Name,
                    stack = ex.StackTrace
                });
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
                if (user == null)
                    return Ok(); // não revela se e-mail existe

                _resets.InvalidarAnteriores(user.Id.ToString());

                var bytes = new byte[32];
                using (var rng = new RNGCryptoServiceProvider())
                    rng.GetBytes(bytes);
                var tokenStr = BitConverter.ToString(bytes).Replace("-", "").ToLower();

                _resets.Create(new PasswordResetToken
                {
                    UserId    = user.Id.ToString(),
                    Token     = tokenStr,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    Used      = false
                });

                var urlBase = ConfigurationManager.AppSettings["App.Url"];
                var link    = string.Format("{0}/redefinir-senha?token={1}", urlBase, tokenStr);

                try
                {
                    _email.EnviarLinkRedefinicao(user.Email, link);
                }
                catch (Exception emailEx)
                {
                    // expõe erro SMTP para diagnóstico — remover após corrigir
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        error = "Falha ao enviar e-mail: " + emailEx.Message,
                        inner = emailEx.InnerException != null ? emailEx.InnerException.Message : null,
                        smtp  = new
                        {
                            host = System.Configuration.ConfigurationManager.AppSettings["Email.Host"],
                            port = System.Configuration.ConfigurationManager.AppSettings["Email.Port"]
                        }
                    });
                }

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

                if (tokenEntity == null || tokenEntity.Used || tokenEntity.ExpiresAt < DateTime.UtcNow)
                    return Content(HttpStatusCode.BadRequest, new { message = "Link inválido ou expirado. Solicite um novo link." });

                var novoHash = PasswordHasher.Hash(req.NewPassword);

                _users.UpdatePassword(Guid.Parse(tokenEntity.UserId), novoHash);

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
}
