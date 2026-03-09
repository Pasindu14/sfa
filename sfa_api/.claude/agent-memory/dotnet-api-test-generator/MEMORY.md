# Test Generator Memory — SFA API

## Key Paths
- Integration tests: `sfa_api/sfa_api.IntegrationTests/`
- Unit tests: `sfa_api/sfa_api.UnitTests/`
- Features: `sfa_api/sfa_api/Features/{Feature}/`

## Test Infrastructure
- `SfaWebApplicationFactory` — `WebApplicationFactory<Program>` using SQLite in-memory (shared connection kept open for schema lifetime). See `Infrastructure/SfaWebApplicationFactory.cs`.
- `SfaApiCollection` — `ICollectionFixture<SfaWebApplicationFactory>` with `Name = "SFA API"`. All integration test classes use `[Collection(SfaApiCollection.Name)]`.
- `AuthHelper` — static helper generating JWT tokens. Use `AdminToken`, `SalesRepToken`, `ManagerToken`. Key = `a8F#9kLm2PqR7tVxY4zW!6nB@3cD$5Gh`, Issuer = `SFA.API`, Audience = `SFA.Clients`.

## Error Codes (from SFAException.cs)
- `NotFoundException("Entity", id)` → code = `{ENTITY}_NOT_FOUND` (e.g. `REGION_NOT_FOUND`)
- `DuplicateResourceException("Name")` → code = `NAME_DUPLICATE`
- `DuplicateResourceException("Email")` → code = `EMAIL_DUPLICATE`
- `ValidationException(fields)` → code = `VALIDATION_FAILED`

## API Response Envelope Shape
Success: `{ success, data, pagination, traceId }`
Error (non-2xx): `{ success: false, error: { code, message, detail, fields, traceId, timestamp } }`
Assert error field as `body.GetProperty("error").GetProperty("code")` — NOT `body.GetProperty("error").GetProperty("code")` on top-level.

## FluentValidation.TestHelper
Available transitively via `FluentValidation` package — no need to add an explicit package reference to `sfa_api.UnitTests.csproj`.

## Unit Test Patterns
- Use `NullLogger<TService>.Instance` from `Microsoft.Extensions.Logging.Abstractions`.
- Mock repo with `Mock<IRepository>` and `Moq`.
- Capture entity mutations via `.Callback<T, CancellationToken>((entity, _) => captured = entity)`.
- Verify `SaveChangesAsync` called with `Times.Once`.
- Audit field timing: use `.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2))`.

## Integration Test Patterns
- One test file per feature: `Features/{Feature}/{Feature}ApiTests.cs`.
- Use `ReadFromJsonAsync<JsonElement>` + `JsonSerializerOptions { PropertyNameCaseInsensitive = true }`.
- Unique names per create test to avoid SQLite unique constraint conflicts across test runs (tests share DB state in one process).
- 204 NoContent for activate/deactivate — verify IsActive by GETting the resource after.
- No hard-delete assertions — check `isActive` flag changes only.

## Strategy Decision Rules
- Thin CRUD services (no branching) → Integration only.
- Services with duplicate checks, NotFoundException guards, flag mutations, pagination math → Unit tests.
- Validators → Unit tests (FluentValidation.TestHelper, never integration).
- Always both for features like Regions that have service logic AND HTTP contract concerns.

## Confirmed Patterns (multi-feature)
- Distributors, Users, Regions all follow same vertical slice layout.
- All validators share same rules: `NotEmpty` + `MaximumLength`, same error messages.
- `ExistsByNameAsync` has two overloads: `(name, ct)` for create, `(name, excludeId, ct)` for update.
