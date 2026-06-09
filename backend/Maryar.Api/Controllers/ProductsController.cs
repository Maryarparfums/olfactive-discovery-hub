using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;
using Newtonsoft.Json;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly IProductRepository _repo;
        public ProductsController() { _repo = null; }
        public ProductsController(IProductRepository repo) { _repo = repo; }

        [HttpGet, Route("")]
        public IHttpActionResult List()
        {
            return Ok(new { ok = true, msg = "hardcoded" });
        }

        [HttpGet, Route("{slug}")]
        public IHttpActionResult GetBySlug(string slug)
        {
            var p = _repo.GetBySlug(slug);
            if (p == null) return NotFound();
            var d = _repo.GetDetailsByProductId(p.Id);

            var dto = new ProductDetailDto
            {
                Id = p.Id,
                Slug = p.Slug,
                Name = p.Name,
                Brand = p.BrandName,
                Family = p.FamilyName,
                Concentration = p.Concentration,
                VolumeMl = p.VolumeMl,
                Price = p.Price,
                StockQty = p.StockQty,
                Description = p.Description,
                ImageUrl = p.ImageUrl,
                DetailImageUrl = p.DetailImageUrl
            };

            if (d != null)
            {
                dto.NotasTopo = JsonConvert.DeserializeObject<List<string>>(d.NotasTopoJson);
                dto.NotasCoracao = JsonConvert.DeserializeObject<List<string>>(d.NotasCoracaoJson);
                dto.NotasBase = JsonConvert.DeserializeObject<List<string>>(d.NotasBaseJson);
                dto.Estacao = JsonConvert.DeserializeObject<Dictionary<string, int>>(d.EstacaoJson);
                dto.Periodo = JsonConvert.DeserializeObject<Dictionary<string, int>>(d.PeriodoJson);
                dto.Ocasiao = JsonConvert.DeserializeObject<Dictionary<string, int>>(d.OcasiaoJson);
                dto.Fixacao = d.Fixacao;
                dto.Projecao = d.Projecao;
                dto.DuracaoHoras = d.DuracaoHoras;
                dto.Similares = JsonConvert.DeserializeObject<List<string>>(d.SimilaresJson);
            }

            return Ok(dto);
        }
    }
}
