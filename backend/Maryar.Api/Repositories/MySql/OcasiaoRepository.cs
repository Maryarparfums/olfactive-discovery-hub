using System.Collections.Generic;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class OcasiaoRepository : IOcasiaoRepository
    {
        private readonly IConnectionFactory _factory;
        public OcasiaoRepository() : this(new MySqlConnectionFactory()) { }
        public OcasiaoRepository(IConnectionFactory factory) { _factory = factory; }

        public IEnumerable<Ocasiao> GetAll()
        {
            var list = new List<Ocasiao>();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, slug, name, description FROM ocasioes ORDER BY name";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Ocasiao
                        {
                            Id          = r["id"].ToString(),
                            Slug        = r["slug"].ToString(),
                            Name        = r["name"].ToString(),
                            Description = r["description"].ToString()
                        });
            }
            return list;
        }
    }
}
