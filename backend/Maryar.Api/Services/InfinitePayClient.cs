using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Maryar.Api.Dtos;
using Newtonsoft.Json;

namespace Maryar.Api.Services
{
    public class InfinitePayLinkItem
    {
        public int Quantity { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }
    }

    public class InfinitePayWebhookPayload
    {
        [JsonProperty("invoice_slug")]
        public string InvoiceSlug { get; set; }
        public int Amount { get; set; }
        [JsonProperty("paid_amount")]
        public int PaidAmount { get; set; }
        public int Installments { get; set; }
        [JsonProperty("capture_method")]
        public string CaptureMethod { get; set; }
        [JsonProperty("transaction_nsu")]
        public string TransactionNsu { get; set; }
        [JsonProperty("order_nsu")]
        public string OrderNsu { get; set; }
        [JsonProperty("receipt_url")]
        public string ReceiptUrl { get; set; }
    }

    public class InfinitePayClient
    {
        private const string Handle = "mariana-martins-f98";
        private static readonly HttpClient _http = new HttpClient();

       public async Task<string> CreatePaymentLinkAsync(
        string orderNsu,
        List<InfinitePayLinkItem> items,
        string redirectUrl,
        string webhookUrl,
        CustomerDto customer,
        ShippingAddressDto shipping)
        {
            var payload = new
            {
                handle = Handle,
                order_nsu = orderNsu,
                
                items = items.Select(i => new
                {
                    quantity = i.Quantity,
                    price = i.Price,
                    description = i.Description
                }),
                redirect_url = redirectUrl,
                webhook_url = webhookUrl,
                customer = new
                {
                    name = customer.Name,
                    email = customer.Email,
                    phone_number = customer.Phone
                },
                address = new
                {
                    cep = (shipping.Zip ?? "").Replace("-", "").Replace(".", ""),
                    street = shipping.Street,
                    neighborhood = shipping.Neighborhood,
                    number = shipping.Number,
                    complement = shipping.Complement ?? ""
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("https://api.checkout.infinitepay.io/links", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"InfinitePay erro {(int)response.StatusCode}: {body}");

            dynamic result = JsonConvert.DeserializeObject(body);

            // Tenta diferentes nomes de campo que a InfinitePay pode retornar
            string url = result?.url ?? result?.payment_url ?? result?.checkout_url ?? result?.link;

            if (string.IsNullOrEmpty(url))
                throw new Exception("InfinitePay não retornou URL. Resposta: " + body);

            return url;
        }
    }
}
