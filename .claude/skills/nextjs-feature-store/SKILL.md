---
name: sfa-nextjs-feature-store
description: Creating Zustand v5 UI stores for a feature module in the SFA Next.js web app. Use this when a feature needs client-side UI state that is NOT server data. Always creates two store files per feature — a dialog store for modal open/close state and selected IDs, and a filter store for search, filter, sort, and pagination state — plus a barrel index.ts that exports only named selector hooks. Never stores server data. Always uses Zustand v5 with TypeScript interfaces, double-parentheses create, devtools middleware, and useShallow for object selectors.
---

# Store Skill

## Location

```
features/{feature}/store/{feature}-dialog.store.ts
features/{feature}/store/{feature}-filter.store.ts
features/{feature}/store/index.ts
```

Has barrel file. Always import through `index.ts` from outside this folder — never from individual store files. Never export raw store instances — only named selector hooks.

---

## Rules Before Writing Anything

1. This project uses **Zustand v5** — import `useShallow` from `"zustand/shallow"`, not `"zustand/react/shallow"`
2. **Always use `create<T>()()` with double parentheses** — single parentheses `create<T>(...)` breaks middleware type inference in TypeScript and is the v4 API
3. **Always wrap with `devtools`** — every store gets devtools for Redux DevTools visibility; pass `{ name: "FeatureDialogStore" }` or `{ name: "FeatureFilterStore" }`
4. **Object and action-bundle selectors must always use `useShallow`** — without it, selectors create a new reference every render and cause `Maximum update depth exceeded` crashes in v5
5. **Two stores per feature — never combine them** — dialog store owns modal and selected ID state; filter store owns search, filters, sort, and pagination state
6. **Zustand is for UI state only** — never put API response data (users, orders, products) here; that lives in TanStack Query
7. **Never export raw store instances** — only export named selector hooks through the barrel

---

## What Belongs in Each Store

| State | Store |
|-------|-------|
| `isCreateOpen`, `isUpdateOpen`, `isDeleteOpen`, `isDetailsOpen` | Dialog store |
| `updateId`, `deleteId`, `detailsId` | Dialog store |
| `search` | Filter store |
| `isActive`, feature-specific filters | Filter store |
| `page`, `pageSize`, `sortBy`, `sortOrder` | Filter store |
| API response data (list of users, order details) | TanStack Query — never Zustand |

---

## Imports — Both Files

```ts
import { create } from "zustand"              // ^5.0.11
import { devtools } from "zustand/middleware"
import { useShallow } from "zustand/shallow"  // v5 path — NOT "zustand/react/shallow"
```

**Rules:**
- Always import all three — `create`, `devtools`, `useShallow`
- `useShallow` from `"zustand/shallow"` — this path changed in v5; `"zustand/react/shallow"` is the v4 path and will cause a module not found error in v5
- Never import `persist` in feature stores — UI state is ephemeral and resets on navigation
- Never import `immer` in feature stores — state is flat enough for plain immutable updates

---

## File 1 — `{feature}-dialog.store.ts`

### What It Tracks

```
isCreateOpen    ← create dialog visibility
isUpdateOpen    ← update dialog visibility
isDeleteOpen    ← delete / confirm dialog visibility
isDetailsOpen   ← details / view dialog visibility
updateId        ← ID of the item being updated
deleteId        ← ID of the item being deleted
detailsId       ← ID of the item being viewed
```

### Structure

