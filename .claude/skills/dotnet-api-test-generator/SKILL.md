---
name: dotnet-api-test-generator
description: Generates complete, production-ready xUnit tests for the SFA .NET API project. Use this skill whenever the user asks to generate tests, write tests, add test coverage, or create unit/integration tests for any feature, service, controller, validator, or endpoint in the SFA API. Trigger on phrases like "generate tests for X", "write tests for X", "add test coverage for X", "create unit tests for X", "create integration tests for X", or when the user has just finished building a feature and wants to validate it. This skill decides the correct test type (unit vs integration vs both), produces all test files following the exact xUnit+Moq+FluentAssertions patterns established in the codebase, runs dotnet test, and fixes failures automatically until a clean test run is achieved.
---

# .NET API Test Generator

Generate complete, production-ready tests for the SFA API following the exact xUnit + Moq + FluentAssertions patterns established in the project.

## Test Projects

| Project | Path | Purpose |
|---------|------|---------|
| Unit tests | `sfa_api/sfa_api.UnitTests/` | Service and validator logic in isolation |
| Integration tests | `sfa_api/sfa_api.IntegrationTests/` | Full HTTP pipeline via WebApplicationFactory |

## Step 1: Decide What to Generate

Read the feature the user wants to test. Choose based on where the logic lives:

| Feature type | Generate |
|---|---|
| Service with business rules (duplicate checks, state transitions, auth checks) | **Unit tests** for the service |
| Validator with non-trivial rules (formats, ranges, conditionals) | **Unit tests** for the validator |
| Simple CRUD endpoint with thin/no business logic | **Integration tests** only |
| Both business logic AND endpoints | **Both** |

Read the relevant source files before writing any tests — check `sfa_api/sfa_api/Features/{Feature}/` to understand what methods and rules exist.

## Step 2: Unit Tests

Place in: `sfa_api/sfa_api.UnitTests/Features/{Feature}/Services/{Feature}ServiceTests.cs`
Validator tests: `sfa_api/sfa_api.UnitTests/Features/{Feature}/Validators/{Validator}Tests.cs`

### Service test anatomy

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.{Feature}.DTOs;
using sfa_api.Features.{Feature}.Entities;
using sfa_api.Features.{Feature}.Repositories;
using sfa_api.Features.{Feature}.Requests;
using sfa_api.Features.{Feature}.Services;

namespace sfa_api.UnitTests.Features.{Feature}.Services;

public class {Feature}ServiceTests
{
    private readonly Mock<I{Feature}Repository> _repoMock;
    private readonly {Feature}Service _sut;

    public {Feature}ServiceTests()
    {
        _repoMock = new Mock<I{Feature}Repository>();
        _sut = new {Feature}Service(_repoMock.Object, NullLogger<{Feature}Service>.Instance);
    }

