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

        private class CouponInfo
        {
            public string  Slug              { get; set; }
            public decimal Percent           { get; set; }
            public Guid?   DealerId          { get; set; }
            public decimal Commission        { get; set; }
            public bool    ShippingFeeCoupon { get; set; }
        }

        public CheckoutController()
            : this(new CartRepository(), new ProductRepository(),
                   new OrderRepository(), new AsaasClient(),
                   new UserRepository()) { }

        public CheckoutController(ICartRepository carts, IProductRepository products,
                                  IOrderRepository orders, AsaasClient asaas,
                                  IUserRepository users)
        {
            _carts    = carts;
            _products = products;
            _orders   = orders;
            _asaas    = asaas;
            _users    = users;
        }

        // ✅ CORRIGIDO: quando logado E com cookie, evita mesclar os dois carrinhos,
        // pois a mesclagem falha com "Duplicate entry" em ux_cart_product quando o
        // mesmo produto existe nos dois carrinhos.
        // Estratégia: se o carrinho do cookie tem itens, usa-o diretamente (é onde
        // o cliente adicionou os produtos). Só usa o carrinho do userId se o cookie
        // estiver vazio ou ausente.
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

            // Usuário logado com cookie de carrinho: verifica qual tem itens
            if (userId.HasValue && !string.IsNullOrEmpty(token))
            {
                // Tenta o carrinho do cookie primeiro (onde os itens foram adicionados)
                var cookieCart = _carts.GetOrCreate(null, token);
                if (cookieCart != null)
                {
                    var cookieItems = _carts.GetItems(cookieCart.Id);
                    if (cookieItems != null && cookieItems.Any())
                        return cookieCart.Id;
                }

                // Cookie vazio ou inexistente: usa carrinho do userId
                return _carts.GetOrCreate(userId, null).Id;
            }

            // Visitante (sem userId) ou logado sem cookie: fluxo normal
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

        // ─── Listar pedidos do usuário logado ─────────────────────────────
        [HttpGet, Route("orders")]
        public IHttpActionResult GetOrders()
        {
            TryAuthenticateRequest();

            var userId = JwtAuthAttribute.CurrentUserId();
            if (!userId.HasValue)
                return Content(HttpStatusCode.Unauthorized, new { error = "Não autenticado." });

            var orders = _orders.GetByUserOrToken(userId, null);

            var result = orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    id            = o.Id,
                    orderNumber   = o.OrderNumber,
                    total         = o.Total,
                    paymentStatus = o.PaymentStatus,
                    orderStatus   = o.OrderStatus,
                    createdAt     = o.CreatedAt
                });

            return Ok(result);
        }

        // ─── Detalhe de um pedido específico ──────────────────────────────
        [HttpGet, Route("orders/{orderId}")]
        public IHttpActionResult GetOrderById(Guid orderId)
        {
            TryAuthenticateRequest();

            var userId = JwtAuthAttribute.CurrentUserId();
            if (!userId.HasValue)
                return Content(HttpStatusCode.Unauthorized, new { error = "Não autenticado." });

            var order = _orders.GetById(orderId);
            if (order == null)
                return NotFound();

            if (order.UserId != userId.Value)
                return Content(HttpStatusCode.Forbidden, new { error = "Acesso negado." });

            var items = _orders.GetItems(orderId);

            return Ok(new
            {
                order = new
                {
                    id            = order.Id,
                    orderNumber   = order.OrderNumber,
                    total         = order.Total,
                    subtotal      = order.Subtotal,
                    shippingFee   = order.ShippingFee,
                    discount      = order.Discount,
                    paymentFee    = order.PaymentFee,   // ← novo campo (taxa/desconto do meio de pagamento)
                    paymentStatus = order.PaymentStatus,
                    paymentMethod = order.PaymentMethod,
                    orderStatus   = order.OrderStatus,
                    customerName  = order.CustomerName,
                    pixQrCode     = order.PixQrCode,
                    pixCopyPaste  = order.PixCopyPaste,
                    createdAt     = order.CreatedAt
                },
                items = items.Select(i => new
                {
                    id          = i.Id,
                    productName = i.ProductName,
                    brandName   = i.BrandName,
                    quantity    = i.Quantity,
                    unitPrice   = i.UnitPrice,
                    lineTotal   = i.LineTotal
                })
            });
        }

        // ─── Finalizar compra ──────────────────────────────────────────────
        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Process([FromBody] CheckoutRequest req)
        {
            try
            {
                TryAuthenticateRequest();

                if (req?.Customer == null || req.Shipping == null)
                    return Content(HttpStatusCode.BadRequest, new { error = "Dados incompletos." });

                if (req.ShippingOption == null)
                    return Content(HttpStatusCode.BadRequest, new { error = "Selecione uma opcao de frete antes de continuar." });

                var method = (req.PaymentMethod ?? "pix").ToLower();
                if (method == "credit_card" && req.CreditCard == null)
                    return Content(HttpStatusCode.BadRequest, new { error = "Dados do cartao ausentes." });

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
                    return Content(HttpStatusCode.BadRequest, new { error = "Nao foi possivel calcular o total." });

                // ── Cupom ──────────────────────────────────────────────────
                var coupon          = LookupCoupon(req.CouponSlug);
                var salesCommission = 0m;

                if (coupon != null)
                {
                    pricing.Discount += pricing.Subtotal * (coupon.Percent / 100m);
                    salesCommission   = pricing.Subtotal * (coupon.Commission / 100m);
                }

                pricing.ShippingFee = (coupon != null && coupon.ShippingFeeCoupon)
                    ? 0m
                    : req.ShippingOption.Price;

                // Total base (sem taxa de pagamento)
                pricing.Total = pricing.Subtotal - pricing.Discount + pricing.ShippingFee;

                // ── Taxa / desconto do meio de pagamento ───────────────────
                // Busca na tabela payment_rates pelo método e número de parcelas.
                // Negativo = desconto (ex: PIX -5%), positivo = juros (ex: 6x +4,49%).
                var installments = method == "credit_card"
                    ? (req.Installments < 1 ? 1 : req.Installments)
                    : (int?)null;

                var paymentRate = LookupPaymentRate(method, installments);
                var paymentFee  = Math.Round(pricing.Total * paymentRate, 2);
                pricing.Total  += paymentFee;   // desconto diminui, juros aumenta

                // ── Perfil do usuário ──────────────────────────────────────
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

                // ── Criar pedido ───────────────────────────────────────────
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
                    PaymentFee           = paymentFee,   // ← taxa/desconto do pagamento
                    Total                = pricing.Total, // ← já inclui taxa/desconto
                    Coupon               = coupon != null ? coupon.Slug : null,
                    DealerId             = coupon != null ? coupon.DealerId : (Guid?)null,
                    SalesCommission      = salesCommission,
                    PaymentMethod        = method,
                    PaymentStatus        = "pending",
                    OrderStatus          = "created"
                };

                foreach (var it in pricing.Items) it.OrderId = orderId;
                _orders.Create(order, pricing.Items);

                // ── Asaas ──────────────────────────────────────────────────
                var customerId = await _asaas.GetOrCreateCustomerAsync(req.Customer);

                if (method == "pix")
                {
                    // pricing.Total já tem o desconto PIX embutido
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
                    // pricing.Total já tem os juros do parcelamento embutidos
                    var card = await _asaas.CreateCreditCardAsync(
                        customerId, pricing.Total, orderNumber,
                        installments.Value, req.CreditCard, req.Customer, req.Shipping);

                    _orders.UpdatePaymentInfo(orderId, card.PaymentId, null, null);

                    var status = card.Status == "CONFIRMED" ? "paid" : "pending";
                    if (status == "paid")
                        _orders.UpdatePaymentStatus(orderId, "paid", "confirmed");

                    return Ok(new CheckoutResponse
                    {
                        OrderId       = orderId,
                        OrderNumber   = orderNumber,
                        PaymentStatus = status,
                        Message       = status == "paid" ? "Pagamento aprovado!" : "Pagamento em analise."
                    });
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "";
                return Content(HttpStatusCode.InternalServerError,
                    new { error = "Erro ao processar pedido: " + ex.Message + inner, tipo = ex.GetType().Name });
            }
        }

        // ─── Webhook Asaas ─────────────────────────────────────────────────
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
            catch
            {
                return InternalServerError();
            }
        }

        // ─── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Busca a taxa de pagamento na tabela payment_rates.
        /// Retorna 0 se não encontrar (sem taxa, sem desconto).
        /// Valor negativo = desconto; positivo = juros.
        /// </summary>
        private decimal LookupPaymentRate(string paymentMethod, int? installments)
        {
            try
            {
                using (var con = new MySqlConnection(_conn))
                {
                    con.Open();

                    // Para PIX: installments IS NULL
                    // Para cartão: installments = N
                    var sql = installments.HasValue
                        ? "SELECT rate FROM payment_rates " +
                          "WHERE payment_method = @method AND installments = @inst AND active = 1 LIMIT 1"
                        : "SELECT rate FROM payment_rates " +
                          "WHERE payment_method = @method AND installments IS NULL AND active = 1 LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@method", paymentMethod);
                        if (installments.HasValue)
                            cmd.Parameters.AddWithValue("@inst", installments.Value);

                        var result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return 0m;
                        return Convert.ToDecimal(result);
                    }
                }
            }
            catch
            {
                // Falha silenciosa: se a tabela ainda não existir, o checkout não é bloqueado.
                // Remova o try/catch após confirmar que a tabela foi criada no banco.
                return 0m;
            }
        }

        private CouponInfo LookupCoupon(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            try
            {
                using (var con = new MySqlConnection(_conn))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(
                        "SELECT slug, percent, user_id, commission, shipping_fee_coupon " +
                        "FROM coupons WHERE UPPER(slug) = UPPER(@slug) LIMIT 1", con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug.Trim());
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (!rd.Read()) return null;
                            Guid? dealerId = null;
                            if (!rd.IsDBNull(rd.GetOrdinal("user_id")))
                            {
                                var raw = rd.GetString(rd.GetOrdinal("user_id"));
                                if (Guid.TryParse(raw, out var g)) dealerId = g;
                            }
                            return new CouponInfo
                            {
                                Slug              = rd.GetString(rd.GetOrdinal("slug")),
                                Percent           = rd.GetDecimal(rd.GetOrdinal("percent")),
                                DealerId          = dealerId,
                                Commission        = rd.GetDecimal(rd.GetOrdinal("commission")),
                                ShippingFeeCoupon = rd.GetInt32(rd.GetOrdinal("shipping_fee_coupon")) == 1
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Erro ao consultar cupom: " + ex.Message, ex);
            }
        }
    }
}
