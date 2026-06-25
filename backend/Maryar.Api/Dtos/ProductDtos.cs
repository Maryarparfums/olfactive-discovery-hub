using System;
using System.Collections.Generic;

namespace Maryar.Api.Dtos
{
  // ── NOVO: representa uma variante de volume/preço de um produto ──────────
  public class ProductVariantDto
  {
      public string  Id       { get; set; }
      public int     VolumeMl { get; set; }
      public decimal Price    { get; set; }
      public int     StockQty { get; set; }
  }

  public class ProductListItemDto
  {
      public Guid    Id            { get; set; }
      public string  Slug          { get; set; }
      public string  Name          { get; set; }
      public string  Brand         { get; set; }
      public string  Family        { get; set; }
      public string  Concentration { get; set; }
      public int     VolumeMl      { get; set; }
      public decimal Price         { get; set; }
      public string  ImageUrl      { get; set; }
      public int     StockQty      { get; set; }
      public string  Genero        { get; set; }
      public string  Inspiracao    { get; set; }
      public string  Status        { get; set; }

      public List<string>            NotasTopo    { get; set; }
      public List<string>            NotasCoracao { get; set; }
      public List<string>            NotasBase    { get; set; }
      public Dictionary<string, int> Estacao      { get; set; }
      public Dictionary<string, int> Periodo      { get; set; }
      public Dictionary<string, int> Ocasiao      { get; set; }

      // Variantes de volume/preço — herdada por ProductDetailDto
      public List<ProductVariantDto> Variants { get; set; }
          = new List<ProductVariantDto>();
  }

  // ProductDetailDto herda tudo de ProductListItemDto, inclusive Variants.
  public class ProductDetailDto : ProductListItemDto
  {
      public string       Description    { get; set; }
      public string       DetailImageUrl { get; set; }
      public int          Fixacao        { get; set; }
      public int          Projecao       { get; set; }
      public string       DuracaoHoras   { get; set; }
      public List<string> Similares      { get; set; }
  }

  public class ProductQueryDto
  {
      public string   Familia  { get; set; }
      public string   Marca    { get; set; }
      public string   Nota     { get; set; }
      public string   Estacao  { get; set; }
      public string   Periodo  { get; set; }
      public string   Ocasiao  { get; set; }
      public decimal? PrecoMin { get; set; }
      public decimal? PrecoMax { get; set; }
      public int      Page     { get; set; }
      public int      PageSize { get; set; }
      public string   Genero   { get; set; }
      public string   Status   { get; set; }
  }
}
