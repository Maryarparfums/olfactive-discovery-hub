using System;

namespace Maryar.Api.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public Guid? UserId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerDocument { get; set; }
        public string CustomerPhone { get; set; }

        public string ShippingZip { get; set; }
        public string ShippingStreet { get; set; }
        public string ShippingNumber { get; set; }
        public string ShippingComplement { get; set; }
        public string ShippingNeighborhood { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }

        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal PaymentFee { get; set; }   // taxa ou desconto por modalidade de pagamento (negativo = desconto)
        public decimal Total { get; set; }

        public string Coupon { get; set; }
        public Guid? DealerId { get; set; }
        public string StatusCommission { get; set; }
        public decimal SalesCommission { get; set; }

        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }

        public string InfinitePayChargeId { get; set; }
        public string PixQrCode { get; set; }
        public string PixCopyPaste { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
