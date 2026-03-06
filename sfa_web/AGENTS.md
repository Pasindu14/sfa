# Project AGENTS.md — Source of Truth for Cascade

Read this entire file before touching any code. Every rule here is non-negotiable.

---

## Stack

| Concern | Package | Version | Notes |
|---------|---------|---------|-------|
| Framework | `next` | 16.1.6 | App Router — server components by default |
| Language | TypeScript | strict | No `any`, no `as` casting |
| Runtime | `react` / `react-dom` | 19.2.3 | — |
| Backend API | `axios` | ^1.13.6 | All API calls — no raw fetch |
| Auth | `next-auth` | ^5.0.0-beta.30 | Auth.js v5 — `auth()` from `@/lib/auth` |
| Validation | `zod` | ^4.3.6 | `import * as z from "zod"` — never `{ z }` |
| UI components | `radix-ui`, `class-variance-authority`, `clsx`, `tailwind-merge` | — | shadcn/ui base — never import other component libraries |
| Icons | `lucide-react` | ^0.575.0 | **Primary icon library** — use for all feature icons |
| Icons (supplemental) | `@radix-ui/react-icons`, `react-icons` | — | Available but use lucide-react first |
| Server state | `@tanstack/react-query` | ^5.90.21 | All server data — v5 single-object API |
| Table | `@tanstack/react-table` | ^8.21.3 | `ColumnDef` for feature columns |
| Client state | `zustand` | ^5.0.11 | UI state only — `create<T>()()` double parens |
| Forms | `react-hook-form` | ^7.71.2 | Always with `zodResolver` on create forms |
| Form resolvers | `@hookform/resolvers` | ^5.2.2 | `zodResolver` from here |
| Toasts | `sonner` | ^2.0.7 | `import { toast } from "sonner"` |
| Dates | `date-fns` | ^4.1.0 | v4 API — never native Date methods |
| Timezones | `date-fns-tz` | ^3.2.0 | Always use for timezone-aware formatting |
| Animations | `motion` | ^12.34.3 | Framer Motion v12 — for UI animations only |
| Theming | `next-themes` | ^0.4.6 | Theme provider in root layout |
| URL state | `nuqs` | ^2.8.8 | For shareable URL search params (e.g. DataTable filters exposed in URL) |
| Debounce | `use-debounce` | ^10.1.0 | For search input debouncing |
| Excel export | `exceljs` | ^4.4.0 | Used by DataTable export — never import `xlsx` directly in feature code |
| Charts | `recharts` | ^2.15.4 | For dashboard/reporting pages |
| Carousel | `embla-carousel-react` | ^8.6.0 | shadcn Carousel dependency |
| Command palette | `cmdk` | ^1.1.1 | shadcn Command dependency |
| Drawer | `vaul` | ^1.1.2 | shadcn Drawer dependency |
| Date picker | `react-day-picker` | ^9.14.0 | shadcn Calendar dependency |
| OTP input | `input-otp` | ^1.4.2 | shadcn InputOTP dependency |
| Resizable panels | `react-resizable-panels` | ^4.6.4 | shadcn ResizablePanels dependency |
| UUID | `uuid` | ^13.0.0 | Client-side UUID generation if needed |
| Password hashing | `bcryptjs` / `@node-rs/bcrypt` | — | Server-side only — never in client components |
| Env | `dotenv` | ^17.3.1 | Loaded via `@/lib/env` |

### Critical Package Rules

- **Spinner**: `<Spinner />` is a **local shadcn component** at `@/components/ui/spinner` — never import from `react-spinners`
- **Icons**: Always use `lucide-react` for feature icons (columns, buttons, dropdowns). `@radix-ui/react-icons` and `react-icons` are available for shadcn internal use only
- **Excel**: `exceljs` powers the DataTable export. Never import `exceljs` or `xlsx` directly in feature code — the shared DataTable component handles export internally
- **Zod**: Always `import * as z from "zod"` — `^4.3.6` is Zod 4, which has a different API from Zod 3
- **date-fns**: Version 4 — some v3 APIs changed. Always use `date-fns` and `date-fns-tz` for any date formatting or timezone handling, never `new Date().toLocaleString()` or similar native methods

---

## Architecture — The Simplified Stack

This project uses a **3-layer architecture**. There are no service or repository layers. Actions call the .NET API directly.

```
components/
  → hooks/ (TanStack Query v5)
    → actions/ ("use server" — calls .NET API via apiClient)
      → .NET Core API
```

