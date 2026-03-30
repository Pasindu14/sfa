---
name: dotnet-feature-generator-v2
description: Generates complete, production-ready ASP.NET Core features for the SFA API project. Lean skill with progressive reference loading — heavy patterns (hierarchies, batch ops, locking, auditing) live in references/ and load only when needed. Use via the dotnet-feature-generator-v2 agent or /create-feature command.
---

# .NET API Feature Generator v2

Generate production-ready ASP.NET Core features for the SFA API — scalable to 50M+ rows and 1000 concurrent users without architectural rewrites.

---

## 14 Scalability Rules (Non-Negotiable)

Every generated file must comply. The agent's self-validation checklist enforces these.

### ALWAYS DO

| # | Rule | Pattern |
|---|------|---------|
| 1 | Async everywhere | `ToListAsync(ct)`, `FirstOrDefaultAsync(ct)`, `SaveChangesAsync(ct)` — every async method accepts `CancellationToken ct` |
| 2 | Scoped lifetimes with comments | `AddScoped<IRepo, Repo>(); // Scoped — per-request DB context` |
| 3 | AsNoTracking on every read | `.AsNoTracking()` on all list/read queries |
| 4 | IsActive filter on every query | `.Where(x => x.IsActive)` — universal soft-delete flag; DELETE also sets `IsDeleted = true` as audit trail |
| 5 | Paginate every list | `.Skip(skip).Take(take)` — no unbounded queries |
| 6 | ExecuteUpdateAsync for status changes | Never load entity just to flip a flag; DELETE sets both `IsActive = false` and `IsDeleted = true` |
| 7 | AddRange + single SaveChangesAsync | Batch all adds; one DB round-trip |
| 8 | Batch WHERE IN for related data | `.Where(u => ids.Contains(u.Id))` — no N+1 |
| 9 | Composite partial indexes | `HasIndex(new{Col,CreatedAt}).HasFilter("\"IsActive\" = true")` |
| 10 | Project to DTO | `.Select(x => new Dto{...})` or `MapToDto()` — never return raw entities |
| 11 | AddDbContextPool | Not `AddDbContext` — ~20% throughput gain |
| 12 | Rate limiting | Sliding-window global + auth limiters |
| 13 | IDistributedCache only | Cache-aside pattern — never `IMemoryCache` |
| 14 | Connection string from config | `GetConnectionString("DefaultConnection") ?? throw` |

### NEVER DO

| Anti-pattern | Failure mode |
|---|---|
| `.Result` / `.Wait()` | Deadlock under ASP.NET sync context |
| `Task.Run()` wrapping sync | Wastes thread pool |
| Sync EF: `ToList()` / `Find()` / `SaveChanges()` | Blocks threads |
| Omitting `CancellationToken` | Client disconnect doesn't abort query |
| `IMemoryCache` | Diverges across Azure instances |
| Unbounded list (no Skip/Take) | OOM at 100k+ rows |
| Load-then-mutate for status change | Extra SELECT per update |
| `SaveChangesAsync` in loop | N round-trips instead of 1 |
| N+1 loop queries | N SELECTs instead of 1 WHERE IN |
| `context.Remove()` / hard delete | Violates soft-delete contract |
| Omitting `IsDeleted = true` in DELETE | Loses the audit distinction between deactivation and deletion |

### Soft Delete vs Deactivation

Every entity has two flags:
- **`IsActive`** — universal status flag. `false` = deactivated (reversible). Queries always filter `.Where(x => x.IsActive)`.
- **`IsDeleted`** — audit flag. `false` by default. Set to `true` only by the DELETE endpoint to mark explicit deletion. Deactivation (`ActivateAsync`/`DeactivateAsync`) only flips `IsActive`, never touches `IsDeleted`.

---

## File Generation Order

