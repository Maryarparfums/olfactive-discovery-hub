using System;

namespace Maryar.Api.Models
{
    public class CartItem
    {
        public Guid Id { get; set; }
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Joins p/ exibição
        public string ProductSlug { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public string BrandName { get; set; }
    }
}
