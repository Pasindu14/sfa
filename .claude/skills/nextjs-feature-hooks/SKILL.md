---
name: sfa-nextjs-feature-hooks
description: Creating TanStack Query v5 hooks for a feature module in the SFA Next.js web app. Use this when adding query and mutation hooks for any feature like users, customers, leads, orders, products, visits, or tasks. Hooks are the only layer that calls server actions — they wrap actions in useQuery and useMutation, manage client-side cache, surface loading and error states, and expose field-level errors to forms. Always uses the v5 single-object API, queryOptions factory pattern, and queryKey hierarchy. Includes query key factory, query options factory, query hooks, DataTable hook, options hook, and mutation hooks — all in one file. Never duplicates query config or writes raw fetch logic.
---

# Hooks Skill

## Location

```
features/{feature}/hooks/{feature}.hooks.ts
```

One file per feature. All hooks for a feature live in a single file. No barrel file. Import directly from this path always.

---

## Rules Before Writing Anything

1. This project uses **TanStack Query v5** — all hooks use the single-object API, never multiple positional arguments
2. `"use client"` is **not** added to hook files — hooks are imported by client components which already declare `"use client"`
3. `onSuccess` and `onError` callbacks do **not** exist on `useQuery` in v5 — they were removed; use them only on `useMutation`
4. Never call server actions directly in components — always go through a hook
5. Never duplicate `queryKey` strings — always reference the feature's `queryKeys` factory object
6. Mutation hooks never call `revalidatePath` — that is the action's responsibility; hooks call `invalidateQueries` for client cache sync
7. Never throw from a mutation's `onError` — always surface errors via the returned `fieldErrors` and `error` from `ActionResult`
8. Mutations invalidate `featureQueryKeys.all()` — one call covers lists, details, dataTable, and options — never use multiple calls
9. No barrel file — import directly from `{feature}.hooks.ts` always

---

## Shared Setup — Create Once

### lib/query/client.ts — Singleton QueryClient

```ts
import { QueryClient } from "@tanstack/react-query"

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,
        retry: 1,
        refetchOnWindowFocus: false,
      },
    },
  })
}

let browserQueryClient: QueryClient | undefined

export function getQueryClient() {
  if (typeof window === "undefined") {
    return makeQueryClient()
  }
  if (!browserQueryClient) browserQueryClient = makeQueryClient()
  return browserQueryClient
}
```

### app/providers.tsx — QueryClientProvider

Wrap root layout once. Never create a new `QueryClient` inside a component or hook.

```tsx
"use client"

import { QueryClientProvider } from "@tanstack/react-query"
import { ReactQueryDevtools } from "@tanstack/react-query-devtools"
import { getQueryClient } from "@/lib/query/client"
import { useState } from "react"

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => getQueryClient())
  return (
    <QueryClientProvider client={queryClient}>
      {children}
      {process.env.NODE_ENV === "development" && <ReactQueryDevtools />}
    </QueryClientProvider>
  )
}
```

---

## File Structure — Exact Order

Follow this exact order inside every hooks file. Never reorder sections.

### 1. Imports

```ts
import {
  useQuery,
  useMutation,
  useQueryClient,
  queryOptions,
  keepPreviousData,
} from "@tanstack/react-query"   // ^5.90.21 — v5 single-object API always
import { toast } from "sonner"   // ^2.0.7
import {
  getUsers,
  getUserById,
  createUser,
  updateUser,
  deleteUser,
} from "../actions/user.actions"
import type { UserFilterDto } from "../schemas/user.schema"
```

**Rules:**
- Import only from the feature's own actions file and schema file
- Import `toast` from `sonner` — version `^2.0.7`, never use `alert()` or other toast libraries
- Import `useRouter` only if a hook needs to redirect after a mutation
- Import `keepPreviousData` for paginated list hooks and the DataTable hook
- This is TanStack Query **v5** — the single-object API is mandatory; v4 positional args will cause runtime errors

---

### 2. Query key factory

Every feature defines a single `queryKeys` object. All hooks and `invalidateQueries` calls reference this object — never write raw string arrays.

```ts
export const userQueryKeys = {
  all: () => ["users"] as const,
  lists: () => [...userQueryKeys.all(), "list"] as const,
  list: (filters?: Partial<UserFilterDto>) =>
    [...userQueryKeys.lists(), filters ?? {}] as const,
  details: () => [...userQueryKeys.all(), "detail"] as const,
  detail: (id: number) => [...userQueryKeys.details(), id] as const,
  dataTable: (params?: unknown) =>
    [...userQueryKeys.all(), "dataTable", params] as const,
  options: () => [...userQueryKeys.all(), "options"] as const,
}
```

