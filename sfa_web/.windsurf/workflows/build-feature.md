# Build Feature Workflow

Scaffolds a complete Next.js feature from scratch. Builds every layer in strict order: schema → repository → service → actions → hooks → store → components. Complete and verify each layer before starting the next.

---

## Step 1 — Gather Information

Ask the user before writing anything:

1. **Feature name** — e.g. category, product, supplier
2. **API base path** — e.g. `/api/v1/categories` — confirm this exists in the SFA API
3. **Soft delete** — does `DELETE /api/v1/{features}/{id}` soft-delete or hard-delete?
4. **Foreign keys** — does this feature reference another feature (dropdown selects)?
5. **Extra filters** — any filters beyond `search` and `isActive`?

Do not proceed until all five are answered.

---

## Step 2 — Read Required Files

Read in this order before writing anything:
1. `AGENTS.md` — source of truth for all conventions
2. `@/lib/api/client.ts` — `apiClient`, `withToken`, `ApiResponse`, `ApiPaginatedResponse`, `ApiError`
3. `@/lib/auth/helpers.ts` — confirm `getAuthToken()` export name
4. `@/lib/actions/wrapper.ts`
5. `@/lib/services/wrapper.ts`
6. `@/lib/queries/wrapper.ts`
7. `@/lib/types/actions.ts`
8. `@/lib/queries/pagination.ts`

Confirm: "I have read all required files and am ready to build {featureName}."

---

## Step 3 — Create Folder Structure

```
features/{feature}/
├── actions/
├── components/dialogs/ forms/ tables/ pages/
├── hooks/
├── repositories/
├── schemas/
├── services/
└── store/
```

---

## Step 4 — Schema Layer

Invoke `@nextjs-feature-schema`.

File: `features/{feature}/schemas/{feature}.schema.ts`

Verify before moving on:
- [ ] `createSchema` — no audit fields, no id, no companyId
- [ ] `CreateDto` inferred with `z.infer`
- [ ] `updateSchema` uses `.partial().extend({ id: z.number() })`
- [ ] `UpdateDto` inferred with `z.infer`
- [ ] `filterSchema` with search, isActive, page, pageSize, sortBy, sortOrder
- [ ] `FilterDto` inferred with `z.infer`
- [ ] Entity type re-exported from `@/db/schema`
- [ ] No barrel file

---

## Step 5 — Repository Layer

Invoke `@nextjs-feature-repository`.

Files: `{feature}.repository.ts` + `index.ts`

Verify before moving on:
- [ ] Zero Drizzle imports — only `apiClient`, `withToken`, `executeQuery`, `getAuthToken`
- [ ] `private static readonly basePath = '/api/v1/{features}'` on the class
- [ ] `FeatureFilters` type exported at top of file
- [ ] `findById` — returns `T | null`, catches `ApiError.status === 404` → null
- [ ] `findAllPaginated` — params use `page` + `pageSize` (camelCase), maps `totalCount` → `total`
- [ ] `getOptions` — calls paginated with `pageSize: 500, isActive: true`, maps to `{ id, name }`
- [ ] `create` — `apiClient.post(url, data, withToken(token))` — data 2nd, config 3rd
- [ ] `update` — `apiClient.put(url, data, withToken(token))` — **PUT not PATCH**, data 2nd, config 3rd
- [ ] `softDelete` — `apiClient.delete(url, withToken(token))`, `Promise<void>`
- [ ] `count` — calls paginated with `pageSize: 1`, reads `totalCount`
- [ ] `exists` — delegates to `findById`
- [ ] `getAuthToken()` called fresh at top of every method
- [ ] `executeQuery` on every method
- [ ] No `tx: DbTransaction` anywhere
- [ ] No `x-company-id` header anywhere
- [ ] Barrel exports class and `FeatureFilters`

---

## Step 6 — Service Layer

Invoke `@nextjs-feature-service`.

Files: `{feature}.service.ts` + `index.ts`

Verify before moving on:
- [ ] Class `{Feature}Service` with `private static readonly context`
- [ ] `getById` — throws `NotFoundError` if null, returns `T` never `T | null`
- [ ] `getAll` and `getOptions` — thin pass-throughs
- [ ] `create` — data type `CreateDto & { createdBy: string }`, conflict check first
- [ ] `update` — data type `UpdateDto & { updatedBy: string }`, calls `this.getById` first
- [ ] `delete` — calls `this.getById` first, then `softDelete`
- [ ] `executeService` on every method — no manual try/catch
- [ ] Never calls `getAuthUser()` or `getAuthToken()`
- [ ] Barrel exports class only

