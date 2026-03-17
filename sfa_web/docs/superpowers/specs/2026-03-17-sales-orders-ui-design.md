# Sales Orders UI — Design Spec
**Date:** 2026-03-17
**Currency:** LKR
**Stack:** Next.js 16, shadcn/ui, TanStack Query v5, Zustand v5, Zod

---

## 0. Required Backend Changes (Prerequisites)

Four small API additions are required before the frontend can be built:

| # | Change | File(s) |
|---|--------|---------|
| 1 | Add `GET /api/v1/pricing-structures/default` — returns `PricingStructureDetailDto` (with items). Auth: `[Authorize]` (all roles). No `[Authorize(Roles = "Admin")]`. Used by the Create page product selector. | `PricingStructuresController.cs` |
| 2 | Add `SubmittedAt DateTime?` to `SalesOrderSummaryDto` (populate from entity). Used by the list table. | `SalesOrderSummaryDto.cs` + repository |
| 3 | Add `IEnumerable<SalesOrderHistoryDto> History` to `SalesOrderDto`. New `SalesOrderHistoryDto` record: `(int Id, string Action, SalesOrderStatus? FromStatus, SalesOrderStatus? ToStatus, int PerformedBy, string? PerformedByName, DateTime PerformedAt, string? Notes)`. Service resolves `PerformedByName` from the Users table. Used by the detail page history timeline. | New `SalesOrderHistoryDto.cs`, `SalesOrderDto.cs`, service `MapToDto` |
| 4 | Add `fromDate DateTime?` and `toDate DateTime?` query params to `GET /api/v1/sales-orders`. Repository filters `CreatedAt >= fromDate` and `CreatedAt <= toDate.Date.AddDays(1)` when provided. | `SalesOrdersController.cs`, service interface + impl, repository |

---

## 1. Overview

Four routes:

| Route | Purpose |
|-------|---------|
| `/sales-orders` | List page |
| `/sales-orders/new` | Create page (full-page form) |
| `/sales-orders/[id]` | Detail page (read + workflow actions) |
| `/sales-orders/[id]/edit` | Edit page (Draft only, same layout as Create) |

**Role visibility summary:**
| Role | Can Create | Can see all orders | Workflow actions |
|------|-----------|-------------------|-----------------|
| Admin | Yes | Yes | All |
| Manager | No | Yes | Approve, Reject at PendingManagerApproval |
| SalesRep | No | Yes | Rep Approve, Reject at PendingRepApproval |
| Distributor | Yes | Own orders only | Submit, Cancel, Acknowledge, Finalize |

---

## 2. TypeScript Types (`types/sales-order.types.ts`)

Infer all types from Zod schemas where possible. These are the raw API response shapes (not inferred — used for typing action return values):

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
  action: string           // 'Created' | 'Submitted' | 'RepApproved' | 'ManagerApproved' | 'Rejected' | 'RejectionAcknowledged' | 'Finalized' | 'Cancelled' | 'ItemsEdited'
  fromStatus: SalesOrderStatus | null
  toStatus: SalesOrderStatus | null
  performedBy: number
  performedByName: string | null
  performedAt: string      // ISO datetime
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
  // Audit trail
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
  // Standard audit
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
  submittedAt: string | null   // added in §0 change #2
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

---

## 3. Zod Schema (`schema/sales-order.schema.ts`)

```ts
// Create order
// distributorId: null is valid for Distributor role (server resolves from JWT).
// Admin role: UI enforces non-null via required <Select> field — schema permits null
// to avoid role-aware Zod logic in the schema layer.
export const createSalesOrderSchema = z.object({
  distributorId: z.number().int().positive().nullable(),
  notes: z.string().max(1000).nullable().optional(),
  items: z.array(z.object({
    productId: z.number().int().positive(),
    quantity: z.number().int().min(1),
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
    quantity: z.number().int().min(1),
    unitPrice: z.number().min(0),
    discount: z.literal(0),
  })).min(1, 'At least one item is required'),
})
export type UpdateSalesOrderInput = z.infer<typeof updateSalesOrderSchema>

// Reject / Cancel reason
export const rejectSalesOrderSchema = z.object({
  reason: z.string().min(5, 'Reason must be at least 5 characters').max(500),
})
export type RejectSalesOrderInput = z.infer<typeof rejectSalesOrderSchema>
```

