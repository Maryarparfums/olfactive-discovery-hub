using System;
using System.Net.Http;
using System.Threading.Tasks;
using Maryar.Api.Dtos;
using Maryar.Api.Infrastructure;
using Newtonsoft.Json;

namespace Maryar.Api.Services
{
    public class AsaasPixResult
    {
        public string PaymentId   { get; set; }
        public string QrCodeImage { get; set; } // base64 PNG
        public string QrCodeText  { get; set; } // copia-e-cola
    }

    public class AsaasCardResult
    {
        public string PaymentId { get; set; }
        public string Status    { get; set; } // CONFIRMED, PENDING, DECLINED
    }

    public class AsaasWebhookPayload
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("payment")]
        public AsaasPaymentData Payment { get; set; }
    }

    public class AsaasPaymentData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("externalReference")]
        public string ExternalReference { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class AsaasClient
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _apiKey;
        private const string BaseUrl = "https://www.asaas.com/api/v3";

        public AsaasClient()
        {
            _apiKey = AppConfig.AsaasApiKey;
        }

        private HttpRequestMessage Req(HttpMethod method, string path, object body = null)
        {
            var req = new HttpRequestMessage(method, BaseUrl + path);
            req.Headers.Add("access_token", _apiKey);
            if (body != null)
                req.Content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    System.Text.Encoding.UTF8,
                    "application/json");
            return req;
        }

        public async Task<string> GetOrCreateCustomerAsync(CustomerDto customer)
        {
            var cpf = (customer.Document ?? "").Replace(".", "").Replace("-", "").Trim();

            if (!string.IsNullOrEmpty(cpf))
            {
                var search = await _http.SendAsync(Req(HttpMethod.Get, $"/customers?cpfCnpj={cpf}&limit=1"));
                var searchJson = await search.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(searchJson);
                if (result?.data != null && result.totalCount > 0)
                    return (string)result.data[0].id;
            }

            var phone = (customer.Phone ?? "").Replace("(","").Replace(")","").Replace("-","").Replace(" ","");
            var create = await _http.SendAsync(Req(HttpMethod.Post, "/customers", new
            {
                name     = customer.Name,
                email    = customer.Email,
                phone    = phone,
                cpfCnpj = cpf
            }));
            var createJson = await create.Content.ReadAsStringAsync();
            if (!create.IsSuccessStatusCode)
                throw new Exception("Asaas erro ao criar cliente: " + createJson);

            dynamic created = JsonConvert.DeserializeObject(createJson);
            return (string)created.id;
        }

        public async Task<AsaasPixResult> CreatePixAsync(
            string customerId, decimal value, string orderNumber)
        {
            var dueDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            var pay = await _http.SendAsync(Req(HttpMethod.Post, "/payments", new
            {
                customer          = customerId,
                billingType       = "PIX",
                value             = value,
                dueDate           = dueDate,
                externalReference = orderNumber,
                description       = "Pedido Maryar " + orderNumber
            }));
            var payJson = await pay.Content.ReadAsStringAsync();
            if (!pay.IsSuccessStatusCode)
                throw new Exception("Asaas erro ao criar cobrança PIX: " + payJson);

            dynamic payment = JsonConvert.DeserializeObject(payJson);
            string pid = (string)payment.id;

            var qr = await _http.SendAsync(Req(HttpMethod.Get, $"/payments/{pid}/pixQrCode"));
            var qrJson = await qr.Content.ReadAsStringAsync();
            if (!qr.IsSuccessStatusCode)
                throw new Exception("Asaas erro ao gerar QR Code: " + qrJson);

            dynamic qrData = JsonConvert.DeserializeObject(qrJson);
            return new AsaasPixResult
            {
                PaymentId   = pid,
                QrCodeImage = (string)qrData.encodedImage,
                QrCodeText  = (string)qrData.payload
            };
        }

        public async Task<AsaasCardResult> CreateCreditCardAsync(
            string customerId, decimal value, string orderNumber,
            int installments, CreditCardDto card, CustomerDto holder,
            ShippingAddressDto shipping)
        {
            var installmentValue = Math.Round(value / installments, 2);
            var dueDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            var cpf = (holder.Document ?? "").Replace(".", "").Replace("-", "").Trim();
            var phone = (holder.Phone ?? "").Replace("(","").Replace(")","").Replace("-","").Replace(" ","");
            var zip = (shipping.Zip ?? "").Replace("-", "").Trim();

            var pay = await _http.SendAsync(Req(HttpMethod.Post, "/payments", new
            {
                customer          = customerId,
                billingType       = "CREDIT_CARD",
                value             = value,
                dueDate           = dueDate,
                externalReference = orderNumber,
                description       = "Pedido Maryar " + orderNumber,
                installmentCount  = installments > 1 ? installments : (int?)null,
                installmentValue  = installments > 1 ? installmentValue : (decimal?)null,
                creditCard = new
                {
                    holderName  = card.HolderName,
                    number      = card.Number.Replace(" ", ""),
                    expiryMonth = card.ExpiryMonth,
                    expiryYear  = card.ExpiryYear,
                    ccv         = card.Ccv
                },
                creditCardHolderInfo = new
                {
                    name         = holder.Name,
                    email        = holder.Email,
                    cpfCnpj      = cpf,
                    postalCode   = zip,
                    addressNumber = shipping.Number,
                    phone        = phone
                }
            }));
            var payJson = await pay.Content.ReadAsStringAsync();
            if (!pay.IsSuccessStatusCode)
                throw new Exception("Asaas erro no cartão: " + payJson);

            dynamic result = JsonConvert.DeserializeObject(payJson);
            return new AsaasCardResult
            {
                PaymentId = (string)result.id,
                Status    = (string)result.status
            };
        }
    }
}
