using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.RateLimiting;

/// <summary>
/// Verifies rate-limit partitioning: the global limiter buckets authenticated callers by user id
/// and anonymous callers by IP, while the "auth" (login/refresh) policy stays strictly per-IP.
/// Every test uses its own client IPs / user ids so buckets never leak between tests.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class RateLimitingApiTests : IClassFixture<RateLimitingWebApplicationFactory>
{
    private const int GlobalLimit = RateLimitingWebApplicationFactory.GlobalPermitLimit;
    private const int AuthLimit = RateLimitingWebApplicationFactory.AuthPermitLimit;

    private readonly RateLimitingWebApplicationFactory _factory;

    public RateLimitingApiTests(RateLimitingWebApplicationFactory factory) => _factory = factory;

    private HttpRequestMessage Request(HttpMethod method, string url, string ip, string? bearer = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(RateLimitingWebApplicationFactory.ClientIpHeader, ip);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return request;
    }

    private async Task<HttpStatusCode> SendAsync(HttpClient client, HttpMethod method, string url,
        string ip, string? bearer = null, object? body = null)
    {
        using var request = Request(method, url, ip, bearer);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    [Fact]
    public async Task Global_TwoUsersBehindSameIp_DoNotShareABucket()
    {
        var client = _factory.CreateClient();
        const string sharedIp = "10.10.0.1";
        var userA = AuthHelper.GenerateToken(91001, "Admin");
        var userB = AuthHelper.GenerateToken(91002, "Admin");

        for (var i = 0; i < GlobalLimit; i++)
            (await SendAsync(client, HttpMethod.Get, "/api/v1/regions", sharedIp, userA))
                .Should().NotBe(HttpStatusCode.TooManyRequests, $"user A request {i + 1} is within the limit");

        (await SendAsync(client, HttpMethod.Get, "/api/v1/regions", sharedIp, userA))
            .Should().Be(HttpStatusCode.TooManyRequests, "user A exhausted their own bucket");

        (await SendAsync(client, HttpMethod.Get, "/api/v1/regions", sharedIp, userB))
            .Should().NotBe(HttpStatusCode.TooManyRequests, "user B has a separate bucket despite the same IP");

        // An anonymous caller on the same IP is also unaffected by user A's usage.
        (await SendAsync(client, HttpMethod.Get, "/health/live", sharedIp))
            .Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Global_Anonymous_IsLimitedPerIp()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < GlobalLimit; i++)
            (await SendAsync(client, HttpMethod.Get, "/health/live", "10.10.0.2"))
                .Should().NotBe(HttpStatusCode.TooManyRequests);

        (await SendAsync(client, HttpMethod.Get, "/health/live", "10.10.0.2"))
            .Should().Be(HttpStatusCode.TooManyRequests, "anonymous traffic is still limited per IP");

        (await SendAsync(client, HttpMethod.Get, "/health/live", "10.10.0.3"))
            .Should().NotBe(HttpStatusCode.TooManyRequests, "a different IP has its own bucket");
    }

    [Fact]
    public async Task Global_InvalidTokens_FallBackToIpBucket_CannotMintFreshPartitions()
    {
        var client = _factory.CreateClient();
        const string ip = "10.10.0.4";

        // Each request carries a different garbage token; none authenticate, so they all
        // share the IP bucket instead of each getting a fresh one.
        for (var i = 0; i < GlobalLimit; i++)
            (await SendAsync(client, HttpMethod.Get, "/health/live", ip, $"forged.token.{i}"))
                .Should().NotBe(HttpStatusCode.TooManyRequests);

        (await SendAsync(client, HttpMethod.Get, "/health/live", ip, "forged.token.last"))
            .Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Global_UnauthenticatedCallsToProtectedEndpoints_AreStillLimited()
    {
        var client = _factory.CreateClient();
        const string ip = "10.10.0.5";

        for (var i = 0; i < GlobalLimit; i++)
            (await SendAsync(client, HttpMethod.Get, "/api/v1/regions", ip))
                .Should().Be(HttpStatusCode.Unauthorized);

        (await SendAsync(client, HttpMethod.Get, "/api/v1/regions", ip))
            .Should().Be(HttpStatusCode.TooManyRequests, "the limiter runs before authorization");
    }

    [Fact]
    public async Task AuthPolicy_Login_StaysPerIp_EvenWithDistinctAuthenticatedUsers()
    {
        var client = _factory.CreateClient();
        const string ip = "10.10.0.6";
        var login = new { username = "no-such-user", password = "wrong-password" };

        // Distinct valid user tokens per attempt: the global limiter buckets each by user, so
        // only the per-IP "auth" policy can produce the 429 below.
        for (var i = 0; i < AuthLimit; i++)
            (await SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", ip,
                    AuthHelper.GenerateToken(92000 + i, "Admin"), login))
                .Should().NotBe(HttpStatusCode.TooManyRequests);

        (await SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", ip,
                AuthHelper.GenerateToken(92999, "Admin"), login))
            .Should().Be(HttpStatusCode.TooManyRequests, "login brute-force protection is keyed by IP");

        (await SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", "10.10.0.7", null, login))
            .Should().NotBe(HttpStatusCode.TooManyRequests, "another IP is unaffected");
    }
}
