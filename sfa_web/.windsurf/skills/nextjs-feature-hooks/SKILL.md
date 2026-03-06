---
name: nextjs-feature-hooks
description: "Use when creating TanStack Query hooks for a new feature like category, product, order or user. Creates query keys file, useQuery hooks file and useMutation hooks file. All hooks call feature actions only. Always sets staleTime, always invalidates scoped query keys, always calls handleErrorToast on error and shows success toast on mutation."
---

# Hooks Skill

## Location

```
features/{feature}/hooks/{feature}.query-keys.ts
features/{feature}/hooks/use-{feature}.queries.ts
features/{feature}/hooks/use-{feature}.mutations.ts
features/{feature}/hooks/index.ts
```

Has barrel file. Always import through `index.ts` from outside this folder.

---

## Rules Before Writing Anything

1. Read `AGENTS.md` at the project root first
2. Read the actions skill output — you need to know every action that exists and its exact input/output types
3. All hooks are `'use client'` — they run in the browser only
4. Hooks never call services or repositories — only actions
5. `staleTime` must be set on every single `useQuery` — never leave it as default
6. Query keys must be scoped precisely — never invalidate more than necessary
7. Build all three files in order: query-keys first → queries second → mutations third

---

## File 1 — `{feature}.query-keys.ts`

Build this first. Everything else depends on it.

### Rules
- No imports needed — pure constants only
- Every key factory function returns `as const`
- Cover every query that exists in the queries file plus DataTable and options
- Export as a single named const object

### Structure
```ts
export const categoryQueryKeys = {
  // Base key — parent of all category keys
  all: ['categories'] as const,

  // List keys
  lists: () => [...categoryQueryKeys.all, 'list'] as const,
  list: (filters?: unknown) => [...categoryQueryKeys.lists(), { filters }] as const,

  // Detail keys
  details: () => [...categoryQueryKeys.all, 'detail'] as const,
  detail: (id: number) => [...categoryQueryKeys.details(), id] as const,

  // DataTable key — used by useFeatureDataTable hook
  dataTable: (params?: unknown) => [...categoryQueryKeys.all, 'dataTable', params] as const,

  // Options key — used by dropdown selects
  options: () => [...categoryQueryKeys.all, 'options'] as const,
} as const
```

### Invalidation Hierarchy — Critical
TanStack Query invalidation is **hierarchical** — invalidating a parent key automatically invalidates all children with that prefix:

```
categoryQueryKeys.all          →  ['categories']
  categoryQueryKeys.lists()    →  ['categories', 'list']          ← child of all
  categoryQueryKeys.details()  →  ['categories', 'detail']        ← child of all
  categoryQueryKeys.dataTable()→  ['categories', 'dataTable', …]  ← child of all
  categoryQueryKeys.options()  →  ['categories', 'options']       ← child of all
```

Invalidating `categoryQueryKeys.all` invalidates EVERYTHING in one call. Use the most specific scope that covers what actually changed:

- After **create** → `all` (lists, options, dataTable all stale)
- After **update** → `all` (lists, detail, dataTable all stale)
- After **delete** → `all` (lists, options, dataTable all stale)

---

## File 2 — `use-{feature}.queries.ts`

### Imports
```ts
'use client'

import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { categoryQueryKeys } from './{feature}.query-keys'
import { getCategoryAction, getCategoriesAction, getCategoryOptionsAction } from '@/features/categories/actions/category.actions'
import type { CategoryFilterDto } from '@/features/categories/schemas/category.schema'
```

Rules:
- Import actions directly from the actions file path — no barrel for actions
- Import query keys from the local query-keys file
- Import types directly from the schema file

---

### `useFeature` — Single record hook
```ts
export function useCategory(id: number | undefined) {
  return useQuery({
    queryKey: categoryQueryKeys.detail(id!),
    queryFn: async () => {
      const result = await getCategoryAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: !!id,
    staleTime: 30_000,
  })
}
```

