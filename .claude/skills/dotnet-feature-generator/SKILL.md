---
name: dotnet-feature-generator
description: Generates complete, production-ready ASP.NET Core features for the SFA API project following the exact patterns from Features/Users/. Use this skill whenever the user asks to add a new feature, module, entity, or CRUD endpoint to the .NET API. Trigger on phrases like "add a feature", "create a new module", "generate a .NET feature", "add CRUD for X", "create X entity", "implement X feature in the API", or when describing a new domain object that needs API endpoints. This skill produces all files, DI registrations, DbContext changes, validates with dotnet build, and fixes errors automatically until a clean build is achieved.
---

# .NET API Feature Generator

Generate complete, production-ready ASP.NET Core features for the SFA API project following the exact architectural patterns established in the Users feature.

## What This Skill Does

This skill automates the creation of a full-stack API feature with:
- Entity models with audit fields and soft delete
- Repository pattern (interface + implementation)
- Service layer with business logic
- Controller with CRUD endpoints
- DTOs for responses
- Request models for inputs
- FluentValidation validators
- Dependency injection registration in Program.cs
- DbContext registration
- EF Core migration command generation
- Automatic build validation with iterative error fixing

## Workflow

### Phase 1: Discovery & Planning

Before generating any code, understand what the user wants to build:

1. **Identify the feature name** - What entity/domain are we building? (e.g., "Product", "Customer", "Order")
2. **Gather requirements** - Ask the user:
   - What properties should the entity have? (name, type, nullable, default values)
   - Are there any enums needed? (like UserRole for Users)
   - What validation rules apply? (required fields, string lengths, formats, ranges)
   - What CRUD operations are needed? (Create, Read, ReadAll, Update, Delete)
   - Any special endpoints? (activate/deactivate, status changes, etc.)
   - Authorization rules? (Admin-only? Role-specific? Self-update allowed?)
3. **Study the Users feature** - Read the following files to understand current patterns:
   - `sfa_api/sfa_api/Features/Users/Entities/User.cs` - Entity pattern
   - `sfa_api/sfa_api/Features/Users/Controllers/UsersController.cs` - Controller pattern
   - `sfa_api/sfa_api/Features/Users/Services/UserService.cs` - Service pattern
   - `sfa_api/sfa_api/Features/Users/Repositories/UserRepository.cs` - Repository pattern
   - `sfa_api/sfa_api/Features/Users/DTOs/UserDto.cs` - DTO pattern
   - `sfa_api/sfa_api/Features/Users/Requests/CreateUserRequest.cs` - Request pattern
   - `sfa_api/sfa_api/Features/Users/Validators/CreateUserValidator.cs` - Validator pattern

Present your understanding to the user and get confirmation before proceeding.

### Phase 2: Code Generation

Generate files in this exact order, following the established patterns:

#### 1. Create Directory Structure

```bash
mkdir -p sfa_api/sfa_api/Features/{FeatureName}/Entities
mkdir -p sfa_api/sfa_api/Features/{FeatureName}/DTOs
mkdir -p sfa_api/sfa_api/Features/{FeatureName}/Requests
mkdir -p sfa_api/sfa_api/Features/{FeatureName}/Validators
mkdir -p sfa_api/sfa_api/Features/{FeatureName}/Repositories
mkdir -p sfa_api/sfa_api/Features/{FeatureName}/Services
mkdir -p sfa_api/sfa_api/Features/{FeatureName}/Controllers
```

#### 2. Generate Entity (`Entities/{FeatureName}.cs`)

**Pattern to follow:**
- Use `int Id` as primary key with auto-increment
- Include audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`, `IsActive`
- Set proper defaults: `= string.Empty` for strings, `= DateTime.UtcNow` for timestamps, `= true` for IsActive
- Add any enum properties (create enum in same file or separate if complex)
- Add navigation properties if relationships exist
- Use collection initializer `= []` for navigation collections

```csharp
namespace sfa_api.Features.{FeatureName}.Entities;

public class {FeatureName}
{
    public int Id { get; set; }
    // ... business properties ...

