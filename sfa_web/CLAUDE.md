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

> TanStack Query v5 hooks pattern, Zustand v5 store pattern, NextAuth v5 details, and import
> rules are in `.claude/rules/web-conventions.md` (auto-loaded when editing `sfa_web/**` files).
