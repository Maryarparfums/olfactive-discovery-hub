using System.Collections.Generic;
using System.Threading;
using System.Security.Claims;
using System.Web.Http;
using MySql.Data.MySqlClient;
using Maryar.Api.Infrastructure;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("favorites")]
    public class FavoritesController : ApiController
    {
        private static string ConnStr =>
            System.Configuration.ConfigurationManager
                  .ConnectionStrings["MaryarDB"].ConnectionString;

        // ── GET /api/favorites ──────────────────────────────────────────
        [HttpGet, Route("")]
        [JwtAuthAttribute]
        public IHttpActionResult Get()
        {
            string userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

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

        // ── POST /api/favorites/{slug} ──────────────────────────────────
        // Adiciona um favorito individual (usuário já logado)
        [HttpPost, Route("{slug}")]
        [JwtAuthAttribute]
        public IHttpActionResult Add(string slug)
        {
            string userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(@"
                    INSERT IGNORE INTO user_favorites (user_id, product_slug, created_at)
                    VALUES (@uid, @slug, NOW())", conn))
                {
                    cmd.Parameters.AddWithValue("@uid",  userId);
                    cmd.Parameters.AddWithValue("@slug", slug.Trim().ToLower());
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok();
        }

        // ── POST /api/favorites/sync ────────────────────────────────────
        // Recebe array de slugs do localStorage e faz merge com o banco.
        // Chamado automaticamente quando o cliente faz login.
        [HttpPost, Route("sync")]
        [JwtAuthAttribute]
        public IHttpActionResult Sync([FromBody] SyncRequest body)
        {
            if (body?.Slugs == null || body.Slugs.Count == 0)
                return Ok(new { synced = 0 });

            string userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            int inserted = 0;

            using (var conn = new MySqlConnection(ConnStr))
            {
                conn.Open();
                foreach (var slug in body.Slugs)
                {
                    if (string.IsNullOrWhiteSpace(slug)) continue;

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
        [HttpDelete, Route("{slug}")]
        [JwtAuthAttribute]
        public IHttpActionResult Remove(string slug)
        {
            string userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

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

        // ── Helper: lê o user_id (GUID string) das claims do JWT ────────
        // O [JwtAuthAttribute] já validou o token e populou Thread.CurrentPrincipal
        private static string GetCurrentUserId()
        {
            var principal = Thread.CurrentPrincipal as ClaimsPrincipal;
            if (principal == null) return null;

            var claim = principal.FindFirst("userId")
                     ?? principal.FindFirst(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirst("sub");

            return string.IsNullOrWhiteSpace(claim?.Value) ? null : claim.Value;
        }
    }

    public class SyncRequest
    {
        public List<string> Slugs { get; set; }
    }
}
