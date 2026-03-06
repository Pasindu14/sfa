---
name: nextjs-feature-store
description: "Use when creating Zustand stores for a new feature like category, product, order or user. Creates two store files: a dialog store for modal and selected item state, and a filter store for search, filter, sort and pagination state. Never stores server data. Always uses Zustand with TypeScript interfaces."
---

# Store Skill

## Location

```
features/{feature}/store/{feature}-dialog.store.ts
features/{feature}/store/{feature}-filter.store.ts
features/{feature}/store/index.ts
```

Has barrel file. Always import through `index.ts` from outside this folder.

---

## Rules Before Writing Anything

1. Read `AGENTS.md` at the project root first
2. Zustand stores hold UI state only — never server data, never TanStack Query data
3. Two stores per feature — never combine them into one file
4. Dialog store — owns all modal open/close state and selected IDs
5. Filter store — owns search, filters, sort, page, pageSize
6. Always define a TypeScript interface for the store state before the store
7. Never call actions, services, or repositories from inside a store
8. Never put async logic inside a store — async belongs in hooks

---

## Imports — Both Files

```ts
import { create } from 'zustand'
```

Only Zustand. No other imports needed in store files.

---

## File 1 — `{feature}-dialog.store.ts`

### What It Tracks

```
isCreateOpen    ← create dialog visibility
isUpdateOpen    ← update dialog visibility
isDeleteOpen    ← delete/confirm dialog visibility
isDetailsOpen   ← details/view dialog visibility
updateId        ← ID of the item being updated
deleteId        ← ID of the item being deleted
detailsId       ← ID of the item being viewed
```

### Structure

```ts
import { create } from 'zustand'

interface CategoryDialogState {
  // State
  isCreateOpen: boolean
  isUpdateOpen: boolean
  isDeleteOpen: boolean
  isDetailsOpen: boolean
  updateId: number | null
  deleteId: number | null
  detailsId: number | null

  // Actions
  openCreate: () => void
  closeCreate: () => void
  openUpdate: (id: number) => void
  closeUpdate: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
  openDetails: (id: number) => void
  closeDetails: () => void
}

export const useCategoryDialogStore = create<CategoryDialogState>((set) => ({
  // Initial state
  isCreateOpen: false,
  isUpdateOpen: false,
  isDeleteOpen: false,
  isDetailsOpen: false,
  updateId: null,
  deleteId: null,
  detailsId: null,

  // Actions
  openCreate: () => set({ isCreateOpen: true }),
  closeCreate: () => set({ isCreateOpen: false }),

  openUpdate: (id) => set({ isUpdateOpen: true, updateId: id }),
  closeUpdate: () => set({ isUpdateOpen: false, updateId: null }),

  openDelete: (id) => set({ isDeleteOpen: true, deleteId: id }),
  closeDelete: () => set({ isDeleteOpen: false, deleteId: null }),

  openDetails: (id) => set({ isDetailsOpen: true, detailsId: id }),
  closeDetails: () => set({ isDetailsOpen: false, detailsId: null }),
}))
```

### Rules

- `openUpdate`, `openDelete`, `openDetails` always set both the boolean AND the ID together — never separately
- `closeUpdate`, `closeDelete`, `closeDetails` always reset both the boolean AND the ID to null together — never separately
- IDs are always `number | null` — never `number | undefined`
- Store name follows: `use{Feature}DialogStore`
- Interface name follows: `{Feature}DialogState`

---

## File 2 — `{feature}-filter.store.ts`

### What It Tracks

```
search      ← search string from DataTable search input
isActive    ← active/inactive filter (boolean | undefined)
page        ← current page number
pageSize    ← items per page
sortBy      ← column being sorted
sortOrder   ← asc or desc
```

### Structure

```ts
import { create } from 'zustand'

interface CategoryFilterState {
  // State
  search: string
  isActive: boolean | undefined
  page: number
  pageSize: number
  sortBy: string | undefined
  sortOrder: 'asc' | 'desc'

  // Actions
  setSearch: (search: string) => void
  setIsActive: (isActive: boolean | undefined) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSort: (sortBy: string, sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}

const initialState = {
  search: '',
  isActive: undefined,
  page: 1,
  pageSize: 10,
  sortBy: undefined,
  sortOrder: 'desc' as const,
}

export const useCategoryFilterStore = create<CategoryFilterState>((set) => ({
  // Initial state
  ...initialState,

  // Actions
  setSearch: (search) => set({ search, page: 1 }),
  setIsActive: (isActive) => set({ isActive, page: 1 }),
  setPage: (page) => set({ page }),
  setPageSize: (pageSize) => set({ pageSize, page: 1 }),
  setSort: (sortBy, sortOrder) => set({ sortBy, sortOrder, page: 1 }),
  resetFilters: () => set(initialState),
}))
```

### Rules

- Always extract `initialState` as a separate const — needed by `resetFilters` to reset cleanly
- `setSearch`, `setIsActive`, `setPageSize`, `setSort` always reset `page` to `1` — changing filters resets pagination
- `setPage` does NOT reset other state — it only changes the page
- `resetFilters` resets to `initialState` — everything back to defaults
- `isActive` is `boolean | undefined` — `undefined` means "show all", `true` means active only, `false` means inactive only
- `sortOrder` type is `'asc' | 'desc'` — never a plain string
- Store name follows: `use{Feature}FilterStore`
- Interface name follows: `{Feature}FilterState`

