using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;
using Maryar.Api.Services;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("checkout")]
    public class CheckoutController : ApiController
    {
        private const string CookieName = "maryar_cart";
        private readonly ICartRepository    _carts;
        private readonly IProductRepository _products;
        private readonly IOrderRepository   _orders;
        private readonly AsaasClient        _asaas;
        private readonly IUserRepository    _users;

        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["MaryarDb"].ConnectionString;

        // Dados completos de um cupom retornados pelo banco
        private class CouponInfo
        {
            public string  Slug       { get; set; }
            public decimal Percent    { get; set; } // desconto % para o cliente
            public Guid?   DealerId   { get; set; } // user_id do dono do cupom
            public decimal Commission { get; set; } // comissão % do dealer
        }

        public CheckoutController()
            : this(new CartRepository(), new ProductRepository(),
                   new OrderRepository(), new AsaasClient(),
                   new UserRepository()) { }

        public CheckoutController(ICartRepository carts, IProductRepository products,
                                  IOrderRepository orders, AsaasClient asaas,
                                  IUserRepository users)
        {
            _carts = carts; _products = products; _orders = orders;
            _asaas = asaas; _users = users;
        }

        private Guid? ResolveCartId()
        {
            var userId = JwtAuthAttribute.CurrentUserId();
            string token = null;
            var ctx = HttpContext.Current;
            if (ctx != null)
            {
                var c = ctx.Request.Cookies[CookieName];
                token = c?.Value;
            }
            if (!userId.HasValue && string.IsNullOrEmpty(token)) return null;
            return _carts.GetOrCreate(userId, token).Id;
        }

        private void TryAuthenticateRequest()
        {
            var auth = Request.Headers.Authorization;
            if (auth == null || auth.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter))
                return;
            try
            {
                var principal = new JwtService().ValidateToken(auth.Parameter);
                Thread.CurrentPrincipal = principal;
            }
            catch { /* token inválido — trata como visitante */ }
        }

        // Busca slug, percent, user_id e commission do cupom no banco.
        // Retorna null se o slug não existir ou estiver vazio.
        private CouponInfo LookupCoupon(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            using (var con = new MySqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    @"SELECT slug, percent, user_id, commission
                        FROM coupons
                       WHERE UPPER(slug) = UPPER(@slug)
                       LIMIT 1", con))
                {
                    cmd.Parameters.AddWithValue("@slug", slug.Trim());
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return null;

                        Guid? dealerId = null;
                        if (!rd.IsDBNull(rd.GetOrdinal("user_id")))
                            dealerId = rd.GetGuid("user_id");

                        return new CouponInfo
                        {
                            Slug       = rd.GetString("slug"),
                            Percent    = rd.GetDecimal("percent"),
                            DealerId   = dealerId,
                            Commission = rd.GetDecimal("commission")
                        };
                    }
                }
            }
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Process([FromBody] CheckoutRequest req)
        {
            TryAuthenticateRequest();

            if (req?.Customer == null || req.Shipping == null)
                return Content(HttpStatusCode.BadRequest, new { error = "Dados incompletos." });

            if (req.ShippingOption == null || req.ShippingOption.Price <= 0)
                return Content(HttpStatusCode.BadRequest, new { error = "Selecione uma opção de frete antes de continuar." });

            var method = (req.PaymentMethod ?? "pix").ToLower();
            if (method == "credit_card" && req.CreditCard == null)
                return Content(HttpStatusCode.BadRequest, new { error = "Dados do cartão ausentes." });

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

            // ── Aplica cupom: desconto + comissão do dealer ──────────────────
            var coupon         = LookupCoupon(req.CouponSlug);
            var salesCommission = 0m;

            if (coupon != null)
            {
                // Desconto concedido ao cliente
                pricing.Discount += pricing.Subtotal * (coupon.Percent / 100m);

                // Comissão calculada sobre o subtotal dos produtos (antes do desconto),
                // pois o dealer recebe pelo volume de vendas que trouxe.
                salesCommission = pricing.Subtotal * (coupon.Commission / 100m);
            }
            // ────────────────────────────────────────────────────────────────

            pricing.ShippingFee = req.ShippingOption.Price;
            pricing.Total       = pricing.Subtotal - pricing.Discount + pricing.ShippingFee;

            var profileDto = new UserProfileDto
            {
                Name        = req.Customer.Name,
                Email       = req.Customer.Email,
                Phone       = req.Customer.Phone,
                Cpf         = req.Customer.Document,
                Cep         = req.Shipping.Zip,
                Logradouro  = req.Shipping.Street,
                Numero      = req.Shipping.Number,
                Complemento = req.Shipping.Complement,
                Bairro      = req.Shipping.Neighborhood,
                Cidade      = req.Shipping.City,
                Estado      = req.Shipping.State
            };

            var currentUserId = JwtAuthAttribute.CurrentUserId();
            Guid userId;
            if (currentUserId.HasValue)
            {
                userId = currentUserId.Value;
                _users.UpdateProfile(userId, profileDto);
            }
            else
            {
                userId = _users.UpsertByEmail(profileDto);
            }

            var orderId     = Guid.NewGuid();
            var orderNumber = "MAR-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            var order = new Order
            {
                Id                   = orderId,
                OrderNumber          = orderNumber,
                UserId               = userId,
                CustomerName         = req.Customer.Name,
                CustomerEmail        = req.Customer.Email,
                CustomerDocument     = req.Customer.Document,
                CustomerPhone        = req.Customer.Phone,
                ShippingZip          = req.Shipping.Zip,
                ShippingStreet       = req.Shipping.Street,
                ShippingNumber       = req.Shipping.Number,
                ShippingComplement   = req.Shipping.Complement,
                ShippingNeighborhood = req.Shipping.Neighborhood,
                ShippingCity         = req.Shipping.City,
                ShippingState        = req.Shipping.State,
                Subtotal             = pricing.Subtotal,
                ShippingFee          = pricing.ShippingFee,
                Discount             = pricing.Discount,
                Total                = pricing.Total,
                Coupon               = coupon?.Slug,
                DealerId             = coupon?.DealerId,
                SalesCommission      = salesCommission,
                PaymentMethod        = method,
                PaymentStatus        = "pending",
                OrderStatus          = "created"
            };
            foreach (var it in pricing.Items) it.OrderId = orderId;
            _orders.Create(order, pricing.Items);

            try
            {
                var customerId = await _asaas.GetOrCreateCustomerAsync(req.Customer);

                if (method == "pix")
                {
                    var pix = await _asaas.CreatePixAsync(customerId, pricing.Total, orderNumber);
                    _orders.UpdatePaymentInfo(orderId, pix.PaymentId, pix.QrCodeImage, pix.QrCodeText);

                    return Ok(new CheckoutResponse
                    {
                        OrderId       = orderId,
                        OrderNumber   = orderNumber,
                        PaymentStatus = "pending",
                        PixQrCode     = pix.QrCodeImage,
                        PixCopyPaste  = pix.QrCodeText,
                        Message       = "PIX gerado com sucesso."
                    });
                }
                else
                {
                    var installments = req.Installments < 1 ? 1 : req.Installments;
                    var card = await _asaas.CreateCreditCardAsync(
                        customerId, pricing.Total, orderNumber,
                        installments, req.CreditCard, req.Customer, req.Shipping);

                    _orders.UpdatePaymentInfo(orderId, card.PaymentId, null, null);

                    var status = card.Status == "CONFIRMED" ? "paid" : "pending";
                    if (status == "paid")
                        _orders.UpdatePaymentStatus(orderId, "paid", "confirmed");

                    return Ok(new CheckoutResponse
                    {
                        OrderId       = orderId,
                        OrderNumber   = orderNumber,
                        PaymentStatus = status,
                        Message       = status == "paid" ? "Pagamento aprovado!" : "Pagamento em análise."
                    });
                }
            }
            catch (Exception ex)
            {
                _orders.UpdatePaymentStatus(orderId, "failed", "canceled");
                return Content(HttpStatusCode.BadGateway, new { error = "Falha no pagamento: " + ex.Message });
            }
        }

        [HttpPost, Route("webhook")]
        public IHttpActionResult Webhook([FromBody] AsaasWebhookPayload payload)
        {
            if (payload?.Payment == null) return BadRequest();
            try
            {
                if (payload.Event == "PAYMENT_RECEIVED" || payload.Event == "PAYMENT_CONFIRMED")
                {
                    var order = _orders.GetByOrderNumber(payload.Payment.ExternalReference);
                    if (order != null)
                        _orders.UpdatePaymentStatus(order.Id, "paid", "confirmed");
                }
                return Ok();
            }
            catch { return BadRequest(); }
        }

        [HttpGet, Route("orders")]
        public IHttpActionResult GetMyOrders()
        {
            TryAuthenticateRequest();

            var userId = JwtAuthAttribute.CurrentUserId();
            string token = null;
            var ctx = HttpContext.Current;
            if (ctx != null)
            {
                var c = ctx.Request.Cookies[CookieName];
                token = c?.Value;
            }
            if (!userId.HasValue && string.IsNullOrEmpty(token))
                return Ok(new List<object>());

            return Ok(_orders.GetByUserOrToken(userId, token));
        }

        [HttpGet, Route("orders/{id:guid}")]
        public IHttpActionResult GetOrder(Guid id)
        {
            TryAuthenticateRequest();

            var o = _orders.GetById(id);
            if (o == null) return NotFound();
            return Ok(new { order = o, items = _orders.GetItems(id) });
        }
    }
}
