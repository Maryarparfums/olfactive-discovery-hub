using System;

namespace Maryar.Api.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
        public Guid BrandId { get; set; }
        public string BrandName { get; set; }
        public Guid? FamilyId { get; set; }
        public string FamilyName { get; set; }
        public string Concentration { get; set; }
        public int VolumeMl { get; set; }
        public decimal Price { get; set; }
        public int StockQty { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string DetailImageUrl { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Genero { get; set; }
        public string Inspiracao { get; set; }
        public string Status { get; set; }
    }
}
