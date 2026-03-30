# Reference: Distributed Locking

Load this reference when the feature has concurrent write conflicts — inventory adjustments, assignment slots, counter updates, approval workflows.

---

## When to Use

| Scenario | Lock Needed? |
|----------|-------------|
| Simple CRUD (one user edits one record) | No — optimistic concurrency sufficient |
| Two users claim the same assignment slot | Yes — prevent double-booking |
| Inventory decrement during order creation | Yes — prevent oversell |
| Sequential number generation (PO-001, PO-002) | Yes — prevent duplicates |
| Approval workflow (one approver at a time) | Yes — prevent race condition |

---

## Infrastructure: RedLock via IDistributedLockService

The SFA API uses `RedLock.net` for distributed locking across multiple app instances.

### Registration (already in Program.cs — verify, don't duplicate)

```csharp
// Singleton — shared Redis connection pool
builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
{
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
    return RedLockFactory.Create(new List<RedLockMultiplexer> { new(redis) });
});
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();
```

### Interface

```csharp
public interface IDistributedLockService
{
    Task<IAsyncDisposable?> TryAcquireLockAsync(
        string resource, TimeSpan expiry, CancellationToken ct = default);
}
```

---

## Service Pattern

```csharp
public class {FeatureName}Service(
    I{FeatureName}Repository repo,
    IDistributedCache cache,
    IDistributedLockService lockService,
    ILogger<{FeatureName}Service> logger) : I{FeatureName}Service
{
    public async Task<{FeatureName}Dto> ClaimSlotAsync(
        int slotId, int userId, CancellationToken ct = default)
    {
        var lockKey = $"lock:{entities}:slot:{slotId}";

        await using var lockHandle = await _lockService.TryAcquireLockAsync(
            lockKey, TimeSpan.FromSeconds(10), ct)
            ?? throw new ConflictException("Slot is being claimed by another user");

        // Inside the lock — safe from concurrent access
        var slot = await _repo.GetByIdAsync(slotId, ct)
            ?? throw new NotFoundException("{FeatureName}", slotId);

        if (slot.ClaimedByUserId != null)
            throw new ConflictException("Slot already claimed");

        slot.ClaimedByUserId = userId;
        slot.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(slot, ct);
        await _repo.SaveChangesAsync(ct);

        return MapToDto(slot);
    }
}
```

---

## Lock Key Conventions

| Pattern | Key Format | Scope |
|---------|-----------|-------|
| Single entity write | `lock:{entities}:{id}` | One record |
| Slot/assignment claim | `lock:{entities}:slot:{slotId}` | One slot |
| Inventory adjustment | `lock:inventory:{productId}:{warehouseId}` | One SKU per location |
| Sequential number | `lock:{entities}:sequence` | Global counter |
| Approval workflow | `lock:approval:{entityType}:{entityId}` | One approval chain |

---

## Timeout and Retry Guidelines

| Scenario | Lock Expiry | Retry |
|----------|------------|-------|
| Quick status toggle | 5s | No retry — fail fast |
| Inventory decrement | 10s | 1 retry after 500ms |
| Sequential number gen | 3s | 2 retries after 200ms |
| Long-running calculation | 30s | No retry — queue instead |

---

## Optimistic Concurrency Alternative

For simpler cases where locking is overkill, use EF Core's concurrency token:

```csharp
// Entity
[ConcurrencyCheck]
public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

// DbContext
modelBuilder.Entity<{FeatureName}>()
    .Property(x => x.UpdatedAt)
    .IsConcurrencyToken();
```

EF Core will throw `DbUpdateConcurrencyException` if the row was modified between read and write. Catch and return a 409 Conflict.

---

## Testing Locked Operations

```csharp
// Mock: lock acquired
_lockServiceMock.Setup(l => l.TryAcquireLockAsync(
    It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(Mock.Of<IAsyncDisposable>());

// Mock: lock contention (returns null)
_lockServiceMock.Setup(l => l.TryAcquireLockAsync(
    It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync((IAsyncDisposable?)null);
```