---

## File 3 — `index.ts` — Barrel

```ts
export { useCategoryDialogStore } from './{feature}-dialog.store'
export { useCategoryFilterStore } from './{feature}-filter.store'
```

Export only the store hooks. Do not export the interfaces — they are internal to each file.

---

## How Stores Connect to Components

Dialog store — used in dialogs, tables, and page components:

```tsx
// In table columns — open dialogs on row actions
const { openUpdate, openDelete, openDetails } = useCategoryDialogStore()

// In dialog components — read open state and selected ID
const { isUpdateOpen, updateId, closeUpdate } = useCategoryDialogStore()

// In page component — open create dialog from header button
const { openCreate } = useCategoryDialogStore()
```

Filter store — used in DataTable or custom filter components:

```tsx
// Read filter state to pass to DataTable or hooks
const { search, isActive, page, pageSize, sortBy, sortOrder } = useCategoryFilterStore()

// Update filters from filter UI components
const { setSearch, setIsActive, resetFilters } = useCategoryFilterStore()
```

---

## Feature-Specific Filters

If the feature has additional filters beyond `search` and `isActive`, add them to the filter store following the same pattern:

```ts
// Example — product feature has categoryId and price range filters
interface ProductFilterState {
  search: string
  isActive: boolean | undefined
  categoryId: number | undefined   // ← feature-specific
  minPrice: string | undefined     // ← feature-specific
  maxPrice: string | undefined     // ← feature-specific
  page: number
  pageSize: number
  sortBy: string | undefined
  sortOrder: 'asc' | 'desc'

  setSearch: (search: string) => void
  setIsActive: (isActive: boolean | undefined) => void
  setCategoryId: (categoryId: number | undefined) => void  // ← feature-specific
  setPriceRange: (min?: string, max?: string) => void      // ← feature-specific
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSort: (sortBy: string, sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}
```

Rules:
- Always add feature-specific filter setters that reset `page` to `1`
- Always include feature-specific fields in `initialState` so `resetFilters` clears them
- Never add filter state that duplicates what TanStack Query already tracks

---

## Complete Example

### `category-dialog.store.ts`
```ts
import { create } from 'zustand'

interface CategoryDialogState {
  isCreateOpen: boolean
  isUpdateOpen: boolean
  isDeleteOpen: boolean
  isDetailsOpen: boolean
  updateId: number | null
  deleteId: number | null
  detailsId: number | null
  openCreate: () => void
  closeCreate: () => void
  openUpdate: (id: number) => void
  closeUpdate: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
  openDetails: (id: number) => void
  closeDetails: () => void
}

export const useCategoryDialogStore = create<CategoryDialogState>((set) => ({
  isCreateOpen: false,
  isUpdateOpen: false,
  isDeleteOpen: false,
  isDetailsOpen: false,
  updateId: null,
  deleteId: null,
  detailsId: null,
  openCreate: () => set({ isCreateOpen: true }),
  closeCreate: () => set({ isCreateOpen: false }),
  openUpdate: (id) => set({ isUpdateOpen: true, updateId: id }),
  closeUpdate: () => set({ isUpdateOpen: false, updateId: null }),
  openDelete: (id) => set({ isDeleteOpen: true, deleteId: id }),
  closeDelete: () => set({ isDeleteOpen: false, deleteId: null }),
  openDetails: (id) => set({ isDetailsOpen: true, detailsId: id }),
  closeDetails: () => set({ isDetailsOpen: false, detailsId: null }),
}))
```

### `category-filter.store.ts`
```ts
import { create } from 'zustand'

interface CategoryFilterState {
  search: string
  isActive: boolean | undefined
  page: number
  pageSize: number
  sortBy: string | undefined
  sortOrder: 'asc' | 'desc'
  setSearch: (search: string) => void
  setIsActive: (isActive: boolean | undefined) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSort: (sortBy: string, sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}

const initialState = {
  search: '',
  isActive: undefined,
  page: 1,
  pageSize: 10,
  sortBy: undefined,
  sortOrder: 'desc' as const,
}

export const useCategoryFilterStore = create<CategoryFilterState>((set) => ({
  ...initialState,
  setSearch: (search) => set({ search, page: 1 }),
  setIsActive: (isActive) => set({ isActive, page: 1 }),
  setPage: (page) => set({ page }),
  setPageSize: (pageSize) => set({ pageSize, page: 1 }),
  setSort: (sortBy, sortOrder) => set({ sortBy, sortOrder, page: 1 }),
  resetFilters: () => set(initialState),
}))
```

### `index.ts`
```ts
export { useCategoryDialogStore } from './category-dialog.store'
export { useCategoryFilterStore } from './category-filter.store'
```

---

## Common Mistakes — Never Do These

- Never put both stores in one file — always two separate files
- Never store server data in Zustand — TanStack Query owns server state
- Never call actions or services from inside a store — stores are pure UI state
- Never use `number | undefined` for IDs — always `number | null`
- Never forget to reset `page` to `1` when any filter changes — stale pagination is a common bug
- Never forget `initialState` as a separate const — without it `resetFilters` cannot work cleanly
- Never export interfaces from the barrel — they are internal to each store file
- Never combine open and ID setters into separate calls — always set both together atomically
- Never add async logic to a store — async belongs in hooks and actions