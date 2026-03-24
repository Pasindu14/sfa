# Sales Invoice Import — Full Plan

## Overview

Import BUSY ERP-generated sales voucher Excel files into SFA. The flow is:
**Excel Upload → SalesInvoice → GRN (full receipt) → Stock Update**

Source file format: `UGB_ListofSalesVouchers510.xlsx`
- Sparse row format: voucher header row embeds the first line item; continuation rows carry only item columns
- ~136 rows × 15 columns per typical export (col 0 = spacer, cols 1–14 = data)
- 11 vouchers, 62 line items in sample file

---

## Excel Column Mapping

| Col | Header | Description | Example |
|-----|--------|-------------|---------|
| 1 | Vch Date | Invoice date (header rows only) | `15-Jan-25` |
| 2 | Vch/Bill No | BUSY invoice number — **idempotency key** | `BIS/25/4752` |
| 3 | BUSY Order Request | BUSY internal order ref | `SOR-26-00010` |
| 4 | SFA PO | SFA purchase order number | `PO-2026-00001` |
| 5 | Alias | BUSY distributor numeric code | `350032` |
| 6 | Party Name | Distributor name (display only) | `UGB DISTRIBUTORS` |
| 7 | Item Alias | BUSY product code | `CF01` |
| 8 | Item Description | Product name | `CLOUD 9 FRESH MINT 20S X 60P` |
| 9 | QTY | Quantity | `200` |
| 10 | Unit | Unit of measure | `CTN` |
| 11 | Rate | Unit price | `385.00` |
| 12 | Amount | Line total | `77000.00` |
| 13 | Free Issue | Free issue flag | `Y` / blank |
| 14 | Gross Amount | Invoice total (header rows only) | `2,299,700.00` |

### Sparse Row Parsing Logic

- **Header row**: Col 1 (Date) is NOT null → new voucher begins; also contains the first line item
- **Continuation row**: Col 1 (Date) is null → item-only row belonging to current voucher

### Key Relationships from Data

| Relationship | Cardinality |
|---|---|
| SFA PO → BUSY Order Requests | 1:many |
| BUSY Order Request → Vch/Bill Nos | 1:many |
| Vch/Bill No | always unique — atomic dispatch document |

---

## ERP Code Resolution

| SFA Entity | ERP Bridge Field | Notes |
|---|---|---|
| `Distributor` | `Alias` (int, already exists) | BUSY numeric code e.g. `350032` |
| `Product` | `ErpCode` (string, **add this**) | BUSY item alias e.g. `CF01` |

`Distributor.Alias` is already an `int` field on the entity — no migration needed.
`Product.ErpCode` must be added as `string?`, nullable, unique index.

---

## Database Schema

### Table Diagram

```
[Excel Upload]
     ↓
SalesInvoiceImportBatches  ←─ one per file upload (audit + error log)
     ↓ (1:many)
SalesInvoices              ←─ one per Vch/Bill No (BIS/25/4752)
     ↓ (1:many)
SalesInvoiceItems          ←─ one per line item
     ↓ (1:1, on GRN creation)
GRNs                       ←─ full-receipt confirmation
     ↓ (1:many)
GRNItems                   ←─ copy of invoice items at receipt time
     ↓ (on GRN confirm)
DistributorStocks          ←─ current on-hand per distributor+product (upserted)
StockTransactions          ←─ immutable append-only ledger (never updated)
```

---

### Modified Entities

#### `Product` — add `ErpCode`

```csharp
public string? ErpCode { get; set; }  // BUSY item alias e.g. "CF01" — nullable, unique
```

AppDbContext:
```csharp
e.HasIndex(x => x.ErpCode).IsUnique().HasFilter("\"ErpCode\" IS NOT NULL");
```

---

### New Entities

#### 1. `SalesInvoiceImportBatch` — one row per Excel upload

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | identity |
| `BatchNumber` | string unique | `IMP-2026-00001` auto-generated |
| `FileName` | string | original `.xlsx` filename |
| `TotalInvoices` | int | count of vouchers in file |
| `TotalItems` | int | count of all line items |
| `TotalAmount` | decimal(18,2) | sum of all invoice totals |
| `Status` | enum string | `Processing / Completed / PartialFailed / Failed` |
| `ErrorSummary` | text? | JSON array of skipped rows + reasons |
| `ImportedBy` | int FK→Users | who uploaded |
| `ImportedAt` | DateTime | |
| `Notes` | string? | |
| `CreatedAt` | DateTime | audit |
| `UpdatedAt` | DateTime | audit |
| `IsActive` | bool | soft delete |

