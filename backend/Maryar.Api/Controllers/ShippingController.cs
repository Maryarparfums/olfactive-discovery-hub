using System;
using System.Threading.Tasks;
using System.Web.Http;
using Maryar.Api.Services;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("api/shipping")]
    public class ShippingController : ApiController
    {
        private readonly CorreiosService _correios = new CorreiosService();

        // GET /api/shipping/calculate?cep=01310100
        [HttpGet, Route("calculate")]
        public async Task<IHttpActionResult> Calculate([FromUri] string cep)
        {
            if (string.IsNullOrWhiteSpace(cep) || cep.Replace("-", "").Trim().Length != 8)
                return BadRequest("CEP inválido.");

            try
            {
                var options = await _correios.CalcularFreteAsync(cep);
                return Ok(options);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Erro ao calcular frete: " + ex.Message));
            }
        }
    }
}
