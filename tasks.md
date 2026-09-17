# Performance Improvement Tasks

Source: static code review of `sfa_api`, `sfa_web`, `sfa_mobile` (2026-09-17). Nothing was profiled — verify impact before/after each change.

Legend: **[API]** `sfa_api/sfa_api/` · **[WEB]** `sfa_web/` · **[MOB]** `sfa_mobile/lib/`

## Status (2026-09-17, branch `feature/perf-billing-sync-cache-auth`)

26 done, 18 open. Tests green at commit: API 1147 unit + 638 integration (8 skipped) · mobile 126 · web tsc/lint (no new)/build.

**Implementation notes (differ from original plan):**
- T1.3 uses `SendEachAsync` batches of 500 via bounded `Channel` (10k); queued pushes lost on shutdown (inbox rows already persisted).
- T1.4 uses random version tokens per key segment (`cachever:{segment}`); sales-summary only cleared on approve / cancel-of-approved.
- T1.8 falls back to Postgres + 30s local "not revoked" cache when Redis is unavailable.
- T2.1 kept query portable (correlated subquery) instead of `DISTINCT ON`.
- T2.2 default is **unbounded** (all reps' last-ever ping, original behaviour) via `LATERAL` per user; `LocationPings:LiveMapWindowHours` optional. Retention job exists but is **OFF** (`LocationPings:RetentionDays: 0`) — route history reads old pings.
- T2.3 kept full response (no pagination): projection + 10-min cache with invalidation on outlet/route/geo/LastBillDate changes.
- T2.6 product full-list kept (used for price/name lookups in PO edit/detail); other 4 converted.
- T2.7 endpoint `GET /billings/portal/dashboard-summary`; revenue excludes rep-cancelled **and** distributor-rejected bills; counts include all bills.
- T2.8 uses ETag / `If-None-Match` → 304 (no delta); outlets/stock still full.
- T3.1 production Serilog Default = `Warning` (not `Error`); Auth info logs (login/logout) no longer emitted in prod — add override `sfa_api.Features.Auth: Information` if needed.
- T3.2 rate limiter moved after `UseAuthentication`; `auth` policy still per IP. Requires `ForwardedHeaders` proxy config.
- T3.11 `cache()` only dedupes during RSC render; server actions still call `auth()` per call.
- T3.12 only `radix-ui`, `@radix-ui/react-icons` added (others already optimised by Next); no bundle analyzer.
- T3.21 upload every ≤10 min (not 15–30: web marks reps stale after 15 min); skip reports stay a separate call (API has no field). Also fixes backlog >500 pings never uploading.

**Pending before merge:** apply migrations `20260917044500_AddStockTransactionLedgerIndexes`, `20260917044625_RepLocationPingRepIdRecordedAtDesc`, `20260917052336_AddGrnNumberTrigramIndex` on staging; verify stock locking under concurrent bills/GRNs, Redis multi-instance cache/revocation, push delivery; mobile DB v19→v21 upgrade, offline fonts, live-map freshness; web click-tests (Excel export/import, maps, dashboard chart, 4xx no-retry).

---

## Tier 1 — Highest impact

- [x] **T1.1 [API] Billing write path: batch stock locks, shorten lock hold**
  - `Features/Billings/Services/BillingService.cs:248` (lock), `:306-351` (item loop), `:377-391` (post-commit work)
  - `Features/Stock/Repositories/StockRepository.cs:121-123`, `:161-174`
  - [x] Lock all stock rows in one `SELECT ... WHERE "ProductId" = ANY(@ids) ORDER BY "Id" FOR UPDATE`; pass tracked entities to deduct/credit (removes ~3 round trips per line)
  - [x] Dispose the RedLock right after commit, not at method return
  - [x] Move cache clears + notifications off the request path
  - [x] Apply same pattern to cancel/reject reversal (`BillingService.cs:583-615`) and GRN confirm (`Features/GRNs/Services/GrnService.cs:187-251`)
  - [x] Keep wrapping manual transactions in `CreateExecutionStrategy().ExecuteAsync`

- [ ] **T1.2 [API] Stock take submit: single batched `FOR UPDATE`**
  - `Features/StockTaking/Services/StockTakingService.cs:266-272` — 2 queries per catalogue line inside the tx; blocks billing

- [x] **T1.3 [API] Push notifications to a background queue**
  - `Infrastructure/Notifications/FirebaseNotificationService.cs:52-53`, `:71-72`
  - Callers: `BillingService.cs:391`, `:835`, `:1193`, `:1201-1204`
  - [x] `Channel<T>` + hosted service; use `SendEachForMulticastAsync`

- [x] **T1.4 [API] Fix cache invalidation (over-broad + broken across instances)**
  - `Infrastructure/Caching/DistributedCacheService.cs:14`, `:65-80`; `BillingService.cs:380`, `:1000-1006`
  - [x] Replace in-memory `_trackedKeys` prefix removal with versioned prefixes stored in Redis (`sales-summary:v{n}:`)
  - [x] Invalidate only the affected rep/date scope, not company-wide
  - [x] Stop flushing `outlets:route:*` just for `LastBillDate`

- [x] **T1.5 [MOB] Bill outbox sync: dedupe, stock once, backoff**
  - `core/sync/bill_sync_service.dart:74-82`, `:182-185`; `core/sync/not_billing_sync_service.dart` (`flushAll`)
  - Triggers: `main.dart:150-155`, `:343-352`
  - [x] Move `_syncStock()` out of per-bill `_sync()`; call once at end of `flushAll()` if ≥1 succeeded
  - [x] Single-flight guard on `flushAll` (return the in-flight Future); honor `_inFlight`
  - [x] Exponential backoff using `sync_attempts` (or `next_attempt_at` column — idempotent migration)
  - [x] Add terminal-error filter to `NotBillingSyncService`

- [ ] **T1.6 [MOB] Bills list reload storm during flush**
  - `features/bills/presentation/bloc/bills_list_bloc.dart:38-40`, `:58-67`; `bill_sync_service.dart:165`, `:218`
  - [ ] `restartable()`/debounce transformer on `BillsOutboxChanged`
  - [ ] Emit one status event at flush end
  - [ ] `buildWhen` on `sales_rep_home_page.dart:1373-1385` builders

- [ ] **T1.7 [WEB] Server-render pages instead of `ssr: false` everywhere**
  - `app/(protected)/products/page.tsx:1,7-13` + ~38 similar pages; `app/(protected)/layout.tsx:1,7-10`; `app/(distributor)/layout.tsx`
  - [ ] Make `page.tsx` server components importing feature pages directly
  - [ ] Add `loading.tsx` per route group
  - [ ] Sidebar collapsed state: cookie instead of `localStorage`, small client wrapper only

- [x] **T1.8 [API] Token revocation check off Postgres**
  - `Common/Extensions/JwtExtensions.cs:46-63`; `Infrastructure/Caching/PostgresTokenRevocationService.cs:18-20`
  - [x] Write revoked JTIs to Redis with TTL = remaining token life; check only there (or short local "not revoked" cache)

---

## Tier 2 — Scales badly with data growth

- [x] **T2.1 [API] Bin card / stock reconciliation indexes + queries**
  - `Features/Stock/Repositories/BinCardRepository.cs:18-32`, `:44-59`; `StockReconciliationRepository.cs:22-32`, `:50-61`; indexes `Infrastructure/Persistence/AppDbContext.cs:783-810`
  - [x] Index `(DistributorId, TransactedAt) INCLUDE (ProductId, StockType, Direction, TransactionType, Quantity)`
  - [x] Index `(DistributorId, ProductId, StockType, Id DESC)`
  - [x] Replace max-Id `IN (...)` two-step with `DISTINCT ON` / `LATERAL` (Postgres-only path; not SQLite-testable)

- [x] **T2.2 [API] Live map: bound location-ping query + retention**
  - `Features/LocationPings/Repositories/LocationPingRepository.cs:18-31`
  - [x] Add `RecordedAt > now() - interval '1 day'` (or maintain `RepLastLocation` table)
  - [x] Project rep name only, drop `.Include(p => p.Rep)`
  - [x] Retention cleanup hosted service (like `AuditLogCleanupService`)

- [x] **T2.3 [API] `GET /outlets/active` unbounded**
  - `Features/Outlets/Repositories/OutletRepository.cs:65-78`; `OutletService.cs:121`; `OutletsController.cs:73`
  - [x] Project needed columns, add pagination or `since` delta, cache

- [x] **T2.4 [API] Billing detail fetch cartesian explosion**
  - `Features/Billings/Repositories/BillingRepository.cs:78-94`
  - [x] `.AsSplitQuery()` or project directly to `BillingDto`

- [ ] **T2.5 [API] Geo repair: set-based updates**
  - `Features/GeoConsistency/Repositories/GeoConsistencyRepository.cs:123-176`
  - [ ] 4 `UPDATE child SET ... FROM parent WHERE ... AND mismatch` statements

- [x] **T2.6 [WEB] Dropdowns loading 200–1000 rows → `AsyncSelect`**
  - `features/purchase-order/components/pages/purchase-order-create-page.tsx:56-68`
  - `features/product/actions/product.actions.ts:102-110` (use existing `getActiveProductsForSelectAction` at `:42`)
  - `features/user-geo-assignment/actions/user-geo-assignment.actions.ts:119`
  - `features/user-reporting-line/actions/user-reporting-line.actions.ts:102`
  - `features/daily-route-assignment/actions/daily-route-assignment.actions.ts:63`
  - Keep active-only server-side filtering (see param names: `status=Active` vs `isActive=true`)

- [x] **T2.7 [WEB+API] Distributor dashboard summary endpoint**
  - `features/distributor-billings/hooks/distributor-billing.hooks.ts:80-90`, `:98-123`; `app/(distributor)/distributor-dashboard/page.tsx:88,129`
  - [x] API endpoint returning today + 7-day aggregates (exclude `RepStatus = Cancelled`); totals currently wrong past 500 bills

- [x] **T2.8 [MOB] Full sync: parallel + delta**
  - `core/background/background_sync_service.dart:78-134`; `features/products/data/repositories/products_repository_impl.dart:19-27`; `features/stock/domain/usecases/sync_distributor_stock_usecase.dart:10-14`
  - [x] `Future.wait` independent downloads
  - [x] `updatedSince` / ETag for products & categories (needs API support)

- [x] **T2.9 [MOB] Stock sync throttle**
  - `sync_distributor_stock_usecase.dart:10`; callers `main.dart:151-155`, `:351`, `background_sync_service.dart:115`
  - [x] Shared in-flight Future + skip if last sync < ~60s

- [ ] **T2.10 [WEB] Virtualize sales summary table**
  - `features/sales-summary/components/table/sales-summary-table.tsx:92` — `@tanstack/react-virtual`

---

## Tier 3 — Quick wins

### API
- [x] **T3.1** Serilog reads levels from config (prod `Default: Error` currently ignored); drop Seq sink when URL empty; async sink; exclude health checks — `Infrastructure/Logging/SerilogConfig.cs:11-22`, `Program.cs:302`
- [x] **T3.2** Rate limiter partition by user id before IP (carrier NAT) — `Common/Extensions/*RateLimit*.cs:54-57`
- [x] **T3.3** GRN search `ILike` + trigram index `IX_GRNs_GrnNumber_Trgm` — `Features/GRNs/Repositories/GrnRepository.cs:59-62`
- [ ] **T3.4** Cache distributor price list per category; invalidate in `BulkUpsertAsync` — `Features/ProductCategoryPricings/Repositories/ProductCategoryPricingRepository.cs:16-75`
- [ ] **T3.5** 60s cache for supervisor dashboard — `Features/Supervisor/Services/SupervisorService.cs:13-23`
- [ ] **T3.6** Lightweight no-tracking user lookup for permission checks — `Features/Users/Repositories/UserRepository.cs:12-15`
- [ ] **T3.7** `AsNoTracking` on `GetLatestRunAsync` — `GeoConsistencyRepository.cs:~183`

### Web
- [x] **T3.8** Dynamic-import ExcelJS/xlsx inside click handlers; standardize on one lib — `components/data-table/utils/export-utils.ts:2`, `features/bin-card/lib/bin-card-export.ts:1`, `features/sales-summary/lib/sales-summary-export.ts:1`, `features/sales-invoice/lib/parse-excel.ts:27`, `features/sales-target/lib/parse-targets-excel.ts:1`
- [x] **T3.9** `next/dynamic` for Recharts (`app/(distributor)/distributor-dashboard/page.tsx:11`) and Google Maps (`field-reps-live-map/page.tsx`, `rep-route-history/page.tsx`)
- [x] **T3.10** Don't retry 4xx — `providers/query-provider.tsx:15` (preserve status on thrown errors)
- [x] **T3.11** `cache(() => auth())` shared by `lib/api/client.ts:113` and `lib/actions/wrapper.ts:48`
- [x] **T3.12** `optimizePackageImports` + `@next/bundle-analyzer` — `next.config.ts`
- [ ] **T3.13** Remove duplicate deps (3× bcrypt, `pg`, `mssql`, 3 icon libs) — `package.json`
- [ ] **T3.14** Narrow invalidations to `lists()` + `setQueryData` for detail — `features/product/hooks/product.hooks.ts:130-220` (and similar)
- [ ] **T3.15** Zustand selector — `features/user-geo-assignment/hooks/user-geo-assignment.hooks.ts:171`
- [ ] **T3.16** Outlet map: bbox fetch or server clustering — `features/outlet/components/pages/outlet-map-page.tsx:49`

### Mobile
- [x] **T3.17** Bundle Barlow fonts; `GoogleFonts.config.allowRuntimeFetching = false`
- [x] **T3.18** Idempotent indexes: `bills(created_at)`, `bills(billing_date)`, `products(code)`, `products(category_id)` — `core/db/database_helper.dart`
- [x] **T3.19** Debounce product picker search (250–300ms), keep previous results, throttle category sync — `features/bills/presentation/widgets/product_search_delegate.dart:102-129`
- [x] **T3.20** Defer notification channel / background service / Workmanager init past first frame — `main.dart:61-105`
- [x] **T3.21** Location: upload every 15–30 min, chunk `LIMIT 200`, fold skip reports into batch — `core/background/location_tracking_service.dart:63-65`, `:96-107`, `:179-197`
- [ ] **T3.22** Batch bill inserts/upserts with `txn.batch()` + single status `SELECT IN (...)` — `features/bills/data/datasources/bills_local_datasource.dart:63-65`, `:120-150`
- [ ] **T3.23** Supervisor billing/not-billing pages → `SliverList.builder` — `supervisor_billing_page.dart:253,1183`, `supervisor_not_billing_page.dart:237,1161`
- [ ] **T3.24** Memoize search filtering — `stock_catalog_page.dart:74-81`, `products_page.dart:241-248`
- [ ] **T3.25** `AnnotatedRegion` instead of `SystemChrome` in `build()` — `stock_catalog_page.dart:85`, `sales_rep_home_page.dart:~89`
- [ ] **T3.26** Reuse one refresh `Dio` — `core/network/token_interceptor.dart:161-180`

---

## Related (non-performance)
- [ ] Rotate Neon password and blank `sfa_api/sfa_api/appsettings.json` `DefaultConnection` before next deploy (tracked exception in `.claude/rules/never-do.md`)
