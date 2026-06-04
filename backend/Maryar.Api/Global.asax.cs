using System;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Maryar.Api
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Força TLS 1.2 para chamadas HTTPS de saída (InfinitePay)
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Resposta a preflight CORS já é tratada pelo EnableCorsAttribute.
        }
    }
}
