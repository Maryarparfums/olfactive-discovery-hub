using System.Web.Http;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("ocasioes")]
    public class OcasioesController : ApiController
    {
        private readonly IOcasiaoRepository _repo;
        public OcasioesController() : this(new OcasiaoRepository()) { }
        public OcasioesController(IOcasiaoRepository repo) { _repo = repo; }

        // GET /api/ocasioes
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            return Ok(_repo.GetAll());
        }
    }
}