**Rules:**
- All keys are function calls — `all()`, `lists()`, `details()`, etc. — never plain arrays
- `dataTable` key — required for the DataTable hook
- `options` key — required for the dropdown options hook
- `invalidateQueries({ queryKey: userQueryKeys.all() })` invalidates everything — lists, details, dataTable, and options
- Prevents typo bugs — `queryClient.invalidateQueries({ queryKey: ["usr"] })` silently does nothing

---

### 3. Query options factory (v5 pattern)

Define reusable `queryOptions` objects that can be shared between `useQuery`, `prefetchQuery`, and server component data fetching.

```ts
export const userQueryOptions = {
  list: (filters?: Partial<UserFilterDto>) =>
    queryOptions({
      queryKey: userQueryKeys.list(filters),
      queryFn: () => getUsers(filters),
      staleTime: 30 * 1000,
      placeholderData: keepPreviousData,
    }),

  detail: (id: number) =>
    queryOptions({
      queryKey: userQueryKeys.detail(id),
      queryFn: () => getUserById(id),
      staleTime: 5 * 60 * 1000,
    }),
}
```

**Rules:**
- `staleTime` on list queries: `30 * 1000` (30 seconds)
- `staleTime` on detail queries: `5 * 60 * 1000` (5 minutes)
- `staleTime` on options queries: `15 * 60 * 1000` (15 minutes) — rarely changes
- `staleTime` on DataTable queries: `30_000` (30 seconds)
- Always add `placeholderData: keepPreviousData` to list queries — prevents blank flicker during pagination
- `keepPreviousData` replaces the removed `keepPreviousData: true` option from v4

---

### 4. Query hooks

Thin wrappers around `useQuery` that consume the `queryOptions` factory. Use `select` to unwrap `ActionResult<T>` into plain data.

```ts
export function useUsers(filters?: Partial<UserFilterDto>) {
  return useQuery({
    ...userQueryOptions.list(filters),
    select: (result) => {
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

export function useUser(id: number) {
  return useQuery({
    ...userQueryOptions.detail(id),
    select: (result) => {
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id > 0,
  })
}
```

**Rules:**
- Always use `select` to unwrap `ActionResult` — converts `{ success, data }` into plain `data` that components consume
- If `success: false`, throw inside `select` so TanStack Query puts the query into `isError` state
- Use `enabled` guards on detail hooks to prevent firing with an unset or invalid ID
  - For User IDs (number): `enabled: id > 0`
  - For Guid IDs (string): `enabled: !!id`
- Never access `result.data` directly in a component — always go through the hook's return value
- v5 removed `onSuccess`/`onError` from `useQuery` — never add them here

---

### 5. DataTable hook

Every feature needs this hook. It follows the exact 6-parameter signature that the DataTable component passes internally.

```ts
export function useUserDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange: { from_date: string; to_date: string },
  sortBy: string,
  sortOrder: string
) {
  return useQuery({
    queryKey: userQueryKeys.dataTable({ page, pageSize, search, dateRange, sortBy, sortOrder }),
    queryFn: async () => {
      const result = await getUsers({ page, pageSize, search, sortBy, sortOrder })
      if (!result.success) throw new Error(result.error)
      return {
        data: result.data.users,
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

// Required — DataTable checks this flag and calls the function as a React hook
useUserDataTable.isQueryHook = true
```

**Rules:**
- Always 6 positional parameters in this exact order — DataTable passes them internally
- `queryFn` calls the action directly and unwraps the result inline — throws on failure
- Returns `{ data: T[], pagination: { page, limit, total_pages, total_items } }` — this shape is what the DataTable expects
- Pagination uses `limit` (not `pageSize`) and `total_items` (not `total`) in the return shape — this is the DataTable's internal contract
- `.isQueryHook = true` must be set after the function definition — never omit this
- `staleTime: 30_000` always on DataTable hooks

---

### 6. Options hook (for FK dropdowns in other features)

Only needed if this feature is referenced as a foreign key in another feature's forms.

