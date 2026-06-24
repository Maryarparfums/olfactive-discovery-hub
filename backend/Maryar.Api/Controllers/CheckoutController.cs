using System;
using System.Collections.Generic;
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

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Process([FromBody] CheckoutRequest req)
        {
            TryAuthenticateRequest(); // ← CORREÇÃO: autentica o JWT antes de qualquer verificação

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
                // Usuário logado: atualiza perfil pelo ID (sem alterar o e-mail — troca de e-mail
                // passa pelo fluxo requestemailchange/verifyemail)
                userId = currentUserId.Value;
                _users.UpdateProfile(userId, profileDto);
            }
            else
            {
                // Visitante: busca pelo e-mail; atualiza se existir, cria se não existir
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