```
sfa_api/sfa_api/Features/{FeatureName}/
├── Entities/{FeatureName}.cs
├── DTOs/{FeatureName}Dto.cs
├── Requests/Create{FeatureName}Request.cs
├── Requests/Update{FeatureName}Request.cs
├── Validators/Create{FeatureName}Validator.cs
├── Validators/Update{FeatureName}Validator.cs
├── Repositories/I{FeatureName}Repository.cs
├── Repositories/{FeatureName}Repository.cs
├── Services/I{FeatureName}Service.cs
├── Services/{FeatureName}Service.cs
└── Controllers/{FeatureName}sController.cs

Plus edits to:
  AppDbContext.cs — DbSet + OnModelCreating config
  Program.cs     — Scoped DI registrations
```

---

## Templates

### Entity

```csharp
namespace sfa_api.Features.{FeatureName}.Entities;

public class {FeatureName}
{
    public int Id { get; set; }
    // ... business properties ...

    // Audit fields — include ALL
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}
```

### Repository Interface

```csharp
namespace sfa_api.Features.{FeatureName}.Repositories;

public interface I{FeatureName}Repository
{
    Task<{FeatureName}?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<{FeatureName}>, int TotalCount)> GetAllAsync(
        int skip, int take, string? search = null, CancellationToken ct = default);
    Task CreateAsync({FeatureName} entity, CancellationToken ct = default);
    Task UpdateAsync({FeatureName} entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    // Domain-specific lookups: ExistsByNameAsync, etc.
}
```

### Repository Implementation

```csharp
using Microsoft.EntityFrameworkCore;
using sfa_api.Features.{FeatureName}.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.{FeatureName}.Repositories;

public class {FeatureName}Repository(AppDbContext context) : I{FeatureName}Repository
{
    private readonly AppDbContext _context = context;

    public async Task<{FeatureName}?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.{Entities}
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

    public async Task<(IEnumerable<{FeatureName}>, int)> GetAllAsync(
        int skip, int take, string? search = null, CancellationToken ct = default)
    {
        var query = _context.{Entities}
            .Where(x => x.IsActive)
            .AsNoTracking();

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

    public async Task CreateAsync({FeatureName} entity, CancellationToken ct = default)
        => await _context.{Entities}.AddAsync(entity, ct);

    public Task UpdateAsync({FeatureName} entity, CancellationToken ct = default)
    {
        _context.{Entities}.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _context.{Entities}
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
```

### DTOs

```csharp
namespace sfa_api.Features.{FeatureName}.DTOs;

public record {FeatureName}Dto(
    int Id,
    // ... business properties ...
    bool IsActive,
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

### Request Models

```csharp
namespace sfa_api.Features.{FeatureName}.Requests;

public class Create{FeatureName}Request
{
    public string Name { get; set; } = string.Empty;
    // ... other writable properties ...
}

public class Update{FeatureName}Request
{
    public string Name { get; set; } = string.Empty;
    // ... other updatable properties ...
}
```

### Validators

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
    }
}
```

### Service Interface

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

### Service Implementation

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using sfa_api.Common.Errors;
using sfa_api.Features.{FeatureName}.DTOs;
using sfa_api.Features.{FeatureName}.Entities;
using sfa_api.Features.{FeatureName}.Repositories;
using sfa_api.Features.{FeatureName}.Requests;

namespace sfa_api.Features.{FeatureName}.Services;

