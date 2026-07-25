using System;
using System.Web.Http;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace MaryAr.Controllers
{
    /// <summary>
    /// GET /api/payment-config
    /// Retorna configurações de pagamento, incluindo o valor mínimo de parcela.
    /// </summary>
    [RoutePrefix("api/payment-config")]
    public class PaymentConfigController : ApiController
    {
        private string ConnectionString =>
            ConfigurationManager.ConnectionStrings["MaryArDb"].ConnectionString;

        // GET /api/payment-config
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            try
            {
                using (var conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        "SELECT min_installment_value FROM payment_config LIMIT 1", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var minInstallmentValue = reader.GetDecimal("min_installment_value");
                            return Ok(new
                            {
                                minInstallmentValue = (double)minInstallmentValue
                            });
                        }

                        // Tabela vazia — sem restrição de parcela mínima
                        return Ok(new { minInstallmentValue = 0.0 });
                    }
                }
            }
            catch (Exception ex)
            {
                // Em produção, logue o erro internamente e não exponha detalhes
                return InternalServerError(new Exception("Erro ao buscar configuração de pagamento."));
            }
        }
    }
}