**Never add services/ or repositories/ folders to any feature.** They do not exist in this architecture.

---

## Project Structure

```
features/
└── {feature}/
    ├── actions/
    │   └── {feature}.actions.ts       ← one file, no barrel
    ├── components/
    │   ├── dialogs/
    │   │   ├── create-{feature}-dialog.tsx
    │   │   ├── update-{feature}-dialog.tsx
    │   │   ├── delete-{feature}-dialog.tsx
    │   │   ├── {feature}-details-dialog.tsx
    │   │   └── index.ts
    │   ├── forms/
    │   │   ├── create-{feature}-form.tsx
    │   │   ├── update-{feature}-form.tsx
    │   │   └── index.ts
    │   ├── tables/
    │   │   ├── {feature}-table.tsx
    │   │   ├── {feature}-columns.tsx
    │   │   └── index.ts
    │   ├── pages/
    │   │   ├── {feature}-list-page.tsx
    │   │   └── index.ts
    │   ├── types.ts
    │   └── index.ts
    ├── hooks/
    │   └── {feature}.hooks.ts         ← one file, no barrel
    ├── schemas/
    │   └── {feature}.schema.ts        ← one file, no barrel
    └── store/
        ├── {feature}-dialog.store.ts
        ├── {feature}-filter.store.ts
        └── index.ts                   ← barrel, always import through this

app/
└── (protected)/
    └── {feature}/
        └── page.tsx    ← server component, created after components layer

lib/
├── api/
│   └── client.ts            ← apiClient (Axios instance, no interceptors)
├── auth.ts                  ← Auth.js v5 exports (auth, signIn, signOut)
├── types/
│   ├── action-result.ts     ← ActionResult<T> discriminated union
│   └── api-response.ts      ← ApiResponse<T>, ApiErrorResponse
└── utils.ts
```

---

## Layer Responsibilities

### `schemas/{feature}.schema.ts`

- Zod 4 schemas only — `import * as z from "zod"`, never `{ z }`
- All schemas in one file — createSchema, updateSchema, filterSchema, entity type, option type
- TypeScript types are **always inferred from Zod** — never written manually (except the entity type and option type which mirror the API response)
- No barrel file — import directly from the schema file path always
- Never include `id`, `createdAt`, `updatedAt`, `createdBy`, `updatedBy`, `tenantId`, or `companyId` in create schemas

### `actions/{feature}.actions.ts`

- First line is always `"use server"` — before any imports
- Calls the .NET API directly via `apiClient` from `@/lib/api/client`
- Auth token injected per-action via `getAuthHeaders()` which calls `auth()` from Auth.js — never via axios interceptors
- Always returns `ActionResult<T>` — never throws, never returns raw data
- Always calls `revalidatePath` after a successful mutation
- Handles both query (GET) and mutation (POST/PUT/DELETE) actions in one file
- Shares one `getAuthHeaders()` helper and one `handleApiError()` handler — both private, not exported
- No barrel file — import directly from the actions file path always

### `hooks/{feature}.hooks.ts`

- TanStack Query v5 — single-object API always (`useQuery({ ... })`, never positional args)
- No `"use client"` directive — hooks are imported by client components which already declare it
- All hooks, query key factory, query options factory, and data table hook in one file
- No barrel file — import directly from the hooks file path always
- Query hooks use `select` to unwrap `ActionResult<T>` into plain data
- Mutation hooks pass the action directly as `mutationFn` and check `result.success` in `onSuccess`
- Mutations invalidate `featureQueryKeys.all()` — one call covers lists, details, dataTable, and options
- `use{Feature}DataTable` hook must have `.isQueryHook = true` set on it

### `store/`

- Zustand v5 — always `create<T>()()` with double parentheses
- Always wrapped with `devtools` middleware
- Object and action-bundle selectors always use `useShallow` from `"zustand/shallow"`
- Two store files per feature — dialog store and filter store — never combined
- Has barrel `index.ts` — always import through it from outside the store folder
- Never export raw store instances — only export named selector hooks

### `components/`

- All files are `"use client"` — no exceptions
- Only shadcn/ui components — no other UI library
- Never call actions directly — always through mutation hooks
- Never fetch data directly — always through query hooks
- All loading states use `<Spinner />` from `@/components/ui/spinner` — never loading text
- No shared `{feature}-form-fields.tsx` — each form owns its fields inline
- Foreign key fields always render as `<Select>` — never `<Input>` — populated from `use{RelatedFeature}Options` called directly inside the form

### `app/(protected)/{feature}/page.tsx`

