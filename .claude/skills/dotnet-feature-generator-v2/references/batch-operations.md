# Reference: Batch Operations

Load this reference when the feature needs bulk create, update, or delete operations.

---

## Principles

1. **One round-trip per batch** — never `SaveChangesAsync` inside a loop
2. **Batch WHERE IN for lookups** — resolve all related entities in a single query
3. **Transaction scope** — wrap multi-step batches in explicit transactions
4. **Chunk large batches** — PostgreSQL has a 65535 parameter limit; chunk at ~1000 rows

---

## Bulk Create

```csharp
// Repository
public async Task CreateRangeAsync(IEnumerable<{FeatureName}> entities, CancellationToken ct = default)
{
    await _context.{Entities}.AddRangeAsync(entities, ct);
}

// Service
public async Task<IEnumerable<{FeatureName}Dto>> CreateBatchAsync(
    IEnumerable<Create{FeatureName}Request> requests, int? callerId, CancellationToken ct = default)
{
    var entities = requests.Select(r => new {FeatureName}
    {
        // ... map properties ...
        CreatedBy = callerId,
        UpdatedBy = callerId,
    }).ToList();

    await _repo.CreateRangeAsync(entities, ct);
    await _repo.SaveChangesAsync(ct);  // single round-trip

    return entities.Select(MapToDto);
}
```

### Chunking for Large Batches

```csharp
public async Task CreateBatchAsync(List<{FeatureName}> entities, CancellationToken ct = default)
{
    const int chunkSize = 1000;

    foreach (var chunk in entities.Chunk(chunkSize))
    {
        await _context.{Entities}.AddRangeAsync(chunk, ct);
        await _context.SaveChangesAsync(ct);
        _context.ChangeTracker.Clear();  // prevent memory bloat
    }
}
```

---

## Bulk Update with ExecuteUpdateAsync

For status changes or field updates across many rows — no entity loading:

```csharp
// Deactivate all entities belonging to a parent
public async Task DeactivateByParentAsync(int parentId, CancellationToken ct = default)
    => await _context.{Entities}
        .Where(x => x.ParentId == parentId && x.IsActive)
        .ExecuteUpdateAsync(s => s
            .SetProperty(x => x.IsActive, false)
            .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);
```

---

## Bulk Delete (Soft)

```csharp
// Soft-delete multiple by IDs
public async Task DeleteRangeAsync(IEnumerable<int> ids, CancellationToken ct = default)
    => await _context.{Entities}
        .Where(x => ids.Contains(x.Id))
        .ExecuteUpdateAsync(s => s
            .SetProperty(x => x.IsActive, false)
            .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);
```

---

## Resolving Related Entities in Batch

**CORRECT — single WHERE IN query:**

```csharp
var relatedIds = requests.Select(r => r.ParentId).Distinct().ToList();
var parents = await _context.Parents
    .Where(p => relatedIds.Contains(p.Id) && p.IsActive)
    .AsNoTracking()
    .ToDictionaryAsync(p => p.Id, ct);

// Validate all exist
var missing = relatedIds.Except(parents.Keys).ToList();
if (missing.Count > 0)
    throw new NotFoundException("Parent", missing.First());
```

**WRONG — N+1 loop:**

```csharp
// DO NOT DO THIS
foreach (var request in requests)
{
    var parent = await _repo.GetByIdAsync(request.ParentId, ct);  // one SELECT per item
}
```

---

## Transaction Wrapper

For operations that must succeed or fail atomically:

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync(ct);
try
{
    await _context.{Entities}.AddRangeAsync(entities, ct);
    await _context.SaveChangesAsync(ct);

    await _context.RelatedEntities
        .Where(x => ids.Contains(x.Id))
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, newStatus), ct);

    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

---

## Controller Endpoint

```csharp
[HttpPost("batch")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> CreateBatch(
    [FromBody] List<Create{FeatureName}Request> requests, CancellationToken ct)
{
    var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

    // Validate each request
    var errors = new Dictionary<string, string>();
    for (var i = 0; i < requests.Count; i++)
    {
        var validation = await _createValidator.ValidateAsync(requests[i], ct);
        if (!validation.IsValid)
            foreach (var e in validation.Errors)
                errors[$"[{i}].{e.PropertyName}"] = e.ErrorMessage;
    }
    if (errors.Count > 0)
        throw new Common.Errors.ValidationException(errors);

    int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
    var result = await _service.CreateBatchAsync(requests, callerId, ct);
    return Ok(ResponseHelper.Ok(result, correlationId));
}
```
