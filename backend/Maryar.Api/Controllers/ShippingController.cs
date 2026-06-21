using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Maryar.Api.Services;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("shipping")]
    public class ShippingController : ApiController
    {
        private readonly CorreiosService _correios;

        public ShippingController() : this(new CorreiosService()) { }

        public ShippingController(CorreiosService correios)
        {
            _correios = correios;
        }

        // GET /api/shipping/calculate?cep=01310100
        [HttpGet, Route("calculate")]
        public async Task<IHttpActionResult> Calculate([FromUri] string cep)
        {
            if (string.IsNullOrWhiteSpace(cep) || cep.Replace("-", "").Trim().Length != 8)
                return Content(HttpStatusCode.BadRequest, new { error = "CEP inválido." });

            try
            {
                var options = await _correios.CalcularFreteAsync(cep);
                return Ok(options);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new { error = "Erro ao calcular frete: " + ex.Message });
            }
        }
    }
}
