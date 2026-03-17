# Sales Order — Add Distributor Rejection Acknowledgement

## Context
The SalesOrder feature is fully implemented. Currently when a Rep or Manager rejects an order it transitions **directly to `Cancelled`** — the Distributor is never notified and cannot confirm they've seen the rejection.

The requirement is: after a Rep or Manager rejects, the order must enter a **`PendingDistributorAcknowledgement`** holding state. The Distributor must then explicitly acknowledge the rejection before the order becomes `Cancelled`. Admin can also acknowledge on the Distributor's behalf.

---

## Updated Status Flow

```
                  submit                rep-approve
   Draft ─────────────────► PendingRepApproval ─────────────────► PendingManagerApproval
     │                              │                                       │
  cancel (Distributor/Admin)      reject                                  reject
                                    │                                       │
                                    ▼                                       ▼
                                PendingDistributorAcknowledgement ◄─────────
                                    │
                              acknowledge (Distributor/Admin)
                                    │
                                    ▼
                                Cancelled
                                                        ──────────────────────────
                               (approval path continues unchanged)
   PendingManagerApproval ──approve──► PendingDistributorFinalization ──finalize──► Finalized
```

| Status                               | Who can edit items | Who can transition                          |
|--------------------------------------|--------------------|---------------------------------------------|
| `Draft`                              | Distributor, Admin | Distributor submits; Distributor/Admin cancels |
| `PendingRepApproval`                 | SalesRep, Admin    | SalesRep approves or rejects; Admin same    |
| `PendingManagerApproval`             | Manager, Admin     | Manager approves or rejects; Admin same     |
| `PendingDistributorAcknowledgement`  | **Nobody**         | Distributor/Admin acknowledges → Cancelled; Admin cancels |
| `PendingDistributorFinalization`     | Admin only         | Distributor finalizes; Admin same           |
| `Finalized`                          | Immutable          | —                                           |
| `Cancelled`                          | Immutable          | —                                           |

---

## Files to Change (surgical edits only — no new files needed)

| File | Change |
|------|--------|
| `Features/SalesOrders/Enums/SalesOrderStatus.cs` | Add `PendingDistributorAcknowledgement = 6` |
| `Features/SalesOrders/Entities/SalesOrder.cs` | Add `AcknowledgedBy`, `AcknowledgedAt` fields |
| `Features/SalesOrders/DTOs/SalesOrderDto.cs` | Add `AcknowledgedBy`, `AcknowledgedAt` params |
| `Features/SalesOrders/Services/ISalesOrderService.cs` | Add `AcknowledgeAsync` signature |
| `Features/SalesOrders/Services/SalesOrderService.cs` | Update `RejectAsync` (→ `PendingDistributorAcknowledgement`), add `AcknowledgeAsync`, update `MapToDto` |
| `Features/SalesOrders/Controllers/SalesOrdersController.cs` | Add `POST /{id}/acknowledge` endpoint |
| `Infrastructure/Persistence/AppDbContext.cs` | No change — EF picks up new entity fields automatically |
| New EF migration | `dotnet ef migrations add AddSalesOrderAcknowledgement` |
| Unit tests | Update `RejectAsync` tests (new target status); add `AcknowledgeAsync` tests |
| Integration tests | Update rejection test; add acknowledge test |

---

## Detailed Implementation

### 1. Enum — `SalesOrderStatus.cs`
```csharp
public enum SalesOrderStatus
{
    Draft = 0,
    PendingRepApproval = 1,
    PendingManagerApproval = 2,
    PendingDistributorFinalization = 3,
    Finalized = 4,
    Cancelled = 5,
    PendingDistributorAcknowledgement = 6   // ← NEW
}
```

### 2. Entity — `SalesOrder.cs`
Add two nullable fields after the existing `CancelledAt`/`CancelReason` block:
```csharp
public int? AcknowledgedBy { get; set; }
public DateTime? AcknowledgedAt { get; set; }
```

