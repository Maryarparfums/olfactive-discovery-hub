using System;
using System.Collections.Generic;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class FamilyRepository : IFamilyRepository
    {
        private readonly IConnectionFactory _factory;
        public FamilyRepository(IConnectionFactory factory) { _factory = factory; }
        public FamilyRepository() : this(new MySqlConnectionFactory()) { }

        public IEnumerable<FragranceFamily> GetAll()
        {
            var list = new List<FragranceFamily>();
            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, slug, name FROM fragrance_families ORDER BY name";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new FragranceFamily
                        {
                            Id = Guid.Parse(r["id"].ToString()),
                            Slug = r["slug"].ToString(),
                            Name = r["name"].ToString()
                        });
            }
            return list;
        }
    }
}
