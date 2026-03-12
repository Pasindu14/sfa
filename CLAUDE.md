# CLAUDE.md — SFA Monorepo

## Structure

| Directory    | Stack                              | Users                          |
|--------------|------------------------------------|--------------------------------|
| `sfa_api/`   | .NET 8 ASP.NET Core, PostgreSQL    | Backend for all clients        |
| `sfa_web/`   | Next.js 16, TypeScript, App Router | Admin / Manager / Executive    |
| `sfa_mobile/`| Flutter                            | Field sales reps (mobile ops)  |

Sub-project rules live in their own CLAUDE.md files:
- `sfa_api/CLAUDE.md` — .NET patterns, EF Core, auth, response envelope
- `sfa_web/CLAUDE.md` — Next.js patterns, actions, hooks, stores, components
- `sfa_mobile/CLAUDE.md` — Flutter patterns, navigation, state management

---

## Shared API Contract

- **Base URL:** `SFA_API_DOMAIN` env var (e.g. `http://localhost:5086`)
- **Auth:** Bearer JWT in `Authorization` header — access token from login
- **Casing:** All requests and responses use camelCase
- **Envelope:** Every response is wrapped in `ApiResponse<T>`:
  ```json
  { "success": true, "data": {...}, "pagination": null, "traceId": "..." }
  ```
- **Errors:** Non-2xx responses use `ApiError`:
  ```json
  { "code": "USER_NOT_FOUND", "message": "...", "fields": {}, "traceId": "..." }
  ```
- **Versioning:** All endpoints are prefixed `/api/v1/`
- **Soft delete:** Entities are never hard-deleted; they have `isDeleted` flag
- **No tenant ID from client:** Multi-tenancy (if implemented) resolves server-side from JWT

---

## Git Conventions

- Branch: `feature/<name>`, `fix/<name>`, `chore/<name>`
- Commits: imperative present tense — `add distributor list endpoint`
- Never force-push `main`

---

## Docker

Root `dockerfile` builds the .NET API only (multi-stage, .NET 8 SDK → runtime).
No compose file found at root — run each project independently in dev.

---

## Dev Tools

- Use `rg` (ripgrep) instead of `grep` for all file searches
- Use `sg` (ast-grep) for structural code pattern searches
- Prefer explicit file paths over broad directory scans

## Never Do (Cross-Project)

- Never hard-delete records — always soft-delete via `isDeleted`
- Never send tenant/company ID from the client — resolve from JWT server-side
- Never commit secrets, `.env` files, or connection strings
- Never expose raw exception stack traces in API responses
- Never use SQL Server — the database is **PostgreSQL**