Rules:
- `id` is always `number | undefined` — hook must be safe to call before ID is known
- `enabled: !!id` — never run the query when ID is undefined
- Always unwrap `ActionResponse` — check `.success`, throw on failure, return `.data`
- `staleTime: 30_000` minimum — 30 seconds

---

### `useFeatures` — Paginated list hook
```ts
export function useCategories(
  filters?: CategoryFilterDto,
  page: number = 1,
  pageSize: number = 10
) {
  return useQuery({
    queryKey: categoryQueryKeys.list({ filters, page, pageSize }),
    queryFn: async () => {
      const result = await getCategoriesAction({ filters, page, pageSize })
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 30_000,
  })
}
```

---

### `useFeatureDataTable` — DataTable hook

**This IS a hook. It uses `useQuery` and sets `.isQueryHook = true` so DataTable calls it as a hook, not inside a `useEffect`.**

The DataTable component checks for the `.isQueryHook` flag on `fetchDataFn`:
- If `true` → calls it as a hook directly — TanStack Query owns the fetch cycle, invalidation works automatically
- If `false`/absent → calls it inside `useEffect` — breaks invalidation, table never refreshes after mutations

Always use the hook pattern:

```ts
export function useCategoryDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange: { from_date: string; to_date: string },
  sortBy: string,
  sortOrder: string
) {
  return useQuery({
    queryKey: categoryQueryKeys.dataTable({ page, pageSize, search, dateRange, sortBy, sortOrder }),
    queryFn: async () => {
      const result = await getCategoriesAction({
        filters: {
          search: search || undefined,
          isActive: undefined,
          page,
          pageSize,
          sortBy: sortBy as 'name' | 'createdAt' | undefined,
          sortOrder: sortOrder as 'asc' | 'desc',
        },
        page,
        pageSize,
      })
      if (!result.success) throw new Error(result.error)

      // API already returns serialized data (strings, not Date objects)
      // so no date serialization needed here — the API layer handles this
      return {
        data: result.data.items,
        pagination: {
          page: result.data.page,
          limit: result.data.pageSize,
          total_pages: result.data.totalPages,
          total_items: result.data.total,
        },
      }
    },
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  })
}

// THIS FLAG IS REQUIRED — tells DataTable to call this as a hook, not inside useEffect
// Without it: mutations invalidate cache but table never refetches
useCategoryDataTable.isQueryHook = true
```

Rules:
- Named `use{Feature}DataTable` with `use` prefix — it IS a hook
- Always `import { keepPreviousData }` from `@tanstack/react-query`
- Always set `placeholderData: keepPreviousData` — prevents loading flash between pages
- Always set `staleTime: 30_000`
- **CRITICAL**: Always set `.isQueryHook = true` on the function after declaration
- **No date serialization needed** — the .NET Core API returns pre-serialized strings, not `Date` objects
- The params signature must match exactly what DataTable passes — 6 positional params: `page, pageSize, search, dateRange, sortBy, sortOrder`
- Transform pagination response from camelCase to snake_case:
  - Action returns: `{ items, page, pageSize, totalPages, total }`
  - DataTable expects: `{ data, pagination: { page, limit, total_pages, total_items } }`

**Why hook, not plain function:**
When a plain async function is passed as `fetchDataFn`, DataTable calls it inside `useEffect`. The `useEffect` dependency array contains only `[page, pageSize, search, dateRange, sortBy, sortOrder, fetchDataFn]`. When `invalidateQueries` fires after a mutation, none of these deps change — so `useEffect` never re-runs and the table shows stale data.

When `.isQueryHook = true`, DataTable calls the function as a hook. TanStack Query owns the fetch cycle. `invalidateQueries({ queryKey: categoryQueryKeys.all })` marks the dataTable query stale and it refetches automatically.

---

### `useFeatureOptions` — Dropdown options hook
```ts
export function useCategoryOptions() {
  return useQuery({
    queryKey: categoryQueryKeys.options(),
    queryFn: async () => {
      const result = await getCategoryOptionsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60_000, // 5 minutes — options change less frequently
  })
}
```

