using System;
using System.Net;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;
using Maryar.Api.Services;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly IUserRepository _users;
        private readonly JwtService _jwt;

        public AuthController() : this(new UserRepository(), new JwtService()) { }
        public AuthController(IUserRepository users, JwtService jwt) { _users = users; _jwt = jwt; }

        [HttpPost, Route("signup")]
        public IHttpActionResult SignUp([FromBody] SignUpRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email)
                || string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
                return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos (senha mínima 8 caracteres)." });

            if (_users.GetByEmail(req.Email) != null)
                return Content(HttpStatusCode.Conflict, new { error = "E-mail já cadastrado." });

            var user = new User
            {
                Email = req.Email.Trim().ToLowerInvariant(),
                Name = req.Name?.Trim(),
                PasswordHash = PasswordHasher.Hash(req.Password),
                Role = "customer"
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

        [HttpPost, Route("signin")]
        public IHttpActionResult SignIn([FromBody] SignInRequest req)
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
    }
}
