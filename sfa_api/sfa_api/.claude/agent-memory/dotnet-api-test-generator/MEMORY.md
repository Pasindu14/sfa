# SFA API Test Generator — Agent Memory

## Key File Paths
- Unit tests:        `sfa_api/sfa_api.UnitTests/Features/{Feature}/Services/{Feature}ServiceTests.cs`
- Integration tests: `sfa_api/sfa_api.IntegrationTests/Features/{Feature}/{Feature}ApiTests.cs`
- Integration infra: `sfa_api/sfa_api.IntegrationTests/Infrastructure/`
  - `SfaWebApplicationFactory.cs` — SQLite in-memory, shared connection, removes hosted services
  - `AuthHelper.cs` — generates signed JWTs; AdminToken (userId=100), SalesRepToken (userId=200), ManagerToken (userId=300)
  - `SfaApiCollection.cs` — xUnit collection fixture name

## Test Infrastructure Conventions
- Integration tests use `[Collection(SfaApiCollection.Name)]` and take `SfaWebApplicationFactory` via constructor
- `_client` is created once per test class from `factory.CreateClient()`
- Auth is set per-test via `_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token)`
- JSON deserialization uses `JsonSerializerOptions { PropertyNameCaseInsensitive = true }` and `ReadFromJsonAsync<JsonElement>`
- Always assert against `body.GetProperty("success")` and `body.GetProperty("data")` — never deserialize to typed DTOs
- Error assertions use `body.GetProperty("error").GetProperty("code")`

## Unit Test Conventions
- SUT instantiated in constructor: `new {Feature}Service(_repoMock.Object, NullLogger<{Feature}Service>.Instance)`
- `_repoMock = new Mock<I{Feature}Repository>()`
- Static factory helpers (`CreateFakeRegion`, `CreateValidCreateRequest`) keep test setup DRY
- Use `It.IsAny<CancellationToken>()` for all CT parameters in mock setups
- `Times.Once` verification after calls to SaveChangesAsync or mutating repo methods

## API Response Envelope
- Success: `{ "success": true, "data": {...}, "traceId": "...", "pagination": null }`
- Error:   `{ "success": false, "error": { "code": "...", "message": "...", "fields": {}, "traceId": "...", "timestamp": "..." } }`
- `data` for list-without-pagination endpoints is a JSON array (`JsonValueKind.Array`)

## Database Strategy
- Integration tests use **SQLite in-memory** (not EF InMemory, not Testcontainers)
- Shared single `SqliteConnection` opened in `SfaWebApplicationFactory` constructor, kept open for test lifetime
- No FK constraint enforcement issues observed in practice — SQLite foreign keys off by default
- Tests that need specific DB state must create records through the API (POST) — no direct DB seeding in tests

## Soft Delete
- Never assert hard deletion; verify `isActive` (or `isDeleted`) flag via GET after mutating calls
- Active/inactive filtering tests: create via POST, deactivate via POST /{id}/deactivate, then verify /active response

## Role-Based Access Patterns
- Read endpoints: `[Authorize]` — all three roles pass; assert 401 with no token
- Write endpoints: `[Authorize(Roles = "Admin")]` — assert 403 for SalesRep and Manager tokens

## See Also
- `patterns.md` — reserved for detailed pattern notes if needed