```ts
import { create } from "zustand"
import { devtools } from "zustand/middleware"
import { useShallow } from "zustand/shallow"

// ── Interfaces ─────────────────────────────────────────────────────────────

interface CategoryDialogState {
  isCreateOpen: boolean
  isUpdateOpen: boolean
  isDeleteOpen: boolean
  isDetailsOpen: boolean
  updateId: string | null
  deleteId: string | null
  detailsId: string | null
}

interface CategoryDialogActions {
  openCreate: () => void
  closeCreate: () => void
  openUpdate: (id: string) => void
  closeUpdate: () => void
  openDelete: (id: string) => void
  closeDelete: () => void
  openDetails: (id: string) => void
  closeDetails: () => void
}

type CategoryDialogStore = CategoryDialogState & CategoryDialogActions

// ── Initial State ──────────────────────────────────────────────────────────

const initialState: CategoryDialogState = {
  isCreateOpen: false,
  isUpdateOpen: false,
  isDeleteOpen: false,
  isDetailsOpen: false,
  updateId: null,
  deleteId: null,
  detailsId: null,
}

// ── Store ──────────────────────────────────────────────────────────────────

const useCategoryDialogStore = create<CategoryDialogStore>()(
  devtools(
    (set) => ({
      ...initialState,

      openCreate: () => set({ isCreateOpen: true }, false, "openCreate"),
      closeCreate: () => set({ isCreateOpen: false }, false, "closeCreate"),

      openUpdate: (id) => set({ isUpdateOpen: true, updateId: id }, false, "openUpdate"),
      closeUpdate: () => set({ isUpdateOpen: false, updateId: null }, false, "closeUpdate"),

      openDelete: (id) => set({ isDeleteOpen: true, deleteId: id }, false, "openDelete"),
      closeDelete: () => set({ isDeleteOpen: false, deleteId: null }, false, "closeDelete"),

      openDetails: (id) => set({ isDetailsOpen: true, detailsId: id }, false, "openDetails"),
      closeDetails: () => set({ isDetailsOpen: false, detailsId: null }, false, "closeDetails"),
    }),
    { name: "CategoryDialogStore" }
  )
)

// ── Selector Hooks ─────────────────────────────────────────────────────────

export const useIsCreateOpen = () =>
  useCategoryDialogStore((state) => state.isCreateOpen)

export const useIsUpdateOpen = () =>
  useCategoryDialogStore((state) => state.isUpdateOpen)

export const useIsDeleteOpen = () =>
  useCategoryDialogStore((state) => state.isDeleteOpen)

export const useIsDetailsOpen = () =>
  useCategoryDialogStore((state) => state.isDetailsOpen)

export const useUpdateId = () =>
  useCategoryDialogStore((state) => state.updateId)

export const useDeleteId = () =>
  useCategoryDialogStore((state) => state.deleteId)

export const useDetailsId = () =>
  useCategoryDialogStore((state) => state.detailsId)

export const useCategoryDialogActions = () =>
  useCategoryDialogStore(
    useShallow((state) => ({
      openCreate: state.openCreate,
      closeCreate: state.closeCreate,
      openUpdate: state.openUpdate,
      closeUpdate: state.closeUpdate,
      openDelete: state.openDelete,
      closeDelete: state.closeDelete,
      openDetails: state.openDetails,
      closeDetails: state.closeDetails,
    }))
  )
```

### Rules

- `openUpdate`, `openDelete`, `openDetails` always set both the boolean AND the ID in the same `set` call — never separately
- `closeUpdate`, `closeDelete`, `closeDetails` always reset both the boolean AND the ID to `null` in the same `set` call — never separately
- ID type is `string | null` for all entities except User — User IDs are `number | null` because they are int auto increment
- IDs are always `type | null` — never `type | undefined`
- `set` second argument is always `false` — never `true` (true replaces entire state)
- `set` third argument is the action name string — shows in Redux DevTools for every state change
- Store type name follows: `{Feature}DialogStore`
- Interface names follow: `{Feature}DialogState`, `{Feature}DialogActions`
- DevTools name follows: `"{Feature}DialogStore"`
- The raw store `useCategoryDialogStore` is never exported — only the named selector hooks below it

---

## File 2 — `{feature}-filter.store.ts`

### What It Tracks

```
search      ← search string from DataTable search input
isActive    ← active / inactive filter (boolean | undefined)
page        ← current page number
pageSize    ← items per page
sortBy      ← column being sorted
sortOrder   ← asc or desc
```

### Structure