### 3. DTO — `SalesOrderDto.cs`
Add two params at the end of the record before `IsActive`:
```csharp
int? AcknowledgedBy,
DateTime? AcknowledgedAt,
```

### 4. Service Interface — `ISalesOrderService.cs`
Add:
```csharp
Task<SalesOrderDto> AcknowledgeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
```

### 5. Service Implementation — `SalesOrderService.cs`

**Update `RejectAsync`** — change the target status from `Cancelled` to `PendingDistributorAcknowledgement`:
```csharp
// OLD:
order.Status = SalesOrderStatus.Cancelled;
order.CancelledBy = callerId;
order.CancelledAt = DateTime.UtcNow;
order.CancelReason = request.Reason;
// History ToStatus = SalesOrderStatus.Cancelled, Action = "Rejected"

// NEW:
order.Status = SalesOrderStatus.PendingDistributorAcknowledgement;
order.CancelReason = request.Reason;           // store reason for Distributor to see
// Do NOT set CancelledBy/CancelledAt here — set them in AcknowledgeAsync
// History ToStatus = SalesOrderStatus.PendingDistributorAcknowledgement, Action = "Rejected"
```

**Add `AcknowledgeAsync`**:
```csharp
public async Task<SalesOrderDto> AcknowledgeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
{
    if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
        throw new AuthorizationException("acknowledge sales order rejections");

    var order = await _repo.GetByIdWithItemsAsync(id, ct)
        ?? throw new NotFoundException("SalesOrder", id);

    if (order.Status != SalesOrderStatus.PendingDistributorAcknowledgement)
        throw new BusinessRuleException("ORDER_NOT_PENDING_ACKNOWLEDGEMENT",
            "Order is not pending Distributor acknowledgement.");

    // Distributor isolation — must be their own order
    if (callerRole == UserRole.Distributor)
    {
        var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
            ?? throw new NotFoundException("User", callerId);
        if (order.DistributorId != caller.DistributorId)
            throw new AuthorizationException("this sales order");
    }

    var fromStatus = order.Status;
    order.Status = SalesOrderStatus.Cancelled;
    order.AcknowledgedBy = callerId;
    order.AcknowledgedAt = DateTime.UtcNow;
    order.CancelledBy = callerId;
    order.CancelledAt = DateTime.UtcNow;
    order.UpdatedBy = callerId;
    order.UpdatedAt = DateTime.UtcNow;

    await _repo.UpdateAsync(order, ct);
    await _repo.AddHistoryAsync(new SalesOrderHistory
    {
        SalesOrderId = id,
        Action = "RejectionAcknowledged",
        FromStatus = fromStatus,
        ToStatus = SalesOrderStatus.Cancelled,
        PerformedBy = callerId,
        PerformedAt = DateTime.UtcNow
    }, ct);

    await _repo.SaveChangesAsync(ct);
    var updated = await _repo.GetByIdWithItemsAsync(id, ct);
    return MapToDto(updated!);
}
```

**Update `MapToDto`** — add the two new fields to the `SalesOrderDto` constructor call:
```csharp
AcknowledgedBy: o.AcknowledgedBy,
AcknowledgedAt: o.AcknowledgedAt,
```

**Update `CancelAsync`** — Admin cancelling a `PendingDistributorAcknowledgement` order is fine (already handled because only `Finalized` and `Cancelled` are blocked). No change needed.

**Update `UpdateAsync` edit-rights** — `PendingDistributorAcknowledgement` is NOT in any role's allowed list, so `ORDER_NOT_EDITABLE` is correctly thrown. No change needed.

