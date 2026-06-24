# Reporting Conventions — sfa_api (active-vs-all)

> Closes data-consistency review finding #10. The goal is that two numbers in a report
> can never *silently* disagree about what they count. The rule below is explicit so every
> aggregation query (and every reader of one) knows which universe it measures.

## The two universes

Every reportable number falls into exactly one of these, and they are **not** interchangeable:

| Universe | Counts… | Filtered by | Examples |
|----------|---------|-------------|----------|
| **Current master data** | entities that exist *right now* | `IsActive` + `!IsDeleted` (mostly via EF **global query filters**) | "active products", "outlets on this route", "users in this region" |
| **Historical facts** | transactions that *happened* | the **fact row's own** state (e.g. `Billing.IsActive && !IsDeleted` + status), **never** the referenced entity's current state | "total sales", "sales by product", "GRN received", "bills today" |

### Why they legitimately differ

A bill written for a product that was later discontinued is **still real revenue**. So:

- "Total active products" = 42 (current master data — discontinued SKUs excluded)
- "Total sales by product" can list 50 products (historical facts — includes the 8 discontinued ones)

This is **correct**, not a bug. A report must never `JOIN ... AND Product.IsActive = true` onto a
*financial* aggregate to make these tie out — doing so would silently delete real revenue from the
total. Deactivation does not cascade (review finding #10), and historical facts must not retroactively
change when a parent is deactivated.

## Rules for new reporting queries

1. **Decide the universe first.** Master-data count → rely on the global query filter (or filter
   `IsActive && !IsDeleted` explicitly and say so). Historical fact → filter the *fact row's* own
   state and status; do **not** filter the referenced entity's current active flag.
2. **State it in a comment** at the query, naming which universe and why — see
   `BillingRepository.GetRepMonthlySalesTotalAsync` and `GetOutletSummaryRawAsync` for the template.
3. **Cancelled/rejected facts:** exclude them from money totals (filter status), but a *listing*
   report may include them if it annotates status (e.g. `GetOutletSummaryRawAsync` returns all bills
   in range with `RepStatus` so the UI can show "Cancelled"). Make that choice explicit too.
4. **Soft-deleted rows** (`IsDeleted == true`) are excluded from **both** universes, always.

## Where the policy is enforced today

- **Master-data counts** — EF global query filters on `User`, `Outlet`, `Distributor` (`!IsDeleted`),
  geo entities and `Outlet` (`IsActive && !IsDeleted`). `Product` has **no** global filter by design
  (its repositories use `IgnoreQueryFilters()` and filter explicitly) — so Product reporting queries
  must filter `IsActive`/`IsDeleted` themselves and comment on it.
- **Sales/financial aggregates** — `Features/Billings/Repositories/BillingRepository.cs` (the
  `GetRep*SalesTotal*` cluster and `GetRepMonthlySalesByProductAsync`): filter bill state + status,
  not the referenced product/outlet.
- **Supervisor dashboard** — `Features/Supervisor/Repositories/SupervisorRepository.cs`: counts
  reps/bills/not-billings filtered to active, non-deleted rows consistently.
