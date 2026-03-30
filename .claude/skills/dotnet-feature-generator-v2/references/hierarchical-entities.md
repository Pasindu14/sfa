# Reference: Hierarchical Entities

Load this reference when the feature entity has a parent FK chain (e.g., Region → Area → Territory → Division).

---

## Hierarchy Pattern

The SFA API uses a 4-level geographic hierarchy where each level stores all ancestor IDs as denormalized columns:

```
Region → Area → Territory → Division
```

| Entity | Stores |
|--------|--------|
| Region | nothing above |
| Area | `RegionId` (direct parent FK) |
| Territory | `AreaId` (direct FK) + `RegionId` (denormalized) |
| Division | `TerritoryId` (direct FK) + `AreaId` + `RegionId` (denormalized) |

**Why denormalize?** A single SELECT returns the full ancestor chain. Without denormalization, resolving a Division's Region requires 3 JOINs. At 50M rows, those JOINs dominate query time.

---

## Entity Pattern

```csharp
public class Territory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Direct parent (FK with navigation)
    public int AreaId { get; set; }
    public Area? Area { get; set; }

    // Denormalized ancestors (FK with navigation)
    public int RegionId { get; set; }
    public Region? Region { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}
```

---

## Repository Pattern

### GetByIdAsync — Include ancestors for display

```csharp
public async Task<Territory?> GetByIdAsync(int id, CancellationToken ct = default)
    => await _context.Territories
        .Include(t => t.Area)
            .ThenInclude(a => a!.Region)
        .FirstOrDefaultAsync(t => t.Id == id && t.IsActive, ct);
```

### GetAreaWithRegionAsync — Resolve ancestor chain for writes

This is the **critical method** for hierarchical entities. The service calls it to get the parent entity with its own ancestors populated, so it can copy denormalized IDs without extra queries.

```csharp
public async Task<Area?> GetAreaWithRegionAsync(int areaId, CancellationToken ct = default)
    => await _context.Areas
        .Include(a => a.Region)
        .FirstOrDefaultAsync(a => a.Id == areaId && a.IsActive, ct);
```

**Important:** The service calls `GetAreaWithRegionAsync` (returns `Area?`), NOT `AreaExistsAsync` (returns `bool`). Mock accordingly in tests:

```csharp
// Correct mock setup
_repoMock.Setup(r => r.GetAreaWithRegionAsync(areaId, It.IsAny<CancellationToken>()))
         .ReturnsAsync(CreateFakeArea(areaId));
```

### GetAllAsync — AsNoTracking, no includes needed for list view

```csharp
public async Task<(IEnumerable<Territory>, int)> GetAllAsync(
    int skip, int take, string? search = null, CancellationToken ct = default)
{
    var query = _context.Territories
        .Where(t => t.IsActive)
        .AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()));

    var total = await query.CountAsync(ct);
    var items = await query
        .OrderBy(t => t.Name)
        .Skip(skip).Take(take)
        .ToListAsync(ct);

    return (items, total);
}
```

---

## Service Pattern — Resolve Ancestors on Create/Update

```csharp
public async Task<TerritoryDto> CreateAsync(
    CreateTerritoryRequest request, int? callerId, CancellationToken ct = default)
{
    // Resolve parent with its ancestors — single query
    var area = await _repo.GetAreaWithRegionAsync(request.AreaId, ct)
        ?? throw new NotFoundException("Area", request.AreaId);

    var territory = new Territory
    {
        Name = request.Name,
        AreaId = area.Id,
        RegionId = area.RegionId,  // denormalized — no extra query
        CreatedBy = callerId,
        UpdatedBy = callerId,
    };

    await _repo.CreateAsync(territory, ct);
    await _repo.SaveChangesAsync(ct);
    return MapToDto(territory);
}
```

**On Update** — if the parent FK changes, re-resolve all denormalized ancestors:

```csharp
if (entity.AreaId != request.AreaId)
{
    var area = await _repo.GetAreaWithRegionAsync(request.AreaId, ct)
        ?? throw new NotFoundException("Area", request.AreaId);
    entity.AreaId = area.Id;
    entity.RegionId = area.RegionId;
}
```

---

## DbContext Pattern — Index per FK

```csharp
modelBuilder.Entity<Territory>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).UseIdentityColumn();

    e.HasIndex(x => new { x.AreaId, x.CreatedAt })
     .HasFilter("\"IsActive\" = true")
     .HasDatabaseName("idx_territories_area_created");

    e.HasIndex(x => new { x.RegionId, x.CreatedAt })
     .HasFilter("\"IsActive\" = true")
     .HasDatabaseName("idx_territories_region_created");

    e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).IsRequired();
    e.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).IsRequired();
});
```

---

## DTO Pattern — Include ancestor names

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

The `MapToDto` method must navigate the included entities:

```csharp
private static TerritoryDto MapToDto(Territory t) => new(
    Id: t.Id,
    Name: t.Name,
    AreaId: t.AreaId,
    AreaName: t.Area?.Name ?? string.Empty,
    RegionId: t.RegionId,
    RegionName: t.Area?.Region?.Name ?? string.Empty,
    IsActive: t.IsActive,
    CreatedAt: t.CreatedAt,
    UpdatedAt: t.UpdatedAt
);
```
