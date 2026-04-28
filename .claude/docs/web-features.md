# Web Features — sfa_web

21 feature modules implemented under `features/` and routed via `app/(protected)/`.

| Feature Module | Route | Description |
|----------------|-------|-------------|
| auth | (auth)/login | Login form, NextAuth session management |
| user | (protected)/users | Full CRUD + password change — reference feature |
| distributor | (protected)/distributors | Distributor CRUD |
| region | (protected)/regions | Region CRUD |
| area | (protected)/areas | Area CRUD |
| territory | (protected)/territories | Territory CRUD |
| division | (protected)/divisions | Division CRUD |
| outlet | (protected)/outlets | Outlet CRUD |
| product | (protected)/products | Product CRUD |
| product-category | (protected)/product-categories | Product category management |
| product-category-pricing | (protected)/product-category-pricings | Category pricing rules |
| pricing-structure | (protected)/pricing-structures | Pricing structure management |
| purchase-order | (protected)/purchase-orders | Purchase order management |
| sales-invoice | (protected)/sales-invoices | Sales invoice management |
| grn | (protected)/grns | Goods Received Notes |
| route | (protected)/routes | Sales route management |
| route-cancellation | (protected)/route-cancellations | Route cancellation records |
| stock | (protected)/stock | Stock level management |
| fleet | (protected)/fleets | Fleet/vehicle management |
| user-geo-assignment | (protected)/geo-assignments | User geographic assignments |
| user-reporting-line | (protected)/user-reporting-lines | Rep → supervisor hierarchy |

The `user` feature is the reference implementation — when generating a new feature, follow its exact patterns for actions, hooks, store, and components.
