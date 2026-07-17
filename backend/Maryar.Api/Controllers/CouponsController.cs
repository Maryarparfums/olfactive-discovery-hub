using System;
using System.Configuration;
using System.Web.Http;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("api/coupons")]
    public class CouponsController : ApiController
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["MaryarDb"].ConnectionString;

        [HttpGet]
        [Route("validate")]
        public IHttpActionResult Validate(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest("Slug obrigatório.");

            using (var con = new MySqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    "SELECT slug, percent, commission FROM coupons WHERE UPPER(slug) = UPPER(@slug) LIMIT 1",
                    con))
                {
                    cmd.Parameters.AddWithValue("@slug", slug.Trim());
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            return NotFound();

                        return Ok(new
                        {
                            slug       = rd.GetString("slug"),
                            percent    = rd.GetDecimal("percent"),
                            commission = rd.GetDecimal("commission")
                        });
                    }
                }
            }
        }
    }
}
