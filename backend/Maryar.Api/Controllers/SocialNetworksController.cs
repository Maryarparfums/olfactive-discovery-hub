using System.Collections.Generic;
using System.Web.Http;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("social-networks")]
    public class SocialNetworksController : ApiController
    {
        private static string ConnStr =>
            System.Configuration.ConfigurationManager
                  .ConnectionStrings["MaryarDB"].ConnectionString;

        // GET /api/social-networks
        // Retorna apenas as redes com active = 1, na ordem definida
        [HttpGet, Route("")]
        public IHttpActionResult Get()
        {
            var list = new List<object>();

            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(@"
                    SELECT name, url
                    FROM social_networks
                    WHERE active = 1
                    ORDER BY sort_order ASC, id ASC", conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        list.Add(new
                        {
                            name = reader.GetString("name"),
                            url  = reader.GetString("url"),
                        });
            }

            return Ok(list);
        }
    }
}