---

#### 2. `SalesInvoice` — one row per BUSY Vch/Bill No

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | identity |
| `VchBillNo` | string unique | `BIS/25/4752` — **idempotency key**, prevents duplicate imports |
| `BusyOrderRequestNo` | string? | `SOR-26-00010` — raw BUSY ref, no FK (BUSY is external) |
| `SfaPoNumber` | string? | `PO-2026-00001` — raw string from Excel, preserved for audit |
| `PurchaseOrderId` | int? FK→PurchaseOrders | resolved from `SfaPoNumber` at import time; null if PO not found |
| `DistributorId` | int FK→Distributors | resolved from `Alias` column |
| `InvoiceDate` | DateOnly | Vch Date column |
| `InvoiceType` | enum string | `Regular / FreeIssue` |
| `TotalAmount` | decimal(18,2) | gross from Excel |
| `ImportBatchId` | int FK→SalesInvoiceImportBatches | which import created this |
| `Status` | enum string | `Pending / GrnReceived / Disputed` |
| `Notes` | string? | |
| `CreatedAt` | DateTime | audit |
| `UpdatedAt` | DateTime | audit |
| `CreatedBy` | int? | audit |
| `UpdatedBy` | int? | audit |
| `IsActive` | bool | soft delete |

> **Why `SfaPoNumber` AND `PurchaseOrderId`?**
> If the PO doesn't exist in SFA at import time, the FK is null but the raw string is preserved.
> Allows reconciliation query: `WHERE SfaPoNumber IS NOT NULL AND PurchaseOrderId IS NULL`

---

#### 3. `SalesInvoiceItem` — line items

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | identity |
| `SalesInvoiceId` | int FK→SalesInvoices | cascade delete |
| `ProductId` | int FK→Products | resolved from `ItemErpCode` |
| `ItemErpCode` | string | raw BUSY alias stored for audit (`CF01`) |
| `ItemDescription` | string | raw description from Excel |
| `Quantity` | decimal(18,4) | QTY column |
| `Unit` | string | CTN / PCS |
| `UnitPrice` | decimal(18,2) | Rate column |
| `TotalPrice` | decimal(18,2) | Amount column |
| `IsFreeIssue` | bool | from Free Issue Y column |
| `LineNumber` | int | order within invoice (1-based) |

---

#### 4. `GRN` (Goods Received Note) — full receipt only, 1:1 with SalesInvoice

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | identity |
| `GrnNumber` | string unique | `GRN-2026-00001` auto-generated |
| `SalesInvoiceId` | int FK→SalesInvoices **unique** | enforces 1:1 |
| `DistributorId` | int FK→Distributors | denormalized for fast queries |
| `Status` | enum string | `Pending / Confirmed / Disputed` |
| `ReceivedAt` | DateTime? | when goods physically arrived |
| `ConfirmedBy` | int? FK→Users | who clicked Confirm |
| `ConfirmedAt` | DateTime? | |
| `Notes` | string? | |
| `CreatedAt` | DateTime | audit |
| `UpdatedAt` | DateTime | audit |
| `CreatedBy` | int? | audit |
| `UpdatedBy` | int? | audit |
| `IsActive` | bool | soft delete |

No partial GRNs — full receipt only. One invoice = one GRN.

---

#### 5. `GRNItem` — snapshot of items at receipt time

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | identity |
| `GrnId` | int FK→GRNs | cascade delete |
| `ProductId` | int FK→Products | |
| `Quantity` | decimal(18,4) | copied from SalesInvoiceItem |
| `Unit` | string | |
| `Notes` | string? | per-item dispute notes |

> Items are copied (not referenced) because invoices could theoretically change after the fact.
> GRNItem captures reality at receipt time.

---

#### 6. `DistributorStock` — current on-hand snapshot

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | identity |
| `DistributorId` | int FK→Distributors | composite unique key |
| `ProductId` | int FK→Products | composite unique key |
| `QuantityOnHand` | decimal(18,4) | live running balance |
| `LastUpdatedAt` | DateTime | |

Unique constraint: `(DistributorId, ProductId)`

---

