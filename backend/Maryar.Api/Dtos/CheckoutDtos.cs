using System;

namespace Maryar.Api.Dtos
{
    public class ShippingAddressDto
    {
        public string Zip          { get; set; }
        public string Street       { get; set; }
        public string Number       { get; set; }
        public string Complement   { get; set; }
        public string Neighborhood { get; set; }
        public string City         { get; set; }
        public string State        { get; set; }
    }

    public class CustomerDto
    {
        public string Name     { get; set; }
        public string Email    { get; set; }
        public string Document { get; set; }
        public string Phone    { get; set; }
    }

    public class CreditCardDto
    {
        public string HolderName  { get; set; }
        public string Number      { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear  { get; set; }
        public string Ccv         { get; set; }
    }

    public class ShippingOptionDto
    {
        public string  Code         { get; set; } // "03220" (SEDEX) ou "03298" (PAC)
        public string  Name         { get; set; } // "SEDEX" ou "PAC"
        public decimal Price        { get; set; } // valor em reais
        public int     DeliveryDays { get; set; } // prazo em dias úteis
        public string  Description  { get; set; } // ex: "Entrega em até 3 dias úteis"
    }

    public class CheckoutRequest
    {
        public CustomerDto        Customer       { get; set; }
        public ShippingAddressDto Shipping       { get; set; }
        public ShippingOptionDto  ShippingOption { get; set; }
        public string             PaymentMethod  { get; set; } // "pix" | "credit_card"
        public CreditCardDto      CreditCard     { get; set; }
        public int                Installments   { get; set; }
        public string             CouponSlug     { get; set; } // cupom de desconto (opcional)
    }

    public class CheckoutResponse
    {
        public Guid   OrderId       { get; set; }
        public string OrderNumber   { get; set; }
        public string PaymentStatus { get; set; }
        public string PixQrCode     { get; set; } // base64 PNG
        public string PixCopyPaste  { get; set; }
        public string Message       { get; set; }
    }
}
