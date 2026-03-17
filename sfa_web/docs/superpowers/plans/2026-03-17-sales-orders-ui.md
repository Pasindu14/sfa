# Sales Orders UI Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the full Sales Orders web UI — list, create, edit, and detail pages — with role-based workflow actions and a multi-step approval state machine.

**Architecture:** Backend prerequisites (4 targeted API changes) are done first in `sfa_api`, then the Next.js feature is scaffolded following the 7-layer pattern (types → schema → store → actions → hooks → components → routes). The detail page uses a two-column layout with a sticky right sidebar for status + workflow actions, matching the Shopify/Jira order management pattern.

**Tech Stack:** .NET 8 ASP.NET Core (backend), Next.js 16 App Router, shadcn/ui, TanStack Query v5, Zustand v5, Zod, react-hook-form + useFieldArray, date-fns

**Spec:** `sfa_web/docs/superpowers/specs/2026-03-17-sales-orders-ui-design.md`

---

## Chunk 1: Backend Prerequisites

### Task 1: Add `GET /api/v1/pricing-structures/default` endpoint

**Files:**
- Modify: `sfa_api/sfa_api/Features/PricingStructures/Services/IPricingStructureService.cs`
- Modify: `sfa_api/sfa_api/Features/PricingStructures/Services/PricingStructureService.cs`
- Modify: `sfa_api/sfa_api/Features/PricingStructures/Controllers/PricingStructuresController.cs`

- [ ] **Step 1: Add `GetDefaultAsync` to the interface**

In `IPricingStructureService.cs`, add after `GetByIdAsync`:
```csharp
Task<PricingStructureDetailDto> GetDefaultAsync(CancellationToken ct = default);
```

- [ ] **Step 2: Implement `GetDefaultAsync` in the service**

In `PricingStructureService.cs`, add the method after `GetByIdAsync`. The repository already has `GetCurrentDefaultAsync` (returns a bare entity without items) and `GetByIdWithItemsAsync` (includes items). Chain them:

```csharp
public async Task<PricingStructureDetailDto> GetDefaultAsync(CancellationToken ct = default)
{
    var structure = await _repo.GetCurrentDefaultAsync(ct)
        ?? throw new NotFoundException("PricingStructure", "default");
    var withItems = await _repo.GetByIdWithItemsAsync(structure.Id, ct)
        ?? throw new NotFoundException("PricingStructure", structure.Id);
    return MapToDetailDto(withItems);
}
```

- [ ] **Step 3: Add the controller endpoint**

In `PricingStructuresController.cs`, add before the existing `GetPricingStructureById` action:
```csharp
/// <summary>
/// GET /api/v1/pricing-structures/default
/// All authenticated roles — used by Create Order product selector
/// </summary>
[HttpGet("default")]
[Authorize]
public async Task<IActionResult> GetDefaultPricingStructure(CancellationToken ct)
{
    var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
    var result = await _pricingStructureService.GetDefaultAsync(ct);
    return Ok(ResponseHelper.Ok(result, correlationId));
}
```

- [ ] **Step 4: Build and verify**

```bash
dotnet build "d:/Github/sfa/sfa_api/sfa_api/sfa_api.csproj"
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
cd "d:/Github/sfa"
git add sfa_api/
git commit -m "add GET /api/v1/pricing-structures/default endpoint for all roles"
```

---

### Task 2: Add `SubmittedAt` to `SalesOrderSummaryDto`

**Files:**
- Modify: `sfa_api/sfa_api/Features/SalesOrders/DTOs/SalesOrderSummaryDto.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Repositories/SalesOrderRepository.cs`

- [ ] **Step 1: Add the field to the DTO**

In `SalesOrderSummaryDto.cs`, add `DateTime? SubmittedAt` after `UpdatedAt`:
```csharp
public record SalesOrderSummaryDto(
    int Id,
    string OrderNumber,
    int DistributorId,
    string DistributorName,
    SalesOrderStatus Status,
    string StatusLabel,
    decimal TotalAmount,
    int ItemCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt
);
```

- [ ] **Step 2: Populate it in `MapToSummaryDto` in the service**

`SalesOrderSummaryDto` is constructed by `MapToSummaryDto` in `SalesOrderService.cs` (line ~564), NOT in the repository. Open `SalesOrderService.cs` and add `SubmittedAt: o.SubmittedAt` to the constructor call inside `MapToSummaryDto`:
```csharp
private static SalesOrderSummaryDto MapToSummaryDto(SalesOrder o)
{
    var total = o.Items?.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount / 100)) ?? 0m;
    return new SalesOrderSummaryDto(
        Id: o.Id,
        OrderNumber: o.OrderNumber,
        DistributorId: o.DistributorId,
        DistributorName: o.Distributor?.Name ?? string.Empty,
        Status: o.Status,
        StatusLabel: o.Status.ToString(),
        TotalAmount: decimal.Round(total, 2),
        ItemCount: o.Items?.Count ?? 0,
        IsActive: o.IsActive,
        CreatedAt: o.CreatedAt,
        UpdatedAt: o.UpdatedAt,
        SubmittedAt: o.SubmittedAt
    );
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build "d:/Github/sfa/sfa_api/sfa_api/sfa_api.csproj"
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add sfa_api/
git commit -m "add SubmittedAt to SalesOrderSummaryDto"
```

---

### Task 3: Add `SalesOrderHistoryDto` and `History` to `SalesOrderDto`

**Files:**
- Create: `sfa_api/sfa_api/Features/SalesOrders/DTOs/SalesOrderHistoryDto.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/DTOs/SalesOrderDto.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Repositories/ISalesOrderRepository.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Repositories/SalesOrderRepository.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Services/SalesOrderService.cs`

- [ ] **Step 1: Create `SalesOrderHistoryDto.cs`**

```csharp
using sfa_api.Features.SalesOrders.Enums;

namespace sfa_api.Features.SalesOrders.DTOs;

public record SalesOrderHistoryDto(
    int Id,
    string Action,
    SalesOrderStatus? FromStatus,
    SalesOrderStatus? ToStatus,
    int PerformedBy,
    string? PerformedByName,
    DateTime PerformedAt,
    string? Notes
);
```

- [ ] **Step 2: Add `History` to `SalesOrderDto`**

In `SalesOrderDto.cs`, add `IEnumerable<SalesOrderHistoryDto> History` after the `Items` field:
```csharp
IEnumerable<SalesOrderItemDto> Items,
IEnumerable<SalesOrderHistoryDto> History,
decimal TotalAmount,
```

- [ ] **Step 3: Add `GetHistoryAsync` to the repository interface**

In `ISalesOrderRepository.cs`, add:
```csharp
Task<IEnumerable<SalesOrderHistory>> GetHistoryAsync(int salesOrderId, CancellationToken ct = default);
```

- [ ] **Step 4: Implement `GetHistoryAsync` in the repository**

`GetHistoryAsync` belongs in `SalesOrderRepository.cs` (which has `_context`), NOT in the service. Add to `SalesOrderRepository.cs`:
```csharp
public async Task<IEnumerable<SalesOrderHistory>> GetHistoryAsync(int salesOrderId, CancellationToken ct = default)
{
    return await _context.SalesOrderHistories
        .Where(h => h.SalesOrderId == salesOrderId)
        .OrderBy(h => h.PerformedAt)
        .ToListAsync(ct);
}
```

- [ ] **Step 5: Update `MapToDto` in `SalesOrderService.cs`**

`MapToDto` is currently `private static SalesOrderDto MapToDto(SalesOrder o)`. Change it to accept optional history and performers (add `History` to the DTO constructor call):

```csharp
private static SalesOrderDto MapToDto(
    SalesOrder o,
    IEnumerable<SalesOrderHistory>? history = null,
    Dictionary<int, string?>? performers = null)
```

In the `SalesOrderDto(...)` constructor call inside `MapToDto`, add after `Items:`:
```csharp
History: (history ?? Enumerable.Empty<SalesOrderHistory>()).Select(h => new SalesOrderHistoryDto(
    h.Id,
    h.Action,
    h.FromStatus,
    h.ToStatus,
    h.PerformedBy,
    performers?.GetValueOrDefault(h.PerformedBy),
    h.PerformedAt,
    h.Notes
)),
```

**All `MapToDto` call sites** — there are ~10 callers: `CreateAsync`, `UpdateAsync`, `SubmitAsync`, `RepApproveAsync`, `ApproveAsync`, `RejectAsync`, `AcknowledgeAsync`, `FinalizeAsync`, `CancelAsync` (each calls `MapToDto(updated!)` at the end), plus `GetByIdAsync`. Only `GetByIdAsync` needs to load and pass history. All other callers pass no arguments — the `null` default produces an empty `History` collection in the DTO, which is correct since the list page never shows history.