    // Factory helpers — create realistic fakes, not empty shells
    private static {Feature} CreateFake{Feature}(int id = 1) => new()
    {
        Id = id,
        // fill all required fields with valid data
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    private static Create{Feature}Request CreateValidRequest() => new()
    {
        // fill with valid data
    };
```

### Test method naming

```
{MethodName}_{Scenario}_{ExpectedResult}

GetByIdAsync_ExistingEntity_ReturnsDto
GetByIdAsync_NonExistentId_ThrowsNotFoundException
CreateAsync_DuplicateName_ThrowsDuplicateResourceException
CreateAsync_ValidRequest_SetsAuditFields
CreateAsync_ValidRequest_CallsSaveChanges
```

Group tests by method with separator comments:

```csharp
// ─────────────────────────────────────────────────
// GetByIdAsync
// ─────────────────────────────────────────────────

[Fact]
public async Task GetByIdAsync_ExistingEntity_ReturnsDto() { ... }

[Fact]
public async Task GetByIdAsync_NonExistentId_ThrowsNotFoundException() { ... }
```

### What to test in each service method

**GetById:**
- Existing entity → returns correct DTO
- Non-existent id → throws `NotFoundException`

**GetAll:**
- Returns paginated list with correct TotalCount, Page, PageSize
- Page 2 → verifies correct skip (skip = (page-1) * pageSize)
- Empty result → returns empty list

**Create:**
- Valid request → returns DTO with correct mapped fields
- Valid request → sets `CreatedBy`, `UpdatedBy`, `CreatedAt` audit fields
- Valid request → calls `SaveChangesAsync` once
- Duplicate unique field → throws `DuplicateResourceException`
- Invalid enum value → throws `ValidationException` with field key
- (If password) → hashes the password (verify with BCrypt.Verify)
- (If hierarchical entity — Territory, Division) → parent not found → throws `NotFoundException` with parent error code

**Update:**
- Valid request → returns updated DTO
- Non-existent id → throws `NotFoundException`
- Duplicate unique field (excluding self) → throws `DuplicateResourceException`
- Sets `UpdatedBy`, `UpdatedAt` audit fields
- Invalid enum → throws `ValidationException` with field key
- (If hierarchical entity — Territory, Division) → parent not found → throws `NotFoundException` with parent error code

**Delete / Deactivate (soft — sets `IsActive = false`, never removes):**
- Existing entity → calls repo Deactivate/Delete and SaveChanges once each
- Non-existent id → throws `NotFoundException`

**Custom actions (activate/deactivate, status changes):**
- Existing entity → flips the flag, calls Update and SaveChanges
- Non-existent id → throws `NotFoundException`
- Sets `UpdatedBy`, `UpdatedAt` audit fields

### Capturing what's passed to the repository

When you need to assert on what was passed into a repo method, use Moq's `.Callback<>`:

```csharp
{Feature}? captured = null;
_repoMock.Setup(r => r.CreateAsync(It.IsAny<{Feature}>(), It.IsAny<CancellationToken>()))
         .Callback<{Feature}, CancellationToken>((e, _) => captured = e)
         .Returns(Task.CompletedTask);

await _sut.CreateAsync(request, callerId: 5);

captured!.CreatedBy.Should().Be(5);
captured.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
```

### Private helper methods

Extract repeated mock setups to private helpers so tests stay readable:

```csharp
private void SetupNoDuplicates()
{
    _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
}

// For update overloads that exclude current id
private void SetupNoDuplicatesForUpdate(int excludeId)
{
    _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), excludeId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
}
```

### Validator tests

```csharp
public class Create{Feature}ValidatorTests
{
    private readonly Create{Feature}Validator _validator = new();

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        var request = new Create{Feature}Request { /* valid data */ };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyName_FailsWithNameError(string? name)
    {
        var request = new Create{Feature}Request { Name = name! };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
    // Test: each required field is empty → validation fails
    // Test: field too long → validation fails
    // Test: invalid format/range → validation fails
    // Test: valid boundary values → validation passes
}
```

### Hierarchical Entity Tests (Territory, Division)

The geographic hierarchy (Region → Area → Territory → Division) uses **denormalized ancestor IDs**.
When a service creates or updates a hierarchical entity, it calls `GetAreaWithRegionAsync` (or equivalent)
to fetch the full parent object — **not** `AreaExistsAsync`. This means mocks must return the parent entity, not a bool.

**Wrong (won't compile or will throw NotFoundException):**
```csharp
_repoMock.Setup(r => r.AreaExistsAsync(request.AreaId, It.IsAny<CancellationToken>()))
         .ReturnsAsync(true);
```

**Correct:**
```csharp
_repoMock.Setup(r => r.GetAreaWithRegionAsync(request.AreaId, It.IsAny<CancellationToken>()))
         .ReturnsAsync(CreateFakeArea(request.AreaId));
```

**Area not found** → return `null`:
```csharp
_repoMock.Setup(r => r.GetAreaWithRegionAsync(request.AreaId, It.IsAny<CancellationToken>()))
         .ReturnsAsync((Area?)null);
```

The `CreateFakeArea` helper must include `RegionId` because the service copies it to the child entity:
```csharp
private static Area CreateFakeArea(int id = 1, int regionId = 10) => new()
{
    Id = id,
    Name = "Test Area",
    RegionId = regionId,
    IsActive = true,
    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
};
```

Similarly, for Division, use `GetTerritoryWithAncestorsAsync` (or equivalent) instead of `TerritoryExistsAsync`.

Apply this to **every** test that exercises Create or Update on a hierarchical entity — including the shared
`SetupSuccessfulCreate` and `SetupSuccessfulUpdate` helper methods.

## Step 3: Integration Tests

Place in: `sfa_api/sfa_api.IntegrationTests/Features/{Feature}/{Feature}sApiTests.cs`

### Integration test anatomy

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.{Feature};

[Collection(SfaApiCollection.Name)]
public class {Feature}sApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public {Feature}sApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Factory helper — unique values per test (avoid collisions in shared in-memory DB)
    private object CreatePayload(
        string name = "Test {Feature}",
        /* other fields */) => new { name /* ... */ };
```

### What to test in integration tests

**Authentication (401):**

```csharp
[Fact]
public async Task GetAll_NoToken_Returns401()
{
    _client.DefaultRequestHeaders.Authorization = null;
    var response = await _client.GetAsync("/api/v1/{entities}");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

**Authorization (403):**
- Test each role that should be blocked from each endpoint

**Happy paths:**
- GET all → 200 with envelope (`success: true`, `data` not null)
- POST → 201, then GET by returned id → 200 with correct fields
- PUT → 200 with updated fields
- DELETE → 204
- Custom actions (activate, deactivate) → correct status code, GET confirms state change

**Validation (400):**

```csharp
[Fact]
public async Task Create_InvalidData_Returns400WithFieldErrors()
{
    SetToken(AuthHelper.AdminToken);
    var payload = new { name = "" /*, other invalid fields */ };

    var response = await _client.PostAsJsonAsync("/api/v1/{entities}", payload);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
    body.GetProperty("success").GetBoolean().Should().BeFalse();
    body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    body.GetProperty("error").GetProperty("fields").ValueKind.Should().Be(JsonValueKind.Object);
}
```

**Not Found (404):**

```csharp
var response = await _client.GetAsync("/api/v1/{entities}/99999");
response.StatusCode.Should().Be(HttpStatusCode.NotFound);
var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
```

**Duplicate (409):**
- Create entity, then POST again with same unique field → 409

**Response envelope structure:**

```csharp
[Fact]
public async Task SuccessResponse_ContainsExpectedEnvelopeFields()
{
    SetToken(AuthHelper.AdminToken);
    var response = await _client.GetAsync("/api/v1/{entities}");
    var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
    body.TryGetProperty("success", out _).Should().BeTrue();
    body.TryGetProperty("data", out _).Should().BeTrue();
    body.TryGetProperty("traceId", out _).Should().BeTrue();
}
```

### Available tokens

```csharp
AuthHelper.AdminToken    // userId=100, role=Admin
AuthHelper.ManagerToken  // userId=300, role=Manager
AuthHelper.SalesRepToken // userId=200, role=SalesRep
```

The shared `SfaWebApplicationFactory` uses **SQLite in-memory** (not PostgreSQL) — no Docker required. Tests that create data persist within the shared factory session, so use unique field values in each test to avoid collisions.

## Step 4: Run & Fix

```bash
cd sfa_api
dotnet test sfa_api.UnitTests --no-build 2>&1
dotnet test sfa_api.IntegrationTests --no-build 2>&1
```

If tests fail:
- Read error messages carefully
- Common issues: wrong method names on the mock (check the interface), missing `using` statements, mismatched async signatures, SQLite compatibility (SQLite doesn't support all EF features — if a query fails in integration tests, simplify the EF query)
- Fix, then re-run
- Repeat up to 5 times or until clean

Report the final test counts: X passed, 0 failed, 0 skipped.

## Key Patterns at a Glance

| Concern | Pattern |
|---|---|
| SUT naming | `_sut` |
| Mock naming | `_repoMock` |
| Logger | `NullLogger<TService>.Instance` |
| Async assertions | `await act.Should().ThrowAsync<TException>()` |
| Time assertions | `.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2))` |
| Verify called once | `_repoMock.Verify(r => r.SaveChangesAsync(...), Times.Once)` |
| Integration token | `AuthHelper.AdminToken` / `ManagerToken` / `SalesRepToken` |
| Read response | `ReadFromJsonAsync<JsonElement>(_jsonOpts)` |
| Envelope check | `body.GetProperty("success").GetBoolean().Should().BeTrue()` |