#### 7. `StockTransaction` — immutable append-only ledger

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | identity |
| `DistributorId` | int FK→Distributors | indexed |
| `ProductId` | int FK→Products | indexed |
| `TransactionType` | enum string | `GRNReceipt / Sale / FreeIssue / Return / Damage / Opening` |
| `Direction` | enum string | `In / Out` |
| `Quantity` | decimal(18,4) | always positive |
| `QuantityBefore` | decimal(18,4) | snapshot BEFORE this transaction |
| `QuantityAfter` | decimal(18,4) | snapshot AFTER — computed at insert |
| `ReferenceType` | string | `"GRN"`, `"Sale"`, etc. |
| `ReferenceId` | int | ID of source document |
| `TransactedAt` | DateTime | event time |
| `TransactedBy` | int FK→Users | who triggered it |
| `Notes` | string? | |

**Never update or delete rows.** This is an audit ledger.

---

## Import Flow

### Step A — Next.js Server Action (SheetJS parse)

```
User selects .xlsx
  ↓
Server Action:
  1. Read file buffer
  2. SheetJS: XLSX.read(buffer) → sheet_to_json({ header: 1 })
  3. Skip rows 0–6 (BUSY header junk)
  4. Group rows into vouchers:
     - Row where col[1] !== null → new voucher header (also first item)
     - Row where col[1] === null → continuation item for current voucher
  5. Build payload JSON
  6. POST /api/v1/sales-invoices/import

Payload shape:
{
  fileName: "UGB_ListofSalesVouchers510.xlsx",
  invoices: [
    {
      vchBillNo: "BIS/25/4752",
      busyOrderRequestNo: "SOR-26-00010",
      sfaPoNumber: "PO-2026-00001",
      distributorAlias: 350032,
      invoiceDate: "2025-01-15",
      invoiceType: "Regular",          // or "FreeIssue"
      totalAmount: 2299700.00,
      items: [
        {
          itemErpCode: "CF01",
          itemDescription: "CLOUD 9 FRESH MINT 20S X 60P",
          quantity: 200,
          unit: "CTN",
          unitPrice: 385.00,
          totalPrice: 77000.00,
          isFreeIssue: false,
          lineNumber: 1
        }
      ]
    }
  ]
}
```

**Size estimate for 3000 rows:**
- ~240 voucher headers + ~2500 items ≈ 380 KB JSON
- SheetJS parse: ~80 ms
- Well under Kestrel's 30 MB default limit
- `next.config.js` needs: `experimental.serverActions.bodySizeLimit: "5mb"`

---

### Step B — .NET API Import Endpoint

`POST /api/v1/sales-invoices/import`

```
1.  Create ImportBatch row (Status = Processing)
2.  Load ALL Distributors into alias→id dictionary  (in-memory)
3.  Load ALL Products into erpCode→id dictionary    (in-memory)
4.  Load ALL PurchaseOrders into orderNumber→id dict (in-memory)
5.  Load existing VchBillNos into HashSet            (dedup check)
6.  For each invoice in payload:
    a. If VchBillNo already in HashSet → skip, add to ErrorSummary
    b. Resolve DistributorId via alias dict
       → if not found: skip, add to ErrorSummary
    c. Resolve PurchaseOrderId via PO number dict (nullable OK)
    d. Build SalesInvoice entity
    e. For each item:
       - Resolve ProductId via erpCode dict
       - Build SalesInvoiceItem entity
7.  context.AddRange(allInvoices)
    context.AddRange(allItems)         ← single round-trip
    await context.SaveChangesAsync()
8.  Update ImportBatch: Status = Completed, counts, totals
9.  Return ImportBatchSummaryDto
```

**Response:**
```json
{
  "batchNumber": "IMP-2026-00001",
  "totalInvoices": 11,
  "importedInvoices": 10,
  "skippedInvoices": 1,
  "totalItems": 62,
  "totalAmount": 24421917.52,
  "errors": [
    { "vchBillNo": "BIS/25/4752", "reason": "Already imported" }
  ]
}
```

---

## GRN Flow

### Create GRN

`POST /api/v1/grns`
Body: `{ salesInvoiceId: 42 }`

```
1. Validate SalesInvoice exists and Status = Pending
2. Validate no GRN already exists for this invoice (SalesInvoiceId unique)
3. Create GRN (Status = Pending)
4. Copy SalesInvoiceItems → GRNItems
5. SalesInvoice.Status → GrnReceived
6. SaveChangesAsync()
7. Return GRN summary
```

