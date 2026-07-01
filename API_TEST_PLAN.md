# SFA API — Pre-Launch Test & Production-Readiness Plan

> Goal: not "100% bug-free" (impossible) but **defense in depth** — layered checks so
> bad data is caught, and if it slips through, the system refuses to corrupt state and
> tells us loudly. Prioritized for go-live.
>
> Scale target: 500 reps, ~50 concurrent, ~200 req/min. Stack: .NET 8, PostgreSQL,
> Redis (cache + idempotency), PostgreSQL advisory locks, Serilog→Seq.

---

## 0. Current State (measured)

- **28 feature slices.** Unit tests cover 17, integration tests cover 17.
- **~873 unit + ~500 integration tests currently green** (per project records).
- **10 features have ZERO tests** (see §1). Several are money- or sync-critical.

---

## 1. Test Coverage Gaps — fill before launch

| Feature | Unit | Integ | Priority | Note |
|---|:---:|:---:|:---:|---|
| Auth | ❌ | ❌ | 🔴 P0 | Login, JWT issue/expiry, token revocation, role claims |
| MobileSync | ❌ | ❌ | 🔴 P0 | Offline batch push/pull, dedupe, conflict handling |
| NotBillings | ❌ | ❌ | 🔴 P0 | Financial reason-codes; unique index correctness |
| ProductCategoryPricings | ❌ | ❌ | 🔴 P0 | Price resolution math |
| Billings | ❌ | ✅ | 🔴 P0 | Add UNIT tests for discount/total/return/cash math |
| StockTaking | ❌ | ❌ | 🟠 P1 | Physical count → reconciliation feed |
| SalesTargets | ❌ | ❌ | 🟠 P1 | Target vs achievement aggregation |
| DailyRouteAssignments | ✅ | ❌ | 🟠 P1 | Add integration test for the assignment pipeline |
| Supervisor | ❌ | ❌ | 🟡 P2 | Rollup/reporting reads |
| LocationPings | ❌ | ❌ | 🟡 P2 | High-volume writes — see perf §4 |
| Notifications | ❌ | ❌ | 🟡 P2 | FCM token handling |
| ProductCategories | ❌ | ❌ | 🟡 P2 | Plain CRUD |

---

## 2. Axis A — Data Accuracy (correctness)  🔴 highest priority

The right answer for a single request, no concurrency.

- [ ] **Money math (unit tests, exact `decimal`)**: pack/case/MRP pricing, discounts,
      billing totals, returns, free-issue, cash-collected. Assert exact values &
      rounding; assert no `double`/`float` anywhere in the money path.
- [ ] **Stock ledger integrity**: reconciliation invariant (ledger SUM == on-hand)
      as an automated test; stock can never go below zero.
- [ ] **Reporting semantics (active-vs-all)**: financial aggregates use the bill's own
      historical state, NOT the referenced entity's current `IsActive`. Master-data
      counts use current/active. One test each way.
- [ ] **Timezone**: a bill/visit/ping at 23:00 Asia/Colombo lands on the correct
      business date; no raw-UTC leakage into date-bucketed reports.
- [ ] **Soft-delete**: `IsActive=false` / `IsDeleted=true` rows excluded from list
      queries but still readable by ID / preserved for audit.
- [ ] **Validation**: every request validator rejects bad input with field-level
      `ApiError.fields`, never a 500.

## 3. Axis B — Concurrency & Consistency  🔴 the SFA killer