### 6. Controller — `SalesOrdersController.cs`
Add new endpoint after the existing `Reject` action:
```csharp
/// <summary>
/// POST /api/v1/sales-orders/{id}/acknowledge
/// Distributor, Admin — acknowledges a rejected order → Cancelled
/// </summary>
[HttpPost("{id}/acknowledge")]
[Authorize(Roles = "Distributor,Admin")]
public async Task<IActionResult> Acknowledge(int id, CancellationToken ct)
{
    var (callerId, callerRole) = GetCallerInfo();
    var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
    var result = await _salesOrderService.AcknowledgeAsync(id, callerId, callerRole, ct);
    return Ok(ResponseHelper.Ok(result, correlationId));
}
```

### 7. History Action Added
- `RejectionAcknowledged` — Distributor/Admin acknowledges → Cancelled

---

## New EF Migration
```bash
dotnet ef migrations add AddSalesOrderAcknowledgement --project "d:\Github\sfa\sfa_api\sfa_api\sfa_api.csproj"
```
This will add `AcknowledgedBy (int?)` and `AcknowledgedAt (datetime?)` columns to `SalesOrders` table. The new enum value `6` requires no schema change (stored as `int`).

---

## Change History Tracking (updated)

All existing history actions are unchanged. The updated actions for the rejection path are:

| Action | When |
|--------|------|
| `Rejected` | Rep or Manager rejects → `PendingDistributorAcknowledgement` (includes reason) |
| `RejectionAcknowledged` | Distributor/Admin acknowledges → `Cancelled` |

---

## API Endpoints (updated table)

| Method | Route | Roles | Status Gate |
|--------|-------|-------|-------------|
| `POST` | `/api/v1/sales-orders/{id}/reject` | SalesRep/Manager/Admin | → `PendingDistributorAcknowledgement` (**changed**) |
| `POST` | `/api/v1/sales-orders/{id}/acknowledge` | Distributor, Admin | `PendingDistributorAcknowledgement` → `Cancelled` (**new**) |

All other endpoints unchanged.

---

## Tests to Update

**Unit tests** (`sfa_api.UnitTests/Features/SalesOrders/Services/SalesOrderServiceTests.cs`):
- Update `RejectAsync` tests: expected `ToStatus` changes from `Cancelled` → `PendingDistributorAcknowledgement`
- Add new `AcknowledgeAsync` tests:
  - Distributor on `PendingDistributorAcknowledgement` (own order) → `Cancelled`
  - Admin on `PendingDistributorAcknowledgement` → `Cancelled`
  - Distributor on wrong status → `BusinessRuleException`
  - SalesRep role → `AuthorizationException`
  - Distributor on another distributor's order → `AuthorizationException`

**Integration tests** (`sfa_api.IntegrationTests/Features/SalesOrders/SalesOrdersApiTests.cs`):
- Update the "reject" test: status after reject = `PendingDistributorAcknowledgement` (not `Cancelled`)
- Add new test: `POST /{id}/acknowledge` → status = `Cancelled`

---

## Implementation Order

1. `SalesOrderStatus.cs` — add enum value `6`
2. `SalesOrder.cs` — add `AcknowledgedBy`, `AcknowledgedAt`
3. `SalesOrderDto.cs` — add two new record params
4. `ISalesOrderService.cs` — add `AcknowledgeAsync`
5. `SalesOrderService.cs` — update `RejectAsync` + add `AcknowledgeAsync` + update `MapToDto`
6. `SalesOrdersController.cs` — add `acknowledge` endpoint
7. `dotnet ef migrations add AddSalesOrderAcknowledgement`
8. `dotnet build` — verify zero errors
9. Update unit + integration tests

---

## Verification

1. `dotnet build` → zero errors
2. `dotnet test --filter SalesOrder` → all tests pass
3. Manual flow:
   - Submit → SalesRep rejects → status = `PendingDistributorAcknowledgement`
   - SalesRep tries to reject again → `422 ORDER_NOT_REJECTABLE`
   - Distributor acknowledges → status = `Cancelled`
   - History shows `Rejected` then `RejectionAcknowledged` entries
