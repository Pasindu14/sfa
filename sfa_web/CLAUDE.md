# CLAUDE.md — sfa_web (Next.js 16)

## How to Run

```bash
cd sfa_web
npm run dev
npm run build
```

No `src/` directory — files live directly under `sfa_web/`.

---

## Directory Layout

```
sfa_web/
├── app/
│   ├── (auth)/login/                  ← login page
│   ├── (protected)/                   ← auth-guarded pages (dashboard, users, distributors)
│   └── api/auth/[...nextauth]/        ← NextAuth route handler
├── features/{feature}/                ← feature modules (see 7-Layer Architecture below)
│   Each has: actions/, hooks/, schema/, store/, components/
├── lib/
│   ├── actions/wrapper.ts             ← createAction() wrapper
│   ├── actions/helpers.ts             ← shared action utilities
│   ├── api/client.ts                  ← axios client (default export)
│   ├── api/query-keys.ts              ← shared query key helpers
│   ├── auth/                          ← auth helpers and wrappers
│   ├── hooks/use-error-toast.ts       ← handleErrorToast()
│   ├── queries/pagination.ts          ← pagination utilities
│   ├── types/actions.ts               ← ActionResponse<T>, PaginatedResponse<T>
│   ├── types/common.ts                ← shared common types
│   ├── errors.ts                      ← AppError and error utilities
│   └── utils.ts                       ← general utilities (cn, etc.)
├── components/
│   ├── ui/                            ← shadcn components
│   ├── data-table/                    ← DataTable, toolbar, pagination, column-header
│   ├── app-sidebar.tsx                ← main sidebar
│   ├── calendar-date-picker.tsx       ← date range picker
│   ├── company-logo.tsx               ← company logo display
│   ├── error-boundary.tsx             ← React error boundary
│   ├── nav-main.tsx                   ← primary nav links
│   ├── nav-projects.tsx               ← project nav links
│   └── nav-user.tsx                   ← user nav/avatar menu
├── providers/                         ← QueryProvider, SessionProvider
├── hooks/                             ← use-debounce, use-mobile, use-error-toast
├── auth.ts + auth.config.ts           ← NextAuth v5 setup
└── e2e/                               ← Playwright tests (POM pattern)
```

### Implemented Features
| Feature      | Description                                    |
|--------------|------------------------------------------------|
| Auth         | Login form                                     |
| Users        | Full CRUD + password change (reference feature)|
| Distributors | Full CRUD                                      |

---

## 7-Layer Architecture

```
schema → actions → hooks → store → components
```

| Layer      | Location                          | Rule                                           |
|------------|-----------------------------------|------------------------------------------------|
| schema     | `features/{f}/schema/`            | Zod schemas + inferred TS types; no manual types |
| actions    | `features/{f}/actions/`           | `'use server'`; only layer that calls `client` |
| hooks      | `features/{f}/hooks/`             | `'use client'`; only layer that calls actions  |
| store      | `features/{f}/store/`             | Zustand; UI/dialog state only — no server data |
| components | `features/{f}/components/`        | Read from hooks/store; never call actions directly |

**Strict data flow — never skip layers.**

---

## API Client (`lib/api/client.ts`)

```ts
import client from '@/lib/api/client'  // ← default export, NOT named
```

- **Request interceptor:** Calls `auth()`, attaches `Authorization: Bearer <accessToken>` automatically
- **Response interceptor:** Normalises all non-2xx into `ApiError` — PascalCase field keys → camelCase, arrays → single string
- Never call `getAuthHeaders()` — it does not exist; the interceptor handles auth

`ApiError` shape: `{ status, code, message, fields: Record<string, string>, detail, currentData, traceId }`

---

## Server Actions (`lib/actions/wrapper.ts`)

```ts
'use server'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'

export const getItemsAction = createAction(
  { name: 'getItemsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page = 1, pageSize = 10) => {
    const res = await client.get('/api/v1/items', { params: { page, pageSize } })
    return res.data.data as ItemsListResponse
  }
)
```

- `createAction` wraps return in `ActionResponse<T>`; catches `ZodError`, `AppError`, `ApiError` — no try/catch in handlers
- Call `revalidatePath()` after mutations

---

## ActionResponse Type (`lib/types/actions.ts`)

```ts
import type { ActionResponse } from '@/lib/types/actions'  // NOT 'action-result', NOT 'ActionResult'
// { success: true; data: T } | { success: false; error: string; code?: string; fields?: Record<string, string> }
```

Also exports: `PaginatedResponse<T>`, `PaginationParams`, `SortParams<T>`, `SearchParams`

---

## TanStack Query v5 — Hooks Pattern

Query key factory: `all: ['items'] as const` (plain array, not a function). Invalidation: `queryClient.invalidateQueries({ queryKey: itemKeys.all })` — no `()` on `.all`.

**DataTable hook:** 8 positional args — `(page, pageSize, search, _dateRange?, _sortBy?, _sortOrder?, _caseConfig?, customFilters?)`. Set `(useItemDataTable as any).isQueryHook = true` outside the function body.

**Mutation hook pattern:**
- `mutationFn`: `if (!result.success) throw result` — throw full `ActionResponse` on failure
- `onSuccess`: invalidate `itemKeys.all`, `close()`, `toast.success()`
- `onError`: `error.fields` → `setFieldErrors`; `handleErrorToast(error, 'item', 'create')` from `@/lib/hooks/use-error-toast`
- Return: `{ ...mutation, fieldErrors, clearFieldErrors }`

---

## Zustand v5 — Store Pattern

Two stores per feature: dialog store (modal open/close + `selectedId`) and filter store (search, sort, pagination).

Barrel `store/index.ts` exports composite selector hooks using `useShallow` from `zustand/react/shallow`:
- `useCreateDialog()` → `{ isOpen, open, close }`
- `useEditDialog()` → `{ isOpen, selectedId, open, close }` (same pattern for delete, changePassword, etc.)
- `useItemFilters()` → all filter state + setters

---

## Auth (Next-Auth v5 Beta)

- Provider: Credentials → POST `/api/v1/auth/login`
- Session strategy: JWT (24h)
- Session shape: `{ user: { id, name, email, role, accessToken } }`
- No `companyId` in session — resolved server-side from JWT on the API
- `auth()` called in client interceptor — never call it in action handlers

---

## Never Do

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
Never call actions from components — go through hooks
Never store server data in Zustand — use TanStack Query
Never write manual TS types — always infer from Zod schemas
Never use `any` type except in mutation onError handlers
```
