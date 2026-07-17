using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;
using Maryar.Api.Services;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("shipping")]
    public class ShippingController : ApiController
    {
        private string ConnStr =>
            ConfigurationManager.ConnectionStrings["MaryarDb"].ConnectionString;

        // GET /api/shipping/calculate?cep=01310100&items=VARIANT_OR_PRODUCT_ID:2,...
        [HttpGet, Route("calculate")]
        public async Task<IHttpActionResult> Calculate(
            [FromUri] string cep,
            [FromUri] string items = null)
        {
            if (string.IsNullOrWhiteSpace(cep) || cep.Replace("-", "").Trim().Length != 8)
                return BadRequest("CEP inválido.");

            try
            {
                var cartItems = ParseCartItems(items);

                List<CartItemInfo> cartInfos;
                List<BoxSize>      todasCaixas;

                using (var conn = new MySqlConnection(ConnStr))
                {
                    await conn.OpenAsync();
                    cartInfos   = await BuscarDadosProdutos(conn, cartItems);
                    todasCaixas = await BuscarCaixas(conn);
                }

                var svc    = new CorreiosService();
                var opcoes = await svc.CalcularFreteAsync(cep, cartInfos, todasCaixas);
                return Ok(opcoes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private List<Tuple<string, int>> ParseCartItems(string items)
        {
            var result = new List<Tuple<string, int>>();
            if (string.IsNullOrWhiteSpace(items)) return result;

            foreach (var part in items.Split(','))
            {
                var seg = part.Trim().Split(':');
                if (seg.Length == 2 && int.TryParse(seg[1], out int qty) && qty > 0)
                    result.Add(Tuple.Create(seg[0].Trim(), qty));
            }
            return result;
        }

        private async Task<List<CartItemInfo>> BuscarDadosProdutos(
            MySqlConnection conn,
            List<Tuple<string, int>> cartItems)
        {
            var result = new List<CartItemInfo>();

            if (cartItems.Count == 0)
            {
                result.Add(new CartItemInfo
                {
                    ProductId   = "default",
                    Quantity    = 1,
                    WeightG     = 300,
                    BoxSizeCode = "P"
                });
                return result;
            }

            foreach (var item in cartItems)
            {
                var id  = item.Item1;
                var qty = item.Item2;

                // 1) Tenta como product_id direto
                var cmd = new MySqlCommand(@"
                    SELECT p.weight_g, p.box_size_code
                    FROM products p
                    WHERE p.id = @id
                    LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@id", id);

                int?   weightG     = null;
                string boxSizeCode = null;

                using (var reader = (MySqlDataReader) await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        weightG     = Convert.ToInt32(reader["weight_g"]);
                        boxSizeCode = Convert.ToString(reader["box_size_code"]);
                    }
                }

                // 2) Não achou como produto — tenta como variant_id
                if (weightG == null)
                {
                    var cmd2 = new MySqlCommand(@"
                        SELECT p.weight_g, p.box_size_code
                        FROM product_variants v
                        INNER JOIN products p ON p.id = v.product_id
                        WHERE v.id = @id
                        LIMIT 1", conn);
                    cmd2.Parameters.AddWithValue("@id", id);

                    using (var reader2 = (MySqlDataReader) await cmd2.ExecuteReaderAsync())
                    {
                        if (await reader2.ReadAsync())
                        {
                            weightG     = Convert.ToInt32(reader2["weight_g"]);
                            boxSizeCode = Convert.ToString(reader2["box_size_code"]);
                        }
                    }
                }

                // 3) Fallback: ID não existe em nenhuma tabela (carrinho antigo/dado obsoleto)
                result.Add(new CartItemInfo
                {
                    ProductId   = id,
                    Quantity    = qty,
                    WeightG     = weightG     ?? 300,
                    BoxSizeCode = boxSizeCode ?? "P"
                });
            }

            return result;
        }

        private async Task<List<BoxSize>> BuscarCaixas(MySqlConnection conn)
        {
            var result = new List<BoxSize>();
            var cmd = new MySqlCommand(
                "SELECT code, comprimento, largura, altura, max_weight_g, sort_order FROM box_sizes ORDER BY sort_order",
                conn);

            using (var reader = (MySqlDataReader) await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new BoxSize
                    {
                        Code        = Convert.ToString(reader["code"]),
                        Comprimento = Convert.ToInt32(reader["comprimento"]),
                        Largura     = Convert.ToInt32(reader["largura"]),
                        Altura      = Convert.ToInt32(reader["altura"]),
                        MaxWeightG  = Convert.ToInt32(reader["max_weight_g"]),
                        SortOrder   = Convert.ToInt32(reader["sort_order"])
                    });
                }
            }

            return result;
        }
    }
}
