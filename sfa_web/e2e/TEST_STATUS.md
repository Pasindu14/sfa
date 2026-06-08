# Playwright E2E Test Status

All specs live under `sfa_web/e2e/features/`.

Status legend: `✅ Passed` | `❌ Failed` | `⏳ Not Run` | `🚧 Not Created`

---

## Users
| Spec | Status | Notes |
|------|--------|-------|
| `users/users-list.spec.ts` | ✅ Passed | 5 tests passed |
| `users/users-create.spec.ts` | ✅ Passed | 4 tests passed |
| `users/users-update.spec.ts` | ✅ Passed | 4 tests passed |
| `users/users-deactivate.spec.ts` | ✅ Passed | 6 tests passed |

## Distributors
| Spec | Status | Notes |
|------|--------|-------|
| `distributor/distributor-list.spec.ts` | ✅ Passed | 5 tests passed |
| `distributor/distributor-create.spec.ts` | ✅ Passed | 6 tests passed |
| `distributor/distributor-update.spec.ts` | ✅ Passed | 5 tests passed; fixed `search()` to press Enter + networkidle |
| `distributor/distributor-deactivate.spec.ts` | ✅ Passed | 6 tests passed |

## Regions
| Spec | Status | Notes |
|------|--------|-------|
| `regions/region-list.spec.ts` | ✅ Passed | 7 tests passed |
| `regions/region-create.spec.ts` | ✅ Passed | 7 tests passed |
| `regions/region-update.spec.ts` | ✅ Passed | 4 tests passed (headed) |
| `regions/region-status.spec.ts` | ✅ Passed | 8 tests passed (headed) |

## Areas
| Spec | Status | Notes |
|------|--------|-------|
| `areas/area-list.spec.ts` | ✅ Passed | 7 tests passed |
| `areas/area-create.spec.ts` | ✅ Passed | 8 tests passed |
| `areas/area-update.spec.ts` | ✅ Passed | 4 tests passed |
| `areas/area-status.spec.ts` | ✅ Passed | 8 tests passed |

## Territories
| Spec | Status | Notes |
|------|--------|-------|
| `territories/territory-list.spec.ts` | ✅ Passed | 7 tests passed |
| `territories/territory-create.spec.ts` | ✅ Passed | 8 tests passed |
| `territories/territory-update.spec.ts` | ✅ Passed | 4 tests passed |
| `territories/territory-status.spec.ts` | ✅ Passed | 8 tests passed |

## Divisions
| Spec | Status | Notes |
|------|--------|-------|
| `divisions/division-list.spec.ts` | ✅ Passed | 7 tests passed |
| `divisions/division-create.spec.ts` | ✅ Passed | 8 tests passed |
| `divisions/division-update.spec.ts` | ✅ Passed | 4 tests passed |
| `divisions/division-status.spec.ts` | ✅ Passed | 8 tests passed |

## Routes
| Spec | Status | Notes |
|------|--------|-------|
| `routes/route-list.spec.ts` | ✅ Passed | 7 tests passed |
| `routes/route-create.spec.ts` | ✅ Passed | 8 tests passed |
| `routes/route-update.spec.ts` | ✅ Passed | 4 tests passed |
| `routes/route-status.spec.ts` | ✅ Passed | 8 tests passed |

## Outlets
| Spec | Status | Notes |
|------|--------|-------|
| `outlet/outlet-list.spec.ts` | ✅ Passed | 7 tests passed |
| `outlet/outlet-create.spec.ts` | ✅ Passed | 11 tests passed; fixed `ownerDOB`/`email` empty-string→null in action |
| `outlet/outlet-update.spec.ts` | ✅ Passed | 8 tests passed |
| `outlet/outlet-delete.spec.ts` | ✅ Passed | 4 tests passed |
| `outlet/outlet-deactivate.spec.ts` | ✅ Passed | 6 tests passed |