```ts
import { create } from "zustand"
import { devtools } from "zustand/middleware"
import { useShallow } from "zustand/shallow"

// ── Interfaces ─────────────────────────────────────────────────────────────

interface CategoryFilterState {
  search: string
  isActive: boolean | undefined
  page: number
  pageSize: number
  sortBy: string | undefined
  sortOrder: "asc" | "desc"
}

interface CategoryFilterActions {
  setSearch: (search: string) => void
  setIsActive: (isActive: boolean | undefined) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSort: (sortBy: string, sortOrder: "asc" | "desc") => void
  resetFilters: () => void
}

type CategoryFilterStore = CategoryFilterState & CategoryFilterActions

// ── Initial State ──────────────────────────────────────────────────────────

const initialState: CategoryFilterState = {
  search: "",
  isActive: undefined,
  page: 1,
  pageSize: 20,
  sortBy: undefined,
  sortOrder: "desc",
}

// ── Store ──────────────────────────────────────────────────────────────────

const useCategoryFilterStore = create<CategoryFilterStore>()(
  devtools(
    (set) => ({
      ...initialState,

      setSearch: (search) => set({ search, page: 1 }, false, "setSearch"),
      setIsActive: (isActive) => set({ isActive, page: 1 }, false, "setIsActive"),
      setPage: (page) => set({ page }, false, "setPage"),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }, false, "setPageSize"),
      setSort: (sortBy, sortOrder) => set({ sortBy, sortOrder, page: 1 }, false, "setSort"),
      resetFilters: () => set(initialState, false, "resetFilters"),
    }),
    { name: "CategoryFilterStore" }
  )
)

// ── Selector Hooks ─────────────────────────────────────────────────────────

export const useSearch = () =>
  useCategoryFilterStore((state) => state.search)

export const useIsActive = () =>
  useCategoryFilterStore((state) => state.isActive)

export const usePage = () =>
  useCategoryFilterStore((state) => state.page)

export const usePageSize = () =>
  useCategoryFilterStore((state) => state.pageSize)

export const useSortBy = () =>
  useCategoryFilterStore((state) => state.sortBy)

export const useSortOrder = () =>
  useCategoryFilterStore((state) => state.sortOrder)

export const useCategoryFilterActions = () =>
  useCategoryFilterStore(
    useShallow((state) => ({
      setSearch: state.setSearch,
      setIsActive: state.setIsActive,
      setPage: state.setPage,
      setPageSize: state.setPageSize,
      setSort: state.setSort,
      resetFilters: state.resetFilters,
    }))
  )
```

### Rules

- Always extract `initialState` as a typed constant — needed by `resetFilters` to reset cleanly
- `setSearch`, `setIsActive`, `setPageSize`, `setSort` always reset `page` to `1` — changing any filter must reset pagination
- `setPage` does NOT reset other state — it only changes the page
- `resetFilters` passes `initialState` directly to `set` — never reconstruct defaults manually
- `isActive` is `boolean | undefined` — `undefined` means show all, `true` means active only, `false` means inactive only
- `sortOrder` type is `"asc" | "desc"` — never a plain `string`
- `pageSize` default is `20` — matches the .NET API default page size
- Store type name follows: `{Feature}FilterStore`
- Interface names follow: `{Feature}FilterState`, `{Feature}FilterActions`
- DevTools name follows: `"{Feature}FilterStore"`
- The raw store `useCategoryFilterStore` is never exported — only the named selector hooks below it

---

## File 3 — `index.ts` — Barrel

```ts
export {
  useCategoryDialogActions,
  useIsCreateOpen,
  useIsUpdateOpen,
  useIsDeleteOpen,
  useIsDetailsOpen,
  useUpdateId,
  useDeleteId,
  useDetailsId,
} from "./category-dialog.store"

export {
  useCategoryFilterActions,
  useSearch,
  useIsActive,
  usePage,
  usePageSize,
  useSortBy,
  useSortOrder,
} from "./category-filter.store"
```

