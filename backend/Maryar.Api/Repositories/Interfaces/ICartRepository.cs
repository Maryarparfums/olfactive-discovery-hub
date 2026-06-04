using System;
using System.Collections.Generic;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Cart GetOrCreate(Guid? userId, string anonymousToken);
        Cart GetById(Guid cartId);
        IEnumerable<CartItem> GetItems(Guid cartId);
        CartItem GetItem(Guid cartId, Guid productId);
        Guid AddItem(Guid cartId, Guid productId, int qty, decimal unitPrice);
        void UpdateItemQty(Guid itemId, int qty);
        void RemoveItem(Guid itemId);
        void Clear(Guid cartId);
    }
}
