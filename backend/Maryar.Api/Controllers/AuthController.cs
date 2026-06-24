using System;
using System.Configuration;
using System.Net;
using System.Security.Claims;
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

        // POST api/auth/signup
        [HttpPost, ActionName("signup")]
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
                _users.UpdatePassword(existing.Id, PasswordHasher.Hash(req.Password));

                if (!string.IsNullOrWhiteSpace(req.Name))
                    _users.UpdateProfile(existing.Id, new UserProfileDto
                    {
                        Name        = req.Name.Trim(),
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
            catch { }

            return Ok(new { message = "Verifique seu e-mail para ativar a conta." });
        }

        // POST api/auth/signin
        [HttpPost, ActionName("signin")]
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
                    UserId      = user.Id,
                    Name        = user.Name,
                    Email       = user.Email,
                    Role        = user.Role,
                    Token       = token,
                    ExpiresAt   = exp,
                    Phone       = user.Phone,
                    Cpf         = user.Cpf,
                    Cep         = user.Cep,
                    Logradouro  = user.Logradouro,
                    Numero      = user.Numero,
                    Complemento = user.Complemento,
                    Bairro      = user.Bairro,
                    Cidade      = user.Cidade,
                    Estado      = user.Estado
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // POST api/auth/changepassword
        [HttpPost, ActionName("changepassword")]
        public IHttpActionResult ChangePassword([FromBody] ChangePasswordRequest req)
        {
            try
            {
                if (req == null
                    || string.IsNullOrWhiteSpace(req.CurrentPassword)
                    || string.IsNullOrWhiteSpace(req.NewPassword)
                    || req.NewPassword.Length < 8)
                    return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos." });

                var principal = ValidateBearerToken();
                if (principal == null)
                    return Content(HttpStatusCode.Unauthorized, new { error = "Não autenticado." });

                if (!Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                    return Content(HttpStatusCode.Unauthorized, new { error = "Token inválido." });

                var user = _users.GetById(userId);
                if (user == null)
                    return Content(HttpStatusCode.NotFound, new { error = "Usuário não encontrado." });

                if (!PasswordHasher.Verify(req.CurrentPassword, user.PasswordHash))
                    return Content(HttpStatusCode.BadRequest, new { error = "Senha atual incorreta." });

                _users.UpdatePassword(userId, PasswordHasher.Hash(req.NewPassword));

                return Ok(new { message = "Senha alterada com sucesso." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // POST api/auth/requestemailchange
        [HttpPost, ActionName("requestemailchange")]
        public IHttpActionResult RequestEmailChange([FromBody] RequestEmailChangeRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.NewEmail))
                    return Content(HttpStatusCode.BadRequest, new { error = "E-mail inválido." });

                var principal = ValidateBearerToken();
                if (principal == null)
                    return Content(HttpStatusCode.Unauthorized, new { error = "Não autenticado." });

                if (!Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                    return Content(HttpStatusCode.Unauthorized, new { error = "Token inválido." });

                var user = _users.GetById(userId);
                if (user == null)
                    return Content(HttpStatusCode.NotFound, new { error = "Usuário não encontrado." });

                var newEmail = req.NewEmail.Trim().ToLowerInvariant();

                // Verifica se o novo e-mail já está em uso por outra conta
                var existing = _users.GetByEmail(newEmail);
                if (existing != null && existing.Id != userId)
                    return Content(HttpStatusCode.Conflict, new { error = "Este e-mail já está em uso por outra conta." });

                // Salva como pendente — NÃO altera o e-mail atual
                _users.SetPendingEmail(userId, newEmail);

                _resets.InvalidarAnteriores(userId.ToString(), "email_change");

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
                    Type      = "email_change"
                });

                var urlBase = ConfigurationManager.AppSettings["App.Url"];
                var link    = string.Format("{0}/confirmar-email?token={1}", urlBase, tokenStr);

                try { _email.EnviarConfirmacaoEmail(newEmail, link); }
                catch { }

                return Ok(new { message = "Link de confirmação enviado para o novo e-mail." });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // POST api/auth/verifyemail
        [HttpPost, ActionName("verifyemail")]
        public IHttpActionResult VerifyEmail([FromBody] VerifyEmailRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Token))
                    return Content(HttpStatusCode.BadRequest, new { error = "Token inválido." });

                var tokenEntity = _resets.GetByToken(req.Token);

                if (tokenEntity == null || tokenEntity.Used || tokenEntity.ExpiresAt < DateTime.UtcNow)
                    return Content(HttpStatusCode.BadRequest, new { error = "Link inválido ou expirado. Faça o cadastro novamente." });

                var userId = Guid.Parse(tokenEntity.UserId);

                if (tokenEntity.Type == "email_change")
                {
                    // Confirmar troca: move pending_email → email
                    var pendingEmail = _users.GetPendingEmail(userId);
                    if (string.IsNullOrWhiteSpace(pendingEmail))
                        return Content(HttpStatusCode.BadRequest, new { error = "Nenhuma alteração de e-mail pendente encontrada." });

                    _users.UpdateEmail(userId, pendingEmail);
                    _users.SetPendingEmail(userId, null);
                    _users.MarkEmailVerified(userId);
                    _resets.MarcarComoUsado(tokenEntity.Id);
                    return Ok(new { message = "E-mail alterado e confirmado com sucesso! Faça login para continuar." });
                }
                else if (tokenEntity.Type == "email_verification")
                {
                    // Confirmação de cadastro novo
                    _users.MarkEmailVerified(userId);
                    _resets.MarcarComoUsado(tokenEntity.Id);
                    return Ok(new { message = "E-mail confirmado com sucesso! Faça login para acessar sua conta." });
                }
                else
                {
                    return Content(HttpStatusCode.BadRequest, new { error = "Tipo de token inválido." });
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // POST api/auth/resendverification
        [HttpPost, ActionName("resendverification")]
        public IHttpActionResult ResendVerification([FromBody] ResendVerificationRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Email))
                    return Ok();

                var user = _users.GetByEmail(req.Email.Trim().ToLowerInvariant());

                if (user == null || user.EmailVerified)
                    return Ok();

                _resets.InvalidarAnteriores(user.Id.ToString(), "email_verification");

                var bytes = new byte[32];
                using (var rng = new RNGCryptoServiceProvider())
                    rng.GetBytes(bytes);
                var tokenStr = BitConverter.ToString(bytes).Replace("-", "").ToLower();

                _resets.Create(new PasswordResetToken
                {
                    UserId    = user.Id.ToString(),
                    Token     = tokenStr,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    Used      = false,
                    Type      = "email_verification"
                });

                var urlBase = ConfigurationManager.AppSettings["App.Url"];
                var link    = string.Format("{0}/confirmar-email?token={1}", urlBase, tokenStr);

                try { _email.EnviarConfirmacaoEmail(user.Email, link); }
                catch { }

                return Ok();
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // POST api/auth/forgotpassword
        [HttpPost, ActionName("forgotpassword")]
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

        // POST api/auth/resetpassword
        [HttpPost, ActionName("resetpassword")]
        public IHttpActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
                    return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos." });

                var tokenEntity = _resets.GetByToken(req.Token);

                if (tokenEntity == null
                    || tokenEntity.Type != "password_reset"
                    || tokenEntity.Used
                    || tokenEntity.ExpiresAt < DateTime.UtcNow)
                    return Content(HttpStatusCode.BadRequest, new { error = "Link inválido ou expirado. Solicite um novo link." });

                _users.UpdatePassword(Guid.Parse(tokenEntity.UserId), PasswordHasher.Hash(req.NewPassword));
                _resets.MarcarComoUsado(tokenEntity.Id);

                return Ok();
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // Helper: lê e valida o Bearer token do header Authorization
        private ClaimsPrincipal ValidateBearerToken()
        {
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer" || string.IsNullOrWhiteSpace(authHeader.Parameter))
                return null;
            try { return _jwt.ValidateToken(authHeader.Parameter); }
            catch { return null; }
        }
    }

    // ── DTOs de request ────────────────────────────────────────────────────────
    public class ChangePasswordRequest      { public string CurrentPassword { get; set; } public string NewPassword { get; set; } }
    public class RequestEmailChangeRequest  { public string NewEmail        { get; set; } }
    public class ForgotPasswordRequest      { public string Email           { get; set; } }
    public class ResetPasswordRequest       { public string Token           { get; set; } public string NewPassword { get; set; } }
    public class VerifyEmailRequest         { public string Token           { get; set; } }
    public class ResendVerificationRequest  { public string Email           { get; set; } }
}
