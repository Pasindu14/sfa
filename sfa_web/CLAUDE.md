# CLAUDE.md — sfa_web (Next.js 16)

## How to Run

```bash
cd sfa_web
npm run dev
npm run build
npx playwright test        # E2E tests (requires dev server running)
```

No `src/` directory — files live directly under `sfa_web/`.

---

## Directory Layout

```
sfa_web/
├── app/
│   ├── (auth)/login/                  ← login page
│   ├── (protected)/                   ← auth-guarded pages
│   └── api/auth/[...nextauth]/        ← NextAuth route handler
├── features/{feature}/                ← 5-layer feature modules
│   Each: actions/ hooks/ schema/ store/ components/
├── lib/
│   ├── actions/wrapper.ts             ← createAction() wrapper
│   ├── api/client.ts                  ← axios client (default export)
│   ├── api/query-keys.ts              ← shared query key helpers
│   ├── auth/                          ← auth helpers and wrappers
│   ├── hooks/use-error-toast.ts       ← handleErrorToast()
│   ├── types/actions.ts               ← ActionResponse<T>, PaginatedResponse<T>
│   └── utils.ts                       ← cn() and general utilities
├── components/
│   ├── ui/                            ← shadcn components
│   └── data-table/                    ← DataTable, toolbar, pagination, column-header
├── providers/                         ← QueryProvider, SessionProvider
└── e2e/                               ← Playwright tests (POM pattern)
```

---

## Implemented Features

> Full feature list with routes → @.claude/docs/web-features.md

| Auth | Users | Distributors | Regions | Areas |
|------|-------|--------------|---------|-------|
| Territories | Divisions | Outlets | Products | ProductCategories |
| ProductCategoryPricings | PurchaseOrders | SalesInvoices | GRNs | Routes |
| RouteCancellations | Stock | Fleets | UserGeoAssignments | UserReportingLines |

---

## Architecture & Patterns

> 7-layer data flow, TanStack Query v5, Zustand v5, import rules → @.claude/docs/web-architecture.md

**Data flow:** `schema → actions → hooks → store → components` — never skip layers

**API client:**
```ts
import client from '@/lib/api/client'  // default export, NOT named
```
- Request interceptor auto-attaches `Authorization: Bearer <accessToken>` — never call auth manually
- Response interceptor normalises all non-2xx into `ApiError`

**Server actions:**
```ts
import { createAction } from '@/lib/actions/wrapper'
// Wraps return in ActionResponse<T>; catches ZodError, AppError, ApiError — no try/catch needed
// Always call revalidatePath() after mutations
```

**Types:**
```ts
import type { ActionResponse } from '@/lib/types/actions'  // NOT 'action-result', NOT 'ActionResult'
// Also exports: PaginatedResponse<T>, PaginationParams, SortParams<T>, SearchParams
```

> TanStack Query v5 hooks, Zustand v5 store pattern, NextAuth v5 session details, and full import rules
> auto-loaded via `.claude/rules/web-conventions.md` when editing `sfa_web/**` files.
