using System;

namespace Maryar.Api.Models
{
    public class Brand
    {
        public Guid Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
