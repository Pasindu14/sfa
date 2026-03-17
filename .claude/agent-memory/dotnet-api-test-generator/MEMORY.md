# DotNet API Test Generator — Persistent Memory

## Project Structure

- API project: `sfa_api/sfa_api/sfa_api.csproj`
- Unit tests:   `sfa_api/sfa_api.UnitTests/sfa_api.UnitTests.csproj`
- Integration tests: `sfa_api/sfa_api.IntegrationTests/sfa_api.IntegrationTests.csproj`
- Architecture: Vertical slice — Controller → Service → Repository (NO MediatR/CQRS)

## Test Infrastructure

- Integration factory: `sfa_api.IntegrationTests/Infrastructure/SfaWebApplicationFactory.cs`
  - Uses SQLite in-memory (shared open connection) — no Docker needed
  - Removes `IHostedService` registrations to avoid PostgreSQL-specific background services
- Auth helper: `sfa_api.IntegrationTests/Infrastructure/AuthHelper.cs`
  - `AdminToken` (userId=100), `SalesRepToken` (userId=200), `ManagerToken` (userId=300)
- Collection fixture: `sfa_api.IntegrationTests/Infrastructure/SfaApiCollection.cs`
  - All integration test classes MUST use `[Collection(SfaApiCollection.Name)]` NOT `IClassFixture<SfaWebApplicationFactory>`
  - CRITICAL: Using `IClassFixture` on multiple test classes causes `InvalidOperationException: The entry point exited without ever building an IHost` race condition

## Key Patterns

### Unit Test Class Structure
```csharp
// Constructor: new Mock<IRepo>(), new Service(mock.Object, NullLogger<Service>.Instance)
// Factory helper: private static Entity CreateFakeEntity(int id = 1)
// Request helper: private static CreateRequest CreateValidRequest()
// Helpers: SetupNoDuplicatesForCreate(), SetupNoDuplicatesForUpdate(excludeId)
```

### Integration Test Class Structure
```csharp
[Collection(SfaApiCollection.Name)]
public class XxxApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
    public XxxApiTests(SfaWebApplicationFactory factory) { _client = factory.CreateClient(); }
}
```

### FluentValidation in Unit Tests
- Package `FluentValidation.TestHelper` is available
- Use `_validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Field).WithErrorMessage("...")`
- Use `result.ShouldNotHaveAnyValidationErrors()` for baseline valid request test

### Exception Error Codes
| Exception                  | ErrorCode pattern         |
|----------------------------|---------------------------|
| `NotFoundException`        | `{ENTITY}_NOT_FOUND`      |
| `DuplicateResourceException` | `{FIELD}_DUPLICATE`     |
| `ValidationException`      | `VALIDATION_FAILED`       |

### Error Response Shape in Integration Tests
Error responses are wrapped under an `error` property, NOT at the root:
```csharp
// CORRECT
body.GetProperty("success").GetBoolean().Should().BeFalse();
body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();

// WRONG — "code" is NOT at the root of the response
body.GetProperty("code").GetString()  // KeyNotFoundException!
```

### ValidationException vs BusinessRuleException in Services
`ValidationException` maps to HTTP 400 — used when product IDs don't exist in bulk item operations.
`BusinessRuleException` maps to HTTP 422 — used for domain rule violations.
Check the service throw site to know which exception is used before writing the IT assertion.

## API Conventions

- All endpoints: `[Authorize(Roles = "Admin")]` on Distributor and User controllers
- Soft delete: repository `DeleteAsync` sets `IsDeleted = true` — never hard deletes
- No global query filter on `IsDeleted` — must add `.Where(x => !x.IsDeleted)` explicitly in repos
- Decimal columns: `decimal(5,2)` for TradeDiscount and Commission

## Verified Features With Tests

