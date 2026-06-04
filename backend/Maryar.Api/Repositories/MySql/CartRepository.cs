using System;
using System.Collections.Generic;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class CartRepository : ICartRepository
    {
        private readonly IConnectionFactory _factory;
        public CartRepository(IConnectionFactory factory) { _factory = factory; }
        public CartRepository() : this(new MySqlConnectionFactory()) { }

        public Cart GetOrCreate(Guid? userId, string anonymousToken)
        {
            using (var cn = _factory.Create())
            {
                // Tenta achar carrinho existente
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = userId.HasValue
                        ? "SELECT id, user_id, anonymous_token, created_at, updated_at FROM carts WHERE user_id = @uid LIMIT 1"
                        : "SELECT id, user_id, anonymous_token, created_at, updated_at FROM carts WHERE anonymous_token = @tok LIMIT 1";
                    if (userId.HasValue) cmd.Parameters.AddWithValue("@uid", userId.Value.ToString());
                    else cmd.Parameters.AddWithValue("@tok", anonymousToken);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                            return new Cart
                            {
                                Id = Guid.Parse(r["id"].ToString()),
                                UserId = r["user_id"] == DBNull.Value ? (Guid?)null : Guid.Parse(r["user_id"].ToString()),
                                AnonymousToken = r["anonymous_token"] == DBNull.Value ? null : r["anonymous_token"].ToString(),
                                CreatedAt = Convert.ToDateTime(r["created_at"]),
                                UpdatedAt = Convert.ToDateTime(r["updated_at"])
                            };
                    }
                }

                var id = Guid.NewGuid();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO carts (id, user_id, anonymous_token) VALUES (@id, @uid, @tok)";
                    cmd.Parameters.AddWithValue("@id", id.ToString());
                    cmd.Parameters.AddWithValue("@uid", (object)userId?.ToString() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tok", (object)anonymousToken ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                return new Cart
                {
                    Id = id,
                    UserId = userId,
                    AnonymousToken = anonymousToken,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
        }

        public Cart GetById(Guid cartId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, user_id, anonymous_token, created_at, updated_at FROM carts WHERE id = @id LIMIT 1";
                cmd.Parameters.AddWithValue("@id", cartId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Cart
                    {
                        Id = Guid.Parse(r["id"].ToString()),
                        UserId = r["user_id"] == DBNull.Value ? (Guid?)null : Guid.Parse(r["user_id"].ToString()),
                        AnonymousToken = r["anonymous_token"] == DBNull.Value ? null : r["anonymous_token"].ToString(),
                        CreatedAt = Convert.ToDateTime(r["created_at"]),
                        UpdatedAt = Convert.ToDateTime(r["updated_at"])
                    };
                }
            }
        }

        public IEnumerable<CartItem> GetItems(Guid cartId)
        {
            var list = new List<CartItem>();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT ci.id, ci.cart_id, ci.product_id, ci.quantity, ci.unit_price, " +
                    "p.slug AS product_slug, p.name AS product_name, p.image_url AS product_image, " +
                    "b.name AS brand_name " +
                    "FROM cart_items ci " +
                    "INNER JOIN products p ON p.id = ci.product_id " +
                    "INNER JOIN brands b ON b.id = p.brand_id " +
                    "WHERE ci.cart_id = @cid";
                cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new CartItem
                        {
                            Id = Guid.Parse(r["id"].ToString()),
                            CartId = Guid.Parse(r["cart_id"].ToString()),
                            ProductId = Guid.Parse(r["product_id"].ToString()),
                            Quantity = Convert.ToInt32(r["quantity"]),
                            UnitPrice = Convert.ToDecimal(r["unit_price"]),
                            ProductSlug = r["product_slug"].ToString(),
                            ProductName = r["product_name"].ToString(),
                            ProductImage = r["product_image"] == DBNull.Value ? null : r["product_image"].ToString(),
                            BrandName = r["brand_name"].ToString()
                        });
            }
            return list;
        }

        public CartItem GetItem(Guid cartId, Guid productId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT id, cart_id, product_id, quantity, unit_price FROM cart_items " +
                    "WHERE cart_id = @cid AND product_id = @pid LIMIT 1";
                cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                cmd.Parameters.AddWithValue("@pid", productId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new CartItem
                    {
                        Id = Guid.Parse(r["id"].ToString()),
                        CartId = Guid.Parse(r["cart_id"].ToString()),
                        ProductId = Guid.Parse(r["product_id"].ToString()),
                        Quantity = Convert.ToInt32(r["quantity"]),
                        UnitPrice = Convert.ToDecimal(r["unit_price"])
                    };
                }
            }
        }

        public Guid AddItem(Guid cartId, Guid productId, int qty, decimal unitPrice)
        {
            var existing = GetItem(cartId, productId);
            if (existing != null)
            {
                UpdateItemQty(existing.Id, existing.Quantity + qty);
                return existing.Id;
            }
            var id = Guid.NewGuid();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO cart_items (id, cart_id, product_id, quantity, unit_price) " +
                    "VALUES (@id, @cid, @pid, @q, @up)";
                cmd.Parameters.AddWithValue("@id", id.ToString());
                cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                cmd.Parameters.AddWithValue("@pid", productId.ToString());
                cmd.Parameters.AddWithValue("@q", qty);
                cmd.Parameters.AddWithValue("@up", unitPrice);
                cmd.ExecuteNonQuery();
            }
            TouchCart(cartId);
            return id;
        }

        public void UpdateItemQty(Guid itemId, int qty)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE cart_items SET quantity = @q WHERE id = @id";
                cmd.Parameters.AddWithValue("@q", qty);
                cmd.Parameters.AddWithValue("@id", itemId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        public void RemoveItem(Guid itemId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM cart_items WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", itemId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        public void Clear(Guid cartId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM cart_items WHERE cart_id = @cid";
                cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        private void TouchCart(Guid cartId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE carts SET updated_at = CURRENT_TIMESTAMP WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", cartId.ToString());
                cmd.ExecuteNonQuery();
            }
        }
    }
}
