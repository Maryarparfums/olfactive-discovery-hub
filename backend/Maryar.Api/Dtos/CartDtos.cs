using System;
using System.Collections.Generic;

namespace Maryar.Api.Dtos
{
    public class CartItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CartDto
    {
        public Guid Id { get; set; }
        public List<CartItemDto> Items { get; set; }
        public decimal Subtotal { get; set; }
        public int ItemCount { get; set; }
    }

    public class AddItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateItemByIdRequest
    {
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
    }
    
    public class RemoveItemByIdRequest
    {
        public Guid ItemId { get; set; }
    }

    public class UpdateItemByIdRequest
    {
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
    }
    
    public class RemoveItemByIdRequest
    {
        public Guid ItemId { get; set; }
    }
}