---

## 4. Store

### Dialog Store (`store/sales-order.dialog-store.ts`)

Flat boolean pattern — one `isXOpen` flag + one `openX`/`closeX` pair per action. Single shared `selectedOrderId`.

```ts
type SalesOrderDialogState = {
  selectedOrderId: number | null
  // Action confirmation dialogs
  isSubmitOpen: boolean
  isRepApproveOpen: boolean
  isApproveOpen: boolean
  isAcknowledgeOpen: boolean
  isFinalizeOpen: boolean
  // Setters
  // openX(id): sets selectedOrderId = id, isXOpen = true
  // closeX():  sets isXOpen = false, selectedOrderId = null
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
// Reject and Cancel use local component state (inline form) — no dialog store entry needed
```

### Filter Store (`store/sales-order.filter-store.ts`)

```ts
type SalesOrderFilterState = {
  page: number        // default 1
  pageSize: number    // default 10
  search: string      // default ''
  status: string      // default '' (empty = all)
  fromDate: string    // default: today as 'YYYY-MM-DD'
  toDate: string      // default: today as 'YYYY-MM-DD'
  // Setters — all date/filter setters reset page to 1
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSearch: (search: string) => void          // resets page to 1
  setStatus: (status: string) => void          // resets page to 1
  setFromDate: (date: string) => void          // resets page to 1
  setToDate: (date: string) => void            // resets page to 1
  resetFilters: () => void                     // resets all to defaults
}
```

### Barrel (`store/index.ts`)

Exports composite selector hooks via `useShallow`:

```ts
export const useSubmitDialog = ()    // { isOpen, selectedOrderId, open, close }
export const useRepApproveDialog = () // { isOpen, selectedOrderId, open, close }
export const useApproveDialog = ()   // { isOpen, selectedOrderId, open, close }
export const useAcknowledgeDialog = () // { isOpen, selectedOrderId, open, close }
export const useFinalizeDialog = ()  // { isOpen, selectedOrderId, open, close }
export const useSalesOrderFilters = () // all filter state + all setters
```

---

## 5. Actions (`actions/sales-order.actions.ts`)

All actions use `requiredRole: undefined` (API enforces auth). `'use server'` at top of file.

| Action function | HTTP | Used by |
|----------------|------|---------|
| `getSalesOrdersAction(page, pageSize, search?, status?, fromDate?, toDate?)` | GET list | DataTable hook |
| `getSalesOrderByIdAction(id)` | GET by ID | detail hook |
| `createSalesOrderAction(data: CreateSalesOrderInput)` | POST create | create mutation |
| `updateSalesOrderAction(id, data: UpdateSalesOrderInput)` | PUT update | edit mutation |
| `submitSalesOrderAction(id)` | POST submit | submit mutation |
| `repApproveSalesOrderAction(id)` | POST rep-approve | rep-approve mutation |
| `approveSalesOrderAction(id)` | POST approve | approve mutation |
| `rejectSalesOrderAction(id, data: RejectSalesOrderInput)` | POST reject | reject mutation |
| `acknowledgeSalesOrderAction(id)` | POST acknowledge | acknowledge mutation |
| `finalizeSalesOrderAction(id)` | POST finalize | finalize mutation |
| `cancelSalesOrderAction(id, data: RejectSalesOrderInput)` | POST cancel — body: `{ reason }` | cancel mutation |
| `getDefaultPricingStructureAction()` | GET pricing-structures/default | create/edit page |

---

## 6. List Page (`/sales-orders`)

### Header
Standard `bg-muted/90 p-10 rounded-lg` banner:
- Title: `Sales Orders`, subtitle: `Manage and track your sales orders`
- **"Create Sales Order"** button (top-right, `<Plus />` icon) — visible only when `session.user.role === 'Admin' || session.user.role === 'Distributor'`. Navigates to `/sales-orders/new`.

