using System;
using System.Collections.Generic;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Repositories.MySql
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IConnectionFactory _factory;
        public OrderRepository(IConnectionFactory factory) { _factory = factory; }
        public OrderRepository() : this(new MySqlConnectionFactory()) { }

        public Guid Create(Order order, IEnumerable<OrderItem> items)
        {
            using (var cn = _factory.Create())
            using (var tx = cn.BeginTransaction())
            {
                try
                {
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"
                            INSERT INTO orders (
                                id, order_number, user_id, customer_name, customer_email,
                                customer_document, customer_phone, shipping_zip, shipping_street,
                                shipping_number, shipping_complement, shipping_neighborhood,
                                shipping_city, shipping_state, subtotal, shipping_fee, discount,
                                total, payment_method, payment_status, order_status
                            ) VALUES (
                                @id, @num, @uid, @cname, @cemail, @cdoc, @cphone,
                                @zip, @street, @snum, @scomp, @sneigh, @scity, @sst,
                                @subtotal, @shipfee, @disc, @total,
                                @pm, @ps, @os
                            )";
                        cmd.Parameters.AddWithValue("@id", order.Id.ToString());
                        cmd.Parameters.AddWithValue("@num", order.OrderNumber);
                        cmd.Parameters.AddWithValue("@uid", (object)order.UserId?.ToString() ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cname", order.CustomerName);
                        cmd.Parameters.AddWithValue("@cemail", order.CustomerEmail);
                        cmd.Parameters.AddWithValue("@cdoc", (object)order.CustomerDocument ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cphone", (object)order.CustomerPhone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@zip", order.ShippingZip);
                        cmd.Parameters.AddWithValue("@street", order.ShippingStreet);
                        cmd.Parameters.AddWithValue("@snum", order.ShippingNumber);
                        cmd.Parameters.AddWithValue("@scomp", (object)order.ShippingComplement ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sneigh", order.ShippingNeighborhood);
                        cmd.Parameters.AddWithValue("@scity", order.ShippingCity);
                        cmd.Parameters.AddWithValue("@sst", order.ShippingState);
                        cmd.Parameters.AddWithValue("@subtotal", order.Subtotal);
                        cmd.Parameters.AddWithValue("@shipfee", order.ShippingFee);
                        cmd.Parameters.AddWithValue("@disc", order.Discount);
                        cmd.Parameters.AddWithValue("@total", order.Total);
                        cmd.Parameters.AddWithValue("@pm", order.PaymentMethod);
                        cmd.Parameters.AddWithValue("@ps", order.PaymentStatus);
                        cmd.Parameters.AddWithValue("@os", order.OrderStatus);
                        cmd.ExecuteNonQuery();
                    }

                    foreach (var it in items)
                    {
                        using (var cmd = cn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"
                                INSERT INTO order_items (
                                    id, order_id, product_id, product_slug, product_name, brand_name,
                                    quantity, unit_price, line_total
                                ) VALUES (
                                    @id, @oid, @pid, @pslug, @pname, @bname, @q, @up, @lt
                                )";
                            cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                            cmd.Parameters.AddWithValue("@oid", order.Id.ToString());
                            cmd.Parameters.AddWithValue("@pid", it.ProductId.ToString());
                            cmd.Parameters.AddWithValue("@pslug", it.ProductSlug);
                            cmd.Parameters.AddWithValue("@pname", it.ProductName);
                            cmd.Parameters.AddWithValue("@bname", it.BrandName);
                            cmd.Parameters.AddWithValue("@q", it.Quantity);
                            cmd.Parameters.AddWithValue("@up", it.UnitPrice);
                            cmd.Parameters.AddWithValue("@lt", it.LineTotal);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                    return order.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public Order GetById(Guid id) => GetBy("id = @id", "@id", id.ToString());
        public Order GetByChargeId(string chargeId) => GetBy("infinitepay_charge_id = @cid", "@cid", chargeId);

        public IEnumerable<Order> GetByUserOrToken(Guid? userId, string guestToken)
        {
            var list = new List<Order>();

            // Sem identificação: retorna vazio
            if (!userId.HasValue && string.IsNullOrEmpty(guestToken))
                return list;

            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                if (userId.HasValue)
                {
                    cmd.CommandText = @"
                        SELECT * FROM orders 
                        WHERE user_id = @uid 
                        ORDER BY created_at DESC";
                    cmd.Parameters.AddWithValue("@uid", userId.Value.ToString());
                }
                else
                {
                    // Pedidos anônimos: busca pelo e-mail do cliente via cookie de carrinho
                    // Como não temos guest_token na tabela, buscamos pelo customer_email
                    // armazenado no pedido mais recente do cookie de sessão.
                    // Por ora retorna lista vazia para usuários não autenticados.
                    return list;
                }

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new Order
                        {
                            Id = Guid.Parse(r["id"].ToString()),
                            OrderNumber = r["order_number"].ToString(),
                            UserId = r["user_id"] == DBNull.Value ? (Guid?)null : Guid.Parse(r["user_id"].ToString()),
                            CustomerName = r["customer_name"].ToString(),
                            CustomerEmail = r["customer_email"].ToString(),
                            CustomerDocument = r["customer_document"] == DBNull.Value ? null : r["customer_document"].ToString(),
                            CustomerPhone = r["customer_phone"] == DBNull.Value ? null : r["customer_phone"].ToString(),
                            ShippingZip = r["shipping_zip"].ToString(),
                            ShippingStreet = r["shipping_street"].ToString(),
                            ShippingNumber = r["shipping_number"].ToString(),
                            ShippingComplement = r["shipping_complement"] == DBNull.Value ? null : r["shipping_complement"].ToString(),
                            ShippingNeighborhood = r["shipping_neighborhood"].ToString(),
                            ShippingCity = r["shipping_city"].ToString(),
                            ShippingState = r["shipping_state"].ToString(),
                            Subtotal = Convert.ToDecimal(r["subtotal"]),
                            ShippingFee = Convert.ToDecimal(r["shipping_fee"]),
                            Discount = Convert.ToDecimal(r["discount"]),
                            Total = Convert.ToDecimal(r["total"]),
                            PaymentMethod = r["payment_method"].ToString(),
                            PaymentStatus = r["payment_status"].ToString(),
                            OrderStatus = r["order_status"].ToString(),
                            InfinitePayChargeId = r["infinitepay_charge_id"] == DBNull.Value ? null : r["infinitepay_charge_id"].ToString(),
                            PixQrCode = r["pix_qr_code"] == DBNull.Value ? null : r["pix_qr_code"].ToString(),
                            PixCopyPaste = r["pix_copy_paste"] == DBNull.Value ? null : r["pix_copy_paste"].ToString(),
                            CreatedAt = Convert.ToDateTime(r["created_at"]),
                            UpdatedAt = Convert.ToDateTime(r["updated_at"])
                        });
                    }
                }
            }

            return list;
        }

        private Order GetBy(string where, string p, object v)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM orders WHERE " + where + " LIMIT 1";
                cmd.Parameters.AddWithValue(p, v);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Order
                    {
                        Id = Guid.Parse(r["id"].ToString()),
                        OrderNumber = r["order_number"].ToString(),
                        UserId = r["user_id"] == DBNull.Value ? (Guid?)null : Guid.Parse(r["user_id"].ToString()),
                        CustomerName = r["customer_name"].ToString(),
                        CustomerEmail = r["customer_email"].ToString(),
                        CustomerDocument = r["customer_document"] == DBNull.Value ? null : r["customer_document"].ToString(),
                        CustomerPhone = r["customer_phone"] == DBNull.Value ? null : r["customer_phone"].ToString(),
                        ShippingZip = r["shipping_zip"].ToString(),
                        ShippingStreet = r["shipping_street"].ToString(),
                        ShippingNumber = r["shipping_number"].ToString(),
                        ShippingComplement = r["shipping_complement"] == DBNull.Value ? null : r["shipping_complement"].ToString(),
                        ShippingNeighborhood = r["shipping_neighborhood"].ToString(),
                        ShippingCity = r["shipping_city"].ToString(),
                        ShippingState = r["shipping_state"].ToString(),
                        Subtotal = Convert.ToDecimal(r["subtotal"]),
                        ShippingFee = Convert.ToDecimal(r["shipping_fee"]),
                        Discount = Convert.ToDecimal(r["discount"]),
                        Total = Convert.ToDecimal(r["total"]),
                        PaymentMethod = r["payment_method"].ToString(),
                        PaymentStatus = r["payment_status"].ToString(),
                        OrderStatus = r["order_status"].ToString(),
                        InfinitePayChargeId = r["infinitepay_charge_id"] == DBNull.Value ? null : r["infinitepay_charge_id"].ToString(),
                        PixQrCode = r["pix_qr_code"] == DBNull.Value ? null : r["pix_qr_code"].ToString(),
                        PixCopyPaste = r["pix_copy_paste"] == DBNull.Value ? null : r["pix_copy_paste"].ToString(),
                        CreatedAt = Convert.ToDateTime(r["created_at"]),
                        UpdatedAt = Convert.ToDateTime(r["updated_at"])
                    };
                }
            }
        }

        public IEnumerable<OrderItem> GetItems(Guid orderId)
        {
            var list = new List<OrderItem>();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM order_items WHERE order_id = @oid";
                cmd.Parameters.AddWithValue("@oid", orderId.ToString());
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new OrderItem
                        {
                            Id = Guid.Parse(r["id"].ToString()),
                            OrderId = Guid.Parse(r["order_id"].ToString()),
                            ProductId = Guid.Parse(r["product_id"].ToString()),
                            ProductSlug = r["product_slug"].ToString(),
                            ProductName = r["product_name"].ToString(),
                            BrandName = r["brand_name"].ToString(),
                            Quantity = Convert.ToInt32(r["quantity"]),
                            UnitPrice = Convert.ToDecimal(r["unit_price"]),
                            LineTotal = Convert.ToDecimal(r["line_total"])
                        });
            }
            return list;
        }

        public void UpdatePaymentInfo(Guid orderId, string chargeId, string pixQr, string pixCopy)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE orders SET infinitepay_charge_id = @cid,
                                    pix_qr_code = @qr, pix_copy_paste = @cp,
                                    updated_at = CURRENT_TIMESTAMP WHERE id = @id";
                cmd.Parameters.AddWithValue("@cid", (object)chargeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@qr", (object)pixQr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cp", (object)pixCopy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", orderId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdatePaymentStatus(Guid orderId, string paymentStatus, string orderStatus)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE orders SET payment_status = @ps, order_status = @os,
                                    updated_at = CURRENT_TIMESTAMP WHERE id = @id";
                cmd.Parameters.AddWithValue("@ps", paymentStatus);
                cmd.Parameters.AddWithValue("@os", orderStatus);
                cmd.Parameters.AddWithValue("@id", orderId.ToString());
                cmd.ExecuteNonQuery();
            }
        }
    }
}
