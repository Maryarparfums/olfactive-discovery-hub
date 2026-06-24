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
                name: "AuthChangePassword",
                routeTemplate: "auth/changepassword",
                defaults: new { controller = "Auth", action = "ChangePassword" }
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
            config.Routes.MapHttpRoute(
                name: "AuthVerifyEmail",
                routeTemplate: "auth/verifyemail",
                defaults: new { controller = "Auth", action = "VerifyEmail" }
            );
            config.Routes.MapHttpRoute(
                name: "AuthResendVerification",
                routeTemplate: "auth/resendverification",
                defaults: new { controller = "Auth", action = "ResendVerification" }
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
            config.Routes.MapHttpRoute(
                name: "AuthRequestEmailChange",
                routeTemplate: "auth/requestemailchange",
                defaults: new { controller = "Auth", action = "RequestEmailChange" }
            );
        }
    }
}