- Server component — no `"use client"`
- Imports `{Feature}ListPage` through the components barrel only
- No data fetching, no props, no state — just renders the page component

---

## Shared Library Files

### `lib/api/client.ts` — Authenticated Axios Instance

```ts
import axios from "axios"
import { env } from "@/lib/env"

export const apiClient = axios.create({
  baseURL: env.SFA_API_URL,
  headers: {
    "Content-Type": "application/json",
  },
})
```

Plain axios instance — no interceptors. Token injection happens per-action via `auth()`, not via a request interceptor, because interceptors cannot access the Auth.js session from within server action execution context.

### `lib/types/action-result.ts` — Typed Return Envelope

Every action returns one of these. Never return raw data or throw from an action.

```ts
export type ActionResult<T = void> =
  | { success: true; data: T }
  | {
      success: false
      error: string
      fieldErrors?: Record<string, string[]>
    }
```

### `lib/types/api-response.ts` — .NET API Response Envelope

```ts
export type ApiResponse<T> = {
  success: boolean
  data: T
  pagination?: {
    page: number
    pageSize: number
    total: number
    totalPages: number
  }
  traceId: string
}

export type ApiErrorResponse = {
  success: false
  error: {
    code: string
    message: string
    detail?: string
    fields?: Record<string, string[]>
    traceId: string
    timestamp: string
  }
}
```

---

## Data Flow — Strict and Non-Negotiable

```
components → hooks → actions → .NET Core API
```

- Components never call actions directly — always through hooks
- Hooks never call the API directly — always through actions
- Actions never skip `getAuthHeaders()` — every action checks auth first
- Actions never throw — always return `ActionResult<T>`

---

## API Contract — SFA .NET Core API

### Base URL
```
env.SFA_API_URL  (from @/lib/env)
```

### Authentication
- Bearer JWT only: `Authorization: Bearer <token>`
- Token retrieved per-action via `auth()` from `@/lib/auth` inside a `getAuthHeaders()` helper
- No `x-company-id` header — tenancy is resolved by the API from the JWT

### Endpoint Patterns
```
GET    /api/v1/{features}          ← paginated list
GET    /api/v1/{features}/{id}     ← single record
POST   /api/v1/{features}         ← create
PUT    /api/v1/{features}/{id}     ← update (always PUT, never PATCH)
DELETE /api/v1/{features}/{id}     ← delete
```

### Pagination Query Params
```
?page=1&pageSize=20
```
Always camelCase. Never `limit`, never `page_size`.

### API Response Shape — Single Record
```ts
// axios response.data shape:
{
  success: boolean
  data: T
  traceId: string
}
// Access the record as: response.data.data
```

### API Response Shape — Paginated List
```ts
// axios response.data shape:
{
  success: boolean
  data: T[]
  pagination: {
    page: number
    pageSize: number
    total: number       // ← "total", not "totalCount"
    totalPages: number
  }
  traceId: string
}
// Access items as: response.data.data
// Access pagination as: response.data.pagination
```

### Axios Argument Order — Critical
```ts
// GET / DELETE — 2 args: url, config
apiClient.get(url, { headers })
apiClient.delete(url, { headers })

// POST / PUT — 3 args: url, body, config
apiClient.post(url, body, { headers })
apiClient.put(url, body, { headers })
```

---

## Actions Pattern

### `getAuthHeaders()` — Private Per-File

```ts
async function getAuthHeaders() {
  const session = await auth()
  if (!session?.user?.accessToken) {
    throw new Error("Unauthorized")
  }
  return {
    Authorization: `Bearer ${session.user.accessToken}`,
  }
}
```

Called at the top of every action before any API call.

### `handleApiError()` — Private Per-File

Maps axios errors to `ActionResult<never>`. Always called in the catch block — never inline error handling per action.

| HTTP Status | Response |
|-------------|----------|
| 400 with fields | `fieldErrors` populated |
| 401 | Session expired message |
| 403 | Permission denied message |
| 404 | `error.message` from API |
| 409 | `error.message` from API (concurrency conflict) |
| 422 | `error.message` from API (business rule) |
| 503 | Generic retry message |

### Mutation actions always call `revalidatePath` after success — never in the catch block.

---

## Hooks Pattern

### Query Key Factory

