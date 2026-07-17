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

        // ── GetOrCreate ──────────────────────────────────────────────────────
        //
        // Casos tratados quando userId está presente:
        //   A) Só carrinho de usuário existe          → retorna ele
        //   B) Só carrinho de visitante existe        → apropria (UPDATE user_id)
        //   C) Ambos existem                          → migra itens do visitante
        //                                               para o carrinho do usuário,
        //                                               apaga o carrinho visitante
        //   D) Nenhum existe                          → cria carrinho para o usuário
        //
        // Quando só token (visitante): busca ou cria pelo token.
        public Cart GetOrCreate(Guid? userId, string anonymousToken)
        {
            using (var cn = _factory.Create())
            {
                // ── Usuário logado ───────────────────────────────────────────
                if (userId.HasValue)
                {
                    // Busca carrinho do usuário
                    Cart userCart = null;
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText =
                            "SELECT id, user_id, anonymous_token, created_at, updated_at " +
                            "FROM carts WHERE user_id = @uid LIMIT 1";
                        cmd.Parameters.AddWithValue("@uid", userId.Value.ToString());
                        using (var r = cmd.ExecuteReader())
                            if (r.Read()) userCart = ReadCart(r);
                    }

                    // Busca carrinho de visitante (se houver token)
                    Cart guestCart = null;
                    if (!string.IsNullOrEmpty(anonymousToken))
                    {
                        using (var cmd = cn.CreateCommand())
                        {
                            cmd.CommandText =
                                "SELECT id, user_id, anonymous_token, created_at, updated_at " +
                                "FROM carts WHERE anonymous_token = @tok LIMIT 1";
                            cmd.Parameters.AddWithValue("@tok", anonymousToken);
                            using (var r = cmd.ExecuteReader())
                                if (r.Read()) guestCart = ReadCart(r);
                        }
                    }

                    // Caso C: ambos existem → migra itens e apaga carrinho visitante
                    if (userCart != null && guestCart != null)
                    {
                        using (var cmd = cn.CreateCommand())
                        {
                            cmd.CommandText =
                                "UPDATE cart_items SET cart_id = @ucid WHERE cart_id = @gcid";
                            cmd.Parameters.AddWithValue("@ucid", userCart.Id.ToString());
                            cmd.Parameters.AddWithValue("@gcid", guestCart.Id.ToString());
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = cn.CreateCommand())
                        {
                            cmd.CommandText = "DELETE FROM carts WHERE id = @id";
                            cmd.Parameters.AddWithValue("@id", guestCart.Id.ToString());
                            cmd.ExecuteNonQuery();
                        }
                        return userCart;
                    }

                    // Caso A: só carrinho do usuário
                    if (userCart != null) return userCart;

                    // Caso B: só carrinho de visitante → apropria para o usuário
                    if (guestCart != null)
                    {
                        using (var cmd = cn.CreateCommand())
                        {
                            cmd.CommandText =
                                "UPDATE carts SET user_id = @uid, anonymous_token = NULL " +
                                "WHERE id = @id";
                            cmd.Parameters.AddWithValue("@uid", userId.Value.ToString());
                            cmd.Parameters.AddWithValue("@id",  guestCart.Id.ToString());
                            cmd.ExecuteNonQuery();
                        }
                        guestCart.UserId         = userId;
                        guestCart.AnonymousToken = null;
                        return guestCart;
                    }

                    // Caso D: nenhum carrinho existe → cria para o usuário
                    var newId = Guid.NewGuid();
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText =
                            "INSERT INTO carts (id, user_id, anonymous_token) VALUES (@id, @uid, NULL)";
                        cmd.Parameters.AddWithValue("@id",  newId.ToString());
                        cmd.Parameters.AddWithValue("@uid", userId.Value.ToString());
                        cmd.ExecuteNonQuery();
                    }
                    return new Cart
                    {
                        Id             = newId,
                        UserId         = userId,
                        AnonymousToken = null,
                        CreatedAt      = DateTime.UtcNow,
                        UpdatedAt      = DateTime.UtcNow
                    };
                }

                // ── Visitante (sem userId) ───────────────────────────────────
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT id, user_id, anonymous_token, created_at, updated_at " +
                        "FROM carts WHERE anonymous_token = @tok LIMIT 1";
                    cmd.Parameters.AddWithValue("@tok", anonymousToken);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return ReadCart(r);
                }

                var anonId = Guid.NewGuid();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO carts (id, user_id, anonymous_token) VALUES (@id, NULL, @tok)";
                    cmd.Parameters.AddWithValue("@id",  anonId.ToString());
                    cmd.Parameters.AddWithValue("@tok", anonymousToken);
                    cmd.ExecuteNonQuery();
                }
                return new Cart
                {
                    Id             = anonId,
                    UserId         = null,
                    AnonymousToken = anonymousToken,
                    CreatedAt      = DateTime.UtcNow,
                    UpdatedAt      = DateTime.UtcNow
                };
            }
        }

        // ── GetById ──────────────────────────────────────────────────────────
        public Cart GetById(Guid cartId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT id, user_id, anonymous_token, created_at, updated_at " +
                    "FROM carts WHERE id = @id LIMIT 1";
                cmd.Parameters.AddWithValue("@id", cartId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return ReadCart(r);
                }
            }
        }

        // ── GetItems ─────────────────────────────────────────────────────────
        public IEnumerable<CartItem> GetItems(Guid cartId)
        {
            var list = new List<CartItem>();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT ci.id, ci.cart_id, ci.product_id, ci.variant_id, ci.volume_ml, " +
                    "       ci.quantity, ci.unit_price, " +
                    "       p.slug AS product_slug, p.name AS product_name, " +
                    "       COALESCE(pv.image_url, p.image_url) AS product_image, " +
                    "       b.name AS brand_name " +
                    "FROM cart_items ci " +
                    "INNER JOIN products         p  ON p.id  = ci.product_id " +
                    "INNER JOIN brands           b  ON b.id  = p.brand_id " +
                    "LEFT  JOIN product_variants pv ON pv.id = ci.variant_id " +
                    "WHERE ci.cart_id = @cid";
                cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new CartItem
                        {
                            Id           = Guid.Parse(r["id"].ToString()),
                            CartId       = Guid.Parse(r["cart_id"].ToString()),
                            ProductId    = Guid.Parse(r["product_id"].ToString()),
                            VariantId    = r["variant_id"] == DBNull.Value ? (Guid?)null : Guid.Parse(r["variant_id"].ToString()),
                            VolumeMl     = r["volume_ml"]  == DBNull.Value ? (int?)null  : Convert.ToInt32(r["volume_ml"]),
                            Quantity     = Convert.ToInt32(r["quantity"]),
                            UnitPrice    = Convert.ToDecimal(r["unit_price"]),
                            ProductSlug  = r["product_slug"].ToString(),
                            ProductName  = r["product_name"].ToString(),
                            ProductImage = r["product_image"] == DBNull.Value ? null : r["product_image"].ToString(),
                            BrandName    = r["brand_name"].ToString()
                        });
            }
            return list;
        }

        // ── GetItem ──────────────────────────────────────────────────────────
        public CartItem GetItem(Guid cartId, Guid productId, Guid? variantId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                if (variantId.HasValue)
                {
                    cmd.CommandText =
                        "SELECT id, cart_id, product_id, variant_id, volume_ml, quantity, unit_price " +
                        "FROM cart_items " +
                        "WHERE cart_id = @cid AND product_id = @pid AND variant_id = @vid LIMIT 1";
                    cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                    cmd.Parameters.AddWithValue("@pid", productId.ToString());
                    cmd.Parameters.AddWithValue("@vid", variantId.Value.ToString());
                }
                else
                {
                    cmd.CommandText =
                        "SELECT id, cart_id, product_id, variant_id, volume_ml, quantity, unit_price " +
                        "FROM cart_items " +
                        "WHERE cart_id = @cid AND product_id = @pid AND variant_id IS NULL LIMIT 1";
                    cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                    cmd.Parameters.AddWithValue("@pid", productId.ToString());
                }

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new CartItem
                    {
                        Id        = Guid.Parse(r["id"].ToString()),
                        CartId    = Guid.Parse(r["cart_id"].ToString()),
                        ProductId = Guid.Parse(r["product_id"].ToString()),
                        VariantId = r["variant_id"] == DBNull.Value ? (Guid?)null : Guid.Parse(r["variant_id"].ToString()),
                        VolumeMl  = r["volume_ml"]  == DBNull.Value ? (int?)null  : Convert.ToInt32(r["volume_ml"]),
                        Quantity  = Convert.ToInt32(r["quantity"]),
                        UnitPrice = Convert.ToDecimal(r["unit_price"])
                    };
                }
            }
        }

        // ── AddItem ──────────────────────────────────────────────────────────
        public Guid AddItem(Guid cartId, Guid productId, Guid? variantId, int? volumeMl, int qty, decimal unitPrice)
        {
            var existing = GetItem(cartId, productId, variantId);
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
                    "INSERT INTO cart_items (id, cart_id, product_id, variant_id, volume_ml, quantity, unit_price) " +
                    "VALUES (@id, @cid, @pid, @vid, @vml, @q, @up)";
                cmd.Parameters.AddWithValue("@id",  id.ToString());
                cmd.Parameters.AddWithValue("@cid", cartId.ToString());
                cmd.Parameters.AddWithValue("@pid", productId.ToString());
                cmd.Parameters.AddWithValue("@vid", (object)(variantId?.ToString()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@vml", (object)volumeMl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@q",   qty);
                cmd.Parameters.AddWithValue("@up",  unitPrice);
                cmd.ExecuteNonQuery();
            }

            TouchCart(cartId);
            return id;
        }

        // ── UpdateItemQty ────────────────────────────────────────────────────
        public void UpdateItemQty(Guid itemId, int qty)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE cart_items SET quantity = @q WHERE id = @id";
                cmd.Parameters.AddWithValue("@q",  qty);
                cmd.Parameters.AddWithValue("@id", itemId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ── RemoveItem ───────────────────────────────────────────────────────
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

        // ── Clear ────────────────────────────────────────────────────────────
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

        // ── TouchCart ────────────────────────────────────────────────────────
        private void TouchCart(Guid cartId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "UPDATE carts SET updated_at = NOW() WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", cartId.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ── Helper ───────────────────────────────────────────────────────────
        private static Cart ReadCart(System.Data.IDataReader r)
        {
            return new Cart
            {
                Id             = Guid.Parse(r["id"].ToString()),
                UserId         = r["user_id"] == DBNull.Value
                                     ? (Guid?)null
                                     : Guid.Parse(r["user_id"].ToString()),
                AnonymousToken = r["anonymous_token"] == DBNull.Value
                                     ? null
                                     : r["anonymous_token"].ToString(),
                CreatedAt      = Convert.ToDateTime(r["created_at"]),
                UpdatedAt      = Convert.ToDateTime(r["updated_at"])
            };
        }
    }
}
