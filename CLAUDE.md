# CLAUDE.md — SFA Monorepo

## Structure

| Directory    | Stack                              | Users                          |
|--------------|------------------------------------|--------------------------------|
| `sfa_api/`   | .NET 8 ASP.NET Core, PostgreSQL    | Backend for all clients        |
| `sfa_web/`   | Next.js 16, TypeScript, App Router | Admin / Manager / Executive    |
| `sfa_mobile/`| Flutter                            | Field sales reps (mobile ops)  |

Sub-project rules live in their own CLAUDE.md files:
- `sfa_api/CLAUDE.md` — run commands, directory layout, feature list, architecture overview
- `sfa_web/CLAUDE.md` — run commands, directory layout, feature list, architecture overview
- `sfa_mobile/CLAUDE.md` — Flutter patterns, navigation, state management

Path-scoped conventions in `.claude/rules/`:
- `never-do.md` — cross-project prohibitions (always loaded)
- `api-conventions.md` — exception mapping, EF Core, auth, infra services (loaded for `sfa_api/**`)
- `web-conventions.md` — TanStack Query, Zustand, NextAuth patterns (loaded for `sfa_web/**`)

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
- **Soft delete:** Entities are never hard-deleted; status is controlled via `IsActive` flag (default `true`); deactivation sets `IsActive = false`
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
