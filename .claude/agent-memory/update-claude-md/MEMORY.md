# Agent Memory — update-claude-md

## Key Patterns

### Glob paths don't work with trailing slash on Windows
Use `Bash` with `ls` to list feature directories. `Glob` with pattern `sfa_api/sfa_api/Features/*/` returns no results.

### Edit tool replaces in-place — no duplicate risk if old_string is unique
The Edit tool replaces the exact old_string with new_string. Always read the file first and confirm the exact text to replace. The git diff will show the full replacement as additions/removals against the last commit, which can look alarming but is correct.

### sfa_api structure (confirmed 2026-03-08)
- Features: Auth (✓), Users (✓), Distributors (✓), Categories (scaffold, no controller)
- Common/: Errors, Middleware, Extensions, Audit
- Infrastructure/: Persistence, Caching, Locking, Logging
- Test projects: sfa_api.IntegrationTests/, sfa_api.UnitTests/

### sfa_web structure (confirmed 2026-03-08)
- Features: auth (components only), distributor (full CRUD), user (full CRUD)
- Protected routes: dashboard, distributors, users
- lib/ has: actions/ (wrapper.ts, helpers.ts), api/ (client.ts, query-keys.ts), auth/, hooks/, queries/, types/ (actions.ts, common.ts), errors.ts, utils.ts
- components/ has: ui/, data-table/, app-sidebar.tsx, calendar-date-picker.tsx, company-logo.tsx, error-boundary.tsx, nav-main.tsx, nav-projects.tsx, nav-user.tsx

### sfa_mobile
- Only CLAUDE.md exists — project not yet initialized (no pubspec.yaml or Flutter files)
