---
description: Cross-project prohibitions that always apply regardless of which file is being edited
---

# Never Do — Cross-Project Rules

These rules apply to ALL projects in the SFA monorepo (API, web, mobile).

- **Never hard-delete records** — soft-delete/deactivate via `IsActive = false`; never call `context.Remove()`. All entities set `IsDeleted = true` on the DELETE endpoint as an audit flag to distinguish deletion from deactivation. `IsActive` is the universal status flag used in queries.
- **Never send or accept tenant/company ID from the client** — multi-tenancy resolves server-side from JWT claims
- **Never commit secrets, `.env` files, or connection strings** — use environment variables and secrets managers
- **Never expose raw exception messages or stack traces** in API responses — use structured error codes
- **Never use SQL Server** — the database is PostgreSQL; use only PostgreSQL-compatible constructs
- **Never hardcode the API base URL** — always read from environment config (`SFA_API_DOMAIN`)
- **Never store JWT tokens in insecure storage** — use secure storage (flutter_secure_storage on mobile, HTTP-only cookies or secure session on web)
