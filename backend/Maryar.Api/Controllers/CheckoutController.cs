using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;
using Maryar.Api.Services;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("checkout")]
    public class CheckoutController : ApiController
    {
        private const string CookieName = "maryar_cart";
        private readonly ICartRepository _carts;
        private readonly IProductRepository _products;
        private readonly IOrderRepository _orders;
        private readonly InfinitePayClient _infinite;

        public CheckoutController()
            : this(new CartRepository(), new ProductRepository(),
                   new OrderRepository(), new InfinitePayClient()) { }

        public CheckoutController(ICartRepository carts, IProductRepository products,
                                  IOrderRepository orders, InfinitePayClient infinite)
        {
            _carts = carts; _products = products; _orders = orders; _infinite = infinite;
        }

        private Guid? ResolveCartId()
        {
            var userId = JwtAuthAttribute.CurrentUserId();
            string token = null;
            var ctx = HttpContext.Current;
            if (ctx != null)
            {
                var c = ctx.Request.Cookies[CookieName];
                token = c != null ? c.Value : null;
            }
            if (!userId.HasValue && string.IsNullOrEmpty(token)) return null;
            return _carts.GetOrCreate(userId, token).Id;
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Process([FromBody] CheckoutRequest req)
        {
            if (req == null || req.Customer == null || req.Shipping == null)
                return Content(HttpStatusCode.BadRequest, new { error = "Dados incompletos." });

            var cartId = ResolveCartId();
            if (!cartId.HasValue)
                return Content(HttpStatusCode.BadRequest, new { error = "Carrinho vazio." });

            var cartItems = _carts.GetItems(cartId.Value).ToList();
            if (cartItems.Count == 0)
                return Content(HttpStatusCode.BadRequest, new { error = "Carrinho vazio." });

            var productsById = cartItems
                .Select(ci => _products.GetById(ci.ProductId))
                .Where(p => p != null)
                .ToDictionary(p => p.Id, p => p);

            var pricing = PricingService.Calculate(cartItems, productsById);
            if (pricing.Total <= 0)
                return Content(HttpStatusCode.BadRequest, new { error = "Não foi possível calcular o total." });

            var orderId = Guid.NewGuid();
            var orderNumber = "MAR-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            var order = new Order
            {
                Id = orderId,
                OrderNumber = orderNumber,
                UserId = JwtAuthAttribute.CurrentUserId(),
                CustomerName = req.Customer.Name,
                CustomerEmail = req.Customer.Email,
                CustomerDocument = req.Customer.Document,
                CustomerPhone = req.Customer.Phone,
                ShippingZip = req.Shipping.Zip,
                ShippingStreet = req.Shipping.Street,
                ShippingNumber = req.Shipping.Number,
                ShippingComplement = req.Shipping.Complement,
                ShippingNeighborhood = req.Shipping.Neighborhood,
                ShippingCity = req.Shipping.City,
                ShippingState = req.Shipping.State,
                Subtotal = pricing.Subtotal,
                ShippingFee = pricing.ShippingFee,
                Discount = pricing.Discount,
                Total = pricing.Total,
                PaymentMethod = "infinitepay",
                PaymentStatus = "pending",
                OrderStatus = "created"
            };
            foreach (var it in pricing.Items) it.OrderId = orderId;
            _orders.Create(order, pricing.Items);

            try
            {
                var baseUrl = "https://maryar.com.br";
                var redirectUrl = $"{baseUrl}/pedido/{orderId}";
                var webhookUrl = $"{baseUrl}/api/checkout/webhook";

                var linkItems = pricing.Items.Select(i => new InfinitePayLinkItem
                {
                    Quantity = i.Quantity,
                    Price = (int)Math.Round(i.UnitPrice * 100),
                    Description = i.ProductName ?? "Perfume"
                }).ToList();

                var paymentUrl = await _infinite.CreatePaymentLinkAsync(
                    orderNumber, linkItems, redirectUrl, webhookUrl,
                    req.Customer, req.Shipping);

                return Ok(new
                {
                    orderId,
                    orderNumber,
                    paymentUrl
                });
            }
            catch (Exception ex)
            {
                _orders.UpdatePaymentStatus(orderId, "failed", "canceled");
                return Content(HttpStatusCode.BadGateway, new { error = "Falha ao gerar link: " + ex.Message });
            }
        }

        [HttpPost, Route("webhook")]
        public IHttpActionResult Webhook([FromBody] InfinitePayWebhookPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.OrderNsu))
                return BadRequest();

            try
            {
                var order = _orders.GetByOrderNumber(payload.OrderNsu);
                if (order == null) return BadRequest();

                _orders.UpdatePaymentStatus(order.Id, "paid", "confirmed");
                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet, Route("orders")]
        public IHttpActionResult GetMyOrders()
        {
            var userId = JwtAuthAttribute.CurrentUserId();
            string token = null;
            var ctx = HttpContext.Current;
            if (ctx != null)
            {
                var c = ctx.Request.Cookies[CookieName];
                token = c != null ? c.Value : null;
            }
            if (!userId.HasValue && string.IsNullOrEmpty(token))
                return Ok(new List<object>());

            var orders = _orders.GetByUserOrToken(userId, token);
            return Ok(orders);
        }

        [HttpGet, Route("orders/{id:guid}")]
        public IHttpActionResult GetOrder(Guid id)
        {
            var o = _orders.GetById(id);
            if (o == null) return NotFound();
            var items = _orders.GetItems(id);
            return Ok(new { order = o, items });
        }
    }
}
