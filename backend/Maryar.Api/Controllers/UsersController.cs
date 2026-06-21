using System.Net;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Infrastructure;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("users")]
    public class UsersController : ApiController
    {
        private readonly IUserRepository _users;

        public UsersController()
            : this(new UserRepository()) { }

        public UsersController(IUserRepository users)
        {
            _users = users;
        }

        // GET /api/users/me
        [HttpGet, Route("me")]
        [JwtAuth]
        public IHttpActionResult GetMe()
        {
            var userId = JwtAuthAttribute.CurrentUserId();
            if (!userId.HasValue)
                return Content(HttpStatusCode.Unauthorized, new { error = "Não autenticado." });

            var user = _users.GetById(userId.Value);
            if (user == null)
                return NotFound();

            return Ok(new UserProfileDto
            {
                Name        = user.Name,
                Email       = user.Email,
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

        // PUT /api/users/me
        [HttpPut, Route("me")]
        [JwtAuth]
        public IHttpActionResult UpdateMe([FromBody] UserProfileDto dto)
        {
            if (dto == null)
                return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos." });

            var userId = JwtAuthAttribute.CurrentUserId();
            if (!userId.HasValue)
                return Content(HttpStatusCode.Unauthorized, new { error = "Não autenticado." });

            var user = _users.GetById(userId.Value);
            if (user == null)
                return NotFound();

            _users.UpdateProfile(userId.Value, dto);

            return Ok(new { message = "Perfil atualizado com sucesso." });
        }
    }
}
