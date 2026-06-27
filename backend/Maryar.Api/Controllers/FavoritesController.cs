using System;
using System.Collections.Generic;
using System.Web.Http;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("favorites")]
    public class FavoritesController : ApiController
    {
        private static string ConnStr =>
            System.Configuration.ConfigurationManager
                  .ConnectionStrings["MaryarDB"].ConnectionString;

        // ── GET /api/favorites ──────────────────────────────────────────
        // Retorna os slugs favoritados pelo usuário logado
        [HttpGet, Route("")]
        [Authorize]
        public IHttpActionResult Get()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var slugs = new List<string>();

            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "SELECT product_slug FROM user_favorites WHERE user_id = @uid ORDER BY created_at DESC",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            slugs.Add(reader.GetString("product_slug"));
                }
            }

            return Ok(new { slugs });
        }

        // ── POST /api/favorites/sync ────────────────────────────────────
        // Recebe array de slugs do localStorage e faz merge com o banco.
        // Chamado automaticamente quando o cliente faz login.
        [HttpPost, Route("sync")]
        [Authorize]
        public IHttpActionResult Sync([FromBody] SyncRequest body)
        {
            if (body?.Slugs == null || body.Slugs.Count == 0)
                return Ok(new { synced = 0 });

            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            int inserted = 0;

            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                foreach (var slug in body.Slugs)
                {
                    if (string.IsNullOrWhiteSpace(slug)) continue;

                    // INSERT IGNORE evita erro de duplicata (UNIQUE KEY uq_user_product)
                    using (var cmd = new MySqlCommand(@"
                        INSERT IGNORE INTO user_favorites (user_id, product_slug, created_at)
                        VALUES (@uid, @slug, NOW())", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid",  userId);
                        cmd.Parameters.AddWithValue("@slug", slug.Trim().ToLower());
                        inserted += cmd.ExecuteNonQuery();
                    }
                }
            }

            return Ok(new { synced = inserted });
        }

        // ── DELETE /api/favorites/{slug} ────────────────────────────────
        // Remove um favorito individual
        [HttpDelete, Route("{slug}")]
        [Authorize]
        public IHttpActionResult Remove(string slug)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "DELETE FROM user_favorites WHERE user_id = @uid AND product_slug = @slug",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@uid",  userId);
                    cmd.Parameters.AddWithValue("@slug", slug.Trim().ToLower());
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok();
        }

        // ── Helper: extrai o user_id do token/sessão ────────────────────
        private int GetCurrentUserId()
        {
            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            if (identity == null) return 0;

            // Tenta a claim "userId" primeiro, depois "sub" (padrão JWT)
            var claim = identity.FindFirst("userId")
                     ?? identity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                     ?? identity.FindFirst("sub");

            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }

    public class SyncRequest
    {
        public List<string> Slugs { get; set; }
    }
}