```ts
export const featureQueryKeys = {
  all: () => ["features"] as const,
  lists: () => [...featureQueryKeys.all(), "list"] as const,
  list: (filters?: Partial<FeatureFilterDto>) =>
    [...featureQueryKeys.lists(), filters ?? {}] as const,
  details: () => [...featureQueryKeys.all(), "detail"] as const,
  detail: (id: string) => [...featureQueryKeys.details(), id] as const,
  dataTable: (params?: unknown) =>
    [...featureQueryKeys.all(), "dataTable", params] as const,
  options: () => [...featureQueryKeys.all(), "options"] as const,
}
```

All keys are function calls — `all()`, `lists()`, etc. (not plain arrays). Always reference this factory — never write raw string arrays.

### Mutation Invalidation

Mutations invalidate `featureQueryKeys.all()` — one call that covers lists, details, dataTable, and options.

```ts
queryClient.invalidateQueries({ queryKey: featureQueryKeys.all() })
```

Never use multiple `invalidateQueries` calls in one mutation.

### DataTable Hook

Every feature's DataTable hook follows this exact signature — 6 positional params that DataTable passes internally:

```ts
export function use{Feature}DataTable(
  page: number,
  pageSize: number,
  search: string,
  dateRange: { from_date: string; to_date: string },
  sortBy: string,
  sortOrder: string
) {
  return useQuery({
    queryKey: featureQueryKeys.dataTable({ page, pageSize, search, dateRange, sortBy, sortOrder }),
    queryFn: async () => {
      const result = await get{Feature}s({ page, pageSize, search, sortBy, sortOrder })
      if (!result.success) throw new Error(result.error)
      return {
        data: result.data.{features},
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

use{Feature}DataTable.isQueryHook = true
```

The `.isQueryHook = true` flag is required — DataTable uses it to call the function as a React hook internally.

### staleTime Reference

| Data | staleTime |
|------|-----------|
| List queries | `30 * 1000` (30 seconds) |
| Detail queries | `5 * 60 * 1000` (5 minutes) |
| Options queries | `15 * 60 * 1000` (15 minutes) |
| DataTable queries | `30_000` (30 seconds) |

---

## Zustand Store Pattern

### ID Types in Dialog Store

| Entity | ID type |
|--------|---------|
| User | `number \| null` (int auto increment) |
| All other entities | `string \| null` (Guid v7) |

IDs are always `type | null` — never `type | undefined`.

### useShallow — Required for Object Selectors

```ts
// Single primitive — no useShallow needed
export const useIsCreateOpen = () =>
  useFeatureDialogStore((state) => state.isCreateOpen)

// Object or action bundle — useShallow always required
export const useFeatureDialogActions = () =>
  useFeatureDialogStore(
    useShallow((state) => ({
      openCreate: state.openCreate,
      closeCreate: state.closeCreate,
      // ...
    }))
  )
```

Import `useShallow` from `"zustand/shallow"` — not `"zustand/react/shallow"`.

---

## Components Pattern

### Spinner — Always, Never Loading Text

```tsx
import { Spinner } from "@/components/ui/spinner"

<div className="flex items-center justify-center py-8">
  <Spinner />
</div>
```

Never `<div>Loading...</div>`. Never text next to a spinner.

### Form Pattern Summary

- Create form: `useForm<CreateFeatureDto>` with `zodResolver(createFeatureSchema)` — all fields required
- Update form: `useForm<UpdateFeatureDto>` **without** `zodResolver` — update fields are all optional; resolver causes false required errors
- Both forms use `mutateAsync` (not `mutate`) to `await` the result and call `form.setError()` for field-level errors
- Create form calls `form.reset()` before `onSuccess?.()` — always reset first, then close

### Delete Dialog

- Uses `AlertDialog` — never `Dialog` — for destructive confirmation
- Uses `mutation.mutate()` with `onSuccess` callback — not `mutateAsync` — no field errors on delete
- Delete button and cancel button both `disabled={mutation.isPending}`
- `AlertDialogAction` always has `className="bg-destructive text-destructive-foreground hover:bg-destructive/90"` — the default is not red

### Store Integration in Components

Always import store selector hooks through the store barrel (`../store` or `@/features/{feature}/store`):

```ts
import { useIsCreateOpen, useFeatureDialogActions } from "@/features/{feature}/store"
```

Never import from individual store files. Never import the raw store instance.

### DataTable Integration

```tsx
<DataTable
  getColumns={() => featureColumns as any}
  fetchDataFn={use{Feature}DataTable}
  idField="id"
  exportConfig={exportConfig}
  pageSizeOptions={[10, 20, 30, 40, 50]}
  config={{
    enableSearch: true,
    enableDateFilter: false,
    enableExport: true,
  }}
/>
```

