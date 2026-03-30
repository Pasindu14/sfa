# Reference: Auditing Patterns

Load this reference when the feature needs compliance auditing beyond the standard `CreatedBy`/`UpdatedBy`/`UpdatedAt` fields.

---

## Standard Audit (Built Into Every Entity)

Every entity already has:

```csharp
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
public int? CreatedBy { get; set; }
public int? UpdatedBy { get; set; }
public bool IsActive { get; set; } = true;    // universal soft-delete / deactivation flag
public bool IsDeleted { get; set; } = false;   // audit flag — set by DELETE endpoint
```

The `AuditInterceptor` in the SFA API automatically populates these on `SaveChangesAsync`. This reference covers **enhanced auditing** for regulated or sensitive entities.

---

## When to Use Enhanced Auditing

| Scenario | Standard Audit | Enhanced Audit |
|----------|---------------|----------------|
| Normal CRUD (products, categories) | Yes | No |
| Financial data (invoices, payments) | Yes | Yes — field-level changes |
| Approval workflows | Yes | Yes — state transitions |
| User role/permission changes | Yes | Yes — security audit |
| Inventory adjustments | Yes | Yes — quantity deltas |
| Data exports/downloads | N/A | Yes — access logging |

---

## Field-Level Change Tracking

For entities where you need to know exactly which fields changed:

### AuditLog Entity

```csharp
namespace sfa_api.Features.AuditLog.Entities;

public class AuditLogEntry
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;  // Create, Update, Delete, StatusChange
    public string? ChangedFields { get; set; }           // JSON: {"Price": {"Old": 10, "New": 15}}
    public int? PerformedByUserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
```

### Service Integration

```csharp
public async Task<{FeatureName}Dto> UpdateAsync(
    int id, Update{FeatureName}Request request, int? callerId, CancellationToken ct = default)
{
    var entity = await _repo.GetByIdAsync(id, ct)
        ?? throw new NotFoundException("{FeatureName}", id);

    // Capture changes before applying
    var changes = new Dictionary<string, object>();

    if (entity.Name != request.Name)
        changes["Name"] = new { Old = entity.Name, New = request.Name };
    if (entity.Price != request.Price)
        changes["Price"] = new { Old = entity.Price, New = request.Price };

    // Apply updates
    entity.Name = request.Name;
    entity.Price = request.Price;
    entity.UpdatedBy = callerId;
    entity.UpdatedAt = DateTime.UtcNow;

    await _repo.UpdateAsync(entity, ct);
    await _repo.SaveChangesAsync(ct);

    // Log audit entry
    if (changes.Count > 0)
    {
        await _auditService.LogAsync(new AuditLogEntry
        {
            EntityType = "{FeatureName}",
            EntityId = id,
            Action = "Update",
            ChangedFields = JsonSerializer.Serialize(changes),
            PerformedByUserId = callerId,
        }, ct);
    }

    return MapToDto(entity);
}
```

---

## State Transition Auditing

For approval workflows and status changes:

```csharp
public async Task<{FeatureName}Dto> ChangeStatusAsync(
    int id, {FeatureName}Status newStatus, int? callerId, string? reason, CancellationToken ct = default)
{
    var entity = await _repo.GetByIdAsync(id, ct)
        ?? throw new NotFoundException("{FeatureName}", id);

    var oldStatus = entity.Status;

    // Validate transition
    if (!IsValidTransition(oldStatus, newStatus))
        throw new BusinessRuleException(
            $"Cannot transition from {oldStatus} to {newStatus}");

    // Apply via ExecuteUpdateAsync for efficiency
    await _context.{Entities}
        .Where(x => x.Id == id)
        .ExecuteUpdateAsync(s => s
            .SetProperty(x => x.Status, newStatus)
            .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
            .SetProperty(x => x.UpdatedBy, callerId), ct);

    // Audit the transition
    await _auditService.LogAsync(new AuditLogEntry
    {
        EntityType = "{FeatureName}",
        EntityId = id,
        Action = "StatusChange",
        ChangedFields = JsonSerializer.Serialize(new
        {
            Status = new { Old = oldStatus.ToString(), New = newStatus.ToString() },
            Reason = reason
        }),
        PerformedByUserId = callerId,
    }, ct);

    return await GetByIdAsync(id, ct);
}
```

---

## Access Auditing

For sensitive data access (exports, bulk reads, PII):

```csharp
[HttpGet("export")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Export(CancellationToken ct)
{
    int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);

    await _auditService.LogAsync(new AuditLogEntry
    {
        EntityType = "{FeatureName}",
        EntityId = 0,  // bulk operation
        Action = "Export",
        PerformedByUserId = callerId,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
    }, ct);

    var data = await _service.ExportAllAsync(ct);
    return File(data, "text/csv", "{entities}_export.csv");
}
```

---

## DbContext for AuditLog

```csharp
modelBuilder.Entity<AuditLogEntry>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).UseIdentityColumn();

    // High-growth entity — index by entity type + date
    e.HasIndex(x => new { x.EntityType, x.PerformedAt })
     .IsDescending(false, true)
     .HasDatabaseName("idx_auditlog_entity_performed");

    e.HasIndex(x => new { x.PerformedByUserId, x.PerformedAt })
     .IsDescending(false, true)
     .HasDatabaseName("idx_auditlog_user_performed");
});

// TODO (PRODUCTION): RANGE-partition "AuditLogEntries" by PerformedAt.
```