Update `GetByIdAsync` to load history before calling `MapToDto`:
```csharp
public async Task<SalesOrderDto> GetByIdAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
{
    var order = await _repo.GetByIdWithItemsAsync(id, ct)
        ?? throw new NotFoundException("SalesOrder", id);

    if (callerRole == UserRole.Distributor)
    {
        var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
            ?? throw new NotFoundException("User", callerId);
        if (order.DistributorId != caller.DistributorId)
            throw new AuthorizationException("this sales order");
    }

    // Load history and resolve performer names
    var history = await _repo.GetHistoryAsync(id, ct);
    var performerIds = history.Select(h => h.PerformedBy).Distinct().ToList();
    var performers = new Dictionary<int, string?>();
    foreach (var pid in performerIds)
    {
        var user = await _userRepo.GetUserByIdAsync(pid, ct);
        performers[pid] = user?.Name;
    }

    return MapToDto(order, history, performers);
}
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build "d:/Github/sfa/sfa_api/sfa_api/sfa_api.csproj"
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add sfa_api/
git commit -m "add SalesOrderHistoryDto and history to SalesOrderDto detail response"
```

---

### Task 4: Add `fromDate`/`toDate` filter to `GET /api/v1/sales-orders`

**Files:**
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Controllers/SalesOrdersController.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Services/ISalesOrderService.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Services/SalesOrderService.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Repositories/ISalesOrderRepository.cs`
- Modify: `sfa_api/sfa_api/Features/SalesOrders/Repositories/SalesOrderRepository.cs`

- [ ] **Step 1: Add params to controller `GetAll`**

In `SalesOrdersController.cs`, update the `GetAll` signature:
```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null,
    [FromQuery] string? status = null,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    CancellationToken ct = default)
```

Pass `fromDate` and `toDate` through to the service call:
```csharp
var result = await _salesOrderService.GetAllAsync(
    page, pageSize, search, statusFilter, fromDate, toDate, callerId, callerRole, ct);
```

- [ ] **Step 2: Update `ISalesOrderService.GetAllAsync`**

```csharp
Task<SalesOrderListDto> GetAllAsync(
    int page, int pageSize, string? search,
    SalesOrderStatus? status,
    DateTime? fromDate, DateTime? toDate,
    int callerId, UserRole callerRole,
    CancellationToken ct = default);
```

- [ ] **Step 3: Update `SalesOrderService.GetAllAsync`**

Add `DateTime? fromDate, DateTime? toDate` parameters and pass them through to `_repo.GetAllAsync`. The current method signature at line ~39 is:
```csharp
public async Task<SalesOrderListDto> GetAllAsync(
    int page, int pageSize, string? search,
    SalesOrderStatus? status,
    int callerId, UserRole callerRole,
    CancellationToken ct = default)
```
Update it to:
```csharp
public async Task<SalesOrderListDto> GetAllAsync(
    int page, int pageSize, string? search,
    SalesOrderStatus? status,
    DateTime? fromDate, DateTime? toDate,
    int callerId, UserRole callerRole,
    CancellationToken ct = default)
{
    var skip = (page - 1) * pageSize;

    int? distributorFilter = null;
    if (callerRole == UserRole.Distributor)
    {
        var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
            ?? throw new NotFoundException("User", callerId);
        distributorFilter = caller.DistributorId;
    }

    var (orders, totalCount) = await _repo.GetAllAsync(
        skip, pageSize, search, status, distributorFilter, fromDate, toDate, ct);

    return new SalesOrderListDto(
        SalesOrders: orders.Select(MapToSummaryDto),
        TotalCount: totalCount,
        Page: page,
        PageSize: pageSize
    );
}
```

- [ ] **Step 4: Update `ISalesOrderRepository.GetAllAsync`**

```csharp
Task<(IEnumerable<SalesOrder> SalesOrders, int TotalCount)> GetAllAsync(
    int skip,
    int take,
    string? search = null,
    SalesOrderStatus? status = null,
    int? distributorId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    CancellationToken ct = default);
```

- [ ] **Step 5: Implement date filtering in `SalesOrderRepository.GetAllAsync`**

After existing filters, add:
```csharp
if (fromDate.HasValue)
    query = query.Where(o => o.CreatedAt >= fromDate.Value);
if (toDate.HasValue)
    query = query.Where(o => o.CreatedAt < toDate.Value.Date.AddDays(1));
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build "d:/Github/sfa/sfa_api/sfa_api/sfa_api.csproj"
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Run existing tests to confirm no regressions**

```bash
dotnet test "d:/Github/sfa/sfa_api/sfa_api.sln" --filter "SalesOrder"
```
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add sfa_api/
git commit -m "add fromDate/toDate filter params to GET /api/v1/sales-orders"
```

---

## Chunk 2: Frontend Foundation

### Task 5: Types

**Files:**
- Create: `sfa_web/features/sales-order/types/sales-order.types.ts`

- [ ] **Step 1: Create the types file**

```ts
export type SalesOrderStatus =
  | 0  // Draft
  | 1  // PendingRepApproval
  | 2  // PendingManagerApproval
  | 3  // PendingDistributorFinalization
  | 4  // Finalized
  | 5  // Cancelled
  | 6  // PendingDistributorAcknowledgement

export type SalesOrderItemDto = {
  id: number
  productId: number
  productCode: string
  productDescription: string
  quantity: number
  unitPrice: number
  discount: number
  lineTotal: number
}

export type SalesOrderHistoryDto = {
  id: number
  action: string
  fromStatus: SalesOrderStatus | null
  toStatus: SalesOrderStatus | null
  performedBy: number
  performedByName: string | null
  performedAt: string
  notes: string | null
}

export type SalesOrderDto = {
  id: number
  orderNumber: string
  distributorId: number
  distributorName: string
  status: SalesOrderStatus
  statusLabel: string
  notes: string | null
  items: SalesOrderItemDto[]
  totalAmount: number
  history: SalesOrderHistoryDto[]
  submittedBy: number | null
  submittedAt: string | null
  repApprovedBy: number | null
  repApprovedAt: string | null
  managerApprovedBy: number | null
  managerApprovedAt: string | null
  finalizedBy: number | null
  finalizedAt: string | null
  cancelledBy: number | null
  cancelledAt: string | null
  cancelReason: string | null
  acknowledgedBy: number | null
  acknowledgedAt: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
  createdBy: number | null
  updatedBy: number | null
}

export type SalesOrderSummaryDto = {
  id: number
  orderNumber: string
  distributorId: number
  distributorName: string
  status: SalesOrderStatus
  statusLabel: string
  totalAmount: number
  itemCount: number
  isActive: boolean
  createdAt: string
  updatedAt: string
  submittedAt: string | null
}

export type SalesOrderListDto = {
  salesOrders: SalesOrderSummaryDto[]
  totalCount: number
  page: number
  pageSize: number
}

export type PricingStructureItemDto = {
  id: number
  pricingStructureId: number
  productId: number
  productCode: string
  productItemDescription: string
  dealerPackPrice: number | null
  dealerCasePrice: number | null
  promotionalPrice: number | null
}