Correct answer under *simultaneous* access. Most of these need **real PostgreSQL**
(SQLite test provider can't reproduce xmin/locks/decimal-SUM).

- [ ] **Optimistic concurrency (xmin/RowVersion)** on User/Outlet/Distributor/Product:
      stale update → **409 Conflict**, never a silent overwrite.
- [ ] **Distributed lock (advisory locks)** on stock deduction: two concurrent sales of
      the last unit → exactly one succeeds, total is correct, no lost update.
- [ ] **Idempotency**: same idempotency key twice (network retry) → one record.
- [ ] **Unique indexes enforced at DB**: duplicate active Route (name+division),
      NotBilling, Fleet — rejected by the database, not just the app.
- [ ] **Migration for these indexes is actually applied** (see §6).

## 4. Axis C — Performance & Speed

Load target is modest (50 concurrent) — focus on query shape, not raw throughput.

- [ ] **Baseline load test** (k6 / NBomber / bombardier) at 50 concurrent, 200 req/min
      against list + login + create-bill. Record **p50 / p95 / p99** per endpoint.
      Set a budget (e.g. p95 < 300 ms reads, < 800 ms writes).
- [ ] **N+1 query hunt**: enable EF SQL logging, hit each list/report endpoint once,
      confirm no per-row queries. Denormalized ancestor IDs should keep geo joins flat —
      verify.
- [ ] **Pagination enforced** on every list endpoint; no unbounded result sets.
- [ ] **LocationPings / MobileSync volume**: these are the write-heavy paths at 500 reps.
      Load-test ping ingestion specifically; confirm indexes support it.
- [ ] **Aggregate/report queries** (SUM/GROUP BY): profile against a realistic data
      volume, confirm indexes are used (`EXPLAIN ANALYZE`).
- [ ] **Cache correctness**: read-after-write returns fresh data (no stale Redis entry);
      cache invalidation on update/deactivate.
- [ ] **`AsNoTracking()`** on all read paths (per convention) — spot-check.

## 5. Axis D — Security & Isolation

- [ ] **AuthZ matrix**: every endpoint tested for each role (Admin / Manager /
      Executive / Rep) — allowed and forbidden cases.
- [ ] **IDOR / data scoping**: a rep cannot read/modify another territory's or another
      rep's data. (Outlets IDOR was fixed — re-verify Billings, Routes, Stock,
      SalesTargets, MobileSync.)
- [ ] **No tenant/company ID accepted from client** — resolved server-side from JWT.
- [ ] **No secret / stack-trace leakage** in any error response.
- [ ] **Rate limiter** actually returns 429 under abuse.
- [ ] **Token revocation** works (revoked JWT rejected).
- [ ] ⚠️ Dev JWT is committed in `sfa_api/CLAUDE.md` — rotate/remove secret handling
      before public launch (currently off-limits per prior decision — revisit).

## 6. Axis E — Operational Readiness  🔴 launch blockers live here

- [ ] **ALL migrations applied.** Known risk: `AddRoutesUniqueNameDivisionIndex`
      (20260615113307) was **blocked on duplicate active-routes data**, and
      `AddBillingClientBillId` (20260622073117) + reconciliation/rowversion migrations
      are queued behind it. **Clean the dup data, apply the chain, verify on a
      prod-like DB.** This is a hard go-live blocker.
- [ ] **Health checks** (DB + Redis reachable) green and monitored.
- [ ] **Structured logging + correlation IDs** flow to Seq for every request; errors
      searchable.
- [ ] **Graceful degradation**: app behaves sanely when Redis or DB is briefly down
      (fails safe, doesn't corrupt or 500-storm).
- [ ] **Backups + restore drill** on PostgreSQL — test a restore, not just a backup.
- [ ] **CI pipeline** runs unit + integration tests on every PR; block merge on red.
- [ ] **Real-PostgreSQL test job** (Testcontainers) for the SQLite-untestable paths
      (xmin 409, decimal SUM, advisory locks).

---

## 7. Suggested Sequence

1. **Launch-blocker triage** (§6 migrations + §3 DB constraints) — must-fix.
2. **P0 test gaps** (§1: Auth, MobileSync, Billings-unit, NotBillings, Pricing).
3. **Concurrency suite on real Postgres** (§3) — where money is lost.
4. **Load-test baseline** (§4) — get p95 numbers on record.
5. **Security matrix + IDOR re-verify** (§5).
6. **Wire CI** (§6) so all of the above runs forever, automatically.

> A test that isn't in CI protects nothing. The last step is what converts this
> whole document from "effort" into a permanent safety net.
