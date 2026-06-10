using System;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Maryar.Api
{
    public class WebApiApplication : HttpApplication
    {
        private static readonly string[] AllowedOrigins =
        {
            "https://maryar.com.br",
            "https://www.maryar.com.br",
            "http://localhost:5173"
        };

        protected void Application_Start()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                SecurityProtocolType.Tls11;

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_BeginRequest()
        {
            var origin = Request.Headers["Origin"];

            if (!string.IsNullOrEmpty(origin) &&
                Array.IndexOf(AllowedOrigins, origin) >= 0)
            {
                Response.Headers.Set("Access-Control-Allow-Origin", origin);
                Response.Headers.Set("Access-Control-Allow-Credentials", "true");
                Response.Headers.Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                Response.Headers.Set("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
            }

            if (Request.HttpMethod == "OPTIONS")
            {
                Response.StatusCode = 200;
                Response.Flush();
                Response.End();
            }
        }
    }
}
