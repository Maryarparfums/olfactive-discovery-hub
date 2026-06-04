using System;
using System.Web.Http;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("api/payments")]
    public class PaymentsController : ApiController
    {
        private readonly IOrderRepository _orders;
        public PaymentsController() : this(new OrderRepository()) { }
        public PaymentsController(IOrderRepository orders) { _orders = orders; }

        // Polling do front pra checar status do PIX (até o webhook chegar).
        [HttpGet, Route("status/{orderId:guid}")]
        public IHttpActionResult Status(Guid orderId)
        {
            var o = _orders.GetById(orderId);
            if (o == null) return NotFound();
            return Ok(new
            {
                orderId = o.Id,
                orderNumber = o.OrderNumber,
                paymentStatus = o.PaymentStatus,
                orderStatus = o.OrderStatus
            });
        }
    }
}
