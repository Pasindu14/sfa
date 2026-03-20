# State Stores

**Location:** `features/{entity}/store/` — three files.

## Sections
- Dialog Store
- Filter Store
- Barrel / Selectors

---

## Dialog Store

**File:** `store/{entity}.dialog-store.ts`

Tracks which dialog is open and the ID of the currently selected entity.

```typescript
import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface {Entity}DialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  selected{Entity}Id: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
}

// Zustand v5 — double-parentheses create syntax required
export const use{Entity}DialogStore = create<{Entity}DialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      selected{Entity}Id: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selected{Entity}Id: id }),
      closeEdit: () => set({ isEditOpen: false, selected{Entity}Id: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selected{Entity}Id: id }),
      closeDelete: () => set({ isDeleteOpen: false, selected{Entity}Id: null }),
    }),
    { name: '{Entity}DialogStore' }
  )
)
```

---

## Filter Store

**File:** `store/{entity}.filter-store.ts`

Holds search, pagination, and sort state. `setSearch` and `setPageSize` reset `page` to 1.

```typescript
import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface {Entity}FilterState {
  search: string
  page: number
  pageSize: number
  sortBy: string
  sortOrder: 'asc' | 'desc'
  setSearch: (search: string) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSortBy: (sortBy: string) => void
  setSortOrder: (sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}

const defaultState = {
  search: '',
  page: 1,
  pageSize: 10,
  sortBy: '',
  sortOrder: 'asc' as const,
}

export const use{Entity}FilterStore = create<{Entity}FilterState>()(
  devtools(
    (set) => ({
      ...defaultState,
      setSearch: (search) => set({ search, page: 1 }),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSortBy: (sortBy) => set({ sortBy }),
      setSortOrder: (sortOrder) => set({ sortOrder }),
      resetFilters: () => set(defaultState),
    }),
    { name: '{Entity}FilterStore' }
  )
)
```

---

## Barrel / Selectors

**File:** `store/index.ts`

Exports composite selector hooks so components import stable, memoized slices instead of the raw store. Import `useShallow` from `'zustand/react/shallow'` — the `'zustand/shallow'` path does not exist in v5.

```typescript
import { useShallow } from 'zustand/react/shallow'   // NOT 'zustand/shallow'
import { use{Entity}DialogStore } from './{entity}.dialog-store'
import { use{Entity}FilterStore } from './{entity}.filter-store'

export { use{Entity}DialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  use{Entity}DialogStore(
    useShallow((s) => ({ isOpen: s.isCreateOpen, open: s.openCreate, close: s.closeCreate }))
  )

export const useEditDialog = () =>
  use{Entity}DialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selected{Entity}Id,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  use{Entity}DialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selected{Entity}Id,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

// --- Filter selectors ---

export const use{Entity}Filters = () =>
  use{Entity}FilterStore(
    useShallow((s) => ({
      search: s.search,
      page: s.page,
      pageSize: s.pageSize,
      sortBy: s.sortBy,
      sortOrder: s.sortOrder,
      setSearch: s.setSearch,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSortBy: s.setSortBy,
      setSortOrder: s.setSortOrder,
      resetFilters: s.resetFilters,
    }))
  )
```

Each dialog selector returns `{ isOpen, open, close }` plus `selectedId` for edit/delete hooks.
