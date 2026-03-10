using System.Text.Json;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Common.Middleware;

public class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    private const string HeaderName = "X-Idempotency-Key";

    // Only cache responses for state-mutating methods
    private static readonly HashSet<string> IdempotentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "DELETE" };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IdempotentMethods.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        var rawKey = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            await next(context);
            return;
        }

        // Scope key to the authenticated user so keys cannot leak across users
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("sub")?.Value
                     ?? "anonymous";

        var scopedKey = $"{userId}:{rawKey}";

        var idempotencyService = context.RequestServices.GetRequiredService<IIdempotencyService>();

        // Check for a cached response
        var cached = await idempotencyService.GetAsync(scopedKey, context.RequestAborted);
        if (cached is not null)
        {
            logger.LogInformation(
                "Returning cached idempotent response for key {Key} (userId={UserId})",
                rawKey, userId);

            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cached.ResponseJson, context.RequestAborted);
            return;
        }

        // Buffer the response so we can capture it before sending to client
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        var statusCode = context.Response.StatusCode;
        buffer.Position = 0;
        var responseJson = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);

        // Only cache successful responses (2xx) to avoid caching transient errors
        if (statusCode >= 200 && statusCode < 300)
        {
            await idempotencyService.StoreAsync(scopedKey, statusCode, responseJson, context.RequestAborted);
        }

        // Write the buffered response to the real stream
        buffer.Position = 0;
        context.Response.ContentLength = buffer.Length;
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
    }
}
