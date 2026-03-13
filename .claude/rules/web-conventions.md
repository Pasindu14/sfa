---
description: Next.js web app conventions — TanStack Query v5, Zustand v5, NextAuth v5, and import rules. Loaded only when editing sfa_web files.
paths:
  - "sfa_web/**"
---

# Web Conventions — sfa_web

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

## Auth (NextAuth v5 Beta)

- Provider: Credentials → POST `/api/v1/auth/login`
- Session strategy: JWT (24h)
- Session shape: `{ user: { id, name, email, role, accessToken } }`
- No `companyId` in session — resolved server-side from JWT on the API
- `auth()` called in client interceptor — never call it in action handlers

---

## Web-Specific Never Do

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
