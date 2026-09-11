using Microsoft.Extensions.Primitives;

namespace Centrocdx.Services;

public class LegacyRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyDictionary<string, string> _redirects;

    public LegacyRedirectMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _redirects = configuration
            .GetSection("LegacyRedirects")
            .Get<Dictionary<string, string>>()
            ?.ToDictionary(
                x => NormalizePath(x.Key),
                x => x.Value,
                StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = NormalizePath(context.Request.Path.Value);
        if (_redirects.TryGetValue(requestPath, out var destination))
        {
            var query = context.Request.QueryString;
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = new StringValues(destination + query);
            return;
        }

        await _next(context);
    }

    private static string NormalizePath(string? path)
    {
        var normalized = "/" + (path ?? string.Empty).Trim().Trim('/');
        return normalized.Length > 1 ? normalized.ToLowerInvariant() : "/";
    }
}
