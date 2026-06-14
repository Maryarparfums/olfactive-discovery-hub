using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Maryar.Api.Dtos;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;
using MySql.Data.MySqlClient;

namespace Maryar.Api.Repositories.MySql
{
    public class ProductRepository : IProductRepository
    {
        private readonly IConnectionFactory _factory;

        public ProductRepository(IConnectionFactory factory) { _factory = factory; }
        public ProductRepository() : this(new MySqlConnectionFactory()) { }

        private static Product MapRow(IDataReader r)
        {
            return new Product
            {
                Id = Guid.Parse(r["id"].ToString()),
                Slug = r["slug"].ToString(),
                Name = r["name"].ToString(),
                BrandId = Guid.Parse(r["brand_id"].ToString()),
                BrandName = r["brand_name"] == DBNull.Value ? null : r["brand_name"].ToString(),
                FamilyId = r["family_id"] == DBNull.Value ? (Guid?)null : Guid.Parse(r["family_id"].ToString()),
                FamilyName = r["family_name"] == DBNull.Value ? null : r["family_name"].ToString(),
                Concentration = r["concentration"] == DBNull.Value ? null : r["concentration"].ToString(),
                VolumeMl = Convert.ToInt32(r["volume_ml"]),
                Price = Convert.ToDecimal(r["price"]),
                StockQty = Convert.ToInt32(r["stock_qty"]),
                Description = r["description"] == DBNull.Value ? null : r["description"].ToString(),
                ImageUrl = r["image_url"] == DBNull.Value ? null : r["image_url"].ToString(),
                DetailImageUrl = r["detail_image_url"] == DBNull.Value ? null : r["detail_image_url"].ToString(),
                Active = Convert.ToBoolean(r["active"]),
                CreatedAt = Convert.ToDateTime(r["created_at"]),
                Genero = r["genero"] == DBNull.Value ? null : r["genero"].ToString(),
                Inspiracao = r["inspiracao"] == DBNull.Value ? null : r["inspiracao"].ToString(),
                Status = r["status"] == DBNull.Value ? null : r["status"].ToString()
            };
        }

        public IEnumerable<Product> Query(ProductQueryDto q, out int total)
        {
            var where = new StringBuilder(" WHERE p.active = 1 ");
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(q.Familia))
            {
                where.Append(" AND f.slug = @familia ");
                parameters.Add(new MySqlParameter("@familia", q.Familia));
            }
            if (!string.IsNullOrWhiteSpace(q.Marca))
            {
                where.Append(" AND b.slug = @marca ");
                parameters.Add(new MySqlParameter("@marca", q.Marca));
            }
            if (q.PrecoMin.HasValue)
            {
                where.Append(" AND p.price >= @precoMin ");
                parameters.Add(new MySqlParameter("@precoMin", q.PrecoMin.Value));
            }
            if (q.PrecoMax.HasValue)
            {
                where.Append(" AND p.price <= @precoMax ");
                parameters.Add(new MySqlParameter("@precoMax", q.PrecoMax.Value));
            }
            if (!string.IsNullOrWhiteSpace(q.Nota))
            {
                // Busca em qualquer pirâmide olfativa via JSON_SEARCH (MySQL 5.7+).
                where.Append(@" AND (
                    JSON_SEARCH(LOWER(d.notas_topo), 'one', LOWER(@nota)) IS NOT NULL OR
                    JSON_SEARCH(LOWER(d.notas_coracao), 'one', LOWER(@nota)) IS NOT NULL OR
                    JSON_SEARCH(LOWER(d.notas_base), 'one', LOWER(@nota)) IS NOT NULL
                ) ");
                parameters.Add(new MySqlParameter("@nota", "%" + q.Nota + "%"));
            }

            var page = q.Page < 1 ? 1 : q.Page;
            var size = q.PageSize < 1 || q.PageSize > 100 ? 24 : q.PageSize;
            var offset = (page - 1) * size;

            var baseFrom = @"
                FROM products p
                INNER JOIN brands b ON b.id = p.brand_id
                LEFT JOIN fragrance_families f ON f.id = p.family_id
                LEFT JOIN perfume_details d ON d.product_id = p.id ";