`fetchDataFn` must always be the `use{Feature}DataTable` hook (with `.isQueryHook = true`) — never a plain async function.

`exportConfig` always in `useMemo` — avoids recreating the object on every render.

DataTable is always `dynamic` imported with `{ ssr: false }` — it uses browser APIs.

---

## Import Rules

**Actions — no barrel, import directly:**
```ts
import { createFeatureAction } from '@/features/{feature}/actions/{feature}.actions'
```

**Schemas — no barrel, import directly:**
```ts
import { createFeatureSchema, type CreateFeatureDto } from '@/features/{feature}/schemas/{feature}.schema'
```

**Hooks — no barrel, import directly:**
```ts
import { useFeature, useCreateFeature } from '@/features/{feature}/hooks/{feature}.hooks'
```

**Store — always through barrel:**
```ts
import { useIsCreateOpen, useFeatureDialogActions } from '@/features/{feature}/store'
```

**Components — through barrel:**
```ts
import { FeatureListPage } from '@/features/{feature}/components'
```

**API client and types — from lib:**
```ts
import { apiClient } from '@/lib/api/client'
import type { ActionResult } from '@/lib/types/action-result'
import type { ApiResponse } from '@/lib/types/api-response'
```

**Auth — from lib:**
```ts
import { auth } from '@/lib/auth'
```

**Cross-feature options hook — inside a form component only:**
```ts
// Inside features/product/components/forms/create-product-form.tsx
import { useCategoryOptions } from '@/features/category/hooks/category.hooks'
```

---

## TypeScript Rules

- Never write types manually when they can be inferred from Zod
- Never use `any` — use `unknown` and narrow, or define proper types
- Never use `as` casting — if you need it, something is wrong with the types
- `ActionResult<T>` for all action return types — imported from `@/lib/types/action-result`
- All component props must have explicit TypeScript interfaces

---

## Cross-Feature Rules

```
✅  features/products/actions     →  features/categories/actions         (pass-through only)
✅  features/products/components  →  features/categories/hooks            (useOptions for dropdowns only)
❌  features/products/actions     →  features/categories/hooks
❌  features/products/hooks       →  features/categories/actions
❌  Any feature                   →  another feature's schemas (import types, never schemas)
```

---

## Naming Conventions

**Files:**
```
{feature}.actions.ts
{feature}.schema.ts
{feature}.hooks.ts
{feature}-dialog.store.ts
{feature}-filter.store.ts
create-{feature}-dialog.tsx
update-{feature}-dialog.tsx
delete-{feature}-dialog.tsx
{feature}-details-dialog.tsx
create-{feature}-form.tsx
update-{feature}-form.tsx
{feature}-table.tsx
{feature}-columns.tsx
{feature}-list-page.tsx
```

**Query key factories:**
```ts
featureQueryKeys.all()
featureQueryKeys.lists()
featureQueryKeys.list(filters)
featureQueryKeys.details()
featureQueryKeys.detail(id)
featureQueryKeys.dataTable(params)
featureQueryKeys.options()
```

**Hooks:**
```
use{Feature}           ← single item
use{Feature}s          ← list (if needed outside DataTable)
use{Feature}DataTable  ← DataTable hook (with .isQueryHook = true)
use{Feature}Options    ← dropdown options
useCreate{Feature}     ← create mutation
useUpdate{Feature}     ← update mutation
useDelete{Feature}     ← delete mutation
```

**Zod schemas and inferred types:**
```
createFeatureSchema   →  CreateFeatureDto
updateFeatureSchema   →  UpdateFeatureDto
featureFilterSchema   →  FeatureFilterDto
```

---

## ID Type Reference

| Entity | Schema ID | Store ID | Entity type |
|--------|-----------|----------|-------------|
| User | `z.int().positive()` | `number \| null` | `number` |
| All others | `z.uuid()` | `string \| null` | `string` |

---

## Zod 4 Rules

```ts
// ✅ Always
import * as z from "zod"
z.email("message")
z.uuid("message")
z.int()
z.iso.datetime()
z.string().min(1, "message")    // plain string error, not { message: "..." }
z.enum(["A", "B"], { error: "message" })

// ❌ Never
import { z } from "zod"         // Zod 3 import
z.string().email()              // Zod 3
z.string().uuid()               // Zod 3
z.number().int()                // Zod 3
z.string().datetime()           // Zod 3
z.string().min(1, { message: "..." })  // Zod 3 error format
```

---

## Audit Fields

Audit fields are managed server-side by the .NET Core API — the frontend never sends or manages timestamps.