public class {FeatureName}Service(
    I{FeatureName}Repository repo,
    IDistributedCache cache,
    ILogger<{FeatureName}Service> logger) : I{FeatureName}Service
{
    private readonly I{FeatureName}Repository _repo = repo;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<{FeatureName}Service> _logger = logger;

    public async Task<{FeatureName}Dto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var key = $"{entities}:{id}";
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<{FeatureName}Dto>(cached)!;

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("{FeatureName}", id);

        var dto = MapToDto(entity);
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }, ct);
        return dto;
    }

    public async Task<{FeatureName}ListDto> GetAllAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default)
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

    public async Task<{FeatureName}Dto> CreateAsync(
        Create{FeatureName}Request request, int? callerId, CancellationToken ct = default)
    {
        var entity = new {FeatureName}
        {
            // ... map request properties ...
            CreatedBy = callerId,
            UpdatedBy = callerId,
        };

        await _repo.CreateAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Created {FeatureName} {Id}", entity.Id);
        return MapToDto(entity);
    }

    public async Task<{FeatureName}Dto> UpdateAsync(
        int id, Update{FeatureName}Request request, int? callerId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("{FeatureName}", id);

        // ... update properties ...
        entity.UpdatedBy = callerId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        await _cache.RemoveAsync($"{entities}:{id}", ct);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("{FeatureName}", id);

        await _repo.DeleteAsync(id, ct);
        await _cache.RemoveAsync($"{entities}:{id}", ct);
    }

    private static {FeatureName}Dto MapToDto({FeatureName} entity) => new(
        Id: entity.Id,
        // ... map properties ...
        IsActive: entity.IsActive,
        CreatedAt: entity.CreatedAt,
        UpdatedAt: entity.UpdatedAt
    );
}
```

### Controller

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

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllAsync(page, pageSize, search, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] Create{FeatureName}Request request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new Common.Errors.ValidationException(
                validation.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage));

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id, [FromBody] Update{FeatureName}Request request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new Common.Errors.ValidationException(
                validation.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage));

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
```

### AppDbContext Registration

```csharp
// DbSet
public DbSet<{FeatureName}> {Entities} => Set<{FeatureName}>();

// OnModelCreating
modelBuilder.Entity<{FeatureName}>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).UseIdentityColumn();

    // Composite partial index — active rows + CreatedAt DESC
    e.HasIndex(x => new { x.IsActive, x.CreatedAt })
     .IsDescending(false, true)
     .HasFilter("\"IsActive\" = true")
     .HasDatabaseName("idx_{entities}_active_created");

    // NOTE: No HasQueryFilter — filter IsActive explicitly per query.
});
```

### Program.cs DI

```csharp
// -- {FeatureName} Feature --------------------------------------------------------
// Scoped — one instance per HTTP request; shares the request's AppDbContext
builder.Services.AddScoped<I{FeatureName}Repository, {FeatureName}Repository>();
builder.Services.AddScoped<I{FeatureName}Service, {FeatureName}Service>();
builder.Services.AddScoped<IValidator<Create{FeatureName}Request>, Create{FeatureName}Validator>();
builder.Services.AddScoped<IValidator<Update{FeatureName}Request>, Update{FeatureName}Validator>();
```

---

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Entity | Singular PascalCase | `Product` |
| Controller | Plural + Controller | `ProductsController` |
| Route | Lowercase plural | `/api/v1/products` |
| DbSet | PascalCase plural | `Products` |
| Namespace | `sfa_api.Features.{Name}.{Layer}` | `sfa_api.Features.Product.Services` |

## Error Handling

- Let `GlobalExceptionMiddleware` handle exceptions
- No try-catch in controllers/services unless recovering
- Throw: `NotFoundException`, `DuplicateResourceException`, `ValidationException`
- Validation errors include field-level dictionary

## DI Lifetime Rules

| Component | Lifetime | Why |
|-----------|----------|-----|
| Repository | Scoped | Shares request's DbContext |
| Service | Scoped | Depends on scoped repository |
| HttpClient factory | Singleton | Reuses connections |
| Redis lock service | Singleton | Shared connection pool |

**Never register Scoped inside Singleton** — captive dependency causes stale data.

---

## References (Load On-Demand)

These files contain advanced patterns. The agent loads them only when the feature requires it.

| Reference | When to Load |
|-----------|-------------|
| `references/hierarchical-entities.md` | Entity has parent FK chain (Region→Area→Territory→Division) |
| `references/batch-operations.md` | Feature needs bulk create/update/delete |
| `references/distributed-locking.md` | Concurrent write conflicts possible (inventory, assignments) |
| `references/caching-patterns.md` | Non-standard caching TTL or invalidation strategy |
| `references/high-growth-indexing.md` | Entity will exceed 10M rows (orders, visits, schedules) |
| `references/auditing-patterns.md` | Compliance/audit trail beyond standard CreatedBy/UpdatedBy |