- **Users**: Service, Validators (Create/Update/ChangePassword/ResetPassword), API endpoints
- **Distributors**: Service, Validators (Create/Update), API endpoints (all 7 endpoints covered)
- **Regions**: Service, API endpoints (including search + pagination IT tests)
- **Areas**: API endpoints (including search + pagination IT tests)
- **PricingStructures**: Service (27 unit tests), API endpoints (26 integration tests incl. default-swap, bulk items, search)
- **SalesOrders**: Service (33 unit tests), Validators (Create 14 tests / Update 13 tests), API endpoints (21 integration tests)

## Multi-Repository Services
`PricingStructureService` takes both `IPricingStructureRepository` AND `IProductRepository`.
When testing `BulkReplaceItemsAsync`, mock `_productRepoMock.Setup(r => r.GetAllAsync(0, int.MaxValue, null, ct))`.
The returned tuple uses named field `.Products` — but service accesses it positionally (first element).

## Pagination + Search Patterns

### Service unit test — verify search is forwarded to repo
```csharp
[Fact]
public async Task GetAllAsync_WithSearch_PassesSearchToRepository()
{
    const string search = "test";
    _repoMock.Setup(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Enumerable.Empty<Region>(), 0));

    await _sut.GetAllAsync(page: 1, pageSize: 10, search: search);

    _repoMock.Verify(r => r.GetAllAsync(0, 10, search, It.IsAny<CancellationToken>()), Times.Once);
}
```
- Must match the exact `search` string in the setup — do NOT use `It.IsAny<string>()` or mock won't trigger
- UserService repo signature: `GetAllUsersAsync(skip, take, search?, role?, ct?)` — pass `null` for role

### Integration test — search param
```csharp
[Fact]
public async Task GetAllXxx_WithSearchParam_Returns200()
{
    // Seed an item whose name contains the search term first
    SetToken(AuthHelper.AdminToken);
    await _client.PostAsJsonAsync("/api/v1/xxx", payload);

    var response = await _client.GetAsync("/api/v1/xxx?search=SomeTerm");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
    body.GetProperty("success").GetBoolean().Should().BeTrue();

    // Conditionally assert returned items contain the search term
    // (use TryGetProperty since list shape may be data.items or data directly)
    if (body.GetProperty("data").TryGetProperty("regions", out var items) && items.ValueKind == JsonValueKind.Array)
        foreach (var item in items.EnumerateArray())
            item.GetProperty("name").GetString()!.ToLower().Should().Contain("someterm");
}
```
- The list wrapper key varies by feature: `data.regions`, `data.areas`, `data.distributors`, `data.users`

## PostgreSQL Sequence Workaround for SQLite Tests

`AppDbContext.OnModelCreating` calls `modelBuilder.HasSequence<long>("sales_order_number_seq")`, which
SQLite's `EnsureCreated()` cannot handle (`NotSupportedException`). Two infrastructure files resolve this:

1. `TestAppDbContext` — subclass of `AppDbContext` that strips the sequence after calling `base.OnModelCreating()`:
   ```csharp
   var seq = modelBuilder.Model.FindSequence("sales_order_number_seq");
   if (seq != null) modelBuilder.Model.RemoveSequence(seq.Name, seq.Schema);
   ```
   Used ONLY for `EnsureCreated()` in `SfaWebApplicationFactory` constructor.

2. `TestSalesOrderRepository` — delegating wrapper that replaces `GetNextOrderNumberAsync`
   (which calls PostgreSQL `nextval()`) with an `Interlocked.Increment` counter.
   Registered in `SfaWebApplicationFactory.ConfigureServices`:
   ```csharp
   services.RemoveAll<ISalesOrderRepository>();
   services.AddScoped<ISalesOrderRepository>(sp => {
       var db = sp.GetRequiredService<AppDbContext>();
       return new TestSalesOrderRepository(new SalesOrderRepository(db));
   });
   ```
   IMPORTANT: Use a **delegating wrapper** pattern — `GetNextOrderNumberAsync` is NOT virtual,
   so inheritance override (`:SalesOrderRepository`) is not possible.

## Distributor Seed Payload (Current Schema)