| Field | Who manages it |
|-------|----------------|
| `createdAt` | API sets automatically on create |
| `updatedAt` | API sets automatically on update |
| `deletedAt` | API sets automatically on soft delete |
| `companyId` / `tenantId` | Resolved by API from JWT — never sent by frontend |

**Never include audit timestamps or `companyId`/`tenantId` in any Zod schema.**

---

## Soft Delete

The .NET Core API handles soft delete internally. The frontend always calls `DELETE /api/v1/{features}/{id}`. The API decides whether to hard-delete or soft-delete.

---

## What Cascade Must Never Do

- Never add `services/` or `repositories/` folders — this architecture does not have those layers
- Never use any UI library other than shadcn
- Never use any validation library other than Zod 4
- Never use `import { z } from "zod"` — always `import * as z from "zod"` (Zod 4 namespace import)
- Never write raw `fetch` calls — always use `apiClient` from `@/lib/api/client`
- Never use axios interceptors to inject auth — always `getAuthHeaders()` per-action
- Never import `Spinner` from `react-spinners` — always use the local shadcn `<Spinner />` at `@/components/ui/spinner`
- Never use `@radix-ui/react-icons` or `react-icons` for feature icons — always use `lucide-react`
- Never import `exceljs` or `xlsx` directly in feature code — DataTable export is handled by the shared DataTable component
- Never use native Date methods for formatting (`toLocaleString`, `toLocaleDateString`, etc.) — always use `date-fns` or `date-fns-tz`
- Never call the API directly from hooks or components — always through actions
- Never call actions directly from components — always through hooks
- Never throw from an action — always return `ActionResult<T>`
- Never store server data in Zustand — API response data belongs in TanStack Query
- Never store UI state in TanStack Query — modal state belongs in Zustand
- Never use `"use server"` inside a function body — it must be the very first line of the actions file
- Never use the TanStack Query v4 positional API — v5 only accepts a single object
- Never add `onSuccess` or `onError` to `useQuery` — removed in v5; only exist on `useMutation`
- Never use `keepPreviousData: true` (v4) — use `placeholderData: keepPreviousData` (v5)
- Never use `isLoading` for mutations — use `isPending` (renamed in v5)
- Never write raw query key strings inline — always reference the `featureQueryKeys` factory
- Never use multiple `invalidateQueries` calls in one mutation — always `featureQueryKeys.all()` once
- Never pass a plain async function as `fetchDataFn` — always a hook with `.isQueryHook = true`
- Never use `create<T>(...)` with single parentheses — always `create<T>()(...)` (Zustand v5)
- Never import `useShallow` from `"zustand/react/shallow"` — use `"zustand/shallow"` (v5)
- Never select objects or arrays without `useShallow` — causes `Maximum update depth exceeded`
- Never export raw Zustand store instances — only export named selector hooks
- Never import store hooks from individual store files — always through the barrel `index.ts`
- Never combine both Zustand stores into one file — always two separate files
- Never create a shared `{feature}-form-fields.tsx` — each form owns its fields inline
- Never use `zodResolver` on the update form — update fields are all optional; causes false required errors
- Never render a foreign key field as a plain `<Input>` — always a `<Select>`
- Never pass dropdown options as props to a form — always call `use{RelatedFeature}Options` inside the form
- Never import another feature's hooks except `use{Feature}Options` inside a form component
- Never add an `x-company-id` header — tenancy is resolved from JWT
- Never use `PATCH` — always `PUT` for updates
- Never use `limit` or `page_size` — always `page` and `pageSize` (camelCase)
- Never use loading text — always `<Spinner />`
- Never use `mutation.mutate()` in create or update forms — use `mutateAsync` to await field errors
- Never use `mutation.mutateAsync()` in delete dialog — use `mutate` with `onSuccess`
- Never close dialogs on a failed mutation — only close after confirming `result.success`
- Never forget `form.reset()` before `onSuccess?.()` on create success
- Never use `Dialog` for delete confirmation — always `AlertDialog`
- Never omit `.isQueryHook = true` on the DataTable hook — DataTable won't call it as a React hook
- Never skip `useMemo` on `exportConfig` in the table component
- Never format dates with native Date methods — always use date-fns or date-fns-tz
- Never leave `staleTime` unset on a `useQuery`
- Never use `PATCH` — the SFA API uses `PUT` for all updates
- Never create a barrel file for actions, schemas, or hooks
- Never forget to create `app/(protected)/{feature}/page.tsx` after building a feature