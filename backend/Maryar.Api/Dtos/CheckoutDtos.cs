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
        public string  Code         { get; set; }
        public string  Name         { get; set; }
        public decimal Price        { get; set; }
        public int     DeliveryDays { get; set; }
        public string  Description  { get; set; }
    }

    public class CheckoutRequest
    {
        public CustomerDto        Customer       { get; set; }
        public ShippingAddressDto Shipping       { get; set; }
        public ShippingOptionDto  ShippingOption { get; set; }
        public string             PaymentMethod  { get; set; }
        public CreditCardDto      CreditCard     { get; set; }
        public int                Installments   { get; set; }
        public string             CouponSlug     { get; set; }
    }

    public class CheckoutResponse
    {
        public Guid   OrderId       { get; set; }
        public string OrderNumber   { get; set; }
        public string PaymentStatus { get; set; }
        public string PixQrCode     { get; set; }
        public string PixCopyPaste  { get; set; }
        public string Message       { get; set; }
    }
}
