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

            // Produtos
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

            // Brands e Families — sem forçar nome de action, Web API seleciona pelo [HttpGet]
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

            // Fallback genérico
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
