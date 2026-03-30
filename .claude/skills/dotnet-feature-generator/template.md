# Feature Generation Plan: {FeatureName}

Fill in every section before generating any code.

---

## Entity Summary

| Field       | Value |
|-------------|-------|
| Feature name (singular PascalCase) | `{FeatureName}` |
| Route segment (lowercase plural)   | `{entities}` |
| DbSet name                         | `{Entities}` |
| Namespace prefix                   | `sfa_api.Features.{FeatureName}` |
| High-growth entity? (orders/visits/schedules) | Yes / No |

---

## Properties

| Property  | C# Type  | Nullable | Default             | Notes |
|-----------|----------|----------|---------------------|-------|
| Id        | int      | No       | identity            | PK, auto-increment |
| Name      | string   | No       | `= string.Empty`    | |
| ...       |          |          |                     | |
| CreatedAt | DateTime | No       | `= DateTime.UtcNow` | audit |
| UpdatedAt | DateTime | No       | `= DateTime.UtcNow` | audit |
| CreatedBy | int?     | Yes      | null                | audit |
| UpdatedBy | int?     | Yes      | null                | audit |
| IsActive  | bool     | No       | `= true`            | universal soft-delete flag |
| IsDeleted | bool     | No       | `= false`           | audit flag — set by DELETE endpoint |

---

## Enums (if any)

```csharp
public enum {FeatureName}Status { Active, Inactive }
```

---

## Validation Rules

| Field | Rule | Error message |
|-------|------|---------------|
| Name  | NotEmpty | "Name is required." |
| Name  | MaximumLength(100) | "Name must not exceed 100 characters." |
| ...   | | |

---

## Endpoints

| Method | Route                    | Auth                         | Notes |
|--------|--------------------------|------------------------------|-------|
| GET    | /api/v1/{entities}       | `[Authorize]`                | paginated, search |
| GET    | /api/v1/{entities}/{id}  | `[Authorize]`                | |
| POST   | /api/v1/{entities}       | `[Authorize(Roles="Admin")]` | |
| PUT    | /api/v1/{entities}/{id}  | `[Authorize(Roles="Admin")]` | |
| DELETE | /api/v1/{entities}/{id}  | `[Authorize(Roles="Admin")]` | soft delete via ExecuteUpdateAsync (IsActive=false + IsDeleted=true) |

---

## Files to Generate

```
sfa_api/sfa_api/Features/{FeatureName}/
├── Entities/
│   └── {FeatureName}.cs                    ← IsActive = true + IsDeleted = false (soft-delete + audit)
├── DTOs/
│   ├── {FeatureName}Dto.cs
│   └── {FeatureName}ListDto.cs
├── Requests/
│   ├── Create{FeatureName}Request.cs
│   └── Update{FeatureName}Request.cs
├── Validators/
│   ├── Create{FeatureName}Validator.cs
│   └── Update{FeatureName}Validator.cs
├── Repositories/
│   ├── I{FeatureName}Repository.cs
│   └── {FeatureName}Repository.cs          ← AsNoTracking + IsActive filter + ExecuteUpdateAsync
├── Services/
│   ├── I{FeatureName}Service.cs
│   └── {FeatureName}Service.cs             ← IDistributedCache cache-aside
└── Controllers/
    └── {FeatureName}sController.cs         ← CancellationToken ct on every action
```

Plus edits to:
- `sfa_api/sfa_api/Infrastructure/Persistence/AppDbContext.cs` — composite partial indexes
- `sfa_api/sfa_api/Program.cs` — Scoped registrations with lifetime comments; verify AddDbContextPool + AddRateLimiter

---

## Entity Template

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
    public bool IsActive { get; set; } = true;   // universal soft-delete flag
    public bool IsDeleted { get; set; } = false;  // audit flag — set by DELETE endpoint
}
```

---

## Repository Template

```csharp
using Microsoft.EntityFrameworkCore;
using sfa_api.Features.{FeatureName}.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.{FeatureName}.Repositories;

public class {FeatureName}Repository(AppDbContext context) : I{FeatureName}Repository
{
    private readonly AppDbContext _context = context;

    // Tracked — for callers that will update this entity afterward
    public async Task<{FeatureName}?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.{Entities}
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

    public async Task<(IEnumerable<{FeatureName}>, int)> GetAllAsync(
        int skip, int take, string? search = null, CancellationToken ct = default)
    {
        var query = _context.{Entities}
            .Where(x => x.IsActive)    // ← always filter soft-deleted
            .AsNoTracking();           // ← always on reads

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)               // ← always paginate
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

    // ExecuteUpdateAsync — direct SQL UPDATE; no entity load, no SaveChangesAsync needed after
    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _context.{Entities}
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive,  false)
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
```

---

## AppDbContext Template

```csharp
// DbSet
public DbSet<{FeatureName}> {Entities} => Set<{FeatureName}>();

