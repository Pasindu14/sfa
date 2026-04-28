# API Features — sfa_api

25 features implemented under `Features/`.

## Feature Table

| Feature | Description |
|---------|-------------|
| Auth | Login, JWT refresh, logout, token revocation |
| Users | User CRUD, password change, status toggle, role management |
| Distributors | Distributor CRUD, activate/deactivate |
| Regions | Region CRUD, activate/deactivate |
| Areas | Area CRUD, activate/deactivate — stores `RegionId` |
| Territories | Territory CRUD, activate/deactivate — stores `AreaId` + `RegionId` (denormalized) |
| Divisions | Division CRUD, activate/deactivate — stores `TerritoryId` + `AreaId` + `RegionId` (denormalized) |
| Outlets | Outlet CRUD, activate/deactivate — customer visit points |
| Products | Product CRUD, activate/deactivate |
| Categories | Top-level product classification |
| ProductCategories | Category grouping for products |
| ProductCategoryPricings | Price rules per product-category combination |
| PricingStructures | Full pricing structure management (distributor-linked) |
| PurchaseOrders | Field rep purchase order creation and tracking |
| SalesInvoices | Sales invoice management |
| GRNs | Goods Received Notes — stock receipt confirmation |
| Billings | Billing record management |
| NotBillings | Not-billing reason recording (why a sale didn't happen) |
| Routes | Sales route definitions |
| DailyRouteAssignments | Daily route-to-rep assignment |
| Stock | Stock level tracking per distributor |
| Fleets | Fleet/vehicle management |
| UserGeoAssignments | Assign users to geographic zones (division/territory/area/region) |
| UserReportingLines | Rep → supervisor reporting hierarchy |
| MobileSync | Offline-first mobile data sync endpoints |

## Geographic Hierarchy

```
Region → Area → Territory → Division
```

- **Denormalized ancestor IDs** — each level stores all parent IDs directly on the entity
  - Territory stores: `AreaId`, `RegionId`
  - Division stores: `TerritoryId`, `AreaId`, `RegionId`
- **Why:** Flat, join-free queries — `WHERE TerritoryId = X` returns the full zone without joins
- When creating Territory or Division, the service calls `GetAreaWithRegionAsync()` (returns `Area?`) — **not** `AreaExistsAsync()` — because it needs the parent's `RegionId` to denormalize onto the new entity
