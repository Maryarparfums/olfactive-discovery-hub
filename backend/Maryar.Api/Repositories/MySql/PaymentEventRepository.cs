using System;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Repositories.MySql
{
    public class PaymentEventRepository : IPaymentEventRepository
    {
        private readonly IConnectionFactory _factory;
        public PaymentEventRepository(IConnectionFactory factory) { _factory = factory; }
        public PaymentEventRepository() : this(new MySqlConnectionFactory()) { }

        public bool TryInsert(PaymentEvent evt)
        {
            try
            {
                using (var cn = _factory.Create())
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO payment_events (id, event_id, order_id, event_type, payload)
                                        VALUES (@id, @eid, @oid, @t, @p)";
                    cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@eid", evt.EventId);
                    cmd.Parameters.AddWithValue("@oid", (object)evt.OrderId?.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@t", evt.EventType);
                    cmd.Parameters.AddWithValue("@p", evt.PayloadJson);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062) // duplicate key (idempotência)
            {
                return false;
            }
        }
    }
}
