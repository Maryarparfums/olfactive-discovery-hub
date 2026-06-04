using System;
using System.Collections.Generic;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Guid Create(Order order, IEnumerable<OrderItem> items);
        Order GetById(Guid id);
        Order GetByChargeId(string chargeId);
        IEnumerable<OrderItem> GetItems(Guid orderId);
        void UpdatePaymentInfo(Guid orderId, string chargeId, string pixQr, string pixCopy);
        void UpdatePaymentStatus(Guid orderId, string paymentStatus, string orderStatus);
    }
}
