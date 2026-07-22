using System;
using System.Collections.Generic;
using System.Web.Http;
using Maryar.Api.Infrastructure;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Controllers
{
    // GET  /api/payment-rates          — lista todas as taxas ativas
    // GET  /api/payment-rates/{id}     — retorna uma taxa pelo id
    // POST /api/payment-rates          — cria uma nova taxa
    // PATCH /api/payment-rates/{id}    — atualiza rate/label/active de uma taxa
    // DELETE /api/payment-rates/{id}   — remove uma taxa

    [RoutePrefix("api/payment-rates")]
    public class PaymentRatesController : ApiController
    {
        private readonly IConnectionFactory _factory;
        public PaymentRatesController() : this(new MySqlConnectionFactory()) { }
        public PaymentRatesController(IConnectionFactory factory) { _factory = factory; }

        // ----------------------------------------------------------------
        // GET /api/payment-rates
        // ----------------------------------------------------------------
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            var list = new List<object>();

            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, payment_method, installments, rate, label, active
                    FROM   payment_rates
                    WHERE  active = 1
                    ORDER  BY payment_method, COALESCE(installments, 0)";

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new
                        {
                            id            = Convert.ToInt32(r["id"]),
                            paymentMethod = r["payment_method"].ToString(),
                            installments  = r["installments"] == DBNull.Value
                                                ? (int?)null
                                                : Convert.ToInt32(r["installments"]),
                            rate          = Convert.ToDecimal(r["rate"]),
                            label         = r["label"].ToString(),
                            active        = Convert.ToBoolean(r["active"])
                        });
                    }
                }
            }

            return Ok(list);
        }

        // ----------------------------------------------------------------
        // GET /api/payment-rates/{id}
        // ----------------------------------------------------------------
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult GetById(int id)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, payment_method, installments, rate, label, active
                    FROM   payment_rates
                    WHERE  id = @id";
                cmd.Parameters.AddWithValue("@id", id);

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return NotFound();

                    return Ok(new
                    {
                        id            = Convert.ToInt32(r["id"]),
                        paymentMethod = r["payment_method"].ToString(),
                        installments  = r["installments"] == DBNull.Value
                                            ? (int?)null
                                            : Convert.ToInt32(r["installments"]),
                        rate          = Convert.ToDecimal(r["rate"]),
                        label         = r["label"].ToString(),
                        active        = Convert.ToBoolean(r["active"])
                    });
                }
            }
        }

        // ----------------------------------------------------------------
        // POST /api/payment-rates
        // Body: { paymentMethod, installments, rate, label }
        // ----------------------------------------------------------------
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] PaymentRateInput dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PaymentMethod))
                return BadRequest("paymentMethod é obrigatório.");

            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO payment_rates (payment_method, installments, rate, label)
                    VALUES (@pm, @inst, @rate, @label)";
                cmd.Parameters.AddWithValue("@pm",   dto.PaymentMethod);
                cmd.Parameters.AddWithValue("@inst", dto.Installments.HasValue ? (object)dto.Installments.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@rate", dto.Rate);
                cmd.Parameters.AddWithValue("@label", dto.Label ?? "");

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex) when (ex.Number == 1062)
                {
                    return Conflict();  // unique key (payment_method, installments) duplicado
                }
            }

            return Ok(new { success = true });
        }

        // ----------------------------------------------------------------
        // PATCH /api/payment-rates/{id}
        // Body: { rate?, label?, active? }  — apenas campos enviados são alterados
        // ----------------------------------------------------------------
        [HttpPatch, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] PaymentRateUpdate dto)
        {
            if (dto == null) return BadRequest("Body inválido.");

            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE payment_rates
                    SET    rate       = COALESCE(@rate,   rate),
                           label      = COALESCE(@label,  label),
                           active     = COALESCE(@active, active),
                           updated_at = CURRENT_TIMESTAMP
                    WHERE  id = @id";

                cmd.Parameters.AddWithValue("@rate",   dto.Rate.HasValue   ? (object)dto.Rate.Value   : DBNull.Value);
                cmd.Parameters.AddWithValue("@label",  dto.Label  != null   ? (object)dto.Label        : DBNull.Value);
                cmd.Parameters.AddWithValue("@active", dto.Active.HasValue  ? (object)(dto.Active.Value ? 1 : 0) : DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);

                var affected = cmd.ExecuteNonQuery();
                if (affected == 0) return NotFound();
            }

            return Ok(new { success = true });
        }

        // ----------------------------------------------------------------
        // DELETE /api/payment-rates/{id}
        // ----------------------------------------------------------------
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM payment_rates WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                var affected = cmd.ExecuteNonQuery();
                if (affected == 0) return NotFound();
            }

            return Ok(new { success = true });
        }

        // ----------------------------------------------------------------
        // DTOs
        // ----------------------------------------------------------------
        public class PaymentRateInput
        {
            public string  PaymentMethod { get; set; }
            public int?    Installments  { get; set; }
            public decimal Rate          { get; set; }
            public string  Label         { get; set; }
        }

        public class PaymentRateUpdate
        {
            public decimal? Rate   { get; set; }
            public string   Label  { get; set; }
            public bool?    Active { get; set; }
        }
    }
}