**Rules:**
- Export all named selector hooks from both store files
- Never export the raw store instances (`useCategoryDialogStore`, `useCategoryFilterStore`)
- Never export the interfaces — they are internal to each store file
- This is the only file that should be imported from outside the store folder

---

## Feature-Specific Filters

If the feature has additional filters beyond `search` and `isActive`, add them to the filter store following the same pattern:

```ts
// Example — product feature adds categoryId and price range
interface ProductFilterState {
  search: string
  isActive: boolean | undefined
  categoryId: string | undefined   // ← feature-specific, string (Guid) or undefined
  minPrice: string | undefined     // ← feature-specific, string (decimal as string)
  maxPrice: string | undefined     // ← feature-specific
  page: number
  pageSize: number
  sortBy: string | undefined
  sortOrder: "asc" | "desc"
}

interface ProductFilterActions {
  setSearch: (search: string) => void
  setIsActive: (isActive: boolean | undefined) => void
  setCategoryId: (categoryId: string | undefined) => void  // ← feature-specific
  setPriceRange: (min?: string, max?: string) => void      // ← feature-specific
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSort: (sortBy: string, sortOrder: "asc" | "desc") => void
  resetFilters: () => void
}
```

**Rules:**
- Feature-specific filter setters always reset `page` to `1`
- Feature-specific fields must be included in `initialState` with `undefined` defaults so `resetFilters` clears them
- Decimal / price values are always `string | undefined` — the .NET API returns decimals as strings

---

## `useShallow` Rules

| Selector type | Needs `useShallow`? |
|---------------|---------------------|
| Single primitive (`string`, `number`, `boolean`, `null`, `undefined`) | No — stable by value |
| Single object or array | Yes — always |
| Action bundle (multiple functions selected together) | Yes — required |
| Single action function | No — stable reference |

```ts
// ✅ No useShallow — single primitive
export const useIsCreateOpen = () =>
  useCategoryDialogStore((state) => state.isCreateOpen)

// ✅ useShallow required — action bundle
export const useCategoryDialogActions = () =>
  useCategoryDialogStore(
    useShallow((state) => ({
      openCreate: state.openCreate,
      closeCreate: state.closeCreate,
    }))
  )

// ❌ Missing useShallow on object selector — causes Maximum update depth exceeded
export const useCategoryDialogActions = () =>
  useCategoryDialogStore((state) => ({
    openCreate: state.openCreate,
    closeCreate: state.closeCreate,
  }))
```

---

## ID Type Reference

| Entity | ID type in store | Reason |
|--------|-----------------|--------|
| User | `number \| null` | int auto increment |
| All other entities | `string \| null` | Guid v7 from .NET API |

---

## Common Mistakes — Never Do These

- Never import `useShallow` from `"zustand/react/shallow"` — use `"zustand/shallow"` in v5
- Never use `create<T>(...)` with single parentheses — always `create<T>()(...)` (Zustand v5)
- Never select objects or arrays without `useShallow` — causes `Maximum update depth exceeded` crash in v5
- Never put both stores in one file — always two separate files
- Never export the raw store instances — only export named selector hooks
- Never export interfaces from the barrel — they are internal to each store file
- Never import from individual store files from outside the store folder — always through `index.ts`
- Never store server data in Zustand — API response data belongs in TanStack Query
- Never combine `openUpdate` boolean and `updateId` into separate `set` calls — always set both atomically
- Never combine `closeUpdate` boolean and `updateId` into separate `set` calls — always reset both atomically
- Never use `number | undefined` or `string | undefined` for IDs — always `number | null` (User) or `string | null` (all others)
- Never forget to reset `page` to `1` when any filter changes — stale pagination is a common bug
- Never reconstruct default values in `resetFilters` — always spread `initialState`
- Never pass `true` as the second argument to `set` — this replaces the entire state instead of merging
- Never omit the action name string (third `set` argument) — makes DevTools unreadable
- Never use `persist` in a feature store — feature UI state should reset on navigation
- Never make actions `async` — actions are synchronous state transitions only