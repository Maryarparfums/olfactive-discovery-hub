[HttpPost, Route("")]
public async Task<IHttpActionResult> Process([FromBody] CheckoutRequest req)
{
    TryAuthenticateRequest();

    if (req?.Customer == null || req.Shipping == null)
        return Content(HttpStatusCode.BadRequest, new { error = "Dados incompletos." });

    if (req.ShippingOption == null || req.ShippingOption.Price <= 0)
        return Content(HttpStatusCode.BadRequest, new { error = "Selecione uma opcao de frete antes de continuar." });

    var method = (req.PaymentMethod ?? "pix").ToLower();
    if (method == "credit_card" && req.CreditCard == null)
        return Content(HttpStatusCode.BadRequest, new { error = "Dados do cartao ausentes." });

    // ── BLOCO DIAGNÓSTICO: captura erros síncronos com mensagem clara ──
    Guid orderId;
    string orderNumber;
    PricingService.PricingResult pricing;
    Guid userId;
    Order order;

    try
    {
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

        pricing = PricingService.Calculate(cartItems, productsById);
        if (pricing.Total <= 0)
            return Content(HttpStatusCode.BadRequest, new { error = "Nao foi possivel calcular o total." });

        var coupon          = LookupCoupon(req.CouponSlug);
        var salesCommission = 0m;

        if (coupon != null)
        {
            pricing.Discount    += pricing.Subtotal * (coupon.Percent / 100m);
            salesCommission      = pricing.Subtotal * (coupon.Commission / 100m);
        }

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
        if (currentUserId.HasValue)
        {
            userId = currentUserId.Value;
            _users.UpdateProfile(userId, profileDto);
        }
        else
        {
            userId = _users.UpsertByEmail(profileDto);
        }

        orderId     = Guid.NewGuid();
        orderNumber = "MAR-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        order = new Order
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
    }
    catch (Exception ex)
    {
        // Retorna o erro real para diagnóstico — remova ou restrinja após identificar o problema
        return Content(HttpStatusCode.InternalServerError, new
        {
            error = "Erro interno antes do pagamento.",
            detalhe = ex.GetType().Name + ": " + ex.Message,
            origem = ex.StackTrace?.Split('\n')[0]?.Trim()
        });
    }
    // ── FIM DO BLOCO DIAGNÓSTICO ──

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
                Message       = status == "paid" ? "Pagamento aprovado!" : "Pagamento em analise."
            });
        }
    }
    catch (Exception ex)
    {
        _orders.UpdatePaymentStatus(orderId, "failed", "canceled");
        return Content(HttpStatusCode.BadGateway, new { error = "Falha no pagamento: " + ex.Message });
    }
}
