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

## API Conventions

- All endpoints: `[Authorize(Roles = "Admin")]` on Distributor and User controllers
- Soft delete: repository `DeleteAsync` sets `IsDeleted = true` — never hard deletes
- No global query filter on `IsDeleted` — must add `.Where(x => !x.IsDeleted)` explicitly in repos
- Decimal columns: `decimal(5,2)` for TradeDiscount and Commission

## Verified Features With Tests

- **Users**: Service, Validators (Create/Update/ChangePassword/ResetPassword), API endpoints
- **Distributors**: Service, Validators (Create/Update), API endpoints (all 7 endpoints covered)

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

sfa_api.IntegrationTests/
  Infrastructure/
    SfaWebApplicationFactory.cs
    AuthHelper.cs
    SfaApiCollection.cs           ← shared collection, add all new IT classes here
  Features/
    Users/UsersApiTests.cs
    Distributors/DistributorsApiTests.cs
```