```ts
export function useUserOptions() {
  return useQuery({
    queryKey: userQueryKeys.options(),
    queryFn: async () => {
      const result = await getUsers({ pageSize: 100 })
      if (!result.success) throw new Error(result.error)
      return result.data.users.map((u) => ({ id: u.id, name: u.name }))
    },
    staleTime: 15 * 60 * 1000,
  })
}
```

**Rules:**
- Always `staleTime: 15 * 60 * 1000` — options lists rarely change
- Returns a minimal `{ id, name }` shape — never the full entity
- Requests a large page (`pageSize: 100`) to get all options in one call
- No `select` needed — `queryFn` already returns the unwrapped array directly

---

### 7. Mutation hooks

One hook per mutation operation. Each hook passes the action directly as `mutationFn` and checks `result.success` in `onSuccess`.

```ts
export function useCreateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createUser,
    onSuccess: (result) => {
      if (!result.success) return // error handled by the form via mutateAsync + form.setError
      queryClient.invalidateQueries({ queryKey: userQueryKeys.all() })
      toast.success("User created successfully.")
    },
    onError: () => {
      toast.error("An unexpected error occurred. Please try again.")
    },
  })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: updateUser,
    onSuccess: (result) => {
      if (!result.success) return
      queryClient.invalidateQueries({ queryKey: userQueryKeys.all() })
      toast.success("User updated successfully.")
    },
    onError: () => {
      toast.error("An unexpected error occurred. Please try again.")
    },
  })
}

export function useDeleteUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteUser,
    onSuccess: (result) => {
      if (!result.success) return
      queryClient.invalidateQueries({ queryKey: userQueryKeys.all() })
      toast.success("User deleted successfully.")
    },
    onError: () => {
      toast.error("An unexpected error occurred. Please try again.")
    },
  })
}
```

**Rules:**
- `mutationFn` is the action directly — never wrap it in an async arrow function
- `onSuccess` receives the full `ActionResult<T>` — always check `result.success` before invalidating
- `onError` fires only for truly unexpected errors (network down, thrown exception) — not for `{ success: false }` returns
- Always invalidate `featureQueryKeys.all()` — one call covers lists, details, dataTable, and options. Never use multiple `invalidateQueries` calls.
- `onSuccess` only shows toast and invalidates when `result.success` is true — failed mutations let the form handle the error via `mutateAsync`
- Never redirect from `onSuccess` inside the hook — redirect from the component after checking `mutation.data?.success`

---

### How Forms Consume Mutation Results

Forms use `mutateAsync` (not `mutate`) to `await` the result and call `form.setError()` inline:

```tsx
const mutation = useCreateUser()

async function onSubmit(values: CreateUserDto) {
  const result = await mutation.mutateAsync(values)

  if (!result.success) {
    if (result.fieldErrors) {
      Object.entries(result.fieldErrors).forEach(([field, messages]) => {
        form.setError(field as keyof CreateUserDto, {
          type: "server",
          message: messages[0],
        })
      })
    }
    return
  }

  form.reset()
  onSuccess?.()
}
```

Delete dialogs use `mutate` (not `mutateAsync`) with an `onSuccess` callback — no field errors on delete.

---

## staleTime Reference

| Data Type | staleTime | Reason |
|-----------|-----------|--------|
| List queries | `30 * 1000` | Lists change often |
| Detail queries | `5 * 60 * 1000` | Detail records are stable |
| Options queries | `15 * 60 * 1000` | Rarely changes |
| DataTable queries | `30_000` | Same as list |

---

## Complete Example — user.hooks.ts