    // Audit fields (ALWAYS include these)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties (if needed)
}
```

#### 3. Generate Repository Interface (`Repositories/I{FeatureName}Repository.cs`)

**Pattern to follow:**
- Define methods for: GetById, GetAll (paginated), Create, Update, Delete (soft), SaveChanges
- Add domain-specific lookup methods (e.g., `GetByEmail`, `GetByUsername` like in UserRepository)
- All methods async with `CancellationToken ct = default`
- GetAll returns tuple: `(IEnumerable<{Entity}>, int TotalCount)`

```csharp
namespace sfa_api.Features.{FeatureName}.Repositories;

public interface I{FeatureName}Repository
{
    Task<{FeatureName}?> GetByIdAsync(int id, CancellationToken ct = default);
    // search is optional — pass null to skip filtering
    Task<(IEnumerable<{FeatureName}>, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default);
    Task CreateAsync({FeatureName} entity, CancellationToken ct = default);
    Task UpdateAsync({FeatureName} entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    // Add domain-specific methods here
}
```

#### 4. Generate Repository Implementation (`Repositories/{FeatureName}Repository.cs`)

**Pattern to follow:**
- Primary constructor with `AppDbContext` injection
- Use `_context.{Entities}.FindAsync([id], ct)` for GetById
- Use `FirstOrDefaultAsync` for custom lookups
- Implement pagination with `Skip().Take()`
- **CRITICAL: Never hard-delete** — deactivate by setting `IsActive = false`, then `Update()` — NEVER use `Remove()`
- **Filter active records** with `.Where(x => x.IsActive)` — there is no global query filter
- Never call `SaveChangesAsync` in repository methods (except the SaveChangesAsync method itself)

```csharp
using Microsoft.EntityFrameworkCore;
using sfa_api.Features.{FeatureName}.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.{FeatureName}.Repositories;

public class {FeatureName}Repository(AppDbContext context) : I{FeatureName}Repository
{
    private readonly AppDbContext _context = context;

    public async Task<{FeatureName}?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.{Entities}.FindAsync([id], ct);