Rules:
- Options are stable — use longer `staleTime` of 5 minutes
- Return type is `{ id: number; name: string }[]`

---

## File 3 — `use-{feature}.mutations.ts`

### Imports
```ts
'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { categoryQueryKeys } from './{feature}.query-keys'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import {
  createCategoryAction,
  updateCategoryAction,
  deleteCategoryAction,
} from '@/features/categories/actions/category.actions'
import type { CreateCategoryDto, UpdateCategoryDto } from '@/features/categories/schemas/category.schema'
```

---

### `useCreateFeature`
```ts
export function useCreateCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: CreateCategoryDto) => {
      const result = await createCategoryAction(input)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoryQueryKeys.all })
      toast.success('Category created successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'category', 'create')
    },
  })
}
```

Rules:
- `mutationFn` always unwraps `ActionResponse` — throw `result` (not `result.error`) on failure so `handleErrorToast` receives the full error object with `.code`
- `onSuccess` — invalidate `categoryQueryKeys.all` — one call covers lists, dataTable, and options
- `onError` — always `handleErrorToast(error, resourceName, actionVerb)`
- `toast.success` message format: `'{Feature} {past-tense verb} successfully'`

---

### `useUpdateFeature`
```ts
export function useUpdateCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: UpdateCategoryDto) => {
      const result = await updateCategoryAction(input)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoryQueryKeys.all })
      toast.success('Category updated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'category', 'update')
    },
  })
}
```

---

### `useDeleteFeature`
```ts
export function useDeleteCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteCategoryAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoryQueryKeys.all })
      toast.success('Category deleted successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'category', 'delete')
    },
  })
}
```

---

## File 4 — `index.ts` — Barrel

```ts
// Query keys
export { categoryQueryKeys } from './{feature}.query-keys'

// Queries
export {
  useCategory,
  useCategories,
  useCategoryDataTable,
  useCategoryOptions,
} from './use-{feature}.queries'

// Mutations
export {
  useCreateCategory,
  useUpdateCategory,
  useDeleteCategory,
} from './use-{feature}.mutations'
```

---

## ActionResponse Unwrapping — Always This Pattern

```ts
// ✅ Correct — queries
queryFn: async () => {
  const result = await getFeatureAction(id)
  if (!result.success) throw new Error(result.error)
  return result.data
}

// ✅ Correct — mutations
mutationFn: async (input) => {
  const result = await createFeatureAction(input)
  if (!result.success) throw result  // throw full result so handleErrorToast gets .code
  return result.data
}

// ❌ Wrong — never return raw ActionResponse to TanStack Query
queryFn: async () => {
  return await getFeatureAction(id)
}
```

---

## staleTime Reference

```
detail queries      →  30_000   (30 seconds)
list queries        →  30_000   (30 seconds)
dataTable queries   →  30_000   (30 seconds)
options queries     →  300_000  (5 minutes)
```

---

## Common Mistakes — Never Do These

- Never call services or repositories from hooks — only actions
- Never leave `staleTime` unset on any `useQuery`
- Never use `queryClient.invalidateQueries()` without a `queryKey`
- Never use multiple `invalidateQueries` calls for the same mutation — use `categoryQueryKeys.all` once
- Never return raw `ActionResponse` to TanStack Query — always unwrap `.data`
- Never throw `new Error(result.error)` in mutations — throw the full `result` object
- Never forget `enabled: !!id` on single record queries
- Never pass a plain async function as `fetchDataFn` to DataTable without `.isQueryHook = true`
- Never name the DataTable fetcher without the `use` prefix — it IS a hook
- Never forget `.isQueryHook = true` on the DataTable hook
- **Never serialize Date fields manually** — the API already returns strings, not Date objects
- Never forget `handleErrorToast` in every `onError`
- Never forget `toast.success` in every `onSuccess`
- Never import from another feature's hooks — features are isolated