## Products
| Spec | Status | Notes |
|------|--------|-------|
| `product/product-list.spec.ts` | ✅ Passed | 9 tests passed |
| `product/product-create.spec.ts` | ✅ Passed | 8 tests passed |
| `product/product-update.spec.ts` | ✅ Passed | 4 tests passed |
| `product/product-deactivate.spec.ts` | ✅ Passed | 4 tests passed |
| `product/product-activate.spec.ts` | ✅ Passed | 5 tests passed |

## Geo Assignments
| Spec | Status | Notes |
|------|--------|-------|
| `geo-assignment/geo-assignment-list.spec.ts` | ✅ Passed | 9 tests passed |
| `geo-assignment/geo-assignment-create.spec.ts` | ✅ Passed | 4 tests passed; fixed `useSalesRepFetcher` → `useAssignableUserFetcher` (all assignable roles); added `selectFirstDivisionInCascade` via API-fetched chain |
| `geo-assignment/geo-assignment-update.spec.ts` | ✅ Passed | 4 tests passed |
| `geo-assignment/geo-assignment-deactivate.spec.ts` | ✅ Passed | 6 tests passed |

## Product Categories
| Spec | Status | Notes |
|------|--------|-------|
| `product-categories/product-category-list.spec.ts` | ✅ Passed | 9 tests passed; fixed name extraction to `div.text-sm.font-medium` (column header is "Category") |
| `product-categories/product-category-create.spec.ts` | ✅ Passed | 6 tests passed |
| `product-categories/product-category-update.spec.ts` | ✅ Passed | 6 tests passed; removed `expectRowNotExists(ORIGINAL_NAME)` (updated name contains original as substring) |
| `product-categories/product-category-status.spec.ts` | ✅ Passed | 8 tests passed |

## Product Category Pricings
| Spec | Status | Notes |
|------|--------|-------|
| `product-category-pricings/pricing-list.spec.ts` | ✅ Passed | 5 tests passed; bulk-edit page (no dialogs) — verified heading, table, columns, Save All button, 4 inputs per row |
| `product-category-pricings/pricing-update.spec.ts` | ✅ Passed | 3 tests passed; edit price → dirty badge → Save All → toast → reload persists value |

## Purchase Orders (Admin)
| Spec | Status | Notes |
|------|--------|-------|
| `purchase-orders/purchase-order-list.spec.ts` | ✅ Passed | 6 tests passed; KPI cards, columns, status filter, New Order nav |
| `purchase-orders/purchase-order-create.spec.ts` | ✅ Passed | 4 tests passed; Admin must select distributor (API-fetched); Save as Draft navigates to detail |
| `purchase-orders/purchase-order-detail.spec.ts` | ✅ Passed | 3 tests passed; view via order # link, PO- heading, back link |

## Purchase Orders (Distributor)
| Spec | Status | Notes |
|------|--------|-------|
| `purchase-orders/purchase-order-distributor.spec.ts` | ✅ Passed | 13 tests; distributor storageState override; list/create/workflow; full finalize via admin API advancement (browser.newContext) |

## Sales Invoices
| Spec | Status | Notes |
|------|--------|-------|
| `sales-invoices/sales-invoice-list.spec.ts` | 🚧 Not Created | |
| `sales-invoices/sales-invoice-create.spec.ts` | 🚧 Not Created | |
| `sales-invoices/sales-invoice-update.spec.ts` | 🚧 Not Created | |
| `sales-invoices/sales-invoice-status.spec.ts` | 🚧 Not Created | |

## GRNs
| Spec | Status | Notes |
|------|--------|-------|
| `grns/grn-list.spec.ts` | 🚧 Not Created | |
| `grns/grn-create.spec.ts` | 🚧 Not Created | |
| `grns/grn-update.spec.ts` | 🚧 Not Created | |
| `grns/grn-status.spec.ts` | 🚧 Not Created | |