### Toolbar
1. Date range picker (`<CalendarDatePicker />`) — `fromDate` / `toDate` from filter store, default today. On change: `setFromDate` + `setToDate` + page resets to 1. Sends as `fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD`.
2. Status filter `<Select>` — maps string value → `status` query param. Options: `''` All, `'Draft'`, `'PendingRepApproval'`, `'PendingManagerApproval'`, `'PendingDistributorFinalization'`, `'PendingDistributorAcknowledgement'`, `'Finalized'`, `'Cancelled'`.
3. Search `<Input>` — debounced, resets page to 1.
4. Column visibility toggle.

### Table Columns

| Column | Source | Notes |
|--------|--------|-------|
| Order Number | `orderNumber` | `<Link href={/sales-orders/${id}}>` |
| Distributor | `distributorName` | Hidden when `session.user.role === 'Distributor'` |
| Status | `status` | `<SalesOrderStatusBadge status={row.status} />` |
| Total Amount | `totalAmount` | `formatLKR(totalAmount)`, right-aligned |
| Submitted At | `submittedAt` | `format(date, 'dd MMM yyyy HH:mm')` or `—` |
| Created At | `createdAt` | `format(date, 'dd MMM yyyy HH:mm')` |
| Actions | — | `⋯` dropdown: **View** (→ `/sales-orders/[id]`), **Edit** (Draft only → `/sales-orders/[id]/edit`) |

### Status Badge Component (`<SalesOrderStatusBadge />`)

Shared component used on both list and detail pages:

| Status | `variant` | Extra className | Label |
|--------|----------|----------------|-------|
| 0 Draft | `secondary` | — | Draft |
| 1 PendingRepApproval | `outline` | `text-blue-600 border-blue-300` | Pending Rep Approval |
| 2 PendingManagerApproval | `outline` | `text-amber-600 border-amber-300` | Pending Manager Approval |
| 3 PendingDistributorFinalization | `outline` | `text-purple-600 border-purple-300` | Pending Finalization |
| 4 Finalized | `default` | — | Finalized |
| 5 Cancelled | `destructive` | — | Cancelled |
| 6 PendingDistributorAcknowledgement | `outline` | `text-orange-600 border-orange-300` | Pending Acknowledgement |

### DataTable Hook (`useSalesOrderDataTable`)

8-arg signature matching project convention. The `dateRange` arg uses `{ from_date, to_date }` snake_case keys (project convention for this slot — see `distributor.hooks.ts`). The filter store uses camelCase `fromDate`/`toDate`; the table component passes them in as `{ from_date: filters.fromDate, to_date: filters.toDate }`.

```ts
export function useSalesOrderDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange?: { from_date: string; to_date: string },  // snake_case — project convention
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: Record<string, unknown>,
)
// dateRange.from_date → fromDate query param
// dateRange.to_date   → toDate query param
// customFilters.status → status query param (enum name string, e.g. 'Draft')
// (useSalesOrderDataTable as any).isQueryHook = true
```

**Status param convention:** The API uses `Enum.TryParse<SalesOrderStatus>(status, ignoreCase: true)`, so it accepts enum name strings (`'Draft'`, `'PendingRepApproval'`, etc.). Do NOT send numeric strings (`'0'`, `'1'`).

### Loading / Empty / Error
- Loading: DataTable built-in skeleton
- Empty: `No sales orders found`
- Error: `handleErrorToast(error, 'sales order', 'fetch')`

---

## 7. Create Page (`/sales-orders/new`)

### Breadcrumb
`Sales Orders > New Order`

### Layout
`flex flex-row gap-6` — left `flex-1`, right `w-80 sticky top-6`

### Left Panel

**Order Details card:**
- `Distributor` — `<Select>` with distributor list. Visible only when `session.user.role === 'Admin'`. For Distributor role: hidden, server resolves from JWT (send `distributorId: null`).
- `Notes` — `<Textarea>` optional, max 1000 chars.

**Line Items card:**
- Loads `getDefaultPricingStructureAction()` on mount. While loading: `<Spinner />` centered in card. On error: inline error message.
- `+ Add Product` button appends a new empty row to the `items` array (react-hook-form `useFieldArray`).

Each row (`items[i]`):