    public async Task<(IEnumerable<{FeatureName}>, int)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default)
    {
        var query = _context.{Entities}.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    // ... implement other methods following UserRepository pattern
}
```

#### 5. Generate DTOs (`DTOs/{FeatureName}Dto.cs`, `DTOs/{FeatureName}ListDto.cs`)

**Pattern to follow:**
- Use C# records for DTOs
- Include all fields user needs to see (omit sensitive data like PasswordHash)
- List DTO wraps collection with pagination metadata

```csharp
namespace sfa_api.Features.{FeatureName}.DTOs;

public record {FeatureName}Dto(
    int Id,
    // ... business properties ...
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record {FeatureName}ListDto(
    IEnumerable<{FeatureName}Dto> {Entities},
    int TotalCount,
    int Page,
    int PageSize
);
```

#### 6. Generate Request Models (`Requests/Create{FeatureName}Request.cs`, `Requests/Update{FeatureName}Request.cs`)

**Pattern to follow:**
- Use classes (not records) for request models
- Initialize strings with `= string.Empty`
- Make nullable fields explicit with `?`
- Update request typically mirrors Create but may omit some fields (like password)

```csharp
namespace sfa_api.Features.{FeatureName}.Requests;

public class Create{FeatureName}Request
{
    public string Name { get; set; } = string.Empty;
    // ... other properties ...
}
```

#### 7. Generate Validators (`Validators/Create{FeatureName}Validator.cs`, `Validators/Update{FeatureName}Validator.cs`)

**Pattern to follow:**
- Inherit from `AbstractValidator<TRequest>`
- Define rules in constructor
- Use FluentValidation's fluent API: `RuleFor(x => x.Property).NotEmpty().WithMessage("...")`
- Common rules: NotEmpty, MinimumLength, MaximumLength, Matches (regex), EmailAddress, Must (custom)
- Enum validation: `.Must(val => Enum.TryParse<EnumType>(val, out _)).WithMessage("Invalid {field}.")`

```csharp
using FluentValidation;
using sfa_api.Features.{FeatureName}.Requests;

namespace sfa_api.Features.{FeatureName}.Validators;

public class Create{FeatureName}Validator : AbstractValidator<Create{FeatureName}Request>
{
    public Create{FeatureName}Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        // ... more rules ...
    }
}
```

#### 8. Generate Service Interface (`Services/I{FeatureName}Service.cs`)

**Pattern to follow:**
- Define business operations (usually mirror repository but work with DTOs)
- Return DTOs, not entities
- All methods async with `CancellationToken ct = default`

```csharp
using sfa_api.Features.{FeatureName}.DTOs;
using sfa_api.Features.{FeatureName}.Requests;

namespace sfa_api.Features.{FeatureName}.Services;

public interface I{FeatureName}Service
{
    Task<{FeatureName}Dto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<{FeatureName}ListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<{FeatureName}Dto> CreateAsync(Create{FeatureName}Request request, int? callerId, CancellationToken ct = default);
    Task<{FeatureName}Dto> UpdateAsync(int id, Update{FeatureName}Request request, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

#### 9. Generate Service Implementation (`Services/{FeatureName}Service.cs`)

**Pattern to follow:**
- Primary constructor with repository and logger injection
- Convert page/pageSize to skip/take for repository
- Throw `NotFoundException` if entity not found
- Throw `DuplicateResourceException` for uniqueness violations
- Throw `ValidationException` for business rule violations
- Hash sensitive data (passwords) before storing
- Set audit fields: `CreatedBy`, `UpdatedBy`, `CreatedAt`, `UpdatedAt`
- Call `SaveChangesAsync()` after repository operations
- Log important operations
- Private `MapToDto` method for entity → DTO conversion

```csharp
using sfa_api.Common.Errors;
using sfa_api.Features.{FeatureName}.DTOs;
using sfa_api.Features.{FeatureName}.Entities;
using sfa_api.Features.{FeatureName}.Repositories;
using sfa_api.Features.{FeatureName}.Requests;

namespace sfa_api.Features.{FeatureName}.Services;

public class {FeatureName}Service(
    I{FeatureName}Repository repo,
    ILogger<{FeatureName}Service> logger) : I{FeatureName}Service
{
    private readonly I{FeatureName}Repository _repo = repo;
    private readonly ILogger<{FeatureName}Service> _logger = logger;

    public async Task<{FeatureName}Dto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("{FeatureName}", id);
        return MapToDto(entity);
    }

    public async Task<{FeatureName}ListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
        return new {FeatureName}ListDto(
            {Entities}: items.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    // ... implement other methods following UserService pattern ...

    private static {FeatureName}Dto MapToDto({FeatureName} entity) => new(
        Id: entity.Id,
        // ... map properties ...
        CreatedAt: entity.CreatedAt,
        UpdatedAt: entity.UpdatedAt
    );
}
```

#### 10. Generate Controller (`Controllers/{FeatureName}sController.cs` or `Controllers/{FeatureName}Controller.cs`)

**Pattern to follow:**
- Use primary constructor for dependency injection (service + validators)
- Route: `[Route("api/v1/{entities}")]` (lowercase, plural)
- Apply `[Authorize]` at method or class level based on requirements
- Use `[Authorize(Roles = "Admin")]` for admin-only endpoints
- Extract correlation ID: `HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty`
- Validate requests manually with FluentValidation before calling service
- On validation failure, throw `ValidationException` with fields dictionary
- Extract caller ID from JWT: `int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId)`
- Return responses using `ResponseHelper.Ok()`, `ResponseHelper.Created()`
- Use `CreatedAtAction()` for POST endpoints
- Use `NoContent()` for DELETE endpoints

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.{FeatureName}.Requests;
using sfa_api.Features.{FeatureName}.Services;

namespace sfa_api.Features.{FeatureName}.Controllers;

[ApiController]
[Route("api/v1/{entities}")]
public class {FeatureName}sController(
    I{FeatureName}Service service,
    IValidator<Create{FeatureName}Request> createValidator,
    IValidator<Update{FeatureName}Request> updateValidator) : ControllerBase
{
    private readonly I{FeatureName}Service _service = service;
    private readonly IValidator<Create{FeatureName}Request> _createValidator = createValidator;
    private readonly IValidator<Update{FeatureName}Request> _updateValidator = updateValidator;

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    // ... implement other endpoints following UsersController pattern ...
}
```

#### 11. Register in Program.cs

Add dependency injection registrations for the new feature:

```csharp
// ── {FeatureName} Feature ─────────────────────────────────────────────
builder.Services.AddScoped<I{FeatureName}Repository, {FeatureName}Repository>();
builder.Services.AddScoped<I{FeatureName}Service, {FeatureName}Service>();
builder.Services.AddScoped<IValidator<Create{FeatureName}Request>, Create{FeatureName}Validator>();
builder.Services.AddScoped<IValidator<Update{FeatureName}Request>, Update{FeatureName}Validator>();
```

Add imports at the top:
```csharp
using sfa_api.Features.{FeatureName}.Repositories;
using sfa_api.Features.{FeatureName}.Services;
using sfa_api.Features.{FeatureName}.Validators;
using sfa_api.Features.{FeatureName}.Requests;
```

#### 12. Register in AppDbContext

Add the DbSet property:
```csharp
public DbSet<{FeatureName}> {Entities} => Set<{FeatureName}>();
```

Add entity configuration in `OnModelCreating`:
```csharp
modelBuilder.Entity<{FeatureName}>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).UseIdentityColumn();
    // Add unique indexes for unique fields
    // e.HasIndex(x => x.Email).IsUnique();
    e.HasIndex(x => x.IsActive);
    e.HasIndex(x => x.UpdatedAt);
    // NOTE: We DO NOT add HasQueryFilter(x => x.IsActive) — filter explicitly in each query.
    // Records are never physically removed; deactivation sets IsActive = false.
});
```

Add import:
```csharp
using sfa_api.Features.{FeatureName}.Entities;
```

### Phase 3: Build Validation & Error Fixing

After generating all files, validate and fix the code:

1. **Navigate to API project directory**:
   ```bash
   cd sfa_api/sfa_api
   ```

2. **Run build**:
   ```bash
   dotnet build
   ```

3. **If build fails**, analyze errors and fix them iteratively:
   - Read the error messages carefully
   - Common issues:
     - Missing `using` statements
     - Typos in class/property names
     - Incorrect namespace references
     - Mismatched type signatures
     - Missing null-checks or null-forgiving operators
   - Fix the identified issues
   - Re-run `dotnet build`
   - **Repeat up to 5 times** or until build succeeds

4. **Build success criteria**: Zero errors and zero warnings

### Phase 4: Migration Command

Once build is clean, provide the user with the EF Core migration command:

```bash
cd sfa_api/sfa_api
dotnet ef migrations add Add{FeatureName}Entity --project . --startup-project .
```

Tell the user: "The feature has been generated successfully and the build is clean. When you're ready to apply the database changes, run the migration command above."

## Hierarchical Features & Denormalization

The SFA API uses a 4-level geographic hierarchy:

```
Region → Area → Territory → Division
```

**Rule: each level stores all ancestor IDs as denormalized columns.**

| Entity    | Stores                          |
|-----------|---------------------------------|
| Region    | nothing above                   |
| Area      | `RegionId` (direct parent FK)   |
| Territory | `AreaId` (direct FK) + `RegionId` (denormalized) |
| Division  | `TerritoryId` (direct FK) + `AreaId` + `RegionId` (denormalized) |

### Generating a Hierarchical Entity

**Entity** — declare all ancestor ID columns and navigation properties:
```csharp
public class Territory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AreaId { get; set; }      // direct parent (FK)
    public int RegionId { get; set; }    // denormalized ancestor
    public bool IsActive { get; set; } = true;
    // ... audit fields ...
    public Area? Area { get; set; }
    public Region? Region { get; set; }
}
```

**Repository** — use `ThenInclude` to populate the full chain:
```csharp
public async Task<Territory?> GetByIdAsync(int id, CancellationToken ct = default)
    => await _context.Territories
        .Include(t => t.Area)
            .ThenInclude(a => a!.Region)
        .FirstOrDefaultAsync(t => t.Id == id, ct);

