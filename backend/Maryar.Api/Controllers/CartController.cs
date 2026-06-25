using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Infrastructure;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
  [RoutePrefix("cart")]
  public class CartController : ApiController
  {
      private const string CookieName = "maryar_cart";
      private readonly ICartRepository _carts;
      private readonly IProductRepository _products;

      public CartController() : this(new CartRepository(), new ProductRepository()) { }
      public CartController(ICartRepository carts, IProductRepository products)
      {
          _carts    = carts;
          _products = products;
      }

      private Guid ResolveCartId()
      {
          var userId = JwtAuthAttribute.CurrentUserId();
          string token = null;

          var ctx = HttpContext.Current;
          if (ctx != null)
          {
              var c = ctx.Request.Cookies[CookieName];
              token = c != null ? c.Value : null;
              if (!userId.HasValue && string.IsNullOrEmpty(token))
              {
                  token = Guid.NewGuid().ToString("N");
                  var newCookie = new HttpCookie(CookieName, token)
                  {
                      HttpOnly = true,
                      Secure   = ctx.Request.IsSecureConnection,
                      Expires  = DateTime.UtcNow.AddDays(30),
                      Path     = "/"
                  };
                  ctx.Response.Cookies.Add(newCookie);
              }
          }

          return _carts.GetOrCreate(userId, token).Id;
      }

      private CartDto BuildDto(Guid cartId)
      {
          var items = _carts.GetItems(cartId).Select(i => new CartItemDto
          {
              Id        = i.Id,
              ProductId = i.ProductId,
              VariantId = i.VariantId,      // ← novo
              Slug      = i.ProductSlug,
              Name      = i.ProductName,
              Brand     = i.BrandName,
              ImageUrl  = i.ProductImage,
              VolumeMl  = i.VolumeMl,       // ← novo
              Quantity  = i.Quantity,
              UnitPrice = i.UnitPrice,
              LineTotal = i.UnitPrice * i.Quantity
          }).ToList();

          return new CartDto
          {
              Id        = cartId,
              Items     = items,
              Subtotal  = items.Sum(x => x.LineTotal),
              ItemCount = items.Sum(x => x.Quantity)
          };
      }

      [HttpGet, Route("")]
      public IHttpActionResult Get()
      {
          var cartId = ResolveCartId();
          return Ok(BuildDto(cartId));
      }

      [HttpPost, Route("items")]
      public IHttpActionResult AddItem([FromBody] AddItemRequest req)
      {
          if (req == null || req.Quantity < 1)
              return Content(HttpStatusCode.BadRequest, new { error = "Quantidade inválida." });

          var product = _products.GetById(req.ProductId);
          if (product == null || !product.Active)
              return Content(HttpStatusCode.NotFound, new { error = "Produto não encontrado." });

          // Resolve preço e volume a partir da variante selecionada
          decimal price    = product.Price;
          Guid?   variantId = null;
          int?    volumeMl  = null;

          if (req.VariantId.HasValue)
          {
              // Busca todas as variantes do produto e localiza a escolhida
              var variant = _products
                  .GetVariantsByProductId(product.Id)
                  .FirstOrDefault(v => v.Id == req.VariantId.Value.ToString());

              if (variant == null)
                  return Content(HttpStatusCode.BadRequest, new { error = "Apresentação não encontrada." });

              if (variant.StockQty == 0)
                  return Content(HttpStatusCode.BadRequest, new { error = "Apresentação esgotada." });

              price     = variant.Price;
              variantId = req.VariantId;
              volumeMl  = variant.VolumeMl;
          }

          var cartId = ResolveCartId();
          _carts.AddItem(cartId, product.Id, variantId, volumeMl, req.Quantity, price);
          return Ok(BuildDto(cartId));
      }

      [HttpPut, Route("items/{itemId:guid}")]
      public IHttpActionResult UpdateItem(Guid itemId, [FromBody] UpdateItemByIdRequest req)
      {
          if (req == null || req.Quantity < 0)
              return Content(HttpStatusCode.BadRequest, new { error = "Quantidade inválida." });

          var cartId = ResolveCartId();
          if (req.Quantity == 0) _carts.RemoveItem(itemId);
          else _carts.UpdateItemQty(itemId, req.Quantity);
          return Ok(BuildDto(cartId));
      }

      [HttpDelete, Route("items/{itemId:guid}")]
      public IHttpActionResult RemoveItem(Guid itemId)
      {
          var cartId = ResolveCartId();
          _carts.RemoveItem(itemId);
          return Ok(BuildDto(cartId));
      }

      [HttpDelete, Route("")]
      public IHttpActionResult Clear()
      {
          var cartId = ResolveCartId();
          _carts.Clear(cartId);
          return Ok(BuildDto(cartId));
      }

      [HttpPost, Route("update-item")]
      public IHttpActionResult UpdateItemPost([FromBody] UpdateItemByIdRequest req)
      {
          if (req == null || req.Quantity < 0)
              return Content(HttpStatusCode.BadRequest, new { error = "Quantidade inválida." });

          var cartId = ResolveCartId();
          if (req.Quantity == 0) _carts.RemoveItem(req.ItemId);
          else _carts.UpdateItemQty(req.ItemId, req.Quantity);
          return Ok(BuildDto(cartId));
      }

      [HttpPost, Route("remove-item")]
      public IHttpActionResult RemoveItemPost([FromBody] RemoveItemByIdRequest req)
      {
          if (req == null)
              return Content(HttpStatusCode.BadRequest, new { error = "Dados inválidos." });

          var cartId = ResolveCartId();
          _carts.RemoveItem(req.ItemId);
          return Ok(BuildDto(cartId));
      }

      [HttpPost, Route("clear")]
      public IHttpActionResult ClearPost()
      {
          var cartId = ResolveCartId();
          _carts.Clear(cartId);
          return Ok(BuildDto(cartId));
      }
  }
}
