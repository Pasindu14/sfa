# SFA API Production-Readiness — Task List
*Generated: 2026-03-27 | Source: `/dotnet-api-reviewer` audit*

## Critical

- [x] **1. [C1] Add distributed lock to `PurchaseOrderService.UpdateAsync`**
  - File: `sfa_api/Features/PurchaseOrders/Services/PurchaseOrderService.cs` line 161
  - Acquire `po:transition:{id}` lock at the top of `UpdateAsync`, matching all transition methods
  - Without this, concurrent edits produce duplicate PO line items (data corruption)

---

## High

- [x] **2. [H1] Scope `GetExistingVchBillNosAsync` to batch bill numbers only**
  - File: `sfa_api/Features/SalesInvoices/Repositories/SalesInvoiceRepository.cs` line 31
  - Change signature to accept `IEnumerable<string> vchBillNosToCheck` and filter with `WHERE VchBillNo IN (...)`
  - Currently loads ALL historical VchBillNos into a `HashSet<string>` on every import → OOM at scale

- [x] **3. [H2] Scope import lookup dictionaries to batch identifiers**
  - File: `sfa_api/Features/SalesInvoices/Services/SalesInvoiceService.cs` lines 36–38
  - `GetDistributorAliasDictionaryAsync`, `GetProductErpCodeDictionaryAsync`, `GetPurchaseOrderNumberDictionaryAsync` all load full tables
  - Extract batch aliases/codes/PO numbers before calling; pass as filter parameters to each repo method

- [x] **4. [H3] Fix `AuthRepository` email/username comparison to restore index use**
  - File: `sfa_api/Features/Auth/Repositories/AuthRepository.cs` lines 17, 23
  - Replace `x.Email.ToLower() == email.ToLower()` with `EF.Functions.ILike(x.Email, email)`
  - Add migration: `CREATE UNIQUE INDEX IX_Users_Email_Lower ON "Users" (LOWER("Email")) WHERE "IsActive" = true`
  - Current code translates to `LOWER("Email") = LOWER(@p)` → prevents B-tree index use → sequential scan on every login

- [x] **5. [H4] Wrap `RedisDistributedLockService.AcquireAsync` in try/catch for `RedisConnectionException`**
  - File: `sfa_api/Infrastructure/Locking/RedisDistributedLockService.cs` line 14
  - Catch `RedisConnectionException` / `SocketException` and throw `InfrastructureException("LOCK_SERVICE_UNAVAILABLE", ...)`
  - Currently propagates as HTTP 500 `INTERNAL_ERROR` instead of HTTP 503 — compare `DistributedCacheService` which already does this correctly

- [x] **6. [H5] Wire OpenTelemetry in `Program.cs`**
  - File: `sfa_api/Program.cs` — packages installed, `AddOpenTelemetry()` never called
  - Add `builder.Services.AddOpenTelemetry().WithTracing(...).WithMetrics(...)` with `AddAspNetCoreInstrumentation`, `AddEntityFrameworkCoreInstrumentation`, `AddOtlpExporter`
  - Zero distributed traces or metrics emitted to any backend in production

---

## Medium

- [x] **7. [M1] Add `ILogger<SalesInvoiceService>` to `SalesInvoiceService`**
  - File: `sfa_api/Features/SalesInvoices/Services/SalesInvoiceService.cs` line 12
  - Inject logger and add log events for import start, partial failure, and batch completion
  - Currently all import logic (including `PartialFailed` status) is completely silent in application logs

- [x] **8. [M2] Add `CommandTimeout(30)` and `EnableRetryOnFailure(3)` to `UseNpgsql`**
  - File: `sfa_api/Program.cs` line 47–49
  - Add `npgsql => npgsql.CommandTimeout(30).EnableRetryOnFailure(3)` to `UseNpgsql`
  - Without a timeout, runaway queries hold thread-pool threads indefinitely

- [x] **9. [M3] Add pagination to `StockRepository.GetStockByDistributorAsync`**
  - File: `sfa_api/Features/Stock/Repositories/StockRepository.cs` line 11
  - Add `page`/`pageSize` with `Math.Clamp(pageSize, 1, 200)` — matches pattern used by all other list repositories
  - Currently returns all DistributorStock rows for a distributor with no limit

- [x] **10. [M4] Add `.AsSplitQuery()` to `PurchaseOrderRepository.GetByIdWithItemsAsync` and `GetAllAsync`**
  - File: `sfa_api/Features/PurchaseOrders/Repositories/PurchaseOrderRepository.cs` lines 17–21, 34–37
  - Both queries use multiple `Include` chains — EF Core generates Cartesian product joins without `AsSplitQuery`

