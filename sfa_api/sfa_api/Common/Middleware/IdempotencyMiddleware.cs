using System.Text.Json;
using sfa_api.Common.Errors;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;

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

        async Task WriteCachedAsync(IdempotencyResult hit)
        {
            context.Response.StatusCode = hit.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(hit.ResponseJson, context.RequestAborted);
        }

        // Check for a cached response
        var cached = await idempotencyService.GetAsync(scopedKey, context.RequestAborted);
        if (cached is not null)
        {
            logger.LogInformation(
                "Returning cached idempotent response for key {Key} (userId={UserId})",
                rawKey, userId);
            await WriteCachedAsync(cached);
            return;
        }

        // In-flight guard — claim the key for the duration of processing so two identical requests
        // that race in before the first has cached its response cannot BOTH execute the controller.
        // Reuses the same distributed-lock primitive the domain services use; the test harness
        // swaps in a no-op that always grants, so this is transparent under SQLite.
        var lockService = context.RequestServices.GetRequiredService<IDistributedLockService>();
        await using var inflight = await lockService.AcquireAsync($"idempotency:{scopedKey}", context.RequestAborted);
        if (inflight is null)
        {
            // A concurrent request with this key is already mid-flight. It may have just finished —
            // re-check the cache once; otherwise reject so the client retries and then gets the
            // cached result (mobile already handles CONCURRENCY_CONFLICT for billing creates).
            var raced = await idempotencyService.GetAsync(scopedKey, context.RequestAborted);
            if (raced is not null)
            {
                logger.LogInformation(
                    "Returning cached idempotent response for key {Key} after in-flight race (userId={UserId})",
                    rawKey, userId);
                await WriteCachedAsync(raced);
                return;
            }

            logger.LogWarning(
                "Duplicate in-flight request for idempotency key {Key} (userId={UserId}) — rejecting as conflict",
                rawKey, userId);
            throw new ConcurrencyConflictException(
                new { message = "A request with this idempotency key is already being processed. Retry shortly." });
        }

        // We hold the in-flight lock. Double-check the cache in case a prior holder completed
        // between our first read and acquiring the lock.
        cached = await idempotencyService.GetAsync(scopedKey, context.RequestAborted);
        if (cached is not null)
        {
            logger.LogInformation(
                "Returning cached idempotent response for key {Key} after acquiring lock (userId={UserId})",
                rawKey, userId);
            await WriteCachedAsync(cached);
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
