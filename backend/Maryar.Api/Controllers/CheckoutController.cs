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
    [RoutePrefix("api/checkout")]
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

            if (req.PaymentMethod != "pix" && req.PaymentMethod != "credit_card")
                return Content(HttpStatusCode.BadRequest, new { error = "Método de pagamento inválido." });

            if (req.PaymentMethod == "credit_card" && string.IsNullOrWhiteSpace(req.CardToken))
                return Content(HttpStatusCode.BadRequest, new { error = "Token de cartão obrigatório." });

            var cartId = ResolveCartId();
            if (!cartId.HasValue)
                return Content(HttpStatusCode.BadRequest, new { error = "Carrinho vazio." });

            var cartItems = _carts.GetItems(cartId.Value).ToList();
            if (cartItems.Count == 0)
                return Content(HttpStatusCode.BadRequest, new { error = "Carrinho vazio." });

            // Recalcula tudo no servidor com preços autoritativos.
            var productsById = cartItems
                .Select(ci => _products.GetById(ci.ProductId))
                .Where(p => p != null)
                .ToDictionary(p => p.Id, p => p);

            var pricing = PricingService.Calculate(cartItems, productsById);
            if (pricing.Total <= 0)
                return Content(HttpStatusCode.BadRequest, new { error = "Não foi possível calcular o total." });

            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                OrderNumber = "MAR-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
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
                PaymentMethod = req.PaymentMethod,
                PaymentStatus = "pending",
                OrderStatus = "created"
            };
            foreach (var it in pricing.Items) it.OrderId = orderId;
            _orders.Create(order, pricing.Items);

            var response = new CheckoutResponse
            {
                OrderId = orderId,
                OrderNumber = order.OrderNumber,
                PaymentStatus = "pending"
            };

            try
            {
                if (req.PaymentMethod == "pix")
                {
                    var pix = await _infinite.CreatePixChargeAsync(order);
                    _orders.UpdatePaymentInfo(orderId, pix.ChargeId, pix.QrCode, pix.CopyPaste);
                    response.PixQrCode = pix.QrCode;
                    response.PixCopyPaste = pix.CopyPaste;
                    response.Message = "Aguardando pagamento PIX.";
                }
                else
                {
                    var card = await _infinite.CreateCardChargeAsync(order, req.CardToken, req.Installments);
                    _orders.UpdatePaymentInfo(orderId, card.ChargeId, null, null);
                    if (string.Equals(card.Status, "approved", StringComparison.OrdinalIgnoreCase))
                    {
                        _orders.UpdatePaymentStatus(orderId, "paid", "paid");
                        response.PaymentStatus = "paid";
                        _carts.Clear(cartId.Value);
                    }
                    else
                    {
                        _orders.UpdatePaymentStatus(orderId, "failed", "canceled");
                        response.PaymentStatus = "failed";
                    }
                    response.Message = card.Message;
                }
            }
            catch (Exception ex)
            {
                _orders.UpdatePaymentStatus(orderId, "failed", "canceled");
                return Content(HttpStatusCode.BadGateway, new { error = "Falha no provedor: " + ex.Message });
            }

            return Ok(response);
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
