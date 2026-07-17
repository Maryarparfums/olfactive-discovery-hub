using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;
using Maryar.Api.Services;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("api/shipping")]
    public class ShippingController : ApiController
    {
        private string ConnStr =>
            ConfigurationManager.ConnectionStrings["MaryarDb"].ConnectionString;

        // GET api/shipping/calculate?cep=01310100&items=PROD_ID1:2,PROD_ID2:1
        // items = lista de "productId:quantidade" separados por vírgula
        [HttpGet, Route("calculate")]
        public async Task<IHttpActionResult> Calculate(
            [FromUri] string cep,
            [FromUri] string items = null)
        {
            if (string.IsNullOrWhiteSpace(cep) || cep.Replace("-","").Trim().Length != 8)
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

        // Faz parse da query string "id1:qtd1,id2:qtd2"
        private List<(string ProductId, int Qty)> ParseCartItems(string items)
        {
            var result = new List<(string, int)>();
            if (string.IsNullOrWhiteSpace(items)) return result;

            foreach (var part in items.Split(','))
            {
                var seg = part.Trim().Split(':');
                if (seg.Length == 2 && int.TryParse(seg[1], out int qty) && qty > 0)
                    result.Add((seg[0].Trim(), qty));
            }
            return result;
        }

        // Busca weight_g e box_size_code de cada produto no banco MySQL
        private async Task<List<CartItemInfo>> BuscarDadosProdutos(
            MySqlConnection conn,
            List<(string ProductId, int Qty)> cartItems)
        {
            var result = new List<CartItemInfo>();

            // Se não vieram itens, usa fallback (caixa P, 300g) para manter compatibilidade
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

            foreach (var (productId, qty) in cartItems)
            {
                // Nota MySQL: GetInt32/GetString só aceitam índice — usamos reader["coluna"] e Convert
                var cmd = new MySqlCommand(
                    "SELECT weight_g, box_size_code FROM products WHERE id = @id AND active = 1 LIMIT 1",
                    conn);
                cmd.Parameters.AddWithValue("@id", productId);

                using (var reader = (MySqlDataReader) await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        result.Add(new CartItemInfo
                        {
                            ProductId   = productId,
                            Quantity    = qty,
                            WeightG     = Convert.ToInt32(reader["weight_g"]),
                            BoxSizeCode = Convert.ToString(reader["box_size_code"])
                        });
                    }
                }
            }

            return result;
        }

        // Busca o catálogo de caixas do banco MySQL
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
