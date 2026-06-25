using System;
using System.Collections.Generic;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
  public interface ICartRepository
  {
      Cart                 GetOrCreate(Guid? userId, string anonymousToken);
      Cart                 GetById(Guid cartId);
      IEnumerable<CartItem> GetItems(Guid cartId);

      // variantId distingue apresentações do mesmo produto (ex: 10ml vs 100ml)
      CartItem GetItem(Guid cartId, Guid productId, Guid? variantId);

      // Inclui variantId e volumeMl para persistir a apresentação escolhida
      Guid AddItem(Guid cartId, Guid productId, Guid? variantId, int? volumeMl, int qty, decimal unitPrice);

      void UpdateItemQty(Guid itemId, int qty);
      void RemoveItem(Guid itemId);
      void Clear(Guid cartId);
  }
}
