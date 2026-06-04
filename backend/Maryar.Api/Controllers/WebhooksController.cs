using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;
using Maryar.Api.Services;
using Newtonsoft.Json.Linq;

namespace Maryar.Api.Controllers
{
    // Endpoint público: a InfinitePay POSTa eventos aqui (status do pagamento).
    // Segurança: assinatura HMAC-SHA256 do corpo bruto + tabela payment_events para idempotência.
    [RoutePrefix("api/public/webhooks")]
    public class WebhooksController : ApiController
    {
        private readonly IOrderRepository _orders;
        private readonly IPaymentEventRepository _events;

        public WebhooksController()
            : this(new OrderRepository(), new PaymentEventRepository()) { }

        public WebhooksController(IOrderRepository orders, IPaymentEventRepository events)
        {
            _orders = orders;
            _events = events;
        }

        [HttpPost, Route("infinitepay")]
        public async Task<IHttpActionResult> InfinitePay()
        {
            var rawBody = await Request.Content.ReadAsStringAsync();
            var secret = AppConfig.Get("InfinitePay:WebhookSecret");

            string signature = null;
            IEnumerable<string> values;
            if (Request.Headers.TryGetValues("X-Infinitepay-Signature", out values))
                signature = values.FirstOrDefault();
            else if (Request.Headers.TryGetValues("X-Signature", out values))
                signature = values.FirstOrDefault();

            if (!HmacValidator.IsValid(rawBody, signature, secret))
                return Content(HttpStatusCode.Unauthorized, new { error = "Assinatura inválida." });

            JObject payload;
            try { payload = JObject.Parse(rawBody); }
            catch { return Content(HttpStatusCode.BadRequest, new { error = "Payload inválido." }); }

            var eventId = (string)payload.SelectToken("id")
                          ?? (string)payload.SelectToken("event_id")
                          ?? Guid.NewGuid().ToString();
            var eventType = (string)payload.SelectToken("type")
                            ?? (string)payload.SelectToken("event")
                            ?? "unknown";
            var chargeId = (string)payload.SelectToken("data.charge.id")
                           ?? (string)payload.SelectToken("data.id")
                           ?? (string)payload.SelectToken("charge_id");
            var newStatus = (string)payload.SelectToken("data.charge.status")
                            ?? (string)payload.SelectToken("data.status")
                            ?? (string)payload.SelectToken("status");

            Order order = null;
            if (!string.IsNullOrEmpty(chargeId))
                order = _orders.GetByChargeId(chargeId);

            var inserted = _events.TryInsert(new PaymentEvent
            {
                EventId = eventId,
                OrderId = order != null ? order.Id : (Guid?)null,
                EventType = eventType,
                PayloadJson = rawBody
            });

            // Idempotência: se já processamos este evento, devolvemos 200 sem reaplicar.
            if (!inserted) return Ok(new { ok = true, duplicate = true });

            if (order != null && !string.IsNullOrEmpty(newStatus))
            {
                if (string.Equals(newStatus, "paid", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(newStatus, "approved", StringComparison.OrdinalIgnoreCase))
                {
                    _orders.UpdatePaymentStatus(order.Id, "paid", "paid");
                }
                else if (string.Equals(newStatus, "failed", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(newStatus, "declined", StringComparison.OrdinalIgnoreCase))
                {
                    _orders.UpdatePaymentStatus(order.Id, "failed", "canceled");
                }
                else if (string.Equals(newStatus, "refunded", StringComparison.OrdinalIgnoreCase))
                {
                    _orders.UpdatePaymentStatus(order.Id, "refunded", "canceled");
                }
            }

            return Ok(new { ok = true });
        }
    }

    // alias para o "using" enxuto
    using global = System.Collections.Generic;
}
