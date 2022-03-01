using Estranged.Lfs.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading;
using static Estranged.Lfs.Api.HeaderDictionaryExtensions;

namespace Estranged.Lfs.Api.Filters
{
    public class BasicAuthFilter : IAsyncActionFilter
    {
        private readonly ILogger<BasicAuthFilter> logger;
        private readonly IAuthenticator authenticator;

        public BasicAuthFilter(ILogger<BasicAuthFilter> logger, IAuthenticator authenticator)
        {
            this.logger = logger;
            this.authenticator = authenticator;
        }

        public string AuthorizationHeader => "Authorization";
        public string BasicPrefix => "Basic";

        private void Unauthorised(ActionExecutingContext context)
        {
            context.Result = new StatusCodeResult(401);
        }

        private LfsPermission GetRequiredPermission(HttpRequest request) => request.Method.ToUpper() == "GET" ? LfsPermission.Read : LfsPermission.Write;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string username;
            string password;
            try
            {
                (username, password) = context.HttpContext.Request.Headers.GetGitCredentials();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error getting Basic credentials");
                Unauthorised(context);
                return;
            }

            try
            {
                await authenticator.Authenticate(username, password, GetRequiredPermission(context.HttpContext.Request), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error from {authenticator.GetType().Name}");
                Unauthorised(context);
                return;
            }

            await next();
        }
    }
}