export type DefaultPricingStructureDto = {
  id: number
  name: string
  description: string | null
  isDefault: boolean
  isActive: boolean
  itemCount: number
  items: PricingStructureItemDto[]
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd "d:/Github/sfa/sfa_web" && npx tsc --noEmit 2>&1 | head -20
```
Expected: No errors in the new file.

---

### Task 6: Zod Schema

**Files:**
- Create: `sfa_web/features/sales-order/schema/sales-order.schema.ts`

- [ ] **Step 1: Create the schema file**

```ts
import { z } from 'zod'

// Create order
// distributorId: null is valid for Distributor role (server resolves from JWT).
// Admin role: UI enforces non-null via required <Select> — schema permits null to avoid role-aware logic here.
export const createSalesOrderSchema = z.object({
  distributorId: z.number().int().positive().nullable(),
  notes: z.string().max(1000).nullable().optional(),
  items: z.array(z.object({
    productId: z.number().int().positive(),
    quantity: z.number().int().min(1, 'Quantity must be at least 1'),
    unitPrice: z.number().min(0),
    discount: z.literal(0),
  })).min(1, 'At least one item is required'),
})
export type CreateSalesOrderInput = z.infer<typeof createSalesOrderSchema>

// Update order (Draft only)
export const updateSalesOrderSchema = z.object({
  notes: z.string().max(1000).nullable().optional(),
  items: z.array(z.object({
    productId: z.number().int().positive(),
    quantity: z.number().int().min(1, 'Quantity must be at least 1'),
    unitPrice: z.number().min(0),
    discount: z.literal(0),
  })).min(1, 'At least one item is required'),
})
export type UpdateSalesOrderInput = z.infer<typeof updateSalesOrderSchema>

// Reject / Cancel reason
export const rejectSalesOrderSchema = z.object({
  reason: z.string()
    .min(5, 'Reason must be at least 5 characters')
    .max(500, 'Reason must not exceed 500 characters'),
})
export type RejectSalesOrderInput = z.infer<typeof rejectSalesOrderSchema>
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd "d:/Github/sfa/sfa_web" && npx tsc --noEmit 2>&1 | head -20
```
Expected: No errors.

---

### Task 7: Stores

**Files:**
- Create: `sfa_web/features/sales-order/store/sales-order.dialog-store.ts`
- Create: `sfa_web/features/sales-order/store/sales-order.filter-store.ts`
- Create: `sfa_web/features/sales-order/store/index.ts`

- [ ] **Step 1: Create the dialog store**

```ts
// sfa_web/features/sales-order/store/sales-order.dialog-store.ts
import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface SalesOrderDialogState {
  selectedOrderId: number | null
  isSubmitOpen: boolean
  isRepApproveOpen: boolean
  isApproveOpen: boolean
  isAcknowledgeOpen: boolean
  isFinalizeOpen: boolean
  openSubmit: (id: number) => void
  closeSubmit: () => void
  openRepApprove: (id: number) => void
  closeRepApprove: () => void
  openApprove: (id: number) => void
  closeApprove: () => void
  openAcknowledge: (id: number) => void
  closeAcknowledge: () => void
  openFinalize: (id: number) => void
  closeFinalize: () => void
}

export const useSalesOrderDialogStore = create<SalesOrderDialogState>()(
  devtools(
    (set) => ({
      selectedOrderId: null,
      isSubmitOpen: false,
      isRepApproveOpen: false,
      isApproveOpen: false,
      isAcknowledgeOpen: false,
      isFinalizeOpen: false,
      openSubmit: (id) => set({ isSubmitOpen: true, selectedOrderId: id }),
      closeSubmit: () => set({ isSubmitOpen: false, selectedOrderId: null }),
      openRepApprove: (id) => set({ isRepApproveOpen: true, selectedOrderId: id }),
      closeRepApprove: () => set({ isRepApproveOpen: false, selectedOrderId: null }),
      openApprove: (id) => set({ isApproveOpen: true, selectedOrderId: id }),
      closeApprove: () => set({ isApproveOpen: false, selectedOrderId: null }),
      openAcknowledge: (id) => set({ isAcknowledgeOpen: true, selectedOrderId: id }),
      closeAcknowledge: () => set({ isAcknowledgeOpen: false, selectedOrderId: null }),
      openFinalize: (id) => set({ isFinalizeOpen: true, selectedOrderId: id }),
      closeFinalize: () => set({ isFinalizeOpen: false, selectedOrderId: null }),
    }),
    { name: 'SalesOrderDialogStore' }
  )
)
```

- [ ] **Step 2: Create the filter store**

```ts
// sfa_web/features/sales-order/store/sales-order.filter-store.ts
import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { format } from 'date-fns'

const today = () => format(new Date(), 'yyyy-MM-dd')

interface SalesOrderFilterState {
  page: number
  pageSize: number
  search: string
  status: string
  fromDate: string
  toDate: string
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSearch: (search: string) => void
  setStatus: (status: string) => void
  setFromDate: (date: string) => void
  setToDate: (date: string) => void
  resetFilters: () => void
}

const getDefaultState = () => ({
  page: 1,
  pageSize: 10,
  search: '',
  status: '',
  fromDate: today(),
  toDate: today(),
})

export const useSalesOrderFilterStore = create<SalesOrderFilterState>()(
  devtools(
    (set) => ({
      ...getDefaultState(),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSearch: (search) => set({ search, page: 1 }),
      setStatus: (status) => set({ status, page: 1 }),
      setFromDate: (fromDate) => set({ fromDate, page: 1 }),
      setToDate: (toDate) => set({ toDate, page: 1 }),
      resetFilters: () => set(getDefaultState()),
    }),
    { name: 'SalesOrderFilterStore' }
  )
)
```

- [ ] **Step 3: Create the barrel `store/index.ts`**

```ts
// sfa_web/features/sales-order/store/index.ts
import { useShallow } from 'zustand/react/shallow'
import { useSalesOrderDialogStore } from './sales-order.dialog-store'
import { useSalesOrderFilterStore } from './sales-order.filter-store'

export { useSalesOrderDialogStore }

export const useSubmitDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isSubmitOpen,
      selectedOrderId: s.selectedOrderId,
      open: s.openSubmit,
      close: s.closeSubmit,
    }))
  )

export const useRepApproveDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isRepApproveOpen,
      selectedOrderId: s.selectedOrderId,
      open: s.openRepApprove,
      close: s.closeRepApprove,
    }))
  )

export const useApproveDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isApproveOpen,
      selectedOrderId: s.selectedOrderId,
      open: s.openApprove,
      close: s.closeApprove,
    }))
  )

export const useAcknowledgeDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isAcknowledgeOpen,
      selectedOrderId: s.selectedOrderId,
      open: s.openAcknowledge,
      close: s.closeAcknowledge,
    }))
  )

export const useFinalizeDialog = () =>
  useSalesOrderDialogStore(
    useShallow((s) => ({
      isOpen: s.isFinalizeOpen,
      selectedOrderId: s.selectedOrderId,
      open: s.openFinalize,
      close: s.closeFinalize,
    }))
  )

export const useSalesOrderFilters = () =>
  useSalesOrderFilterStore(
    useShallow((s) => ({
      page: s.page,
      pageSize: s.pageSize,
      search: s.search,
      status: s.status,
      fromDate: s.fromDate,
      toDate: s.toDate,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSearch: s.setSearch,
      setStatus: s.setStatus,
      setFromDate: s.setFromDate,
      setToDate: s.setToDate,
      resetFilters: s.resetFilters,
    }))
  )
```

- [ ] **Step 4: Verify TypeScript compiles**

```bash
cd "d:/Github/sfa/sfa_web" && npx tsc --noEmit 2>&1 | head -20
```
Expected: No errors.

---

### Task 8: Server Actions

**Files:**
- Create: `sfa_web/features/sales-order/actions/sales-order.actions.ts`

- [ ] **Step 1: Create the actions file**

```ts
'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  SalesOrderDto,
  SalesOrderListDto,
  DefaultPricingStructureDto,
} from '../types/sales-order.types'
import type {
  CreateSalesOrderInput,
  UpdateSalesOrderInput,
  RejectSalesOrderInput,
} from '../schema/sales-order.schema'

export const getSalesOrdersAction = createAction(
  { name: 'getSalesOrdersAction', requireAuth: true },
  async (
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    status?: string,
    fromDate?: string,
    toDate?: string,
  ) => {
    const res = await client.get('/api/v1/sales-orders', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: status || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      },
    })
    return res.data.data as SalesOrderListDto
  }
)