// Expose a dedicated method so the service can resolve ancestors on write
public async Task<Area?> GetAreaWithRegionAsync(int areaId, CancellationToken ct = default)
    => await _context.Areas
        .Include(a => a.Region)
        .FirstOrDefaultAsync(a => a.Id == areaId, ct);
```

**Service** — resolve the full ancestor chain from the parent on create/update:
```csharp
// Creating a Territory
var area = await _repo.GetAreaWithRegionAsync(request.AreaId, ct)
    ?? throw new NotFoundException("Area", request.AreaId);

var territory = new Territory
{
    Name = request.Name,
    AreaId = area.Id,
    RegionId = area.RegionId,   // already denormalized on Area — no extra query
    // ...
};
```

**Key insight:** because Area already stores `RegionId`, Territory only needs to read from its direct parent to get all ancestor IDs. Each level's denormalization compounds, so you never traverse more than one level up.

**DbContext** — add FK relationship and index for every ancestor ID column:
```csharp
modelBuilder.Entity<Territory>(e =>
{
    // ...
    e.HasIndex(x => x.AreaId);
    e.HasIndex(x => x.RegionId);
    e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).IsRequired();
    e.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).IsRequired();
});
```

**DTO** — include both the ID and Name for every ancestor:
```csharp
public record TerritoryDto(
    int Id,
    string Name,
    int AreaId,
    string AreaName,
    int RegionId,
    string RegionName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

`RegionName` in the DTO is safe even though `RegionId` is denormalized — the name is read
fresh from the navigation property at query time, never stored in the Territories table.
So renaming a Region is automatically reflected on the next read.

---

## Key Principles

### Naming Conventions
- **Entities**: Singular PascalCase (e.g., `User`, `Product`, `Order`)
- **Controllers**: Plural with "Controller" suffix (e.g., `UsersController`, `ProductsController`)
- **Routes**: Lowercase plural (e.g., `/api/v1/users`, `/api/v1/products`)
- **Files**: Match class names exactly

### Consistency is Critical
- Study the Users feature files before generating anything
- Follow the EXACT patterns for constructor injection, method signatures, error handling
- Maintain the same level of detail in XML comments
- Use the same exception types (`NotFoundException`, `DuplicateResourceException`, `ValidationException`)

### Soft Delete / Deactivation Pattern (CRITICAL)
- **NEVER hard-delete records** — no `context.Remove()`, no `_context.{Entities}.Remove(entity)`
- **`IsActive` is the universal status flag** — every entity has it, defaulting to `true`
- **Deactivation:** set `IsActive = false` then call `Update()` — this is "soft delete"
- **Active-only queries:** always filter with `.Where(x => x.IsActive)` — there is no global query filter
- **Do NOT add** `HasQueryFilter(x => x.IsActive)` in DbContext — filter explicitly per query
- Records are NEVER physically removed — they are deactivated
- Example deactivation implementation:
  ```csharp
  public async Task DeactivateAsync(int id, CancellationToken ct = default)
  {
      var entity = await _context.{Entities}.FindAsync([id], ct);
      if (entity != null)
      {
          entity.IsActive = false;
          _context.{Entities}.Update(entity);
      }
  }
  ```

### Audit Trail
- Always set `CreatedBy`/`UpdatedBy` from caller ID
- Always update `UpdatedAt = DateTime.UtcNow` on modifications
- `CreatedAt` is set once on creation

### Error Handling Strategy
- Let the `GlobalExceptionMiddleware` handle exceptions
- Don't use try-catch in controllers/services unless recovering from errors
- Throw domain exceptions for business rule violations
- Validation exceptions must include field-level error dictionary

### Authorization Patterns
- Admin-only operations: `[Authorize(Roles = "Admin")]`
- Self-update allowed: Check if `currentUserId == resourceUserId`
- Throw `AuthorizationException` for permission violations

## Improvement Suggestions

While following the Users feature patterns exactly, consider suggesting these improvements to the user after successful generation:

1. **Pagination defaults**: If the user wants different default page size (currently 10)
2. **Additional indexes**: Based on query patterns they describe
3. **Computed properties**: If derived values would be useful in DTOs
4. **Search/filtering**: If they need more than basic pagination
5. **Bulk operations**: If they'll need to create/update many records at once
6. **Caching**: For read-heavy entities
7. **Background jobs**: For long-running operations
8. **Webhooks/events**: If other systems need to know about changes

Present these as optional enhancements AFTER the core feature is working.

## Example Workflow

**User says**: "Create a Product feature with name, SKU, price, and category"

**You do**:
1. Confirm requirements:
   - "I'll create a Product feature with: string Name, string SKU, decimal Price, string Category. Should SKU be unique? Any validation rules for price (min/max)? Admin-only or accessible by SalesRep too?"
2. Read Users feature files to understand patterns
3. Generate all files following the patterns
4. Register in Program.cs and AppDbContext
5. Run `dotnet build`, fix any errors iteratively
6. Provide migration command
7. Ask if they want any of the improvement suggestions

**✶ Insight ─────────────────────────────────────**
This skill treats the Users feature as the "source of truth"
for architectural patterns. By studying it first and mirroring
its structure, we ensure consistency across the entire codebase.
The iterative build-fix loop catches integration issues early,
before the user even tries to run the code.
**─────────────────────────────────────────────────**
