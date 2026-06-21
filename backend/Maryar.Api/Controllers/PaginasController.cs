using System.Net;
using System.Web.Http;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("paginas")]
    public class PaginasController : ApiController
    {
        private readonly IPaginaRepository _paginas;

        public PaginasController() : this(new PaginaRepository()) { }
        public PaginasController(IPaginaRepository paginas) { _paginas = paginas; }

        // GET /api/paginas/termos
        // GET /api/paginas/privacidade
        // GET /api/paginas/cookies
        [HttpGet, Route("{slug}")]
        public IHttpActionResult Get(string slug)
        {
            var page = _paginas.GetBySlug(slug);
            if (page == null)
                return Content(HttpStatusCode.NotFound, new { error = "Página não encontrada." });
            return Ok(page);
        }

        // GET /api/paginas/faq/faq
        // GET /api/paginas/faq/devolucoes
        [HttpGet, Route("faq/{tipo}")]
        public IHttpActionResult GetFaq(string tipo)
        {
            if (tipo != "faq" && tipo != "devolucoes")
                return Content(HttpStatusCode.BadRequest, new { error = "Tipo inválido." });
            return Ok(_paginas.GetFaqByTipo(tipo));
        }
    }
}
