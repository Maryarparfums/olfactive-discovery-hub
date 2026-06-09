using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace Maryar.Api.Infrastructure
{
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext ctx)
        {
            Trace.TraceError(ctx.Exception.ToString());

            var status = HttpStatusCode.InternalServerError;
            string message = "Erro interno do servidor.";

            if (ctx.Exception is UnauthorizedAccessException)
            {
                status = HttpStatusCode.Unauthorized;
                message = "Não autorizado.";
            }
            else if (ctx.Exception is ArgumentException)
            {
                status = HttpStatusCode.BadRequest;
                message = ctx.Exception.Message;
            }

            ctx.Response = ctx.Request.CreateResponse(status, new { error = message });
        }
    }
}
