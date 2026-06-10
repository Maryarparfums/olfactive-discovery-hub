using System.Web.Http;
using Maryar.Api.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Maryar.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            json.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            json.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Filters.Add(new ApiExceptionFilter());

            config.Routes.MapHttpRoute(
                name: "ProductsBySlug",
                routeTemplate: "products/{slug}",
                defaults: new { controller = "Products", action = "GetBySlug" }
            );
            config.Routes.MapHttpRoute(
                name: "ProductsList",
                routeTemplate: "products",
                defaults: new { controller = "Products" }
            );
            config.Routes.MapHttpRoute(
                name: "BrandsList",
                routeTemplate: "brands",
                defaults: new { controller = "Brands" }
            );
            config.Routes.MapHttpRoute(
                name: "FamiliesList",
                routeTemplate: "families",
                defaults: new { controller = "Families" }
            );
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
