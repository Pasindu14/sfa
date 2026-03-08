namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Shared collection fixture that ensures a single SfaWebApplicationFactory instance
/// is used across all integration test classes. This prevents the WebApplicationFactory
/// entry-point race condition that occurs when multiple IClassFixture instances try to
/// boot the same Program in the same test process.
/// </summary>
[CollectionDefinition(Name)]
public class SfaApiCollection : ICollectionFixture<SfaWebApplicationFactory>
{
    public const string Name = "SFA API";
}
