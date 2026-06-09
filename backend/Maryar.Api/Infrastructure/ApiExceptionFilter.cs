using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Maryar.Api.Infrastructure;
namespace Maryar.Api.Infrastructure
{
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext ctx)
        {
            var ex = ctx.Exception;
            Trace.TraceError(ex.ToString());
            var status = HttpStatusCode.InternalServerError;
            string message = "Erro interno do servidor.";
            if (ex is UnauthorizedAccessException)
            {
                status = HttpStatusCode.Unauthorized;
                message = "Não autorizado.";
            }
            else if (ex is ArgumentException)
            {
                status = HttpStatusCode.BadRequest;
                message = ex.Message;
            }
            // Modo diagnóstico: expõe a exceção real no JSON
            // REMOVA antes de ir para produção definitiva
            var diagnostico = AppConfig.Get("Maryar:DiagnosticoAtivo");
            if (diagnostico == "true")
            {
                ctx.Response = ctx.Request.CreateResponse(status, new
                {
                    error = message,
                    exceptionType = ex.GetType().FullName,
                    exceptionMessage = ex.Message,
                    innerException = ex.InnerException != null ? ex.InnerException.Message : null,
                    stackTrace = ex.StackTrace
                });
                return;
            }
            ctx.Response = ctx.Request.CreateResponse(status, new { error = message });
        }
    }
}
