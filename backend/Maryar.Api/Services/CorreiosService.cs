using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public string PesoKg      { get; set; }
        public string Comprimento { get; set; }
        public string Largura     { get; set; }
        public string Altura      { get; set; }
        public string CaixaCodigo { get; set; }
    }

    public class CorreiosService
    {
        private static readonly HttpClient _http = new HttpClient();

        private readonly string _codigoAcesso;
        private readonly string _cartao;
        private readonly string _contrato;
        private readonly int    _dr;
        private readonly string _cepOrigem;

        private const string TokenUrl = "https://api.correios.com.br/token/v1/autentica/cartaopostagem";
        private const string PrecoUrl = "https://api.correios.com.br/preco/v1/nacional";

        private static string   _cachedToken;
        private static DateTime _tokenExpira = DateTime.MinValue;

        private static readonly string[] OrdemCaixas = { "P", "M", "G", "GG" };

        public CorreiosService()
        {
            _codigoAcesso = ConfigurationManager.AppSettings["Correios.CodigoAcesso"] ?? throw new Exception("Correios.CodigoAcesso não configurado no web.config");
            _cartao       = ConfigurationManager.AppSettings["Correios.Cartao"]       ?? throw new Exception("Correios.Cartao não configurado no web.config");
            _contrato     = ConfigurationManager.AppSettings["Correios.Contrato"]     ?? throw new Exception("Correios.Contrato não configurado no web.config");
            _cepOrigem    = ConfigurationManager.AppSettings["Correios.CepOrigem"]    ?? throw new Exception("Correios.CepOrigem não configurado no web.config");

            var drStr = ConfigurationManager.AppSettings["Correios.DR"] ?? throw new Exception("Correios.DR não configurado no web.config");
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

            var caixasOrdenadas = todasCaixas
                .OrderBy(c => Array.IndexOf(OrdemCaixas, c.Code))
                .ToList();

            int indiceMinimo = Array.IndexOf(OrdemCaixas, codigoMinimoExigido);

            BoxSize caixaEscolhida = caixasOrdenadas
                .Where(c => Array.IndexOf(OrdemCaixas, c.Code) >= indiceMinimo
                         && c.MaxWeightG >= pesoTotalG)
                .FirstOrDefault();

            if (caixaEscolhida == null)
                throw new Exception($"Nenhuma caixa disponível para {pesoTotalG}g.");

            return new PackageInfo
            {
                PesoKg      = (pesoTotalG / 1000.0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
                Comprimento = caixaEscolhida.Comprimento.ToString(),
                Largura     = caixaEscolhida.Largura.ToString(),
                Altura      = caixaEscolhida.Altura.ToString(),
                CaixaCodigo = caixaEscolhida.Code
            };
        }

        private async Task<string> GetTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpira)
                return _cachedToken;

            // Basic auth usa só o código de acesso como username, senha vazia
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_codigoAcesso}:"));

            var body = new
            {
                numero   = _cartao,
                contrato = _contrato,
                dr       = _dr
            };

            var req = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            req.Headers.Add("User-Agent", "Maryar/1.0");
            req.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json");

            var res  = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception(
                    $"Correios auth falhou | HTTP {(int)res.StatusCode} {res.ReasonPhrase} | " +
                    $"Cartao={_cartao} | Contrato={_contrato} | DR={_dr} | " +
                    $"Body={( string.IsNullOrEmpty(json) ? "(vazio)" : json )}");

            dynamic data = JsonConvert.DeserializeObject(json);
            _cachedToken = (string)data.token;
            _tokenExpira = DateTime.UtcNow.AddHours(1);
            return _cachedToken;
        }

        private async Task<ShippingOption> GetPrecoAsync(
            string token, string codigoServico, string nomeServico,
            string cepDestino, PackageInfo pkg)
        {
            var body = new
            {
                idServico          = codigoServico,
                cepOrigem          = _cepOrigem.Replace("-", "").Trim(),
                cepDestino         = cepDestino.Replace("-", "").Trim(),
                psObjeto           = pkg.PesoKg,
                tpObjeto           = "2",
                comprimento        = pkg.Comprimento,
                largura            = pkg.Largura,
                altura             = pkg.Altura,
                servicosAdicionais = new string[] { },
                vlDeclarado        = "0"
            };

            var req = new HttpRequestMessage(HttpMethod.Post, PrecoUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Add("User-Agent", "Maryar/1.0");
            req.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json");

            var res  = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception(
                    $"Correios: erro ao calcular {nomeServico} | HTTP {(int)res.StatusCode} {res.ReasonPhrase} | " +
                    $"Body={( string.IsNullOrEmpty(json) ? "(vazio)" : json )}");

            dynamic data  = JsonConvert.DeserializeObject(json);
            dynamic item  = data[0];
            decimal preco = decimal.Parse(
                ((string)item.pcFinal).Replace(",", "."),
                System.Globalization.CultureInfo.InvariantCulture);
            int prazo = (int)item.prazoEntrega;

            return new ShippingOption
            {
                Code         = codigoServico,
                Name         = nomeServico,
                Price        = preco,
                DeliveryDays = prazo,
                Description  = $"Entrega em até {prazo} dia{(prazo > 1 ? "s" : "")} útil{(prazo > 1 ? "is" : "")}"
            };
        }

        public async Task<List<ShippingOption>> CalcularFreteAsync(
            string cepDestino,
            List<CartItemInfo> itens,
            List<BoxSize> todasCaixas)
        {
            var pkg   = EscolherEmbalagem(itens, todasCaixas);
            var token = await GetTokenAsync();

            var sedexTask = GetPrecoAsync(token, "03220", "SEDEX", cepDestino, pkg);
            var pacTask   = GetPrecoAsync(token, "03298", "PAC",   cepDestino, pkg);
            await Task.WhenAll(sedexTask, pacTask);

            var sedex = sedexTask.Result;
            var pac   = pacTask.Result;

            return pac.Price <= sedex.Price
                ? new List<ShippingOption> { pac, sedex }
                : new List<ShippingOption> { sedex, pac };
        }
    }
}
