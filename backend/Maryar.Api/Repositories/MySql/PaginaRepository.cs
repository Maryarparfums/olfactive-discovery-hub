using System;
using System.Collections.Generic;
using System.Data;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class PaginaRepository : IPaginaRepository
    {
        private readonly IConnectionFactory _factory;
        public PaginaRepository(IConnectionFactory factory) { _factory = factory; }
        public PaginaRepository() : this(new MySqlConnectionFactory()) { }

        public PaginaEstatica GetBySlug(string slug)
        {
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, slug, titulo, corpo, created_at, ativo
                    FROM paginas_estaticas
                    WHERE slug = @slug AND ativo = 1
                    LIMIT 1";
                cmd.Parameters.AddWithValue("@slug", slug);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new PaginaEstatica
                    {
                        Id        = Convert.ToInt32(r["id"]),
                        Slug      = r["slug"].ToString(),
                        Titulo    = r["titulo"].ToString(),
                        Corpo     = r["corpo"].ToString(),
                        CreatedAt = Convert.ToDateTime(r["created_at"]),
                        Ativo     = Convert.ToBoolean(r["ativo"])
                    };
                }
            }
        }

        public IEnumerable<FaqItem> GetFaqByTipo(string tipo)
        {
            var list = new List<FaqItem>();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, tipo, titulo, corpo, ordem, created_at, ativo
                    FROM faq_items
                    WHERE tipo = @tipo AND ativo = 1
                    ORDER BY ordem ASC, id ASC";
                cmd.Parameters.AddWithValue("@tipo", tipo);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new FaqItem
                        {
                            Id        = Convert.ToInt32(r["id"]),
                            Tipo      = r["tipo"].ToString(),
                            Titulo    = r["titulo"].ToString(),
                            Corpo     = r["corpo"].ToString(),
                            Ordem     = Convert.ToInt32(r["ordem"]),
                            CreatedAt = Convert.ToDateTime(r["created_at"]),
                            Ativo     = Convert.ToBoolean(r["ativo"])
                        });
            }
            return list;
        }
    }
}
