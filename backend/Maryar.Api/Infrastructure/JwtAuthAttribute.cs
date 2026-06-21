using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Maryar.Api.Services;

namespace Maryar.Api.Infrastructure
{
    public class JwtAuthAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext ctx)
        {
            var auth = ctx.Request.Headers.Authorization;
            if (auth == null || auth.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter))
            {
                ctx.Response = ctx.Request.CreateResponse(HttpStatusCode.Unauthorized,
                    new { error = "Token ausente." });
                return;
            }

            try
            {
                var principal = new JwtService().ValidateToken(auth.Parameter);
                Thread.CurrentPrincipal = principal;
            }
            catch (Exception ex)
            {
                ctx.Response = ctx.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    new
                    {
                        error = "Token inválido.",
                        detail = ex.ToString()
                    });
            }
        }

        public static Guid? CurrentUserId()
        {
            var p = Thread.CurrentPrincipal as ClaimsPrincipal;
            if (p == null) return null;

            var sub = p.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            Guid id;
            return sub != null && Guid.TryParse(sub.Value, out id)
                ? id
                : (Guid?)null;
        }
    }
}