// OnModelCreating configuration
modelBuilder.Entity<{FeatureName}>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).UseIdentityColumn();

    // Unique constraint (add only if applicable)
    // e.HasIndex(x => x.Name).IsUnique();

    // Composite partial index — active rows + CreatedAt DESC for sorted lists
    // Partial filter keeps index smaller as inactive rows accumulate
    e.HasIndex(x => new { x.IsActive, x.CreatedAt })
     .IsDescending(false, true)
     .HasFilter("\"IsActive\" = true")
     .HasDatabaseName("idx_{entities}_active_created");

    // Add one index per distinct FK query pattern:
    // e.HasIndex(x => new { x.OwnerId, x.CreatedAt })
    //  .IsDescending(false, true)
    //  .HasFilter("\"IsActive\" = true")
    //  .HasDatabaseName("idx_{entities}_owner_created");

    // NOTE: No HasQueryFilter — filter IsActive explicitly in each query.
    // Records are never physically removed — deactivation sets IsActive = false.
});

// High-growth entity: add partitioning TODO after the table definition
// TODO (PRODUCTION): RANGE-partition "{Entities}" by CreatedAt in PostgreSQL.
// See SKILL.md → Phase 2 → Step 12 for the exact ALTER TABLE commands.
```

---

## Program.cs DI Template

```csharp
// ── {FeatureName} Feature ─────────────────────────────────────────────────────
// Scoped — one instance per HTTP request; shares the request's AppDbContext
builder.Services.AddScoped<I{FeatureName}Repository, {FeatureName}Repository>();
builder.Services.AddScoped<I{FeatureName}Service, {FeatureName}Service>();
builder.Services.AddScoped<IValidator<Create{FeatureName}Request>, Create{FeatureName}Validator>();
builder.Services.AddScoped<IValidator<Update{FeatureName}Request>, Update{FeatureName}Validator>();
```

**Verify these already exist — add if missing:**

```csharp
// AddDbContextPool — recycles DbContext instances (~20% throughput gain over AddDbContext)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// IDistributedCache — works across 2+ Azure instances; IMemoryCache does NOT
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string not configured.");
});

// Rate limiting — 500 req/60s global, 10 req/60s auth
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("global", o =>
    {
        o.PermitLimit  = 500;
        o.Window       = TimeSpan.FromSeconds(60);
        o.SegmentCount = 6;
        o.QueueLimit   = 0;
    });
    options.AddSlidingWindowLimiter("auth", o =>
    {
        o.PermitLimit  = 10;
        o.Window       = TimeSpan.FromSeconds(60);
        o.SegmentCount = 6;
        o.QueueLimit   = 0;
    });
});
```

---

## Migration Command

```bash
cd sfa_api/sfa_api
dotnet ef migrations add Add{FeatureName}Entity --project . --startup-project .
```

---

## Pre-Completion Self-Validation Checklist

### Async / CancellationToken
- [ ] Every controller action has `CancellationToken ct` as the last parameter
- [ ] Every service method has `CancellationToken ct = default`
- [ ] Every repository method has `CancellationToken ct = default`
- [ ] No `.Result`, `.Wait()`, or `Task.Run()` in any generated file
- [ ] Every EF call ends with `Async(ct)`: `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, `AnyAsync`, `CountAsync`, `ExecuteUpdateAsync`

### EF Core Read Patterns
- [ ] Every list query has `.AsNoTracking()`
- [ ] Every query filters `x.IsActive`
- [ ] Every list query has `.Skip(skip).Take(take)`
- [ ] Results projected to DTOs — no raw entity returns from service methods

### Writes & Deletes
- [ ] Soft delete uses `ExecuteUpdateAsync` — no load-then-mutate for deactivation
- [ ] Multi-row inserts use `AddRange` + single `SaveChangesAsync`
- [ ] Related entities resolved with batch `WHERE IN` — no loop queries

### Entity & Schema
- [ ] Entity has `IsActive bool = true` (universal soft-delete flag)
- [ ] Entity has `IsDeleted bool = false` (audit flag — set by DELETE endpoint)
- [ ] Entity has `UpdatedAt DateTime` updated on every write
- [ ] DbContext has composite partial index with `HasFilter("\"IsActive\" = true")`
- [ ] High-growth entity has partitioning TODO comment in migration

### Infrastructure
- [ ] Program.cs uses `AddDbContextPool` (not `AddDbContext`)
- [ ] Program.cs has `AddRateLimiter` with global + auth limiters
- [ ] All repos and services registered as `Scoped` with lifetime comment
- [ ] Connection string via `GetConnectionString(...) ?? throw`
- [ ] Service uses `IDistributedCache` with cache-aside (not `IMemoryCache`)