## Route Cancellations
| Spec | Status | Notes |
|------|--------|-------|
| `route-cancellations/route-cancellation-list.spec.ts` | 🚧 Not Created | |
| `route-cancellations/route-cancellation-create.spec.ts` | 🚧 Not Created | |
| `route-cancellations/route-cancellation-status.spec.ts` | 🚧 Not Created | |

## Stock
| Spec | Status | Notes |
|------|--------|-------|
| `stock/stock-list.spec.ts` | 🚧 Not Created | |
| `stock/stock-update.spec.ts` | 🚧 Not Created | |

## Fleets
| Spec | Status | Notes |
|------|--------|-------|
| `fleets/fleet-list.spec.ts` | ✅ Passed | 7 tests passed |
| `fleets/fleet-create.spec.ts` | ✅ Passed | 6 tests passed; fixed `getByLabel('Name *')` → `getByPlaceholder('Enter fleet name')` (label has asterisk) |
| `fleets/fleet-update.spec.ts` | ✅ Passed | 4 tests passed |
| `fleets/fleet-status.spec.ts` | ✅ Passed | 8 tests passed |

## User Reporting Lines
| Spec | Status | Notes |
|------|--------|-------|
| `user-reporting-lines/reporting-line-list.spec.ts` | 🚧 Not Created | |
| `user-reporting-lines/reporting-line-create.spec.ts` | 🚧 Not Created | |
| `user-reporting-lines/reporting-line-update.spec.ts` | 🚧 Not Created | |
| `user-reporting-lines/reporting-line-status.spec.ts` | 🚧 Not Created | |

## Sales Targets
| Spec | Status | Notes |
|------|--------|-------|
| `sales-targets/sales-target-list.spec.ts` | 🚧 Not Created | |
| `sales-targets/sales-target-create.spec.ts` | 🚧 Not Created | |
| `sales-targets/sales-target-update.spec.ts` | 🚧 Not Created | |

## Stock Taking
| Spec | Status | Notes |
|------|--------|-------|
| `stock-taking/stock-taking-list.spec.ts` | 🚧 Not Created | |
| `stock-taking/stock-taking-create.spec.ts` | 🚧 Not Created | |
| `stock-taking/stock-taking-status.spec.ts` | 🚧 Not Created | |

---

## Summary

| Feature | Specs | ✅ Passed | ❌ Failed | ⏳ Not Run | 🚧 Not Created |
|---------|------:|--------:|--------:|----------:|-------------:|
| Users | 4 | 4 | 0 | 0 | 0 |
| Distributors | 4 | 4 | 0 | 0 | 0 |
| Regions | 4 | 4 | 0 | 0 | 0 |
| Areas | 4 | 4 | 0 | 0 | 0 |
| Territories | 4 | 4 | 0 | 0 | 0 |
| Divisions | 4 | 4 | 0 | 0 | 0 |
| Routes | 4 | 4 | 0 | 0 | 0 |
| Outlets | 5 | 5 | 0 | 0 | 0 |
| Products | 5 | 5 | 0 | 0 | 0 |
| Geo Assignments | 4 | 4 | 0 | 0 | 0 |
| Product Categories | 4 | 4 | 0 | 0 | 0 |
| Product Category Pricings | 2 | 2 | 0 | 0 | 0 |
| Purchase Orders (Admin) | 3 | 3 | 0 | 0 | 0 |
| Purchase Orders (Distributor) | 1 | 1 | 0 | 0 | 0 |
| Sales Invoices | 4 | 0 | 0 | 0 | 4 |
| GRNs | 4 | 0 | 0 | 0 | 4 |
| Route Cancellations | 3 | 0 | 0 | 0 | 3 |
| Stock | 2 | 0 | 0 | 0 | 2 |
| Fleets | 4 | 4 | 0 | 0 | 0 |
| User Reporting Lines | 4 | 0 | 0 | 0 | 4 |
| Sales Targets | 3 | 0 | 0 | 0 | 3 |
| Stock Taking | 3 | 0 | 0 | 0 | 3 |
| **Total** | **83** | **60** | **0** | **0** | **23** |
