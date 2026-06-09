using System.Net.Http;
using System.Net;
using System.Web.Http.Filters;

namespace Maryar.Api.Infrastructure
{
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext ctx)
        {
            ctx.Response = ctx.Request.CreateResponse(
                HttpStatusCode.InternalServerError,
                new
                {
                    exception = ctx.Exception.ToString()
                });
        }
    }
}
