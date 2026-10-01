using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Middleware;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;

namespace sfa_api.UnitTests.Common;

public class IdempotencyMiddlewareTests
{
    private const string ScopedKey = "POST:/api/v1/stock-transfers:7:key-1";

    /// <summary>Records stores and, like the real service, honours cancellation — a cancelled token means nothing is persisted.</summary>
    private sealed class FakeIdempotencyService : IIdempotencyService
    {
        public Dictionary<string, IdempotencyResult> Stored { get; } = new();
        public List<CancellationToken> StoreTokens { get; } = [];

        public Task<IdempotencyResult?> GetAsync(string key, CancellationToken ct = default)
            => Task.FromResult(Stored.TryGetValue(key, out var v) ? v : null);

        public Task StoreAsync(string key, int statusCode, string responseJson, CancellationToken ct = default)
        {
            StoreTokens.Add(ct);
            ct.ThrowIfCancellationRequested();
            Stored[key] = new IdempotencyResult(statusCode, responseJson);
            return Task.CompletedTask;
        }
    }

    private readonly FakeIdempotencyService _idempotency = new();

    private (IdempotencyMiddleware Sut, DefaultHttpContext Context, MemoryStream Wire) Build(
        RequestDelegate handler, CancellationToken requestAborted = default)
    {
        var lockMock = new Mock<IDistributedLockService>();
        lockMock.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<IAsyncDisposable>().Object);

        var services = new ServiceCollection()
            .AddSingleton<IIdempotencyService>(_idempotency)
            .AddSingleton(lockMock.Object)
            .BuildServiceProvider();

        var wire = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = services, RequestAborted = requestAborted };
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/stock-transfers";
        context.Request.Headers["X-Idempotency-Key"] = "key-1";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "7")], "test"));
        context.Response.Body = wire;

        return (new IdempotencyMiddleware(handler, NullLogger<IdempotencyMiddleware>.Instance), context, wire);
    }

    private static async Task Created(HttpContext ctx, string body)
    {
        ctx.Response.StatusCode = 201;
        await ctx.Response.WriteAsync(body);
    }

    [Fact]
    public async Task Invoke_SuccessfulHandler_StoresTheResponseUnderTheScopedKey()
    {
        var (sut, ctx, wire) = Build(c => Created(c, "{\"id\":5}"));

        await sut.InvokeAsync(ctx);

        _idempotency.Stored.Should().ContainKey(ScopedKey);
        _idempotency.Stored[ScopedKey].Should().Be(new IdempotencyResult(201, "{\"id\":5}"));
        wire.ToArray().Should().Equal("{\"id\":5}"u8.ToArray());
    }

    [Fact]
    public async Task Invoke_ClientDisconnectsWhileHandlerRuns_StillStoresTheKey()
    {
        // The handler has committed; the client then drops (RequestAborted fires). Without a
        // non-cancellable store the retry would find no key and re-execute the whole operation.
        using var cts = new CancellationTokenSource();
        var (sut, ctx, _) = Build(async c =>
        {
            await Created(c, "{\"id\":9}");
            cts.Cancel();
        }, cts.Token);

        try { await sut.InvokeAsync(ctx); }
        catch (OperationCanceledException) { /* writing to the dropped client may throw; the store has already happened */ }

        _idempotency.Stored.Should().ContainKey(ScopedKey, "the store must not be tied to RequestAborted");
        _idempotency.Stored[ScopedKey].ResponseJson.Should().Be("{\"id\":9}");
        _idempotency.StoreTokens.Should().ContainSingle().Which.CanBeCanceled.Should().BeFalse();
    }

    [Fact]
    public async Task Invoke_RequestAlreadyAbortedWhenHandlerCompletes_StillStoresTheKey()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var (sut, ctx, _) = Build(c => Created(c, "{\"id\":3}"), cts.Token);

        try { await sut.InvokeAsync(ctx); }
        catch (OperationCanceledException) { }

        _idempotency.Stored.Should().ContainKey(ScopedKey);
    }

    [Fact]
    public async Task Invoke_NonSuccessResponse_IsNotStored()
    {
        var (sut, ctx, _) = Build(async c =>
        {
            c.Response.StatusCode = 422;
            await c.Response.WriteAsync("{\"success\":false}");
        });

        await sut.InvokeAsync(ctx);

        _idempotency.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task Invoke_KeyAlreadyCached_ReplaysWithoutCallingTheHandler()
    {
        _idempotency.Stored[ScopedKey] = new IdempotencyResult(201, "{\"id\":1}");
        var handlerCalled = false;
        var (sut, ctx, wire) = Build(_ => { handlerCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(ctx);

        handlerCalled.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(201);
        wire.ToArray().Should().Equal("{\"id\":1}"u8.ToArray());
    }
}