| Field | Control | Behaviour |
|-------|---------|-----------|
| Product | `<Select>` | Options from `defaultPricingStructure.items`. Label: `${item.productItemDescription} (${item.productCode})`. On select: auto-fill SKU + price fields. |
| SKU | `<Input readOnly />` | `item.productCode` |
| Qty | `<Input type="number" min={1} />` | User editable |
| Dealer Case Price | `<Input readOnly />` | `formatLKR(item.dealerCasePrice ?? 0)`. If null, show `N/A` and disable Add. |
| Line Total | `<Input readOnly />` | `formatLKR(qty * dealerCasePrice)`, live-calculated |
| Remove | `<Button variant="ghost" size="icon">` 🗑 | Removes row via `useFieldArray.remove(i)` |

**Hidden fields per item** (sent in request body, not shown):
- `productId` — from selected pricing structure item
- `unitPrice` — `dealerCasePrice` value (number)
- `discount` — always `0`

**API request body:**
```ts
{
  distributorId: session.user.role === 'Admin' ? selectedDistributorId : null,
  notes: notes || null,
  items: items.map(i => ({
    productId: i.productId,
    quantity: i.quantity,
    unitPrice: i.unitPrice,      // = dealerCasePrice
    discount: 0,
  }))
}
```

On success: navigate to `/sales-orders/${result.id}`.

### Right Panel (Summary card)
- Items count
- Total: `formatLKR(sum of lineTotals)` — live recalculated
- `Save as Draft` — primary button, `isPending` shows `<Spinner />`
- `Cancel` — ghost button → back to `/sales-orders`

---

## 8. Edit Page (`/sales-orders/[id]/edit`)

Same layout as Create page. Differences:
- Breadcrumb: `Sales Orders > {orderNumber} > Edit`
- Page loads existing order via `getSalesOrderByIdAction(id)` on mount; pre-populates all fields
- Submit calls `updateSalesOrderAction(id, data)` → PUT `/api/v1/sales-orders/{id}`
- Only accessible when `order.status === 0` (Draft). If status is not Draft, redirect to `/sales-orders/[id]`.
- No Distributor selector (not editable after creation)
- Save button label: `Save Changes`

### Pre-population Mapping (existing order → form fields)

`SalesOrderDto.items` maps to `useFieldArray` rows as follows:

| Form field | Source from `SalesOrderItemDto` | Notes |
|-----------|--------------------------------|-------|
| `productId` | `item.productId` | |
| `quantity` | `item.quantity` | |
| `unitPrice` | `item.unitPrice` | Shown as Dealer Case Price — read-only display |
| `discount` | `item.discount` (always 0) | |
| SKU display | `item.productCode` | Read-only display field |
| Product label | `item.productDescription` | Shown as the selected option label |
| Case Price display | `item.unitPrice` | `formatLKR(item.unitPrice)` — no re-fetch from pricing structure needed |

Since the edit form displays `unitPrice` directly from the stored order (not re-fetched from the pricing structure), the pricing structure is still loaded on mount to populate the product `<Select>` options, but each existing row's price display comes from the saved `unitPrice` value.

`order.notes` → Notes textarea default value.

---

## 9. Detail Page (`/sales-orders/[id]`)

### Breadcrumb
`Sales Orders > {orderNumber}`

### Layout
`flex flex-row gap-6` — left `flex-1`, right `w-80`

### Left Column

**Order header card:**
- Order number (large bold `text-2xl font-bold`)
- Distributor name, Created date

**Line items table (read-only):**

| Column | Source | Format |
|--------|--------|--------|
| Product Name | `item.productDescription` | |
| SKU | `item.productCode` | `font-mono text-sm` |
| Qty | `item.quantity` | right-aligned |
| Unit Price | `item.unitPrice` | `formatLKR(unitPrice)` |
| Line Total | `item.lineTotal` | `formatLKR(lineTotal)` |

Footer row: **Total** right-aligned `formatLKR(order.totalAmount)` in bold.

**Notes section:** shown only if `order.notes` is non-null and non-empty.

**History Timeline:**

Each `order.history` entry:

```
● [dot]  [action label]  by [performedByName ?? 'User #' + performedBy]  ·  [dd MMM yyyy HH:mm]
         [notes if present]
```

Action → human-readable label:

| `action` string | Label | Dot color class |
|----------------|-------|----------------|
| `Created` | Created | `bg-gray-400` |
| `Submitted` | Submitted for review | `bg-blue-500` |
| `RepApproved` | Approved by Sales Rep | `bg-green-500` |
| `ManagerApproved` | Approved by Manager | `bg-green-600` |
| `Rejected` | Rejected | `bg-red-500` |
| `RejectionAcknowledged` | Rejection acknowledged | `bg-orange-500` |
| `Finalized` | Finalized | `bg-purple-500` |
| `Cancelled` | Cancelled | `bg-red-600` |
| `ItemsEdited` | Items edited | `bg-gray-400` |

### Right Sidebar (`sticky top-6`)

**Status card:**
- `<SalesOrderStatusBadge />` (large, `text-sm`)
- Description line:

| Status | Description |
|--------|------------|
| 0 Draft | Awaiting submission by distributor |
| 1 PendingRepApproval | Awaiting Sales Rep review |
| 2 PendingManagerApproval | Awaiting Manager approval |
| 3 PendingDistributorFinalization | Awaiting distributor finalization |
| 4 Finalized | Order has been finalized |
| 5 Cancelled | Order has been cancelled |
| 6 PendingDistributorAcknowledgement | Distributor must acknowledge the rejection |

**Actions section:**

Buttons rendered based on `order.status` × `session.user.role`. Explicit render guard conditions:

```ts
const role = session.user.role
const status = order.status

// Submit & Cancel: shown for Distributor or Admin at Draft
const showSubmit  = status === 0 && (role === 'Distributor' || role === 'Admin')
const showCancel  = status === 0 && (role === 'Distributor' || role === 'Admin')

// Rep Approve & Reject (at status 1): SalesRep or Admin only
const showRepApprove = status === 1 && (role === 'SalesRep' || role === 'Admin')
const showReject1    = status === 1 && (role === 'SalesRep' || role === 'Admin')

// Manager Approve & Reject (at status 2): Manager or Admin only
const showApprove = status === 2 && (role === 'Manager' || role === 'Admin')
const showReject2 = status === 2 && (role === 'Manager' || role === 'Admin')

// Combined reject visibility:
const showReject = showReject1 || showReject2

const showAcknowledge = status === 6 && (role === 'Distributor' || role === 'Admin')
const showFinalize    = status === 3 && (role === 'Distributor' || role === 'Admin')
```

The action table below summarises the same logic visually:

| Status | Distributor | SalesRep | Manager | Admin |
|--------|-------------|----------|---------|-------|
| 0 Draft | [Submit] [Cancel] | — | — | [Submit] [Cancel] |
| 1 PendingRepApproval | — | [Rep Approve] [Reject] | — | [Rep Approve] [Reject] |
| 2 PendingManagerApproval | — | — | [Approve] [Reject] | [Approve] [Reject] |
| 6 PendingDistributorAcknowledgement | [Acknowledge] | — | — | [Acknowledge] |
| 3 PendingDistributorFinalization | [Finalize] | — | — | [Finalize] |
| 4 Finalized / 5 Cancelled | _(no buttons)_ | | | |

**Submit / Rep Approve / Approve / Acknowledge / Finalize** → `AlertDialog`:

| Action | Title | Body |
|--------|-------|------|
| Submit | Submit Order | Submit this order for Sales Rep review? This cannot be undone. |
| Rep Approve | Approve Order | Approve this order and forward to Manager for final approval? |
| Approve | Approve Order | Approve this order? The distributor will be asked to finalize. |
| Acknowledge | Acknowledge Rejection | Confirm you have read the rejection reason. The order will be cancelled. |
| Finalize | Finalize Order | Finalize this order? This action cannot be undone. |

**Reject / Cancel** → inline form (no dialog):

Clicking Reject or Cancel shows below the button:
```
[  Reason...                    ]   ← <Textarea>, 3 rows
[ Confirm ]   [ ← back ]
```
- `reason` field — required, min 5 chars, max 500 chars (validated by `rejectSalesOrderSchema`)
- Inline error below textarea if invalid
- `Confirm` calls `rejectSalesOrderAction(id, { reason })` or `cancelSalesOrderAction(id, { reason })`
- `← back` link collapses the form back to buttons
- `Confirm` button shows `<Spinner />` while `isPending`

**Audit Trail card:**

Grid, only rows where value is non-null:

| Label | Source |
|-------|--------|
| Created by | `order.createdBy` |
| Created at | `format(order.createdAt, 'dd MMM yyyy HH:mm')` |
| Submitted by | `order.submittedBy` |
| Submitted at | `order.submittedAt` |
| Rep approved by | `order.repApprovedBy` |
| Rep approved at | `order.repApprovedAt` |
| Manager approved by | `order.managerApprovedBy` |
| Manager approved at | `order.managerApprovedAt` |
| Rejection reason | `order.cancelReason` |
| Acknowledged by | `order.acknowledgedBy` |
| Acknowledged at | `order.acknowledgedAt` |
| Finalized by | `order.finalizedBy` |
| Finalized at | `order.finalizedAt` |
| Cancelled by | `order.cancelledBy` |
| Cancelled at | `order.cancelledAt` |

### Loading / Error States
- Page loading: centered `<Spinner />` (full page height)
- 404 / not found: `Order not found` message + `← Back to Sales Orders` link
- Mutation loading: button disabled + `<Spinner />` inline
- Mutation error: `handleErrorToast(error, 'sales order', 'action')`

---

## 10. API Endpoints Reference

| Action | Method + Path | Body | Roles (API-enforced) |
|--------|--------------|------|---------------------|
| List | `GET /api/v1/sales-orders?page&pageSize&search&status&fromDate&toDate` | — | All |
| Get by ID | `GET /api/v1/sales-orders/{id}` | — | All |
| Create | `POST /api/v1/sales-orders` | `CreateSalesOrderRequest` | Distributor, Admin |
| Update | `PUT /api/v1/sales-orders/{id}` | `UpdateSalesOrderRequest` | Distributor, SalesRep, Manager, Admin (status-gated) |
| Submit | `POST /api/v1/sales-orders/{id}/submit` | — | Distributor, Admin |
| Rep Approve | `POST /api/v1/sales-orders/{id}/rep-approve` | — | SalesRep, Admin |
| Approve | `POST /api/v1/sales-orders/{id}/approve` | — | Manager, Admin |
| Reject | `POST /api/v1/sales-orders/{id}/reject` | `{ reason: string }` | SalesRep, Manager, Admin |
| Acknowledge | `POST /api/v1/sales-orders/{id}/acknowledge` | — | Distributor, Admin |
| Finalize | `POST /api/v1/sales-orders/{id}/finalize` | — | Distributor, Admin |
| Cancel | `POST /api/v1/sales-orders/{id}/cancel` | `{ reason: string }` | Distributor, Admin |
| Default pricing structure | `GET /api/v1/pricing-structures/default` | — | All authenticated |

All `requiredRole` values in server actions: `undefined` (API enforces).

---

## 11. File Structure

```
features/sales-order/
├── actions/
│   └── sales-order.actions.ts
├── components/
│   ├── columns/
│   │   └── sales-order-columns.tsx
│   ├── dialogs/
│   │   └── sales-order-dialogs.tsx       ← AlertDialogs: Submit, RepApprove, Approve, Acknowledge, Finalize
│   ├── forms/
│   │   └── sales-order-form.tsx           ← Line items builder (shared by create + edit pages)
│   ├── pages/
│   │   ├── sales-order-list-page.tsx
│   │   ├── sales-order-create-page.tsx
│   │   ├── sales-order-edit-page.tsx
│   │   └── sales-order-detail-page.tsx
│   ├── table/
│   │   └── sales-order-table.tsx
│   ├── sales-order-status-badge.tsx       ← shared badge component
│   └── index.ts
├── hooks/
│   └── sales-order.hooks.ts
├── schema/
│   └── sales-order.schema.ts
├── store/
│   ├── sales-order.dialog-store.ts
│   ├── sales-order.filter-store.ts
│   └── index.ts
└── types/
    └── sales-order.types.ts

app/(protected)/sales-orders/
├── page.tsx                   → <SalesOrderListPage />
├── new/
│   └── page.tsx               → <SalesOrderCreatePage />
└── [id]/
    ├── page.tsx               → <SalesOrderDetailPage />
    └── edit/
        └── page.tsx           → <SalesOrderEditPage />
```

---

## 12. Currency Formatting

```ts
// lib/utils.ts — add this helper
export function formatLKR(value: number): string {
  return new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR' }).format(value)
}
```
