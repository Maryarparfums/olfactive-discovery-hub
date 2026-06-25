namespace Maryar.Api.Dtos
{
    public class ProductVariantDto
    {
        public string  Id       { get; set; }
        public int     VolumeMl { get; set; }
        public decimal Price    { get; set; }
        public int     StockQty { get; set; }
    }
}
