# CLAUDE.md — SFA Project

## Monorepo Structure

| Directory | Stack | Notes |
|-----------|-------|-------|
| `sfa_api/` | .NET 8 ASP.NET Core | REST API, EF Core, SQL Server |
| `sfa_web/` | Next.js 16, TypeScript, App Router | Primary frontend |
| `sfa_mobile/` | flutter| Mobile client |

---

## Read First

**`sfa_web/AGENTS.md` is the authoritative spec for all Next.js patterns.**
Read it in full before generating any feature code in `sfa_web/`.

This file only documents what differs from `AGENTS.md`, plus context Claude Code needs that a human dev already knows.

---

## sfa_web — Source Layout

The `src/` directory does NOT exist. Files live directly under `sfa_web/`:

```
sfa_web/
├── app/
├── features/
├── lib/
│   ├── actions/
│   │   └── wrapper.ts        ← createAction() wrapper
│   ├── api/
│   │   └── client.ts         ← default export `client` (Axios)
│   ├── types/
│   │   └── actions.ts        ← ActionResponse<T>
│   └── ...
├── components/
├── hooks/
├── providers/
└── types/
```

---

## Corrections to AGENTS.md — What the Code Actually Does

### 1. API Client — Default Export, Has Interceptors

`AGENTS.md` shows a named export `apiClient` with no interceptors. **The actual file is different:**

```ts
// lib/api/client.ts
import client from '@/lib/api/client'   // ← default export, named "client"
```

The real `client.ts`:
- **Default export**: `export default client`
- **Has a request interceptor** that calls `auth()` and attaches the Bearer token automatically
- **Has a response interceptor** that normalises all non-2xx responses into an `ApiError` class
- No `getAuthHeaders()` is needed in actions — the interceptor handles auth

**Import it as:**
```ts
import client from '@/lib/api/client'
```

### 2. Actions Pattern — `createAction()` Wrapper, Not Manual Auth

`AGENTS.md` describes manual `getAuthHeaders()` + `handleApiError()` in every action file. **The actual pattern uses a wrapper:**

```ts
'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import { revalidatePath } from 'next/cache'

export const getUsersAction = createAction(
  { name: 'getUsersAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10) => {
    const res = await client.get('/api/v1/users', { params: { page, pageSize } })
    revalidatePath('/users')
    return res.data.data as UsersListResponse
  }
)
```

`createAction(config, handler)` provides:
- Auth check (`requireAuth: true`) and optional role guard (`requiredRole`)
- Wraps the return in `ActionResponse<T>` — `{ success: true, data }` or `{ success: false, error, code?, fields? }`
- Catches `ZodError`, `AppError`, and `ApiError` automatically — handler never needs try/catch
- No `getAuthHeaders()`, no `handleApiError()` — those do not exist in this codebase

### 3. ActionResponse Type — Location and Shape

`AGENTS.md` references `ActionResult<T>` from `@/lib/types/action-result`. **The actual type is:**

```ts
// lib/types/actions.ts
export type ActionResponse<T = void> =
  | { success: true; data: T }
  | { success: false; error: string; code?: string; fields?: Record<string, string> }
```

- Import from `@/lib/types/actions` (not `action-result`)
- Named `ActionResponse<T>` (not `ActionResult<T>`)
- `fields` is `Record<string, string>` (already flattened by the response interceptor — one string per field, not `string[]`)

### 4. Hooks Directive — `'use client'` IS Present

`AGENTS.md` says hooks files have no `'use client'`. **`user.hooks.ts` starts with `'use client'`.** Follow the actual file — add `'use client'` to hooks files.

### 5. Query Key Factory — Plain Values, Not Functions

`AGENTS.md` shows all keys as function calls (`all: () => ['users']`). **The actual pattern uses plain `as const` for the root key:**