export const getSalesOrderByIdAction = createAction(
  { name: 'getSalesOrderByIdAction', requireAuth: true },
  async (id: number) => {
    const res = await client.get(`/api/v1/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const createSalesOrderAction = createAction(
  { name: 'createSalesOrderAction', requireAuth: true },
  async (data: CreateSalesOrderInput) => {
    const res = await client.post('/api/v1/sales-orders', data)
    revalidatePath('/sales-orders')
    return res.data.data as SalesOrderDto
  }
)

export const updateSalesOrderAction = createAction(
  { name: 'updateSalesOrderAction', requireAuth: true },
  async (id: number, data: UpdateSalesOrderInput) => {
    const res = await client.put(`/api/v1/sales-orders/${id}`, data)
    revalidatePath('/sales-orders')
    return res.data.data as SalesOrderDto
  }
)

export const submitSalesOrderAction = createAction(
  { name: 'submitSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/submit`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const repApproveSalesOrderAction = createAction(
  { name: 'repApproveSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/rep-approve`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const approveSalesOrderAction = createAction(
  { name: 'approveSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/approve`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const rejectSalesOrderAction = createAction(
  { name: 'rejectSalesOrderAction', requireAuth: true },
  async (id: number, data: RejectSalesOrderInput) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/reject`, data)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const acknowledgeSalesOrderAction = createAction(
  { name: 'acknowledgeSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/acknowledge`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const finalizeSalesOrderAction = createAction(
  { name: 'finalizeSalesOrderAction', requireAuth: true },
  async (id: number) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/finalize`)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const cancelSalesOrderAction = createAction(
  { name: 'cancelSalesOrderAction', requireAuth: true },
  async (id: number, data: RejectSalesOrderInput) => {
    const res = await client.post(`/api/v1/sales-orders/${id}/cancel`, data)
    revalidatePath('/sales-orders')
    revalidatePath(`/sales-orders/${id}`)
    return res.data.data as SalesOrderDto
  }
)

export const getDefaultPricingStructureAction = createAction(
  { name: 'getDefaultPricingStructureAction', requireAuth: true },
  async () => {
    const res = await client.get('/api/v1/pricing-structures/default')
    return res.data.data as DefaultPricingStructureDto
  }
)
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd "d:/Github/sfa/sfa_web" && npx tsc --noEmit 2>&1 | head -20
```
Expected: No errors.

---

### Task 9: TanStack Query Hooks

**Files:**
- Create: `sfa_web/features/sales-order/hooks/sales-order.hooks.ts`

- [ ] **Step 1: Create the hooks file**

```ts
'use client'

import { useState } from 'react'
import {
  queryOptions,
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import { useRouter } from 'next/navigation'
import {
  getSalesOrdersAction,
  getSalesOrderByIdAction,
  createSalesOrderAction,
  updateSalesOrderAction,
  submitSalesOrderAction,
  repApproveSalesOrderAction,
  approveSalesOrderAction,
  rejectSalesOrderAction,
  acknowledgeSalesOrderAction,
  finalizeSalesOrderAction,
  cancelSalesOrderAction,
  getDefaultPricingStructureAction,
} from '../actions/sales-order.actions'
import {
  useSubmitDialog,
  useRepApproveDialog,
  useApproveDialog,
  useAcknowledgeDialog,
  useFinalizeDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  CreateSalesOrderInput,
  UpdateSalesOrderInput,
  RejectSalesOrderInput,
} from '../schema/sales-order.schema'

// ── Query key factory ────────────────────────────────────────────────────────

export const salesOrderKeys = {
  all: ['salesOrders'] as const,
  lists: () => [...salesOrderKeys.all, 'list'] as const,
  list: (filters: object) => [...salesOrderKeys.lists(), filters] as const,
  details: () => [...salesOrderKeys.all, 'detail'] as const,
  detail: (id: number) => [...salesOrderKeys.details(), id] as const,
  defaultPricing: ['defaultPricingStructure'] as const,
}

// ── Query options factory ────────────────────────────────────────────────────

export function salesOrderQueryOptions(id: number) {
  return queryOptions({
    queryKey: salesOrderKeys.detail(id),
    queryFn: async () => {
      const result = await getSalesOrderByIdAction(id)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// ── Query hooks ──────────────────────────────────────────────────────────────

export function useSalesOrder(id: number | null) {
  return useQuery({
    queryKey: salesOrderKeys.detail(id!),
    queryFn: async () => {
      const result = await getSalesOrderByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

export function useDefaultPricingStructure() {
  return useQuery({
    queryKey: salesOrderKeys.defaultPricing,
    queryFn: async () => {
      const result = await getDefaultPricingStructureAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000, // pricing structure rarely changes
  })
}

// ── DataTable hook ────────────────────────────────────────────────────────────

export function useSalesOrderDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: Record<string, unknown>,
) {
  return useQuery({
    queryKey: salesOrderKeys.list({ page, pageSize, search, dateRange, customFilters }),
    queryFn: async () => {
      const status = customFilters?.status as string | undefined
      const result = await getSalesOrdersAction(
        page,
        pageSize,
        search || undefined,
        status || undefined,
        dateRange?.from_date,
        dateRange?.to_date,
      )
      if (!result.success) throw new Error(result.error)
      const { salesOrders, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: salesOrders,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    placeholderData: keepPreviousData,
  })
}

;(useSalesOrderDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Mutation hooks ────────────────────────────────────────────────────────────

export function useCreateSalesOrder() {
  const queryClient = useQueryClient()
  const router = useRouter()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateSalesOrderInput) => {
      const result = await createSalesOrderAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      setFieldErrors(null)
      toast.success('Sales order created successfully')
      router.push(`/sales-orders/${data.id}`)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'sales order', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateSalesOrder(orderId: number) {
  const queryClient = useQueryClient()
  const router = useRouter()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: UpdateSalesOrderInput) => {
      const result = await updateSalesOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.all })
      setFieldErrors(null)
      toast.success('Sales order updated successfully')
      router.push(`/sales-orders/${orderId}`)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'sales order', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useSubmitSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useSubmitDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await submitSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(data.id) })
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() })
      close()
      toast.success('Order submitted for review')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'submit')
    },
  })
}

export function useRepApproveSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useRepApproveDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await repApproveSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(data.id) })
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() })
      close()
      toast.success('Order approved by Sales Rep')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'approve')
    },
  })
}

export function useApproveSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useApproveDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await approveSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(data.id) })
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() })
      close()
      toast.success('Order approved')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'approve')
    },
  })
}

export function useRejectSalesOrder(orderId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: RejectSalesOrderInput) => {
      const result = await rejectSalesOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(data.id) })
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() })
      toast.success('Order rejected')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'reject')
    },
  })
}

export function useAcknowledgeSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useAcknowledgeDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await acknowledgeSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(data.id) })
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() })
      close()
      toast.success('Rejection acknowledged')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'acknowledge')
    },
  })
}

export function useFinalizeSalesOrder() {
  const queryClient = useQueryClient()
  const { close } = useFinalizeDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await finalizeSalesOrderAction(id)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(data.id) })
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() })
      close()
      toast.success('Order finalized')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'finalize')
    },
  })
}

export function useCancelSalesOrder(orderId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: RejectSalesOrderInput) => {
      const result = await cancelSalesOrderAction(orderId, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(data.id) })
      queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() })
      toast.success('Order cancelled')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'sales order', 'cancel')
    },
  })
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd "d:/Github/sfa/sfa_web" && npx tsc --noEmit 2>&1 | head -20
```
Expected: No errors.

---

### Task 10: Shared utilities + Status Badge + `formatLKR`

**Files:**
- Modify: `sfa_web/lib/utils.ts`
- Create: `sfa_web/features/sales-order/components/sales-order-status-badge.tsx`
- Create: `sfa_web/features/sales-order/components/index.ts`

- [ ] **Step 1: Add `formatLKR` to `lib/utils.ts`**

Open `sfa_web/lib/utils.ts` and add at the end:
```ts
export function formatLKR(value: number): string {
  return new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR' }).format(value)
}
```

- [ ] **Step 2: Create `SalesOrderStatusBadge` component**

```tsx
// sfa_web/features/sales-order/components/sales-order-status-badge.tsx
import { Badge } from '@/components/ui/badge'
import type { SalesOrderStatus } from '../types/sales-order.types'

const statusConfig: Record<SalesOrderStatus, { label: string; variant: 'default' | 'secondary' | 'outline' | 'destructive'; className?: string }> = {
  0: { label: 'Draft', variant: 'secondary' },
  1: { label: 'Pending Rep Approval', variant: 'outline', className: 'text-blue-600 border-blue-300' },
  2: { label: 'Pending Manager Approval', variant: 'outline', className: 'text-amber-600 border-amber-300' },
  3: { label: 'Pending Finalization', variant: 'outline', className: 'text-purple-600 border-purple-300' },
  4: { label: 'Finalized', variant: 'default' },
  5: { label: 'Cancelled', variant: 'destructive' },
  6: { label: 'Pending Acknowledgement', variant: 'outline', className: 'text-orange-600 border-orange-300' },
}

interface SalesOrderStatusBadgeProps {
  status: SalesOrderStatus
  className?: string
}

export function SalesOrderStatusBadge({ status, className }: SalesOrderStatusBadgeProps) {
  const config = statusConfig[status]
  return (
    <Badge variant={config.variant} className={`text-xs font-medium ${config.className ?? ''} ${className ?? ''}`}>
      {config.label}
    </Badge>
  )
}
```

- [ ] **Step 3: Create the barrel `components/index.ts`**

```ts
export { SalesOrderStatusBadge } from './sales-order-status-badge'
```

- [ ] **Step 4: Verify build**

```bash
cd "d:/Github/sfa/sfa_web" && npx tsc --noEmit 2>&1 | head -20
```
Expected: No errors.

- [ ] **Step 5: Commit the entire foundation**

```bash
cd "d:/Github/sfa"
git add sfa_web/features/sales-order/ sfa_web/lib/utils.ts
git commit -m "add sales-order feature foundation: types, schema, store, actions, hooks, status badge"
```

---

## Chunk 3: List Page

### Task 11: Table Columns

**Files:**
- Create: `sfa_web/features/sales-order/components/columns/sales-order-columns.tsx`

- [ ] **Step 1: Create the columns file**

```tsx
'use client'

import type { ColumnDef } from '@tanstack/react-table'
import Link from 'next/link'
import { MoreHorizontal, Pencil, Eye } from 'lucide-react'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { SalesOrderStatusBadge } from '../sales-order-status-badge'
import { formatLKR } from '@/lib/utils'
import type { SalesOrderSummaryDto } from '../../types/sales-order.types'

export function getColumns(
  role: string
): ColumnDef<SalesOrderSummaryDto>[] {
  const columns: ColumnDef<SalesOrderSummaryDto>[] = [
    {
      accessorKey: 'orderNumber',
      header: 'Order #',
      cell: ({ row }) => (
        <Link
          href={`/sales-orders/${row.original.id}`}
          className="font-medium text-primary hover:underline"
        >
          {row.original.orderNumber}
        </Link>
      ),
    },
  ]

  if (role !== 'Distributor') {
    columns.push({
      accessorKey: 'distributorName',
      header: 'Distributor',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.distributorName}</span>
      ),
    })
  }

  columns.push(
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <SalesOrderStatusBadge status={row.original.status} />,
    },
    {
      accessorKey: 'totalAmount',
      header: () => <div className="text-right">Total</div>,
      cell: ({ row }) => (
        <div className="text-right font-medium">
          {formatLKR(row.original.totalAmount)}
        </div>
      ),
    },
    {
      accessorKey: 'submittedAt',
      header: 'Submitted At',
      cell: ({ row }) =>
        row.original.submittedAt
          ? format(new Date(row.original.submittedAt), 'dd MMM yyyy HH:mm')
          : '—',
    },
    {
      accessorKey: 'createdAt',
      header: 'Created At',
      cell: ({ row }) =>
        format(new Date(row.original.createdAt), 'dd MMM yyyy HH:mm'),
    },
    {
      id: 'actions',
      cell: ({ row }) => {
        const order = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-8 w-8 p-0">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={`/sales-orders/${order.id}`}>
                  <Eye className="mr-2 h-4 w-4" />
                  View
                </Link>
              </DropdownMenuItem>
              {order.status === 0 && (
                <DropdownMenuItem asChild>
                  <Link href={`/sales-orders/${order.id}/edit`}>
                    <Pencil className="mr-2 h-4 w-4" />
                    Edit
                  </Link>
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  )

  return columns
}
```

---

### Task 12: Sales Order Table with Toolbar

**Files:**
- Create: `sfa_web/features/sales-order/components/table/sales-order-table.tsx`

- [ ] **Step 1: Create the table component**

```tsx
'use client'

import { useSession } from 'next-auth/react'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { DataTable } from '@/components/data-table/data-table'
import { useSalesOrderDataTable } from '../../hooks/sales-order.hooks'
import { useSalesOrderFilters } from '../../store'
import { getColumns } from '../columns/sales-order-columns'

const STATUS_OPTIONS = [
  { value: '', label: 'All Statuses' },
  { value: 'Draft', label: 'Draft' },
  { value: 'PendingRepApproval', label: 'Pending Rep Approval' },
  { value: 'PendingManagerApproval', label: 'Pending Manager Approval' },
  { value: 'PendingDistributorFinalization', label: 'Pending Finalization' },
  { value: 'PendingDistributorAcknowledgement', label: 'Pending Acknowledgement' },
  { value: 'Finalized', label: 'Finalized' },
  { value: 'Cancelled', label: 'Cancelled' },
]

export function SalesOrderTable() {
  const { data: session } = useSession()
  const role = session?.user?.role ?? ''
  const filters = useSalesOrderFilters()
  const columns = getColumns(role)

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: true,
        enableColumnVisibility: true,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: 'sales-orders-table',
        searchPlaceholder: 'Search orders...',
      }}
      getColumns={() => columns}
      fetchDataFn={useSalesOrderDataTable}
      idField="id"
      initialDateRange={{ from_date: filters.fromDate, to_date: filters.toDate }}
      renderCustomFilters={(_, setFilters) => (
        <Select
          value={filters.status}
          onValueChange={(val) => {
            filters.setStatus(val)
            setFilters((prev: Record<string, unknown>) => ({ ...prev, status: val }))
          }}
        >
          <SelectTrigger className="w-[220px]">
            <SelectValue placeholder="All Statuses" />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
    />
  )
}
```

---

### Task 13: List Page Component + App Route + Sidebar

**Files:**
- Create: `sfa_web/features/sales-order/components/pages/sales-order-list-page.tsx`
- Create: `sfa_web/app/(protected)/sales-orders/page.tsx`
- Modify: `sfa_web/components/app-sidebar.tsx`

- [ ] **Step 1: Create the list page component**

The "Create Sales Order" button belongs in the header banner (industry standard: page-level CTA in the header, not inside the table toolbar). Use `useSession` here to read the role — the table no longer needs to know about create visibility.

```tsx
// sfa_web/features/sales-order/components/pages/sales-order-list-page.tsx
'use client'

import Link from 'next/link'
import { Plus } from 'lucide-react'
import { useSession } from 'next-auth/react'
import { Button } from '@/components/ui/button'
import { SalesOrderTable } from '../table/sales-order-table'

export function SalesOrderListPage() {
  const { data: session } = useSession()
  const role = session?.user?.role ?? ''
  const canCreate = role === 'Admin' || role === 'Distributor'

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Orders</h1>
          <p className="text-muted-foreground">Manage and track your sales orders</p>
        </div>
        {canCreate && (
          <Button asChild className="gap-2">
            <Link href="/sales-orders/new">
              <Plus className="h-4 w-4" />
              Create Sales Order
            </Link>
          </Button>
        )}
      </div>
      <SalesOrderTable />
    </div>
  )
}
```

- [ ] **Step 2: Create the app route**

```tsx
// sfa_web/app/(protected)/sales-orders/page.tsx
import { SalesOrderListPage } from '@/features/sales-order/components/pages/sales-order-list-page'

export default function SalesOrdersPage() {
  return <SalesOrderListPage />
}
```

- [ ] **Step 3: Add Sales Orders to the sidebar**

In `sfa_web/components/app-sidebar.tsx`, add to the `Masters` nav group items array (after `Pricing Structures`):
```ts
{
  title: 'Sales Orders',
  url: '/sales-orders',
},
```

- [ ] **Step 4: Verify build**

```bash
cd "d:/Github/sfa/sfa_web" && npm run build 2>&1 | tail -20
```
Expected: `✓ Compiled successfully` (or similar success output, no errors)

- [ ] **Step 5: Commit**

```bash
cd "d:/Github/sfa"
git add sfa_web/
git commit -m "add sales orders list page with status filter and date range"
```

---

## Chunk 4: Create & Edit Pages

### Task 14: Line Items Form Component

**Files:**
- Create: `sfa_web/features/sales-order/components/forms/sales-order-form.tsx`

- [ ] **Step 1: Create the line items form**

```tsx
'use client'

import { useFieldArray, useFormContext } from 'react-hook-form'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import type { PricingStructureItemDto } from '../../types/sales-order.types'
import type { CreateSalesOrderInput, UpdateSalesOrderInput } from '../../schema/sales-order.schema'

// Both Create and Update schemas share the same items shape — accept either context
type WithItems = Pick<CreateSalesOrderInput, 'items'> | Pick<UpdateSalesOrderInput, 'items'>

interface SalesOrderFormProps {
  pricingItems: PricingStructureItemDto[]
  isLoadingPricing: boolean
  pricingError: boolean
}

export function SalesOrderLineItemsForm({
  pricingItems,
  isLoadingPricing,
  pricingError,
}: SalesOrderFormProps) {
  const { control, setValue, watch } = useFormContext<WithItems>()
  const { fields, append, remove } = useFieldArray({ control, name: 'items' })

  const items = watch('items') ?? []

  function handleProductSelect(index: number, productId: string) {
    const pid = parseInt(productId)
    const pItem = pricingItems.find((p) => p.productId === pid)
    if (!pItem) return
    setValue(`items.${index}.productId`, pItem.productId)
    setValue(`items.${index}.unitPrice`, pItem.dealerCasePrice ?? 0)
    setValue(`items.${index}.discount`, 0)
  }

  function addRow() {
    append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-base">Line Items</CardTitle>
        <Button type="button" variant="outline" size="sm" onClick={addRow} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Product
        </Button>
      </CardHeader>
      <CardContent>
        {isLoadingPricing && (
          <div className="flex justify-center py-8">
            <Spinner />
          </div>
        )}
        {pricingError && (
          <p className="text-sm text-destructive text-center py-4">
            Failed to load product catalogue. Please refresh.
          </p>
        )}
        {!isLoadingPricing && !pricingError && fields.length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-4">
            No items added. Click &quot;Add Product&quot; to start.
          </p>
        )}
        {!isLoadingPricing && fields.length > 0 && (
          <div className="space-y-2">
            <div className="grid grid-cols-[2fr_1fr_1fr_1fr_1fr_auto] gap-2 text-xs font-medium text-muted-foreground px-1">
              <span>Product</span>
              <span>SKU</span>
              <span>Qty</span>
              <span>Case Price</span>
              <span>Total</span>
              <span />
            </div>
            {fields.map((field, index) => {
              const selectedProductId = items[index]?.productId
              const selectedItem = pricingItems.find((p) => p.productId === selectedProductId)
              const qty = items[index]?.quantity ?? 0
              const unitPrice = items[index]?.unitPrice ?? 0
              const lineTotal = qty * unitPrice

              return (
                <div key={field.id} className="grid grid-cols-[2fr_1fr_1fr_1fr_1fr_auto] gap-2 items-start">
                  {/* Product selector */}
                  <FormField
                    control={control}
                    name={`items.${index}.productId`}
                    render={() => (
                      <FormItem>
                        <FormControl>
                          <Select
                            value={selectedProductId ? String(selectedProductId) : ''}
                            onValueChange={(val) => handleProductSelect(index, val)}
                          >
                            <SelectTrigger className="h-8 text-xs">
                              <SelectValue placeholder="Select product" />
                            </SelectTrigger>
                            <SelectContent>
                              {pricingItems.map((p) => (
                                <SelectItem key={p.productId} value={String(p.productId)}>
                                  {p.productItemDescription} ({p.productCode})
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  {/* SKU (read-only) */}
                  <Input
                    readOnly
                    value={selectedItem?.productCode ?? ''}
                    className="h-8 text-xs bg-muted font-mono"
                    tabIndex={-1}
                  />
                  {/* Quantity */}
                  <FormField
                    control={control}
                    name={`items.${index}.quantity`}
                    render={({ field: f }) => (
                      <FormItem>
                        <FormControl>
                          <Input
                            type="number"
                            min={1}
                            className="h-8 text-xs"
                            {...f}
                            onChange={(e) => f.onChange(parseInt(e.target.value) || 1)}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  {/* Dealer Case Price (read-only) */}
                  <Input
                    readOnly
                    value={selectedItem?.dealerCasePrice != null ? formatLKR(selectedItem.dealerCasePrice) : 'N/A'}
                    className="h-8 text-xs bg-muted"
                    tabIndex={-1}
                  />
                  {/* Line Total (read-only) */}
                  <Input
                    readOnly
                    value={selectedProductId ? formatLKR(lineTotal) : '—'}
                    className="h-8 text-xs bg-muted"
                    tabIndex={-1}
                  />
                  {/* Remove button */}
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-destructive hover:text-destructive"
                    onClick={() => remove(index)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              )
            })}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
```

---

### Task 15: Create Page

**Files:**
- Create: `sfa_web/features/sales-order/components/pages/sales-order-create-page.tsx`
- Create: `sfa_web/app/(protected)/sales-orders/new/page.tsx`

- [ ] **Step 1: Create the create page component**

```tsx
'use client'

import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import { useForm, FormProvider } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
} from '@/components/ui/form'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import {
  createSalesOrderSchema,
  type CreateSalesOrderInput,
} from '../../schema/sales-order.schema'
import { useCreateSalesOrder, useDefaultPricingStructure } from '../../hooks/sales-order.hooks'
import { useDistributorsForSelect } from '@/features/distributor/hooks/distributor.hooks'
import { SalesOrderLineItemsForm } from '../forms/sales-order-form'

export function SalesOrderCreatePage() {
  const { data: session } = useSession()
  const router = useRouter()
  const role = session?.user?.role ?? ''

  const { data: pricing, isLoading: isLoadingPricing, isError: pricingError } =
    useDefaultPricingStructure()
  const { distributors, isLoading: isLoadingDistributors } = useDistributorsForSelect()

  const { mutate, isPending, fieldErrors } = useCreateSalesOrder()

  const form = useForm<CreateSalesOrderInput>({
    resolver: zodResolver(createSalesOrderSchema),
    defaultValues: {
      distributorId: null,
      notes: '',
      items: [],
    },
  })

  const items = form.watch('items') ?? []
  const total = items.reduce((sum, item) => {
    const pItem = pricing?.items.find((p) => p.productId === item.productId)
    return sum + (item.quantity * (pItem?.dealerCasePrice ?? item.unitPrice))
  }, 0)

  function onSubmit(data: CreateSalesOrderInput) {
    mutate({
      ...data,
      distributorId: role === 'Admin' ? data.distributorId : null,
    })
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <button onClick={() => router.push('/sales-orders')} className="hover:text-foreground">
          Sales Orders
        </button>
        <span>/</span>
        <span className="text-foreground font-medium">New Order</span>
      </div>

      <FormProvider {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-row gap-6">
          {/* Left panel */}
          <div className="flex-1 flex flex-col gap-4">
            {/* Order Details card */}
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Order Details</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-4">
                {role === 'Admin' && (
                  <FormField
                    control={form.control}
                    name="distributorId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Distributor</FormLabel>
                        <Select
                          value={field.value ? String(field.value) : ''}
                          onValueChange={(val) => field.onChange(val ? parseInt(val) : null)}
                          disabled={isLoadingDistributors}
                        >
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue placeholder={isLoadingDistributors ? 'Loading...' : 'Select distributor'} />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {distributors.map((d) => (
                              <SelectItem key={d.id} value={String(d.id)}>
                                {d.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                        {fieldErrors?.distributorId && (
                          <p className="text-xs text-destructive">{fieldErrors.distributorId}</p>
                        )}
                      </FormItem>
                    )}
                  />
                )}
                <FormField
                  control={form.control}
                  name="notes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Notes (optional)</FormLabel>
                      <FormControl>
                        <Textarea
                          placeholder="Add any order notes..."
                          className="resize-none"
                          rows={3}
                          {...field}
                          value={field.value ?? ''}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </CardContent>
            </Card>

            {/* Line items */}
            <SalesOrderLineItemsForm
              pricingItems={pricing?.items ?? []}
              isLoadingPricing={isLoadingPricing}
              pricingError={pricingError}
            />
          </div>

          {/* Right summary sidebar */}
          <div className="w-80 sticky top-6 self-start">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Order Summary</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-3">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Items</span>
                  <span>{items.length}</span>
                </div>
                <Separator />
                <div className="flex justify-between text-sm font-medium">
                  <span>Total</span>
                  <span>{formatLKR(total)}</span>
                </div>
                <Separator />
                <Button type="submit" disabled={isPending} className="w-full gap-2">
                  {isPending && <Spinner className="h-4 w-4" />}
                  Save as Draft
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="w-full"
                  onClick={() => router.push('/sales-orders')}
                >
                  Cancel
                </Button>
              </CardContent>
            </Card>
          </div>
        </form>
      </FormProvider>
    </div>
  )
}
```

- [ ] **Step 2: Create the app route**

```tsx
// sfa_web/app/(protected)/sales-orders/new/page.tsx
import { SalesOrderCreatePage } from '@/features/sales-order/components/pages/sales-order-create-page'

export default function NewSalesOrderPage() {
  return <SalesOrderCreatePage />
}
```

- [ ] **Step 3: Verify build**

```bash
cd "d:/Github/sfa/sfa_web" && npm run build 2>&1 | tail -20
```
Expected: No errors.

---

### Task 16: Edit Page

**Files:**
- Create: `sfa_web/features/sales-order/components/pages/sales-order-edit-page.tsx`
- Create: `sfa_web/app/(protected)/sales-orders/[id]/edit/page.tsx`

- [ ] **Step 1: Create the edit page component**

```tsx
'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useForm, FormProvider } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
} from '@/components/ui/form'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import {
  updateSalesOrderSchema,
  type UpdateSalesOrderInput,
} from '../../schema/sales-order.schema'
import {
  useSalesOrder,
  useUpdateSalesOrder,
  useDefaultPricingStructure,
} from '../../hooks/sales-order.hooks'
import { SalesOrderLineItemsForm } from '../forms/sales-order-form'

interface SalesOrderEditPageProps {
  orderId: number
}

export function SalesOrderEditPage({ orderId }: SalesOrderEditPageProps) {
  const router = useRouter()

  const { data: order, isLoading: isLoadingOrder } = useSalesOrder(orderId)
  const { data: pricing, isLoading: isLoadingPricing, isError: pricingError } =
    useDefaultPricingStructure()
  const { mutate, isPending, fieldErrors } = useUpdateSalesOrder(orderId)

  const form = useForm<UpdateSalesOrderInput>({
    resolver: zodResolver(updateSalesOrderSchema),
    defaultValues: { notes: '', items: [] },
  })

  // Redirect if not Draft
  useEffect(() => {
    if (order && order.status !== 0) {
      router.replace(`/sales-orders/${orderId}`)
    }
  }, [order, orderId, router])

  // Pre-populate form from existing order
  useEffect(() => {
    if (order) {
      form.reset({
        notes: order.notes ?? '',
        items: order.items.map((item) => ({
          productId: item.productId,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discount: 0 as const,
        })),
      })
    }
  }, [order, form])

  const items = form.watch('items') ?? []
  const total = items.reduce((sum, item) => {
    return sum + (item.quantity * item.unitPrice)
  }, 0)

  if (isLoadingOrder) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <Spinner />
      </div>
    )
  }

  if (!order) {
    return (
      <div className="flex flex-col items-center gap-4 p-6">
        <p className="text-muted-foreground">Order not found.</p>
        <Button variant="outline" onClick={() => router.push('/sales-orders')}>
          ← Back to Sales Orders
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <button onClick={() => router.push('/sales-orders')} className="hover:text-foreground">
          Sales Orders
        </button>
        <span>/</span>
        <button
          onClick={() => router.push(`/sales-orders/${orderId}`)}
          className="hover:text-foreground"
        >
          {order.orderNumber}
        </button>
        <span>/</span>
        <span className="text-foreground font-medium">Edit</span>
      </div>

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((data) => mutate(data))}
          className="flex flex-row gap-6"
        >
          <div className="flex-1 flex flex-col gap-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Order Details</CardTitle>
              </CardHeader>
              <CardContent>
                <FormField
                  control={form.control}
                  name="notes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Notes (optional)</FormLabel>
                      <FormControl>
                        <Textarea
                          placeholder="Add any order notes..."
                          className="resize-none"
                          rows={3}
                          {...field}
                          value={field.value ?? ''}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </CardContent>
            </Card>

            <SalesOrderLineItemsForm
              pricingItems={pricing?.items ?? []}
              isLoadingPricing={isLoadingPricing}
              pricingError={pricingError}
            />
          </div>

          <div className="w-80 sticky top-6 self-start">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Order Summary</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-3">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Items</span>
                  <span>{items.length}</span>
                </div>
                <Separator />
                <div className="flex justify-between text-sm font-medium">
                  <span>Total</span>
                  <span>{formatLKR(total)}</span>
                </div>
                <Separator />
                <Button type="submit" disabled={isPending} className="w-full gap-2">
                  {isPending && <Spinner className="h-4 w-4" />}
                  Save Changes
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="w-full"
                  onClick={() => router.push(`/sales-orders/${orderId}`)}
                >
                  Cancel
                </Button>
              </CardContent>
            </Card>
          </div>
        </form>
      </FormProvider>
    </div>
  )
}
```

- [ ] **Step 2: Create the app route**

```tsx
// sfa_web/app/(protected)/sales-orders/[id]/edit/page.tsx
import { SalesOrderEditPage } from '@/features/sales-order/components/pages/sales-order-edit-page'

// Next.js 15+: params is a Promise — must be async and awaited
interface EditPageProps {
  params: Promise<{ id: string }>
}

export default async function EditSalesOrderPage({ params }: EditPageProps) {
  const { id } = await params
  return <SalesOrderEditPage orderId={parseInt(id)} />
}
```

- [ ] **Step 3: Verify build**

```bash
cd "d:/Github/sfa/sfa_web" && npm run build 2>&1 | tail -20
```
Expected: No errors.

- [ ] **Step 4: Commit**

```bash
cd "d:/Github/sfa"
git add sfa_web/
git commit -m "add sales orders create and edit pages with line items builder"
```

---

## Chunk 5: Detail Page

### Task 17: Action Dialogs

**Files:**
- Create: `sfa_web/features/sales-order/components/dialogs/sales-order-dialogs.tsx`

- [ ] **Step 1: Create the dialogs component**

```tsx
'use client'

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Spinner } from '@/components/ui/spinner'
import {
  useSubmitDialog,
  useRepApproveDialog,
  useApproveDialog,
  useAcknowledgeDialog,
  useFinalizeDialog,
} from '../../store'
import {
  useSubmitSalesOrder,
  useRepApproveSalesOrder,
  useApproveSalesOrder,
  useAcknowledgeSalesOrder,
  useFinalizeSalesOrder,
} from '../../hooks/sales-order.hooks'

function SubmitDialog() {
  const { isOpen, selectedOrderId, close } = useSubmitDialog()
  const { mutate, isPending } = useSubmitSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Submit Order</AlertDialogTitle>
          <AlertDialogDescription>
            Submit this order for Sales Rep review? This cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Submit
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function RepApproveDialog() {
  const { isOpen, selectedOrderId, close } = useRepApproveDialog()
  const { mutate, isPending } = useRepApproveSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve Order</AlertDialogTitle>
          <AlertDialogDescription>
            Approve this order and forward to Manager for final approval?
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Approve
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function ApproveDialog() {
  const { isOpen, selectedOrderId, close } = useApproveDialog()
  const { mutate, isPending } = useApproveSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve Order</AlertDialogTitle>
          <AlertDialogDescription>
            Approve this order? The distributor will be asked to finalize.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Approve
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function AcknowledgeDialog() {
  const { isOpen, selectedOrderId, close } = useAcknowledgeDialog()
  const { mutate, isPending } = useAcknowledgeSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Acknowledge Rejection</AlertDialogTitle>
          <AlertDialogDescription>
            Confirm you have read the rejection reason. The order will be cancelled.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Acknowledge
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

function FinalizeDialog() {
  const { isOpen, selectedOrderId, close } = useFinalizeDialog()
  const { mutate, isPending } = useFinalizeSalesOrder()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Finalize Order</AlertDialogTitle>
          <AlertDialogDescription>
            Finalize this order? This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedOrderId && mutate(selectedOrderId)}
          >
            {isPending && <Spinner className="mr-2 h-4 w-4" />}
            Finalize
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

export function SalesOrderDialogs() {
  return (
    <>
      <SubmitDialog />
      <RepApproveDialog />
      <ApproveDialog />
      <AcknowledgeDialog />
      <FinalizeDialog />
    </>
  )
}
```

---

### Task 18: Detail Page

**Files:**
- Create: `sfa_web/features/sales-order/components/pages/sales-order-detail-page.tsx`
- Create: `sfa_web/app/(protected)/sales-orders/[id]/page.tsx`

- [ ] **Step 1: Create the detail page component**

```tsx
'use client'

import { useState } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import { format } from 'date-fns'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Textarea } from '@/components/ui/textarea'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import { SalesOrderStatusBadge } from '../sales-order-status-badge'
import { SalesOrderDialogs } from '../dialogs/sales-order-dialogs'
import {
  useSalesOrder,
  useRejectSalesOrder,
  useCancelSalesOrder,
} from '../../hooks/sales-order.hooks'
import {
  useSubmitDialog,
  useRepApproveDialog,
  useApproveDialog,
  useAcknowledgeDialog,
  useFinalizeDialog,
} from '../../store'
import {
  rejectSalesOrderSchema,
  type RejectSalesOrderInput,
} from '../../schema/sales-order.schema'
import type { SalesOrderDto } from '../../types/sales-order.types'

const STATUS_DESCRIPTIONS: Record<number, string> = {
  0: 'Awaiting submission by distributor',
  1: 'Awaiting Sales Rep review',
  2: 'Awaiting Manager approval',
  3: 'Awaiting distributor finalization',
  4: 'Order has been finalized',
  5: 'Order has been cancelled',
  6: 'Distributor must acknowledge the rejection',
}

const HISTORY_ACTION_LABELS: Record<string, { label: string; dotClass: string }> = {
  Created: { label: 'Created', dotClass: 'bg-gray-400' },
  Submitted: { label: 'Submitted for review', dotClass: 'bg-blue-500' },
  RepApproved: { label: 'Approved by Sales Rep', dotClass: 'bg-green-500' },
  ManagerApproved: { label: 'Approved by Manager', dotClass: 'bg-green-600' },
  Rejected: { label: 'Rejected', dotClass: 'bg-red-500' },
  RejectionAcknowledged: { label: 'Rejection acknowledged', dotClass: 'bg-orange-500' },
  Finalized: { label: 'Finalized', dotClass: 'bg-purple-500' },
  Cancelled: { label: 'Cancelled', dotClass: 'bg-red-600' },
  ItemsEdited: { label: 'Items edited', dotClass: 'bg-gray-400' },
}

interface InlineReasonFormProps {
  label: string
  isPending: boolean
  onConfirm: (data: RejectSalesOrderInput) => void
  onCancel: () => void
}

function InlineReasonForm({ label, isPending, onConfirm, onCancel }: InlineReasonFormProps) {
  const form = useForm<RejectSalesOrderInput>({
    resolver: zodResolver(rejectSalesOrderSchema),
    defaultValues: { reason: '' },
  })

  return (
    <form onSubmit={form.handleSubmit(onConfirm)} className="flex flex-col gap-2 mt-2">
      <Textarea
        placeholder="Reason..."
        rows={3}
        {...form.register('reason')}
        className="text-sm"
      />
      {form.formState.errors.reason && (
        <p className="text-xs text-destructive">{form.formState.errors.reason.message}</p>
      )}
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={isPending} className="gap-1">
          {isPending && <Spinner className="h-3 w-3" />}
          Confirm {label}
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={onCancel}>
          ← Back
        </Button>
      </div>
    </form>
  )
}

interface AuditRowProps {
  label: string
  value: string | number | null | undefined
}

function AuditRow({ label, value }: AuditRowProps) {
  if (value == null) return null
  return (
    <div className="flex justify-between text-xs gap-2">
      <span className="text-muted-foreground shrink-0">{label}</span>
      <span className="text-right break-all">{String(value)}</span>
    </div>
  )
}

interface OrderActionsProps {
  order: SalesOrderDto
  role: string
}

function OrderActions({ order, role }: OrderActionsProps) {
  const [showRejectForm, setShowRejectForm] = useState(false)
  const [showCancelForm, setShowCancelForm] = useState(false)

  const { open: openSubmit } = useSubmitDialog()
  const { open: openRepApprove } = useRepApproveDialog()
  const { open: openApprove } = useApproveDialog()
  const { open: openAcknowledge } = useAcknowledgeDialog()
  const { open: openFinalize } = useFinalizeDialog()

  const { mutate: reject, isPending: isRejecting } = useRejectSalesOrder(order.id)
  const { mutate: cancel, isPending: isCancelling } = useCancelSalesOrder(order.id)

  const s = order.status

  // Explicit render guard conditions from spec
  const showSubmit = s === 0 && (role === 'Distributor' || role === 'Admin')
  const showCancel = s === 0 && (role === 'Distributor' || role === 'Admin')
  const showRepApprove = s === 1 && (role === 'SalesRep' || role === 'Admin')
  const showReject1 = s === 1 && (role === 'SalesRep' || role === 'Admin')
  const showApprove = s === 2 && (role === 'Manager' || role === 'Admin')
  const showReject2 = s === 2 && (role === 'Manager' || role === 'Admin')
  const showReject = showReject1 || showReject2
  const showAcknowledge = s === 6 && (role === 'Distributor' || role === 'Admin')
  const showFinalize = s === 3 && (role === 'Distributor' || role === 'Admin')

  const hasActions = showSubmit || showRepApprove || showApprove || showReject ||
    showAcknowledge || showFinalize || showCancel

  if (!hasActions) return null

  return (
    <div className="flex flex-col gap-2">
      {showSubmit && (
        <Button onClick={() => openSubmit(order.id)} className="w-full">
          Submit Order
        </Button>
      )}
      {showRepApprove && (
        <Button onClick={() => openRepApprove(order.id)} className="w-full">
          Rep Approve
        </Button>
      )}
      {showApprove && (
        <Button onClick={() => openApprove(order.id)} className="w-full">
          Approve Order
        </Button>
      )}
      {showAcknowledge && (
        <Button onClick={() => openAcknowledge(order.id)} variant="outline" className="w-full">
          Acknowledge Rejection
        </Button>
      )}
      {showFinalize && (
        <Button onClick={() => openFinalize(order.id)} className="w-full">
          Finalize Order
        </Button>
      )}
      {showReject && !showRejectForm && (
        <Button variant="destructive" className="w-full" onClick={() => setShowRejectForm(true)}>
          Reject
        </Button>
      )}
      {showReject && showRejectForm && (
        <InlineReasonForm
          label="Reject"
          isPending={isRejecting}
          onConfirm={(data) => reject(data, { onSuccess: () => setShowRejectForm(false) })}
          onCancel={() => setShowRejectForm(false)}
        />
      )}
      {showCancel && !showCancelForm && (
        <Button variant="outline" className="w-full text-destructive border-destructive hover:bg-destructive/10"
          onClick={() => setShowCancelForm(true)}>
          Cancel Order
        </Button>
      )}
      {showCancel && showCancelForm && (
        <InlineReasonForm
          label="Cancel"
          isPending={isCancelling}
          onConfirm={(data) => cancel(data, { onSuccess: () => setShowCancelForm(false) })}
          onCancel={() => setShowCancelForm(false)}
        />
      )}
    </div>
  )
}

interface SalesOrderDetailPageProps {
  orderId: number
}

export function SalesOrderDetailPage({ orderId }: SalesOrderDetailPageProps) {
  const { data: session } = useSession()
  const router = useRouter()
  const role = session?.user?.role ?? ''

  const { data: order, isLoading } = useSalesOrder(orderId)

  if (isLoading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <Spinner />
      </div>
    )
  }

  if (!order) {
    return (
      <div className="flex flex-col items-center gap-4 p-6">
        <p className="text-muted-foreground">Order not found.</p>
        <Button variant="outline" onClick={() => router.push('/sales-orders')}>
          ← Back to Sales Orders
        </Button>
      </div>
    )
  }

  const formatDate = (d: string | null | undefined) =>
    d ? format(new Date(d), 'dd MMM yyyy HH:mm') : null

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <button onClick={() => router.push('/sales-orders')} className="hover:text-foreground">
          Sales Orders
        </button>
        <span>/</span>
        <span className="text-foreground font-medium">{order.orderNumber}</span>
      </div>

      <div className="flex flex-row gap-6">
        {/* Left column */}
        <div className="flex-1 flex flex-col gap-4">
          {/* Order header */}
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-start justify-between">
                <div>
                  <h1 className="text-2xl font-bold">{order.orderNumber}</h1>
                  <p className="text-muted-foreground text-sm mt-1">{order.distributorName}</p>
                  <p className="text-muted-foreground text-xs mt-1">
                    Created {formatDate(order.createdAt)}
                  </p>
                </div>
                <SalesOrderStatusBadge status={order.status} className="text-sm" />
              </div>
            </CardContent>
          </Card>

          {/* Line items */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Line Items</CardTitle>
            </CardHeader>
            <CardContent>
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-xs text-muted-foreground">
                    <th className="text-left py-2">Product</th>
                    <th className="text-left py-2">SKU</th>
                    <th className="text-right py-2">Qty</th>
                    <th className="text-right py-2">Unit Price</th>
                    <th className="text-right py-2">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {order.items.map((item) => (
                    <tr key={item.id} className="border-b last:border-0">
                      <td className="py-2">{item.productDescription}</td>
                      <td className="py-2 font-mono text-xs text-muted-foreground">{item.productCode}</td>
                      <td className="py-2 text-right">{item.quantity}</td>
                      <td className="py-2 text-right">{formatLKR(item.unitPrice)}</td>
                      <td className="py-2 text-right font-medium">{formatLKR(item.lineTotal)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <td colSpan={4} className="pt-3 text-right font-semibold text-sm">Total</td>
                    <td className="pt-3 text-right font-bold">{formatLKR(order.totalAmount)}</td>
                  </tr>
                </tfoot>
              </table>
            </CardContent>
          </Card>

          {/* Notes */}
          {order.notes && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Notes</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">{order.notes}</p>
              </CardContent>
            </Card>
          )}

          {/* History timeline */}
          {order.history && order.history.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">History</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col gap-4">
                  {order.history.map((entry) => {
                    const cfg = HISTORY_ACTION_LABELS[entry.action] ?? {
                      label: entry.action,
                      dotClass: 'bg-gray-400',
                    }
                    return (
                      <div key={entry.id} className="flex gap-3 items-start">
                        <div className={`mt-1 h-2.5 w-2.5 rounded-full shrink-0 ${cfg.dotClass}`} />
                        <div className="flex flex-col gap-0.5">
                          <div className="text-sm font-medium">
                            {cfg.label}
                            <span className="text-muted-foreground font-normal ml-2 text-xs">
                              by {entry.performedByName ?? `User #${entry.performedBy}`}
                            </span>
                            <span className="text-muted-foreground font-normal ml-2 text-xs">
                              · {format(new Date(entry.performedAt), 'dd MMM yyyy HH:mm')}
                            </span>
                          </div>
                          {entry.notes && (
                            <p className="text-xs text-muted-foreground italic">{entry.notes}</p>
                          )}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right sidebar */}
        <div className="w-80 flex flex-col gap-4 sticky top-6 self-start">
          {/* Status card */}
          <Card>
            <CardContent className="pt-6 flex flex-col gap-3">
              <SalesOrderStatusBadge status={order.status} className="self-start text-sm" />
              <p className="text-xs text-muted-foreground">
                {STATUS_DESCRIPTIONS[order.status]}
              </p>
              <Separator />
              <OrderActions order={order} role={role} />
            </CardContent>
          </Card>

          {/* Audit Trail */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Audit Trail</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-2">
              <AuditRow label="Created by" value={order.createdBy} />
              <AuditRow label="Created at" value={formatDate(order.createdAt)} />
              <AuditRow label="Submitted by" value={order.submittedBy} />
              <AuditRow label="Submitted at" value={formatDate(order.submittedAt)} />
              <AuditRow label="Rep approved by" value={order.repApprovedBy} />
              <AuditRow label="Rep approved at" value={formatDate(order.repApprovedAt)} />
              <AuditRow label="Manager approved by" value={order.managerApprovedBy} />
              <AuditRow label="Manager approved at" value={formatDate(order.managerApprovedAt)} />
              <AuditRow label="Rejection reason" value={order.cancelReason} />
              <AuditRow label="Acknowledged by" value={order.acknowledgedBy} />
              <AuditRow label="Acknowledged at" value={formatDate(order.acknowledgedAt)} />
              <AuditRow label="Finalized by" value={order.finalizedBy} />
              <AuditRow label="Finalized at" value={formatDate(order.finalizedAt)} />
              <AuditRow label="Cancelled by" value={order.cancelledBy} />
              <AuditRow label="Cancelled at" value={formatDate(order.cancelledAt)} />
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Action dialogs (portaled) */}
      <SalesOrderDialogs />
    </div>
  )
}
```

- [ ] **Step 2: Create the app route**

```tsx
// sfa_web/app/(protected)/sales-orders/[id]/page.tsx
import { SalesOrderDetailPage } from '@/features/sales-order/components/pages/sales-order-detail-page'

// Next.js 15+: params is a Promise — must be async and awaited
interface DetailPageProps {
  params: Promise<{ id: string }>
}

export default async function SalesOrderDetailRoute({ params }: DetailPageProps) {
  const { id } = await params
  return <SalesOrderDetailPage orderId={parseInt(id)} />
}
```

- [ ] **Step 3: Final build verification**

```bash
cd "d:/Github/sfa/sfa_web" && npm run build 2>&1 | tail -30
```
Expected: `✓ Compiled successfully` — 0 errors.

- [ ] **Step 4: Final commit**

```bash
cd "d:/Github/sfa"
git add sfa_web/
git commit -m "add sales orders detail page with workflow actions, history timeline, and audit trail"
```
