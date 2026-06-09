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
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                SecurityProtocolType.Tls11;

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_Error()
        {
            var ex = Server.GetLastError();
            if (ex == null) return;

            Server.ClearError();

            Response.Clear();
            Response.StatusCode = 500;
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Write("=== ERRO CAPTURADO EM Application_Error ===\r\n\r\n");
            Response.Write("TIPO: " + ex.GetType().FullName + "\r\n");
            Response.Write("MENSAGEM: " + ex.Message + "\r\n");

            if (ex.InnerException != null)
            {
                Response.Write("\r\nINNER EXCEPTION:\r\n");
                Response.Write("TIPO: " + ex.InnerException.GetType().FullName + "\r\n");
                Response.Write("MENSAGEM: " + ex.InnerException.Message + "\r\n");
            }

            Response.Write("\r\nSTACK TRACE:\r\n" + ex.StackTrace);
            Response.End();
        }
    }
}
