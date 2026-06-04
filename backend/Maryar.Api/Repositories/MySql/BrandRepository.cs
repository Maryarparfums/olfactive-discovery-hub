using System;
using System.Collections.Generic;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class BrandRepository : IBrandRepository
    {
        private readonly IConnectionFactory _factory;
        public BrandRepository(IConnectionFactory factory) { _factory = factory; }
        public BrandRepository() : this(new MySqlConnectionFactory()) { }

        public IEnumerable<Brand> GetAll()
        {
            var list = new List<Brand>();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, slug, name, description FROM brands ORDER BY name";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Brand
                        {
                            Id = Guid.Parse(r["id"].ToString()),
                            Slug = r["slug"].ToString(),
                            Name = r["name"].ToString(),
                            Description = r["description"] == DBNull.Value ? null : r["description"].ToString()
                        });
            }
            return list;
        }
    }
}
