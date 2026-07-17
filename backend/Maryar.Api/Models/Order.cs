using System.Collections.Generic;
using System.Linq;
using Maryar.Api.Models;

namespace Maryar.Api.Services
{
    public class PricingResult
    {
        public decimal Subtotal    { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount    { get; set; }
        public decimal Total       { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    // Recalcula totais NO SERVIDOR. Nunca confie em valores enviados pelo cliente.
    public static class PricingService
    {
        // Política simples: frete fixo R$ 25, grátis acima de R$ 350.
        public const decimal ShippingFlat          = 25m;
        public const decimal FreeShippingThreshold = 350m;

        public static PricingResult Calculate(
            IEnumerable<CartItem> cartItems,
            IDictionary<System.Guid, Product> productsById)
        {
            var items    = new List<OrderItem>();
            decimal subtotal = 0m;

            foreach (var ci in cartItems)
            {
                if (!productsById.ContainsKey(ci.ProductId)) continue;
                var p = productsById[ci.ProductId];

                // Preço autoritativo: usa o UnitPrice gravado no CartItem (reflete a variante
                // escolhida pelo cliente, ex: decant 5 ml vs frasco 100 ml).
                // Fallback para p.Price apenas se o item for antigo e UnitPrice não tiver sido
                // preenchido ainda.
                var unit = ci.UnitPrice > 0 ? ci.UnitPrice : p.Price;
                var line = unit * ci.Quantity;
                subtotal += line;

                items.Add(new OrderItem
                {
                    ProductId   = p.Id,
                    ProductSlug = p.Slug,
                    ProductName = p.Name,
                    BrandName   = p.BrandName,
                    Quantity    = ci.Quantity,
                    UnitPrice   = unit,
                    LineTotal   = line
                });
            }

            var shipping = subtotal >= FreeShippingThreshold ? 0m : ShippingFlat;
            var discount = 0m;
            var total    = subtotal + shipping - discount;

            return new PricingResult
            {
                Subtotal    = subtotal,
                ShippingFee = shipping,
                Discount    = discount,
                Total       = total,
                Items       = items
            };
        }
    }
}