```ts
export const userKeys = {
  all: ['users'] as const,                                   // ← plain array, NOT all()
  lists: () => [...userKeys.all, 'list'] as const,
  list: (filters: object) => [...userKeys.lists(), filters] as const,
  details: () => [...userKeys.all, 'detail'] as const,
  detail: (id: number) => [...userKeys.details(), id] as const,
}
```

**Invalidation uses the plain array directly:**
```ts
queryClient.invalidateQueries({ queryKey: userKeys.all })   // ← no () call
```

### 6. DataTable Hook — 8 Positional Args

`AGENTS.md` shows 6 args. **The actual signature has 8:**

```ts
export function useUserDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: Record<string, unknown>,
)
```

Args 4–7 may be unused (prefixed `_`) but must be present for DataTable compatibility.

The flag is set outside the function body:
```ts
;(useUserDataTable as any).isQueryHook = true
```

### 7. Store Barrel — Composite Dialog Hooks

`AGENTS.md` describes individual primitive selector hooks. **The actual barrel exports composite hooks:**

```ts
// features/user/store/index.ts
import { useShallow } from 'zustand/react/shallow'   // ← "zustand/react/shallow"

export const useCreateDialog = () =>
  useUserDialogStore(useShallow((s) => ({
    isOpen: s.isCreateOpen,
    open: s.openCreate,
    close: s.closeCreate,
  })))

export const useEditDialog = () =>
  useUserDialogStore(useShallow((s) => ({
    isOpen: s.isEditOpen,
    selectedId: s.selectedUserId,
    open: s.openEdit,
    close: s.closeEdit,
  })))
```

Each dialog hook returns `{ isOpen, open, close }` (plus `selectedId` for edit/delete/etc).

Also note: **`useShallow` is imported from `"zustand/react/shallow"`**, not `"zustand/shallow"` as AGENTS.md states.

### 8. Mutation Hooks — fieldErrors State Pattern

Mutation hooks manage field errors internally and expose them alongside the mutation:

```ts
export function useCreateUser() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateUserInput) => {
      const result = await createUserAction(data)
      if (!result.success) throw result   // ← throw the whole ActionResponse on failure
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.all })
      setFieldErrors(null)
      close()
      toast.success('...')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'user', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}
```

Key points:
- `mutationFn` throws `result` (the full `ActionResponse`) on failure — not a new `Error`
- `onError` receives that thrown object and reads `error.fields`
- The hook spreads `mutation` and appends `fieldErrors` and `clearFieldErrors`
- `handleErrorToast` is imported from `@/lib/hooks/use-error-toast`

---

## Quick Don'ts (Most Critical)

```
Never write: import { apiClient } from '@/lib/api/client'
Always use:  import client from '@/lib/api/client'

Never write: import type { ActionResult } from '@/lib/types/action-result'
Always use:  import type { ActionResponse } from '@/lib/types/actions'

Never write: async function getAuthHeaders() { ... }
Always use:  createAction() wrapper from '@/lib/actions/wrapper'

Never write: all: () => ['feature'] as const
Always use:  all: ['feature'] as const  (plain array, no function)

Never write: queryClient.invalidateQueries({ queryKey: featureKeys.all() })
Always use:  queryClient.invalidateQueries({ queryKey: featureKeys.all })

Never write: import { useShallow } from 'zustand/shallow'
Always use:  import { useShallow } from 'zustand/react/shallow'

Never omit 'use client' from hooks files — actual hooks files have it
Never use 6-arg DataTable hook signature — it takes 8 args
```

---

## sfa_api — .NET 8

Use the `sfa-feature-generator` skill when adding new API features. It follows the exact patterns from `Features/Users/`.

Key conventions:
- CQRS with MediatR — one file per command/query
- EF Core with repository pattern
- Response wrapped in `ApiResponse<T>` with `success`, `data`, `pagination`, `traceId`
- Soft delete via `deletedAt` — never hard delete
- JWT tenancy — `companyId` resolved from token, never sent by client