---

## Step 7 — Actions Layer

Invoke `@nextjs-feature-actions`.

File: `features/{feature}/actions/{feature}.actions.ts`

Verify before moving on:
- [ ] `'use server'` is the very first line
- [ ] `AuthContext = { companyId: number; userId: string }` defined at top
- [ ] `getFeatureAction` — companyId only from auth
- [ ] `getFeaturesAction` — `safeParse` for filters, page + pageSize at top level
- [ ] `getFeatureOptionsAction` — input is `void`
- [ ] `createFeatureAction` — `.parse()`, injects `createdBy: userId`, `revalidatePath`
- [ ] `updateFeatureAction` — `.parse()`, injects `updatedBy: userId`, `revalidatePath`
- [ ] `deleteFeatureAction` — `id: number`, `revalidatePath`
- [ ] Every action has `requireAuth: true` and `auth?: AuthContext`
- [ ] No barrel file

---

## Step 8 — Hooks Layer

Invoke `@nextjs-feature-hooks`.

Files: `{feature}.query-keys.ts` + `use-{feature}.queries.ts` + `use-{feature}.mutations.ts` + `index.ts`

Verify before moving on:
- [ ] Query keys: `all`, `lists`, `list`, `details`, `detail`, `dataTable`, `options`
- [ ] `useFeature(id)` — `enabled: !!id`, `staleTime: 30_000`
- [ ] `useFeatureDataTable` — IS a hook, has `.isQueryHook = true` after declaration
- [ ] `useFeatureDataTable` — `placeholderData: keepPreviousData`, `staleTime: 30_000`
- [ ] `useFeatureDataTable` — **no manual date serialization** — API returns strings already
- [ ] `useFeatureDataTable` — pagination mapped to `{ page, limit, total_pages, total_items }`
- [ ] `useFeatureOptions` — `staleTime: 300_000`
- [ ] All mutations invalidate `featureQueryKeys.all` — single call
- [ ] All mutations throw full `result` on failure, have `toast.success` and `handleErrorToast`

---

## Step 9 — Store Layer

Invoke `@nextjs-feature-store`.

Files: `{feature}-dialog.store.ts` + `{feature}-filter.store.ts` + `index.ts`

Verify before moving on:
- [ ] Dialog IDs all `number | null`
- [ ] Open/close actions set boolean AND id atomically
- [ ] `initialState` as separate const
- [ ] All filter setters reset `page: 1` except `setPage`

---

## Step 10 — Components Layer

Invoke `@nextjs-feature-components`. Build in order:

10a `types.ts` → 10b `create-form` → 10c `update-form` → 10d `create-dialog` → 10e `update-dialog` → 10f `delete-dialog` → 10g `details-dialog` → 10h `columns` → 10i `table` → 10j `list-page` → 10k barrels

Key checks:
- [ ] `fetchDataFn={use{Feature}DataTable}` — hook not plain function
- [ ] `AlertDialog` for delete — never `Dialog`
- [ ] All loading states use `<Spinner />` — never text

---

## Step 11 — App Router Page

File: `app/(protected)/{feature}/page.tsx`

```tsx
import { {Feature}ListPage } from '@/features/{feature}/components'

export default function {Feature}Page() {
  return <{Feature}ListPage />
}
```

---

## Step 12 — List All Files Created

---

## Step 13 — Summary Report

```
Manual steps required:
1. Add navigation link to sidebar
2. Confirm API endpoints are live at /api/v1/{features}

⚠️ Flag anything that needs attention
```

---

## Step 14 — Final Verification

```bash
npx tsc --noEmit
```

- [ ] Zero Drizzle imports in any repository file
- [ ] All repository methods use `PUT` for updates — never `PATCH`
- [ ] All repository methods use `page` + `pageSize` params — never `limit`
- [ ] `getAuthToken()` fresh per method — never shared
- [ ] `withToken` used on every API call — never raw headers
- [ ] No `x-company-id` header anywhere
- [ ] `.isQueryHook = true` on DataTable hook
- [ ] No manual date serialization in hooks
- [ ] All mutations invalidate `featureQueryKeys.all` — single call