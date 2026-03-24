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
- Distributors, Users, Regions, Areas all follow same vertical slice layout.
- All validators share same rules: `NotEmpty` + `MaximumLength`, same error messages.
- `ExistsByNameAsync` has two overloads: `(name, ct)` for create, `(name, excludeId, ct)` for update.
- Areas adds `RegionId` validator rule (`GreaterThan(0)`) and `RegionExistsAsync` guard in service — unique to this feature.
- Areas `ExistsByNameAsync` scoped to `(name, regionId, ct)` and `(name, regionId, excludeId, ct)` — region-scoped uniqueness.

## Known Infrastructure Limitation
- Running multiple integration test classes together in one `dotnet test` invocation hits the in-process rate limiter (429s).
- Each class MUST be run in isolation with `--filter "FullyQualifiedName~{Feature}"`. This is a pre-existing repo issue — not caused by test code.
- When paginated list tests check `ids.Should().Contain(x)`, always use `?pageSize=1000` to prevent false negatives when other tests have inserted rows that push the seed data off page 1.

## PostgreSQL-only Features That Need Stubs in SQLite Integration Tests

### Sequences (nextval)
Three repositories call PostgreSQL `nextval()` sequences — all must be wrapped:
- `ISalesInvoiceRepository` → `TestSalesInvoiceRepository` stubs `GetNextBatchNumberAsync`
- `IGrnRepository` → `TestGrnRepository` stubs `GetNextGrnNumberAsync`
- `IPurchaseOrderRepository` → `TestPurchaseOrderRepository` stubs `GetNextOrderNumberAsync`
Each stub uses `Interlocked.Increment(ref _counter)`. Remove sequences in `TestAppDbContext.OnModelCreating`.

### Advisory Locks (pg_try_advisory_lock)
`PostgresAdvisoryLockService.AcquireAsync` calls `pg_try_advisory_lock()` — fails on SQLite.
Replaced in factory with `NoOpDistributedLockService` that always grants the lock immediately.

### SalesInvoiceImportBatch.ImportedBy FK
This column has a `DeleteBehavior.Restrict` FK to Users.
The DataSeeder only runs in Development/Staging — NOT in the "Testing" environment.
The factory constructor seeds admin user ID=1 manually so import tests can use `GenerateToken(1, "Admin")`.

### Distributor Creation Now Requires TerritoryId
`CreateDistributorValidator` has `.NotNull()` on `TerritoryId`. Pre-existing integration tests that seed
distributors without a territory now fail. Seed the full geographic hierarchy (Region→Area→Territory) before
seeding a distributor. This is a pre-existing failure — it's not caused by test code.

## Test Suite Status (2026-03-24)

After adding SalesInvoices + GRNs tests:
- **Unit:** 60 new tests (SalesInvoiceServiceTests, GrnServiceTests, validators) — all passing
- **Integration:** 24 new tests (SalesInvoicesApiTests, GrnsApiTests) — all passing
- **Pre-existing failures (not caused by our code):**
  - 46 PurchaseOrderServiceTests unit tests — SQLite EnsureCreated fails due to new sequences in AppDbContext
  - ~50 integration test failures — SQLite LINQ incompatibilities + missing TerritoryId in old seed helpers

## SalesInvoice List Endpoint — Data Shape
`GET /api/v1/sales-invoices` returns `data` as a plain JSON **array** (not `data.items`).
Assert as: `body.GetProperty("data").ValueKind == JsonValueKind.Array` then `data[0].GetProperty("id")`.

## GRN ConfirmAsync — Mock Order
`ConfirmAsync` calls `GetGrnWithItemsAsync` twice: once to load the GRN, and once after commit to reload.
Use a call-count-based `ReturnsAsync(() => ...)` factory when both calls need different return values.

## Object Mutation in Moq Callbacks (Critical)
When a service adds an entity to a collection then mutates it, Moq's callback captures the **object reference**.
By the time `Verify` runs, the property may have been changed by the service. Do NOT verify `QuantityOnHand == 0`
after `AddStockAsync` — verify the `StockTransaction.QuantityBefore == 0` instead (it captures the value at
the time the transaction was built, not the current stock object state).

## Areas Feature Notes (confirmed)
- `AreaRepository.GetAllAsync` signature: `(int skip, int take, int? regionId, bool? isActive, CancellationToken)` — both filters are optional.
- `AreaListDto.Areas` property (not `Items`) — assert as `body.GetProperty("data").GetProperty("areas")`.
- `GET /api/v1/areas/active` returns a flat array (no pagination wrapper) — assert `data.ValueKind == JsonValueKind.Array`.
- `GET /api/v1/areas` returns `data.areas` (nested in `AreaListDto`) — assert `data.GetProperty("areas")`.
- Area creation requires a pre-existing Region — always seed a Region first via `POST /api/v1/regions` in integration tests.
