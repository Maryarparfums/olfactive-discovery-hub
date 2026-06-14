using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;
using Newtonsoft.Json;

namespace Maryar.Api.Controllers
{
    public class ProductsController : ApiController
    {
        private readonly IProductRepository _repo;
        public ProductsController() : this(new ProductRepository()) { }
        public ProductsController(IProductRepository repo) { _repo = repo; }

        [HttpGet]
        public IHttpActionResult List([FromUri] ProductQueryDto query)
        {
            if (query == null) query = new ProductQueryDto();
            int total;
            var items = _repo.Query(query, out total)
                .Select(p => new ProductListItemDto
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Name = p.Name,
                    Brand = p.BrandName,
                    Family = p.FamilyName,
                    Concentration = p.Concentration,
                    VolumeMl = p.VolumeMl,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl
                })
                .ToList();

            return Ok(new
            {
                total,
                page = query.Page < 1 ? 1 : query.Page,
                pageSize = query.PageSize < 1 ? 24 : query.PageSize,
                items
            });
        }

        [HttpGet]
        public IHttpActionResult GetBySlug(string slug)
        {
            var p = _repo.GetBySlug(slug);
            if (p == null) return NotFound();

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

            dto.Fixacao = 99;
            dto.Projecao = 99;
            dto.DuracaoHoras = "TESTE";
            
            dto.NotasTopo = new List<string> { "Teste" };

            try
            {
                var d = _repo.GetDetailsByProductId(p.Id);
                if (d != null)
                {
                    dto.NotasTopo    = Deserialize<List<string>>(d.NotasTopoJson);
                    dto.NotasCoracao = Deserialize<List<string>>(d.NotasCoracaoJson);
                    dto.NotasBase    = Deserialize<List<string>>(d.NotasBaseJson);
                    dto.Estacao      = Deserialize<Dictionary<string, int>>(d.EstacaoJson);
                    dto.Periodo      = Deserialize<Dictionary<string, int>>(d.PeriodoJson);
                    dto.Ocasiao      = Deserialize<Dictionary<string, int>>(d.OcasiaoJson);
                    dto.Similares    = Deserialize<List<string>>(d.SimilaresJson);
                    dto.Fixacao      = d.Fixacao;
                    dto.Projecao     = d.Projecao;
                    dto.DuracaoHoras = d.DuracaoHoras;
                }
            }
            catch { }

            return Ok(dto);
        }

        private static T Deserialize<T>(string json) where T : new()
        {
            if (string.IsNullOrWhiteSpace(json)) return new T();
            try { return JsonConvert.DeserializeObject<T>(json); }
            catch { return new T(); }
        }
    }
}
