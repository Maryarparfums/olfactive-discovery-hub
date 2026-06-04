using System;

namespace Maryar.Api.Models
{
    public class PaymentEvent
    {
        public Guid Id { get; set; }
        public string EventId { get; set; } // ID externo do provedor (idempotência)
        public Guid? OrderId { get; set; }
        public string EventType { get; set; }
        public string PayloadJson { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