`ContactPerson` field has been removed from `CreateDistributorRequest`. `Alias` (int > 0) is now required.
Correct seed payload for integration tests:
```csharp
new { name, address, phone = $"077{alias:D7}", email, alias, tradeDiscount = 5.0, commission = 2.5 }
```
Use a static counter (`Interlocked.Increment`) to generate unique alias + phone per test run to avoid 409 conflicts.

## PricingStructureItem Renamed Fields

`PricingStructureItem` entity was renamed during development:
- `UnitPrice` → `DealerPackPrice`
- `PackPrice` → `DealerCasePrice`

If older test files reference the old names, they will fail to compile. Update all 5 occurrences in
`PricingStructureServiceTests.cs` when this is encountered.

## SalesOrder Multi-Status Workflow Tests

`SalesOrderService` takes both `ISalesOrderRepository` and `IUserRepository`.
Status transitions use role-based authorization:
- `SubmitForApproval`: SalesRep → Draft to PendingRepApproval
- `ApproveAsRep`: Manager → PendingRepApproval to PendingManagerApproval
- `ApproveAsManager`: Admin/Manager → PendingManagerApproval to PendingDistributorFinalization
- `FinalizeByDistributor`: Distributor (matching distributorId) → PendingDistributorFinalization to Finalized
- `Reject`: Any approver → any pending status → Rejected
- `Cancel`: Admin/Manager, only on Draft/Rejected orders → Cancelled

For `.Callback()` pattern when mocking sequential calls to same method:
```csharp
var callCount = 0;
_repoMock.Setup(r => r.GetByIdWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
         .ReturnsAsync(() => callCount++ == 0 ? order : updatedOrder);
```

## FluentValidation ChildRules — Item-Level Errors

When validating list items with `ChildRules`, use string-based path in `ShouldHaveValidationErrorFor`:
```csharp
result.ShouldHaveValidationErrorFor("Items[0].ProductId");
result.ShouldHaveValidationErrorFor("Items[0].Quantity");
```
NOT lambda form — lambda form only works for top-level properties.
Item request class name follows the pattern: `Create{Feature}ItemRequest`, `Update{Feature}ItemRequest`.

## Pre-existing Test Failures (Known)

`OutletValidatorTests` — 6 tests fail as of 2026-03-17. These are pre-existing failures unrelated to
SalesOrder work. Do not investigate unless explicitly asked.

## Test File Locations

```
sfa_api.UnitTests/
  Features/
    Users/
      Services/UserServiceTests.cs
      Validators/CreateUserValidatorTests.cs
      Validators/UpdateUserValidatorTests.cs
      Validators/ChangePasswordValidatorTests.cs
      Validators/ResetPasswordValidatorTests.cs
    Distributors/
      Services/DistributorServiceTests.cs
      Validators/CreateDistributorValidatorTests.cs
      Validators/UpdateDistributorValidatorTests.cs
    Regions/
      Services/RegionServiceTests.cs
    PricingStructures/
      Services/PricingStructureServiceTests.cs
    SalesOrders/
      Services/SalesOrderServiceTests.cs
      Validators/CreateSalesOrderValidatorTests.cs
      Validators/UpdateSalesOrderValidatorTests.cs

sfa_api.IntegrationTests/
  Infrastructure/
    SfaWebApplicationFactory.cs    ← includes TestSalesOrderRepository DI replacement
    TestAppDbContext.cs            ← suppresses PostgreSQL sequence for SQLite EnsureCreated
    TestSalesOrderRepository.cs    ← delegating wrapper with Interlocked counter
    AuthHelper.cs
    SfaApiCollection.cs           ← shared collection, add all new IT classes here
  Features/
    Users/UsersApiTests.cs
    Distributors/DistributorsApiTests.cs
    Regions/RegionsApiTests.cs
    Areas/AreasApiTests.cs
    Territories/TerritoriesApiTests.cs
    Divisions/DivisionsApiTests.cs
    PricingStructures/PricingStructuresApiTests.cs
    SalesOrders/SalesOrdersApiTests.cs
```
