using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

#if !NETSTANDARD2_0
using Microsoft.AspNetCore.Http.Features;
#endif

namespace Sentry.AspNetCore.Extensions
{
    internal static class HttpContextExtensions
    {
        public static string? TryGetRouteTemplate(this HttpContext context)
        {
#if !NETSTANDARD2_0 // endpoint routing is only supported after ASP.NET Core 3.0
            // Requires .UseRouting()/.UseEndpoints()
            var endpoint = context.Features.Get<IEndpointFeature?>()?.Endpoint as RouteEndpoint;
            var routePattern = endpoint?.RoutePattern.RawText;

            if (!string.IsNullOrWhiteSpace(routePattern))
            {
                return routePattern;
            }
#endif

            // Fallback for legacy .UseMvc().
            // Uses context.Features.Get<IRoutingFeature?>() under the hood and CAN be null,
            // despite the annotations claiming otherwise.
            var routeData = context.GetRouteData();

            var controller = routeData?.Values["controller"]?.ToString();
            var action = routeData?.Values["action"]?.ToString();
            var area = routeData?.Values["area"]?.ToString();

            if (!string.IsNullOrWhiteSpace(action))
            {
                return !string.IsNullOrWhiteSpace(area)
                    ? $"{area}.{controller}.{action}"
                    : $"{controller}.{action}";
            }

            // If the handler doesn't use routing (i.e. it checks `context.Request.Path` directly),
            // then there is no way for us to extract anything that resembles a route template.
            return null;
        }

        /// <summary>
        /// Get the route template of the current path, or the path string itself if there is no route template
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetTransactionName(this HttpContext context)
        {
            // Try to get the route template or fallback to the request path

            var method = context.Request.Method.ToUpperInvariant();
            var route = context.TryGetRouteTemplate() ?? "Unknown Route";

            return $"{method} {route}";
        }
    }
}
