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
            // CORS — lê origens do Web.config
            var origins = AppConfig.Get("Maryar:CorsOrigins") ?? "https://maryar.com.br";
            var cors = new EnableCorsAttribute(
                origins: origins,
                headers: "Content-Type, Authorization, X-Requested-With",
                methods: "GET, POST, PUT, DELETE, OPTIONS"
            );
            cors.SupportsCredentials = true;
            config.EnableCors(cors);

            // JSON
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            json.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            json.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Filters.Add(new ApiExceptionFilter());

            config.MapHttpAttributeRoutes();

            // Auth
            config.Routes.MapHttpRoute(
                name: "AuthSignUp",
                routeTemplate: "auth/signup",
                defaults: new { controller = "Auth", action = "SignUp" }
            );
            config.Routes.MapHttpRoute(
                name: "AuthSignIn",
                routeTemplate: "auth/signin",
                defaults: new { controller = "Auth", action = "SignIn" }
            );
            config.Routes.MapHttpRoute(
                name: "AuthForgotPassword",
                routeTemplate: "auth/forgotpassword",
                defaults: new { controller = "Auth", action = "ForgotPassword" }
            );
            config.Routes.MapHttpRoute(
                name: "AuthResetPassword",
                routeTemplate: "auth/resetpassword",
                defaults: new { controller = "Auth", action = "ResetPassword" }
            );

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

            // Brands e Families
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