```ts
import {
  useQuery,
  useMutation,
  useQueryClient,
  queryOptions,
  keepPreviousData,
} from "@tanstack/react-query"
import { toast } from "sonner"
import {
  getUsers,
  getUserById,
  createUser,
  updateUser,
  deleteUser,
} from "../actions/user.actions"
import type { UserFilterDto } from "../schemas/user.schema"

// ── Query Key Factory ──────────────────────────────────────────────────────

export const userQueryKeys = {
  all: () => ["users"] as const,
  lists: () => [...userQueryKeys.all(), "list"] as const,
  list: (filters?: Partial<UserFilterDto>) =>
    [...userQueryKeys.lists(), filters ?? {}] as const,
  details: () => [...userQueryKeys.all(), "detail"] as const,
  detail: (id: number) => [...userQueryKeys.details(), id] as const,
  dataTable: (params?: unknown) =>
    [...userQueryKeys.all(), "dataTable", params] as const,
  options: () => [...userQueryKeys.all(), "options"] as const,
}

// ── Query Options Factory ──────────────────────────────────────────────────

export const userQueryOptions = {
  list: (filters?: Partial<UserFilterDto>) =>
    queryOptions({
      queryKey: userQueryKeys.list(filters),
      queryFn: () => getUsers(filters),
      staleTime: 30 * 1000,
      placeholderData: keepPreviousData,
    }),

  detail: (id: number) =>
    queryOptions({
      queryKey: userQueryKeys.detail(id),
      queryFn: () => getUserById(id),
      staleTime: 5 * 60 * 1000,
    }),
}

// ── Query Hooks ────────────────────────────────────────────────────────────

export function useUsers(filters?: Partial<UserFilterDto>) {
  return useQuery({
    ...userQueryOptions.list(filters),
    select: (result) => {
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

export function useUser(id: number) {
  return useQuery({
    ...userQueryOptions.detail(id),
    select: (result) => {
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id > 0,
  })
}

// ── DataTable Hook ─────────────────────────────────────────────────────────

export function useUserDataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange: { from_date: string; to_date: string },
  sortBy: string,
  sortOrder: string
) {
  return useQuery({
    queryKey: userQueryKeys.dataTable({ page, pageSize, search, dateRange, sortBy, sortOrder }),
    queryFn: async () => {
      const result = await getUsers({ page, pageSize, search, sortBy, sortOrder })
      if (!result.success) throw new Error(result.error)
      return {
        data: result.data.users,
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

useUserDataTable.isQueryHook = true

// ── Options Hook ───────────────────────────────────────────────────────────

export function useUserOptions() {
  return useQuery({
    queryKey: userQueryKeys.options(),
    queryFn: async () => {
      const result = await getUsers({ pageSize: 100 })
      if (!result.success) throw new Error(result.error)
      return result.data.users.map((u) => ({ id: u.id, name: u.name }))
    },
    staleTime: 15 * 60 * 1000,
  })
}

// ── Mutation Hooks ─────────────────────────────────────────────────────────

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createUser,
    onSuccess: (result) => {
      if (!result.success) return
      queryClient.invalidateQueries({ queryKey: userQueryKeys.all() })
      toast.success("User created successfully.")
    },
    onError: () => {
      toast.error("An unexpected error occurred. Please try again.")
    },
  })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateUser,
    onSuccess: (result) => {
      if (!result.success) return
      queryClient.invalidateQueries({ queryKey: userQueryKeys.all() })
      toast.success("User updated successfully.")
    },
    onError: () => {
      toast.error("An unexpected error occurred. Please try again.")
    },
  })
}

export function useDeleteUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteUser,
    onSuccess: (result) => {
      if (!result.success) return
      queryClient.invalidateQueries({ queryKey: userQueryKeys.all() })
      toast.success("User deleted successfully.")
    },
    onError: () => {
      toast.error("An unexpected error occurred. Please try again.")
    },
  })
}
```

---

## Common Mistakes — Never Do These

- Never use the v4 positional API: `useQuery(queryKey, queryFn, options)` — v5 only accepts a single object
- Never add `onSuccess` or `onError` to `useQuery` — these callbacks were removed in v5; they only exist on `useMutation`
- Never write raw queryKey strings inline — always reference the `queryKeys` factory
- Never create a new `QueryClient` inside a hook or component — always use `getQueryClient()` or `useQueryClient()`
- Never call `revalidatePath` from a hook — that belongs in the action layer
- Never skip the `select` unwrap on query hooks — components should receive plain data, not `ActionResult`
- Never throw inside `onSuccess` — check `result.success` and return early if false
- Never use `keepPreviousData: true` (v4 option) — use `placeholderData: keepPreviousData` (v5)
- Never use `isLoading` for mutations — use `isPending` (v5 renamed it)
- Never duplicate `staleTime` or `queryFn` — define once in `queryOptions` factory, spread into `useQuery`
- Never use multiple `invalidateQueries` calls in one mutation — always `featureQueryKeys.all()` once
- Never import from another feature's hooks — only from your own actions and schema files
- Never create a barrel file — import directly from `{feature}.hooks.ts`
- Never omit `.isQueryHook = true` on the DataTable hook — DataTable won't call it as a React hook
- Never omit `dataTable` or `options` from the query key factory — they are required for cache invalidation
- Never wrap the action in `mutationFn` with an arrow function — pass the action directly
- Never leave `staleTime` unset on any `useQuery`