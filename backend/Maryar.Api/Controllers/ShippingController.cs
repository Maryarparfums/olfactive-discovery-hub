using System;
using System.Threading.Tasks;
using Maryar.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maryar.Api.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class ShippingController : ControllerBase
    {
        private readonly CorreiosService _correios;

        public ShippingController(CorreiosService correios)
        {
            _correios = correios;
        }

        // GET /api/shipping/calculate?cep=01310100
        [HttpGet("calculate")]
        public async Task<IActionResult> Calculate([FromQuery] string cep)
        {
            if (string.IsNullOrWhiteSpace(cep) || cep.Replace("-", "").Trim().Length != 8)
                return BadRequest(new { error = "CEP inválido." });

            try
            {
                var options = await _correios.CalcularFreteAsync(cep);
                return Ok(options);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao calcular frete: " + ex.Message });
            }
        }
    }
}
