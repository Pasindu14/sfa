# Web Architecture — sfa_web

## 7-Layer Architecture

```
schema → actions → hooks → store → components
```

| Layer | Location | Rule |
|-------|----------|------|
| schema | `features/{f}/schema/` | Zod schemas + inferred TS types; never write manual types |
| actions | `features/{f}/actions/` | `'use server'`; only layer that calls `client`; call `revalidatePath()` after mutations |
| hooks | `features/{f}/hooks/` | `'use client'`; only layer that calls actions; wraps TanStack Query |
| store | `features/{f}/store/` | Zustand; UI/dialog state only — no server data ever |
| components | `features/{f}/components/` | Read from hooks/store only; never call actions directly |

**Never skip layers.** Components never call actions; hooks never touch the store's server data.

## TanStack Query v5 — Hook Patterns

```ts
// Query key factory — plain array, NOT a function
export const itemKeys = {
  all: ['items'] as const,
  detail: (id: number) => [...itemKeys.all, id] as const,
}

// Invalidation — no () on .all
queryClient.invalidateQueries({ queryKey: itemKeys.all })
```

**DataTable hook — 8 positional args:**
```ts
(page, pageSize, search, _dateRange?, _sortBy?, _sortOrder?, _caseConfig?, customFilters?)
```
Set `(useItemDataTable as any).isQueryHook = true` outside the function body.

**Mutation hook pattern:**
```ts
mutationFn: async (input) => {
  const result = await someAction(input)
  if (!result.success) throw result   // throw full ActionResponse — not just an Error
  return result.data
},
onSuccess: () => {
  queryClient.invalidateQueries({ queryKey: itemKeys.all })
  close()
  toast.success('...')
},
onError: (error: any) => {
  if (error.fields) setFieldErrors(error.fields)
  handleErrorToast(error, 'item', 'create')  // from @/lib/hooks/use-error-toast
},
// Return shape:
return { ...mutation, fieldErrors, clearFieldErrors }
```

## Zustand v5 — Store Pattern

Two stores per feature:
- **Dialog store:** `isOpen`, `selectedId`, `open(id?)`, `close()`
- **Filter store:** `search`, `sortBy`, `sortOrder`, `page`, `pageSize` + setters

Barrel `store/index.ts` exports composite selector hooks using `useShallow`:
```ts
import { useShallow } from 'zustand/react/shallow'  // NOT 'zustand/shallow'

export const useCreateDialog = () => useDialogStore(useShallow(s => ({ isOpen: s.isOpen, open: s.open, close: s.close })))
export const useEditDialog = () => useDialogStore(useShallow(s => ({ isOpen: s.isOpen, selectedId: s.selectedId, open: s.open, close: s.close })))
```

## Server Actions Pattern

```ts
'use server'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'

export const getItemsAction = createAction(
  { name: 'getItemsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page = 1, pageSize = 10, search = '') => {
    const res = await client.get('/api/v1/items', { params: { page, pageSize, search } })
    return res.data.data as ItemsListResponse
  }
)
```

`createAction` wraps return in `ActionResponse<T>` and catches `ZodError`, `AppError`, `ApiError` — no try/catch in handlers.

## Critical Import Rules

```
Never:  import { apiClient } from '@/lib/api/client'
Always: import client from '@/lib/api/client'

Never:  import type { ActionResult } from '@/lib/types/action-result'
Always: import type { ActionResponse } from '@/lib/types/actions'

Never:  all: () => ['feature'] as const
Always: all: ['feature'] as const

Never:  queryClient.invalidateQueries({ queryKey: featureKeys.all() })
Always: queryClient.invalidateQueries({ queryKey: featureKeys.all })

Never:  import { useShallow } from 'zustand/shallow'
Always: import { useShallow } from 'zustand/react/shallow'

Never omit 'use client' from hooks files
Never use 6-arg DataTable hook — it takes 8 args
Never call actions from components — always go through hooks
Never store server data in Zustand — use TanStack Query
Never write manual TS types — always infer from Zod schemas
Never use `any` type except in mutation onError handlers
```
