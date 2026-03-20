---
name: nextjs-sfa-features
description: Scaffolds a complete, production-ready Next.js feature for the SFA web app. Use this skill whenever the user asks to add a frontend feature, create web UI, connect sfa_web to a .NET API endpoint, generate CRUD pages, or scaffold any frontend slice for any entity — distributors, customers, products, orders, leads, visits, tasks, territories, regions, areas, divisions, outlets, or any new entity. Trigger on: "create frontend for X", "add web feature for X", "build UI for X", "generate Next.js for X", "scaffold X page", "add X to the web app". Produces all files following the exact Users feature pattern: schema, actions, hooks, Zustand stores, form, columns, dialogs, table, list page, and app route — with server-side pagination, TanStack Query, shadcn/ui, and field-level validation. All output targets sfa_web/features/ and sfa_web/app/(protected)/.
---

# Next.js SFA Feature Scaffold

## Step 0 — Understand the API First

**Before writing any code**, locate and read the relevant API feature in `sfa_api/Features/{Entity}/`:

1. Read the **Controller** — note exact route paths, HTTP verbs, and response shapes
2. Read the **DTOs / request models** — derive the `{Entity}Dto` and `Create{Entity}Input` from these, not from assumptions
3. Read the **service/validator** if field validation rules are not obvious from the DTO

Use what you find to fill in:
- Correct API endpoint paths for every action (`/api/v1/{entity}s`, `/api/v1/{entity}s/{id}`, etc.)
- All fields on `{Entity}Dto` (match 1:1 with API response camelCase)
- All required/optional fields for create and update schemas
- Any enums or constrained values

Only start generating files once you understand the API contract.

---

## SFA-Specific Constants

| Concern | Value |
|---------|-------|
| API client | `client` — default import from `@/lib/api/client` (never `apiClient`) |
| Auth headers | Injected by client interceptor — never set manually |
| Update verb | Always `PUT` (never `PATCH`) |
| Tenancy | Resolved server-side from JWT — no `companyId` in any client call |
| List envelope | `res.data.data.{entity}s` + `res.data.data.totalCount` |
| Single envelope | `res.data.data` |
| Soft delete | Server sets `IsActive = false`; client calls `DELETE` with no body |
| Status badge | `isActive ? 'default' : 'secondary'` variant, pill-shaped |
| Primary colour | `#E8500A` orange |

## File Generation Order (typical — adjust as needed)

Generate data-layer files before UI files since components import from them. The order within each layer is flexible.

```text
── Data layer ──────────────────────────────────────────────
  schema/{entity}.schema.ts               follow pattern in schema-and-actions.md
  actions/{entity}.actions.ts             follow pattern in schema-and-actions.md

── State layer ─────────────────────────────────────────────
  store/{entity}.dialog-store.ts          follow pattern in state-stores.md
  store/{entity}.filter-store.ts          follow pattern in state-stores.md
  store/index.ts                          follow pattern in state-stores.md

── Hook layer ──────────────────────────────────────────────
  hooks/{entity}.hooks.ts                 follow pattern in data-hooks.md

── UI layer ────────────────────────────────────────────────
  components/types/{entity}.types.ts      follow pattern in ui-components.md
  components/forms/{entity}-form.tsx      follow pattern in ui-components.md
  components/columns/{entity}-columns.tsx follow pattern in ui-components.md
  components/dialogs/{entity}-dialogs.tsx follow pattern in ui-components.md
  components/table/{entity}-table.tsx     follow pattern in ui-components.md
  components/pages/{entity}-list-page.tsx follow pattern in ui-components.md
  components/index.ts                     (named export of ListPage)

── Route ───────────────────────────────────────────────────
  app/(protected)/{entity}s/page.tsx      (dynamic ssr:false only)
```

## L3 Reference Files

| Topic | File |
|-------|------|
| Zod schemas + server actions | [schema-and-actions.md](schema-and-actions.md) |
| TanStack Query hooks + DataTable hook | [data-hooks.md](data-hooks.md) |
| Zustand stores + barrel selectors | [state-stores.md](state-stores.md) |
| Form, Columns, Dialogs, Table, ListPage, Route | [ui-components.md](ui-components.md) |
| Planning template | [template.md](template.md) |
| Worked example (Category entity) | [examples/category-feature.md](examples/category-feature.md) |

## Quality Checklist

- [ ] Schema: `z.infer<>` types, separate create/update/filter schemas, `Dto` mirrors API exactly
- [ ] Actions: `'use server'`, `createAction` wrapper, `revalidatePath` after every mutation, `PUT` for update
- [ ] DataTable hook: exactly 8 params (unused prefixed `_`), `.isQueryHook = true` set **outside** fn body
- [ ] Mutations: spread `...mutation`, expose `fieldErrors` + `clearFieldErrors`
- [ ] Stores: `create<T>()(devtools(...))` double-paren syntax, `useShallow` from `'zustand/react/shallow'`
- [ ] Form: `useEffect` applies `fieldErrors` via `form.setError`
- [ ] Dialogs: clear field errors on close; Edit shows `Spinner` while entity is loading
- [ ] App route: `dynamic(..., { ssr: false })` — never a direct import

## Critical Pitfalls

1. **Client import** — `client` (default), not `apiClient`
2. **Update verb** — `PUT`, never `PATCH`
3. **Query key** — `{entity}Keys.all` is already an array; never call `.all()`
4. **Client-side filter** — never `.filter()` in DataTable hook; pass `search` to action, let API filter
5. **Empty search param** — `search: search || undefined` so Axios omits the param when empty
6. **SSR** — `ssr: false` in app route prevents Radix UI `useId()` hydration mismatch
7. **`useShallow` path** — `'zustand/react/shallow'`, not `'zustand/shallow'`