- [x] **11. [M5] Add trigram GIN indexes for all `ILike('%term%')` search columns**
  - Files: Multiple repositories (Regions, Areas, Territories, Divisions, Users, Distributors, Products, PurchaseOrders, Outlets, Routes, PricingStructures)
  - Add migration: `CREATE EXTENSION IF NOT EXISTS pg_trgm` + GIN indexes on all searched Name/Email/Code columns
  - Without `pg_trgm`, every search request is a sequential scan

- [x] **12. [M6] Add composite covering index on `StockTransaction (DistributorId, ProductId, TransactedAt DESC)`**
  - File: `AppDbContext.cs` lines 572–575 — current indexes are three separate single-column indexes
  - Migration: `CREATE INDEX IX_StockTransactions_Distributor_Product_Date ON "StockTransactions" ("DistributorId", "ProductId", "TransactedAt" DESC)`

- [x] **13. [M7] Add `ProductId` index to `PurchaseOrderItem`, `GRNItem`, `SalesInvoiceItem`**
  - File: `AppDbContext.cs` lines 387–398, 526–538, 476–491
  - Three separate `e.HasIndex(x => x.ProductId)` additions in `OnModelCreating`
  - Needed for product reverse-navigation queries (discontinuation checks, ERP reconciliation)

- [x] **14. [M8] Add partial `IsActive = true` indexes for query-filtered entities**
  - Affected entities: Region, Area, Territory, Division, Route, Outlet (all have `HasQueryFilter(x => x.IsActive)`)
  - Migration: replace full B-tree indexes with `WHERE "IsActive" = true` partial indexes on Name and UpdatedAt columns
  - Reduces index size and speeds up the active-records query path (the only path EF ever uses for these entities)

- [x] **15. [M9] Connect `ICacheService` to reference-data list endpoints**
  - File: All service files under Regions, Areas, Territories, Divisions, Products, Distributors
  - Apply cache-aside pattern: check cache → on miss, query DB → store with 5-minute TTL → invalidate on write
  - `ICacheService` is fully built and registered in DI but has zero callers in the Features layer

---

## Low

- [ ] **16. [L1] Fix `SalesInvoiceRepository` search to use `ILike` instead of `Contains()`**
  - File: `sfa_api/Features/SalesInvoices/Repositories/SalesInvoiceRepository.cs` lines 59–61
  - Replace `.VchBillNo.Contains(search)` etc. with `EF.Functions.ILike(x.VchBillNo, $"%{search}%")`
  - Only repository using case-sensitive `LIKE` — inconsistent with the rest of the codebase

- [ ] **17. [L2] Add response compression middleware**
  - File: `sfa_api/Program.cs`
  - Add `AddResponseCompression` (Brotli + Gzip) to services and `UseResponseCompression()` to pipeline before `UseCors`
  - Reduces JSON payload 60–80%; meaningful improvement for mobile clients on constrained networks

- [ ] **18. [L3] Switch `AddDbContext` to `AddDbContextPool`**
  - File: `sfa_api/Program.cs` line 47
  - Verify `AuditInterceptor` compatibility (safe — it reads `HttpContext` at save-time, stores no state on the context)
  - Reduces per-request heap allocation for DB operations

- [ ] **19. [L4] Add composite covering indexes on `PurchaseOrder` and `SalesInvoice` list queries**
  - Migration:
    - `CREATE INDEX IX_PurchaseOrders_Distributor_Status_Date ON "PurchaseOrders" ("DistributorId", "Status", "CreatedAt" DESC)`
    - `CREATE INDEX IX_SalesInvoices_Distributor_Status_Date ON "SalesInvoices" ("DistributorId", "Status", "InvoiceDate" DESC)`

- [ ] **20. [L5] Add standalone `ProductId` index to `DistributorStock`**
  - File: `AppDbContext.cs` line 546 — only composite `(DistributorId, ProductId)` exists
  - Add `e.HasIndex(x => x.ProductId)` to enable "which distributors hold product X?" queries

- [ ] **21. [L6] Add Npgsql connection pool tuning to connection string**
  - Append `Minimum Pool Size=5;Maximum Pool Size=50;Connection Idle Lifetime=300` to `DefaultConnection`
  - Prevents cold-start latency spikes and connection exhaustion under concurrent load

---

## Summary

| Severity | Count | Est. Total Effort |
|---|---|---|
| Critical | 1 | Low |
| High | 5 | Low–Medium |
| Medium | 9 | Medium |
| Low | 6 | Low |
| **Total** | **21** | |

**Recommended sequence:** C1 → H1 → H2 → H3 → H4 → H5 → M1 → M2 → M3 → M4 → M5–M8 (migration batch) → M9 → L1–L6
