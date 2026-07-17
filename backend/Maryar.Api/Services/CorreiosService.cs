using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Maryar.Api.Services
{
    public class ShippingOption
    {
        public string  Code         { get; set; }
        public string  Name         { get; set; }
        public decimal Price        { get; set; }
        public int     DeliveryDays { get; set; }
        public string  Description  { get; set; }
    }

    public class CartItemInfo
    {
        public string ProductId   { get; set; }
        public int    Quantity    { get; set; }
        public int    WeightG     { get; set; }
        public string BoxSizeCode { get; set; }
    }

    public class BoxSize
    {
        public string Code        { get; set; }
        public int    Comprimento { get; set; }
        public int    Largura     { get; set; }
        public int    Altura      { get; set; }
        public int    MaxWeightG  { get; set; }
        public int    SortOrder   { get; set; }
    }

    public class PackageInfo
    {
        public string PesoG       { get; set; }
        public string Comprimento { get; set; }
        public string Largura     { get; set; }
        public string Altura      { get; set; }
        public string CaixaCodigo { get; set; }
    }

    public class CorreiosService
    {
        private static readonly HttpClient _http = new HttpClient();

        private readonly string _chaveAcesso;
        private readonly int    _dr;
        private readonly string _cepOrigem;

        private const string PrecoUrl = "https://api.correios.com.br/preco/v1/nacional";
        private const string PrazoUrl = "https://api.correios.com.br/prazo/v1/nacional";

        private static readonly string[] OrdemCaixas = { "P", "M", "G", "GG" };

        public CorreiosService()
        {
            _chaveAcesso = ConfigurationManager.AppSettings["Correios.ChaveAcesso"] ?? throw new Exception("Correios.ChaveAcesso não configurado no web.config");
            var drStr    = ConfigurationManager.AppSettings["Correios.DR"]          ?? throw new Exception("Correios.DR não configurado no web.config");
            _cepOrigem   = ConfigurationManager.AppSettings["Correios.CepOrigem"]   ?? throw new Exception("Correios.CepOrigem não configurado no web.config");
            _dr = int.Parse(drStr);
        }

        public PackageInfo EscolherEmbalagem(List<CartItemInfo> itens, List<BoxSize> todasCaixas)
        {
            if (itens == null || itens.Count == 0)
                throw new Exception("Carrinho vazio.");

            int pesoTotalG = itens.Sum(i => i.WeightG * i.Quantity);

            string codigoMinimoExigido = itens
                .Select(i => i.BoxSizeCode)
                .OrderByDescending(c => Array.IndexOf(OrdemCaixas, c))
                .First();

            int indiceMinimo = Array.IndexOf(OrdemCaixas, codigoMinimoExigido);

            BoxSize caixaEscolhida = todasCaixas
                .OrderBy(c => Array.IndexOf(OrdemCaixas, c.Code))
                .Where(c => Array.IndexOf(OrdemCaixas, c.Code) >= indiceMinimo
                         && c.MaxWeightG >= pesoTotalG)
                .FirstOrDefault();

            if (caixaEscolhida == null)
                throw new Exception($"Nenhuma caixa disponível para {pesoTotalG}g.");

            return new PackageInfo
            {
                PesoG       = pesoTotalG.ToString(),
                Comprimento = caixaEscolhida.Comprimento.ToString(),
                Largura     = caixaEscolhida.Largura.ToString(),
                Altura      = caixaEscolhida.Altura.ToString(),
                CaixaCodigo = caixaEscolhida.Code
            };
        }

        private async Task<Dictionary<string, decimal>> BuscarPrecosAsync(
            string cepDestino, PackageInfo pkg)
        {
            var body = new
            {
                idLote = "001",
                parametrosProduto = new[]
                {
                    new {
                        coProduto    = "03220",
                        nuRequisicao = "0001",
                        nuDR         = _dr,
                        cepOrigem    = _cepOrigem.Replace("-", "").Trim(),
                        cepDestino   = cepDestino.Replace("-", "").Trim(),
                        psObjeto     = pkg.PesoG,
                        nuUnidade    = "",
                        tpObjeto     = "2",
                        comprimento  = pkg.Comprimento,
                        largura      = pkg.Largura,
                        altura       = pkg.Altura
                    },
                    new {
                        coProduto    = "03298",
                        nuRequisicao = "0002",
                        nuDR         = _dr,
                        cepOrigem    = _cepOrigem.Replace("-", "").Trim(),
                        cepDestino   = cepDestino.Replace("-", "").Trim(),
                        psObjeto     = pkg.PesoG,
                        nuUnidade    = "",
                        tpObjeto     = "2",
                        comprimento  = pkg.Comprimento,
                        largura      = pkg.Largura,
                        altura       = pkg.Altura
                    }
                }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, PrecoUrl);
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_chaveAcesso}");
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            req.Headers.TryAddWithoutValidation("User-Agent", "Maryar/1.0");
            req.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json");

            var res  = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception(
                    $"Correios preço falhou | HTTP {(int)res.StatusCode} | Body={json}");

            dynamic lista = JsonConvert.DeserializeObject(json);
            var precos = new Dictionary<string, decimal>();

            foreach (var item in lista)
            {
                string txErro = (string)item.txErro;
                if (!string.IsNullOrEmpty(txErro)) continue;

                string codigo = (string)item.coProduto;
                decimal preco = decimal.Parse(
                    ((string)item.pcFinal).Replace(",", "."),
                    System.Globalization.CultureInfo.InvariantCulture);
                precos[codigo] = preco;
            }

            return precos;
        }

        private async Task<Dictionary<string, int>> BuscarPrazosAsync(
            string cepDestino)
        {
            var body = new
            {
                idLote = "001",
                parametrosPrazo = new[]
                {
                    new {
                        coProduto    = "03220",
                        nuRequisicao = "0001",
                        cepOrigem    = _cepOrigem.Replace("-", "").Trim(),
                        cepDestino   = cepDestino.Replace("-", "").Trim()
                    },
                    new {
                        coProduto    = "03298",
                        nuRequisicao = "0002",
                        cepOrigem    = _cepOrigem.Replace("-", "").Trim(),
                        cepDestino   = cepDestino.Replace("-", "").Trim()
                    }
                }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, PrazoUrl);
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_chaveAcesso}");
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            req.Headers.TryAddWithoutValidation("User-Agent", "Maryar/1.0");
            req.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json");

            var res  = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return new Dictionary<string, int> { { "03220", 2 }, { "03298", 7 } };

            try
            {
                dynamic lista = JsonConvert.DeserializeObject(json);
                var prazos = new Dictionary<string, int>();

                foreach (var item in lista)
                {
                    string txErro = (string)item.txErro;
                    if (!string.IsNullOrEmpty(txErro)) continue;

                    string codigo = (string)item.coProduto;
                    int prazo = (int)item.prazoEntrega;
                    prazos[codigo] = prazo;
                }

                if (!prazos.ContainsKey("03220")) prazos["03220"] = 2;
                if (!prazos.ContainsKey("03298")) prazos["03298"] = 7;

                return prazos;
            }
            catch
            {
                return new Dictionary<string, int> { { "03220", 2 }, { "03298", 7 } };
            }
        }

        public async Task<List<ShippingOption>> CalcularFreteAsync(
            string cepDestino,
            List<CartItemInfo> itens,
            List<BoxSize> todasCaixas)
        {
            var pkg = EscolherEmbalagem(itens, todasCaixas);

            var precosTask = BuscarPrecosAsync(cepDestino, pkg);
            var prazosTask = BuscarPrazosAsync(cepDestino);
            await Task.WhenAll(precosTask, prazosTask);

            var precos = precosTask.Result;
            var prazos = prazosTask.Result;

            var opcoes = new List<ShippingOption>();

            if (precos.ContainsKey("03220"))
            {
                int prazo = prazos.ContainsKey("03220") ? prazos["03220"] : 2;
                opcoes.Add(new ShippingOption
                {
                    Code         = "03220",
                    Name         = "SEDEX",
                    Price        = precos["03220"],
                    DeliveryDays = prazo,
                    Description  = $"Entrega em até {prazo} dia{(prazo > 1 ? "s" : "")} útil{(prazo > 1 ? "is" : "")}"
                });
            }

            if (precos.ContainsKey("03298"))
            {
                int prazo = prazos.ContainsKey("03298") ? prazos["03298"] : 7;
                opcoes.Add(new ShippingOption
                {
                    Code         = "03298",
                    Name         = "PAC",
                    Price        = precos["03298"],
                    DeliveryDays = prazo,
                    Description  = $"Entrega em até {prazo} dia{(prazo > 1 ? "s" : "")} útil{(prazo > 1 ? "is" : "")}"
                });
            }

            if (opcoes.Count == 0)
                throw new Exception("Nenhuma opção de frete disponível para o CEP informado.");

            return opcoes.OrderBy(o => o.Price).ToList();
        }
    }
}
