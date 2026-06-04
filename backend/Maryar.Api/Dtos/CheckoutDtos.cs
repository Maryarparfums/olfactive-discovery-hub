using System;

namespace Maryar.Api.Dtos
{
    public class ShippingAddressDto
    {
        public string Zip { get; set; }
        public string Street { get; set; }
        public string Number { get; set; }
        public string Complement { get; set; }
        public string Neighborhood { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    public class CustomerDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Document { get; set; } // CPF
        public string Phone { get; set; }
    }

    public class CheckoutRequest
    {
        public CustomerDto Customer { get; set; }
        public ShippingAddressDto Shipping { get; set; }
        public string PaymentMethod { get; set; } // "pix" | "credit_card"
        public string CardToken { get; set; }     // só para credit_card (token do SDK browser)
        public int Installments { get; set; }     // 1..12
    }

    public class CheckoutResponse
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string PaymentStatus { get; set; }
        public string PixQrCode { get; set; }
        public string PixCopyPaste { get; set; }
        public string Message { get; set; }
    }
}
