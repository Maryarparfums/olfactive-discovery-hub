using System.Linq;
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
            // CORS - origens permitidas no appSettings (Maryar:CorsOrigins)
            var origins = AppConfig.Get("Maryar:CorsOrigins") ?? "*";
            var cors = new EnableCorsAttribute(origins, "*", "*")
            {
                SupportsCredentials = true
            };
            config.EnableCors(cors);

            // JSON: camelCase + ignorar nulos + sem referências circulares
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            json.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            json.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            // Remove XML para simplificar respostas
            var xml = config.Formatters.XmlFormatter;
            config.Formatters.Remove(xml);

            // Filtro global de exceções
            config.Filters.Add(new ApiExceptionFilter());

            // Rotas attribute-based
            config.MapHttpAttributeRoutes();

            // Rota fallback
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
