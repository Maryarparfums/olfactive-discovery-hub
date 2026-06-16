using System.Web.Http;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("families")]
    public class FamiliesController : ApiController
    {
        private readonly IFamilyRepository _repo;
        public FamiliesController() : this(new FamilyRepository()) { }
        public FamiliesController(IFamilyRepository repo) { _repo = repo; }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll() => Ok(_repo.GetAll());
    }
}
