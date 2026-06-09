using System.Web.Http;
using System.Web.Http.Cors;
using Maryar.Api.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Maryar.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var origins = AppConfig.Get("Maryar:CorsOrigins") ?? "*";
            var cors = new EnableCorsAttribute(origins, "*", "*")
            {
                SupportsCredentials = true
            };
            config.EnableCors(cors);

            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            json.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            json.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Filters.Add(new ApiExceptionFilter());

            // Rotas explícitas — sem MapHttpAttributeRoutes (incompatível com Medium Trust)
            config.Routes.MapHttpRoute(
                name: "ProductsList",
                routeTemplate: "api/products",
                defaults: new { controller = "Products", action = "List" }
            );
            config.Routes.MapHttpRoute(
                name: "ProductsBySlug",
                routeTemplate: "api/products/{slug}",
                defaults: new { controller = "Products", action = "GetBySlug" }
            );
            config.Routes.MapHttpRoute(
                name: "BrandsList",
                routeTemplate: "api/brands",
                defaults: new { controller = "Brands", action = "List" }
            );
            config.Routes.MapHttpRoute(
                name: "FamiliesList",
                routeTemplate: "api/families",
                defaults: new { controller = "Families", action = "List" }
            );
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
