using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Maryar.Api.Dtos;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Maryar.Api.Services
{
    // Cliente HTTP da InfinitePay (endpoints e formatos exatos podem variar — revise contra a doc oficial
    // ao integrar). Toda comunicação é server-to-server e usa TLS 1.2 (forçado em Global.asax).
    public class InfinitePayClient
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _merchantHandle;

        public InfinitePayClient()
        {
            _baseUrl = AppConfig.Get("InfinitePay:BaseUrl") ?? "https://api.infinitepay.io";
            _apiKey = AppConfig.Get("InfinitePay:ApiKey");
            _merchantHandle = AppConfig.Get("InfinitePay:MerchantHandle");
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object body)
        {
            var req = new HttpRequestMessage(method, _baseUrl.TrimEnd('/') + path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return req;
        }

        public async Task<InfinitePayPixResult> CreatePixChargeAsync(Order order)
        {
            var payload = new
            {
                payment_method = "pix",
                amount = (long)(order.Total * 100),  // em centavos
                currency = "BRL",
                reference_id = order.OrderNumber,
                merchant_handle = _merchantHandle,
                customer = new
                {
                    name = order.CustomerName,
                    email = order.CustomerEmail,
                    document = order.CustomerDocument
                },
                description = "Pedido " + order.OrderNumber
            };

            using (var req = BuildRequest(HttpMethod.Post, "/v1/charges", payload))
            using (var resp = await Http.SendAsync(req))
            {
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException("InfinitePay PIX: " + (int)resp.StatusCode + " - " + body);

                var json = JObject.Parse(body);
                return new InfinitePayPixResult
                {
                    ChargeId = (string)json.SelectToken("id") ?? (string)json.SelectToken("charge_id"),
                    Status = (string)json.SelectToken("status"),
                    QrCode = (string)json.SelectToken("pix.qr_code")
                             ?? (string)json.SelectToken("qr_code"),
                    CopyPaste = (string)json.SelectToken("pix.copy_paste")
                                ?? (string)json.SelectToken("pix.payload")
                                ?? (string)json.SelectToken("copy_paste")
                };
            }
        }

        public async Task<InfinitePayCardResult> CreateCardChargeAsync(Order order, string cardToken, int installments)
        {
            var payload = new
            {
                payment_method = "credit_card",
                amount = (long)(order.Total * 100),
                currency = "BRL",
                reference_id = order.OrderNumber,
                merchant_handle = _merchantHandle,
                installments = installments < 1 ? 1 : installments,
                card_token = cardToken, // token gerado no navegador via SDK da InfinitePay
                customer = new
                {
                    name = order.CustomerName,
                    email = order.CustomerEmail,
                    document = order.CustomerDocument
                }
            };

            using (var req = BuildRequest(HttpMethod.Post, "/v1/charges", payload))
            using (var resp = await Http.SendAsync(req))
            {
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException("InfinitePay Card: " + (int)resp.StatusCode + " - " + body);

                var json = JObject.Parse(body);
                return new InfinitePayCardResult
                {
                    ChargeId = (string)json.SelectToken("id") ?? (string)json.SelectToken("charge_id"),
                    Status = (string)json.SelectToken("status"),
                    Message = (string)json.SelectToken("message")
                };
            }
        }
    }
}