### Confirm GRN

`PATCH /api/v1/grns/{id}/confirm`
Body: `{ receivedAt: "2025-01-20T10:00:00Z", notes?: "..." }`

```
1. Validate GRN exists and Status = Pending
2. GRN.Status → Confirmed, ConfirmedBy/At set
3. For each GRNItem (all within single DB transaction):
   a. Load DistributorStock WHERE (DistributorId, ProductId)
      → if null, create new with QuantityOnHand = 0
   b. QuantityBefore = current QuantityOnHand
   c. QuantityAfter  = QuantityBefore + item.Quantity
   d. UPDATE DistributorStock.QuantityOnHand = QuantityAfter
   e. INSERT StockTransaction {
        Type = GRNReceipt, Direction = In,
        QuantityBefore, QuantityAfter,
        ReferenceType = "GRN", ReferenceId = grnId
      }
4. SaveChangesAsync()  ← single transaction covers all stock updates
```

---

## Tracking Capabilities

| Question | How to Answer |
|----------|--------------|
| Which batch imported this invoice? | `SalesInvoice.ImportBatchId` → `SalesInvoiceImportBatch` |
| What PO does this invoice fulfill? | `SalesInvoice.PurchaseOrderId` → `PurchaseOrder` |
| Invoices with unresolved POs? | `WHERE SfaPoNumber IS NOT NULL AND PurchaseOrderId IS NULL` |
| Has this invoice been GRN'd? | `SalesInvoice.Status = GrnReceived` + `GRN.SalesInvoiceId` |
| Current stock for distributor X? | `DistributorStocks WHERE DistributorId = X` |
| Full stock history for product Y? | `StockTransactions WHERE ProductId = Y ORDER BY TransactedAt` |
| Who confirmed GRN #123? | `GRN.ConfirmedBy` → `Users` |
| Was this invoice imported twice? | `VchBillNo` unique → blocked at DB + shows in `ErrorSummary` |
| Stock before this delivery? | `StockTransaction.QuantityBefore` snapshot |
| Failed imports from batch X? | `ImportBatch.ErrorSummary` (JSON) |
| All invoices for PO-2026-00001? | `SalesInvoices WHERE PurchaseOrderId = X` |
| All free-issue invoices? | `SalesInvoices WHERE InvoiceType = 'FreeIssue'` |
| Free-issue items within a regular invoice? | `SalesInvoiceItems WHERE IsFreeIssue = true` |

---

## Implementation Order

```
Step 1   Product.ErpCode — add field + migration
         (prerequisite for all product resolution logic)

Step 2   SalesInvoiceImportBatch entity + migration

Step 3   SalesInvoice + SalesInvoiceItem entities + migration

Step 4   GRN + GRNItem entities + migration

Step 5   DistributorStock + StockTransaction entities + migration

Step 6   .NET API — SalesInvoices import feature
         (ImportController, ImportService, ImportRepository)

Step 7   .NET API — GRNs feature
         (create endpoint + confirm endpoint)

Step 8   .NET API — Stock update logic
         (inside GRN confirm service method)

Step 9   Next.js — SheetJS Server Action
         (parse sparse Excel, build payload, POST to API)

Step 10  Next.js — Import dialog + batch result UI

Step 11  Next.js — SalesInvoice list page + detail drawer

Step 12  Next.js — GRN list + confirm flow

Step 13  Next.js — Stock dashboard per distributor
```

---

## Enum Values Reference

```csharp
// SalesInvoiceImportBatchStatus
Processing, Completed, PartialFailed, Failed

// SalesInvoiceType
Regular, FreeIssue

// SalesInvoiceStatus
Pending, GrnReceived, Disputed

// GrnStatus
Pending, Confirmed, Disputed

// StockTransactionType
GRNReceipt, Sale, FreeIssue, Return, Damage, Opening

// StockTransactionDirection
In, Out
```

---

## Next.js Config Change Required

```js
// next.config.js
module.exports = {
  experimental: {
    serverActions: {
      bodySizeLimit: "5mb"
    }
  }
}
```

Default Server Action body limit is 1 MB. A 3000-row file parses to ~380 KB JSON — bumping to 5 MB gives comfortable headroom.