            using (var cn = _factory.Create())
            {
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) " + baseFrom + where;
                    foreach (var p in parameters) cmd.Parameters.Add(p);
                    total = Convert.ToInt32(cmd.ExecuteScalar());
                }

                var list = new List<Product>();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT p.id, p.slug, p.name, p.brand_id, b.name AS brand_name, " +
                        "p.family_id, f.name AS family_name, p.concentration, p.volume_ml, " +
                        "p.price, p.stock_qty, p.description, p.image_url, p.detail_image_url, " +
                        "p.active, p.created_at, p.genero, p.inspiracao, p.status " + baseFrom + where +
                        " ORDER BY p.created_at DESC LIMIT @limit OFFSET @offset";
                    foreach (var p in parameters)
                        cmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));
                    cmd.Parameters.AddWithValue("@limit", size);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapRow(r));
                }
                return list;
            }
        }

        private Product GetSingle(string whereClause, string paramName, object paramValue)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT p.id, p.slug, p.name, p.brand_id, b.name AS brand_name, " +
                    "p.family_id, f.name AS family_name, p.concentration, p.volume_ml, " +
                    "p.price, p.stock_qty, p.description, p.image_url, p.detail_image_url, " +
                    "p.active, p.created_at, p.genero, p.inspiracao, p.status " +
                    "FROM products p " +
                    "INNER JOIN brands b ON b.id = p.brand_id " +
                    "LEFT JOIN fragrance_families f ON f.id = p.family_id " +
                    "WHERE " + whereClause + " LIMIT 1";
                cmd.Parameters.AddWithValue(paramName, paramValue);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapRow(r) : null;
            }
        }

        public Product GetById(Guid id) =>
            GetSingle("p.id = @id", "@id", id.ToString());

        public Product GetBySlug(string slug) =>
            GetSingle("p.slug = @slug AND p.active = 1", "@slug", slug);

        public PerfumeDetails GetDetailsByProductId(Guid productId)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText =
                    @"SELECT
                        product_id,
                        CAST(notas_topo AS CHAR) AS notas_topo,
                        CAST(notas_coracao AS CHAR) AS notas_coracao,
                        CAST(notas_base AS CHAR) AS notas_base,
                        CAST(estacao AS CHAR) AS estacao,
                        CAST(periodo AS CHAR) AS periodo,
                        CAST(ocasiao AS CHAR) AS ocasiao,
                        fixacao,
                        projecao,
                        duracao_horas,
                        CAST(similares AS CHAR) AS similares
                    FROM perfume_details
                    WHERE product_id = @id
                    LIMIT 1";
                cmd.Parameters.AddWithValue("@id", productId.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new PerfumeDetails
                    {
                        ProductId = Guid.Parse(r["product_id"].ToString()),
                        NotasTopoJson = r["notas_topo"] == DBNull.Value ? "[]" : r["notas_topo"].ToString(),
                        NotasCoracaoJson = r["notas_coracao"] == DBNull.Value ? "[]" : r["notas_coracao"].ToString(),
                        NotasBaseJson = r["notas_base"] == DBNull.Value ? "[]" : r["notas_base"].ToString(),
                        EstacaoJson = r["estacao"] == DBNull.Value ? "{}" : r["estacao"].ToString(),
                        PeriodoJson = r["periodo"] == DBNull.Value ? "{}" : r["periodo"].ToString(),
                        OcasiaoJson = r["ocasiao"] == DBNull.Value ? "{}" : r["ocasiao"].ToString(),
                        Fixacao = r["fixacao"] == DBNull.Value ? 0 : Convert.ToInt32(r["fixacao"]),
                        Projecao = r["projecao"] == DBNull.Value ? 0 : Convert.ToInt32(r["projecao"]),
                        DuracaoHoras = r["duracao_horas"] == DBNull.Value ? null : r["duracao_horas"].ToString(),
                        SimilaresJson = r["similares"] == DBNull.Value ? "[]" : r["similares"].ToString()
                    };
                }
            }
        }
    }
}
