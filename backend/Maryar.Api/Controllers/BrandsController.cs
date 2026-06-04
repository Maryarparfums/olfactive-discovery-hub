using System.Web.Http;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("api/brands")]
    public class BrandsController : ApiController
    {
        private readonly IBrandRepository _repo;
        public BrandsController() : this(new BrandRepository()) { }
        public BrandsController(IBrandRepository repo) { _repo = repo; }

        [HttpGet, Route("")]
        public IHttpActionResult GetAll() => Ok(_repo.GetAll());
    }
}
