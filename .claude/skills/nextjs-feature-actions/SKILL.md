---
name: sfa-nextjs-feature-actions
description: Creating server action files for a feature module in the SFA Next.js web app. Use this when adding actions for any feature like users, customers, leads, orders, products, visits, or tasks. Actions are the only layer that communicates with the .NET API — they call it directly using an authenticated axios client, never through a service or repository layer. Handles auth token injection via Auth.js, typed ActionResult returns, API error interception, field-level error mapping, and revalidation after mutations.
---

# Actions Skill

## Location

```
features/{feature}/actions/{feature}.actions.ts
```

One file per feature. All actions for a feature live in a single file. No barrel file. Import directly from this path always.

---

## Rules Before Writing Anything

1. Every action file starts with `"use server"` — this is mandatory for Next.js server actions, and it must be the very first line before any imports
2. Auth token is fetched inside each action using `auth()` from Auth.js — never from a shared axios interceptor (interceptors cannot reliably access the Auth.js session from server action context)
3. Never throw errors — always return a typed `ActionResult<T>` discriminated union
4. Actions never import from other feature's actions, hooks, or components — only from their own schemas and the shared API client
5. All mutations call `revalidatePath` after a successful API response so the page data refreshes automatically
6. Actions handle both GET queries and mutations — one file per feature covers everything
7. There are no service or repository layers — actions call the .NET API directly

---

## Shared Files — Create Once, Use Everywhere

These files live in `lib/` and are shared across all features. Create them once and never duplicate.

### lib/api/client.ts — Authenticated Axios Client

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

This is a plain axios instance with no interceptors. Token injection happens per-action via `auth()` — not via a request interceptor — because axios interceptors cannot access the Auth.js server session from within server action execution context.

### lib/types/action-result.ts — Typed Return Envelope

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

**Why a discriminated union:**
- TypeScript enforces checking `success` before accessing `data`
- Hooks and components can handle field-level errors without extra parsing
- Never throws across the server/client boundary — serializable always

### lib/types/api-response.ts — .NET API Response Envelope

Matches the standard response shape returned by the .NET API for all endpoints.

```ts
export type ApiResponse<T> = {
  success: boolean
  data: T
  pagination?: {
    page: number
    pageSize: number
    total: number       // ← "total", not "totalCount"
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

## File Structure — Exact Order

Follow this exact order inside every actions file. Never reorder sections.

### 1. Directive and imports

```ts
"use server"

import { revalidatePath } from "next/cache"
import { auth } from "@/lib/auth"           // next-auth v5 beta — always from this path
import { apiClient } from "@/lib/api/client" // axios instance — never raw fetch
import { isAxiosError } from "axios"         // axios ^1.13.6
import type { ActionResult } from "@/lib/types/action-result"
import type { ApiResponse } from "@/lib/types/api-response"
import type { CreateUserDto, UpdateUserDto, UserFilterDto, User } from "../schemas/user.schema"
```

**Rules:**
- `"use server"` must be the very first line — before any imports
- Always import `auth` from `@/lib/auth` — this is next-auth v5 beta (`^5.0.0-beta.30`) configured at that path
- Always import `isAxiosError` from `axios` — used in the error handler to distinguish API errors from unexpected ones
- Never use raw `fetch` — always use `apiClient` from `@/lib/api/client`
- Import DTO types and entity types from the feature's own schema file only

---

### 2. Auth helper (internal — not exported)

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

**Rules:**
- Private — never exported
- Throws if no session — the calling action catches this and returns `{ success: false, error: "Unauthorized" }`
- Every action calls this at the top before any API call — never skip it

---

### 3. Error handler (internal — not exported)

Centralised error mapping from axios errors to `ActionResult`. Called at the bottom of every action's catch block.

```ts
function handleApiError(error: unknown): ActionResult<never> {
  if (isAxiosError(error)) {
    const status = error.response?.status
    const apiError = error.response?.data?.error

    // Auth.js session exists but API returned 401 — token mismatch or expiry
    if (status === 401) {
      return { success: false, error: "Session expired. Please sign in again." }
    }

    // 403 — user does not have permission for this resource
    if (status === 403) {
      return { success: false, error: "You do not have permission to perform this action." }
    }

    // 404 — resource not found
    if (status === 404) {
      return { success: false, error: apiError?.message ?? "Resource not found." }
    }

    // 409 — optimistic concurrency conflict
    if (status === 409) {
      return { success: false, error: apiError?.message ?? "This record was modified by another user. Please refresh and try again." }
    }

    // 422 — business rule violation (e.g. InsufficientStockException)
    if (status === 422) {
      return { success: false, error: apiError?.message ?? "This action cannot be completed due to a business rule violation." }
    }

    // 400 — validation errors from FluentValidation — map field errors
    if (status === 400 && apiError?.fields) {
      return {
        success: false,
        error: "Validation failed. Please correct the errors below.",
        fieldErrors: apiError.fields,
      }
    }

    // 503 — infrastructure unavailable
    if (status === 503) {
      return { success: false, error: "Service temporarily unavailable. Please try again shortly." }
    }

    // Any other API error with a message
    if (apiError?.message) {
      return { success: false, error: apiError.message }
    }
  }

  // Non-axios error — unexpected (network down, JSON parse failure, etc.)
  console.error("[action] unexpected error:", error)
  return { success: false, error: "An unexpected error occurred. Please try again." }
}
```

**Rules:**
- Always call `handleApiError` in the catch block — never write inline error handling per action
- 400 with `fields` maps to `fieldErrors` so hooks can surface them as inline form errors
- 409 conflicts are surfaced to the user — never silently swallowed
- Unexpected errors are logged server-side but only a generic message is returned to the client — never expose stack traces or internal details

---

### 4. Query actions (GET — for list and detail fetching)

Query actions are used by hooks to fetch data. They return `ActionResult<T>`.

```ts
export async function getUsers(
  filters?: Partial<UserFilterDto>
): Promise<ActionResult<{ users: User[]; total: number; page: number; pageSize: number; totalPages: number }>> {
  try {
    const headers = await getAuthHeaders()
    const params = {
      page: filters?.page ?? 1,
      pageSize: filters?.pageSize ?? 20,
      ...(filters?.search && { search: filters.search }),
      ...(filters?.role && { role: filters.role }),
      ...(filters?.isActive !== undefined && { isActive: filters.isActive }),
      ...(filters?.sortBy && { sortBy: filters.sortBy }),
      ...(filters?.sortOrder && { sortOrder: filters.sortOrder }),
    }
    const response = await apiClient.get<ApiResponse<User[]>>("/api/v1/users", {
      headers,
      params,
    })
    return {
      success: true,
      data: {
        users: response.data.data,
        total: response.data.pagination?.total ?? 0,
        page: response.data.pagination?.page ?? 1,
        pageSize: response.data.pagination?.pageSize ?? 20,
        totalPages: response.data.pagination?.totalPages ?? 1,
      },
    }
  } catch (error) {
    return handleApiError(error)
  }
}

export async function getUserById(id: number): Promise<ActionResult<User>> {
  try {
    const headers = await getAuthHeaders()
    const response = await apiClient.get<ApiResponse<User>>(`/api/v1/users/${id}`, { headers })
    return { success: true, data: response.data.data }
  } catch (error) {
    return handleApiError(error)
  }
}
```

**Rules:**
- Use `params` spread pattern — only include filter params when they have a value (avoids sending `?search=undefined`)
- Always access list items as `response.data.data` — the outer `.data` is the axios envelope, inner `.data` is the API array
- Always access pagination as `response.data.pagination` with null-coalescing defaults
- Pagination field is `total` — not `totalCount`

---

### 5. Mutation actions (POST / PUT / DELETE)

Always call `revalidatePath` after a successful mutation. Never call it inside the catch block.

```ts
export async function createUser(dto: CreateUserDto): Promise<ActionResult<User>> {
  try {
    const headers = await getAuthHeaders()
    const response = await apiClient.post<ApiResponse<User>>("/api/v1/users", dto, { headers })
    revalidatePath("/users")
    return { success: true, data: response.data.data }
  } catch (error) {
    return handleApiError(error)
  }
}

export async function updateUser(dto: UpdateUserDto): Promise<ActionResult<User>> {
  try {
    const headers = await getAuthHeaders()
    const response = await apiClient.put<ApiResponse<User>>(`/api/v1/users/${dto.id}`, dto, { headers })
    revalidatePath("/users")
    revalidatePath(`/users/${dto.id}`)
    return { success: true, data: response.data.data }
  } catch (error) {
    return handleApiError(error)
  }
}

export async function deleteUser(id: number): Promise<ActionResult<void>> {
  try {
    const headers = await getAuthHeaders()
    await apiClient.delete(`/api/v1/users/${id}`, { headers })
    revalidatePath("/users")
    return { success: true, data: undefined }
  } catch (error) {
    return handleApiError(error)
  }
}
```

**Rules:**
- POST and PUT — 3 args: url, body, config — never pass `{ headers }` as the body arg
- GET and DELETE — 2 args: url, config — config contains `{ headers }` and optional `{ params }`
- Always call `revalidatePath` for the list page after create and delete
- Always call `revalidatePath` for both the list and the detail page after update
- Never pass `rowVersion` from the DTO into the URL — pass it inside the request body for optimistic concurrency
- Never call `redirect()` from inside an action — redirect from the hook or component after checking `result.success`

---

## Complete Example — user.actions.ts

```ts
"use server"

import { revalidatePath } from "next/cache"
import { auth } from "@/lib/auth"
import { apiClient } from "@/lib/api/client"
import { isAxiosError } from "axios"
import type { ActionResult } from "@/lib/types/action-result"
import type { ApiResponse } from "@/lib/types/api-response"
import type {
  CreateUserDto,
  UpdateUserDto,
  UserFilterDto,
  User,
} from "../schemas/user.schema"

// ── Auth Helper ────────────────────────────────────────────────────────────

async function getAuthHeaders() {
  const session = await auth()
  if (!session?.user?.accessToken) {
    throw new Error("Unauthorized")
  }
  return {
    Authorization: `Bearer ${session.user.accessToken}`,
  }
}

// ── Error Handler ──────────────────────────────────────────────────────────

function handleApiError(error: unknown): ActionResult<never> {
  if (isAxiosError(error)) {
    const status = error.response?.status
    const apiError = error.response?.data?.error

    if (status === 401) return { success: false, error: "Session expired. Please sign in again." }
    if (status === 403) return { success: false, error: "You do not have permission to perform this action." }
    if (status === 404) return { success: false, error: apiError?.message ?? "Resource not found." }
    if (status === 409) return { success: false, error: apiError?.message ?? "This record was modified by another user. Please refresh and try again." }
    if (status === 422) return { success: false, error: apiError?.message ?? "This action cannot be completed." }
    if (status === 400 && apiError?.fields) {
      return { success: false, error: "Validation failed.", fieldErrors: apiError.fields }
    }
    if (status === 503) return { success: false, error: "Service temporarily unavailable. Please try again shortly." }
    if (apiError?.message) return { success: false, error: apiError.message }
  }

  console.error("[action] unexpected error:", error)
  return { success: false, error: "An unexpected error occurred. Please try again." }
}

// ── Query Actions ──────────────────────────────────────────────────────────

export async function getUsers(
  filters?: Partial<UserFilterDto>
): Promise<ActionResult<{ users: User[]; total: number; page: number; pageSize: number; totalPages: number }>> {
  try {
    const headers = await getAuthHeaders()
    const params = {
      page: filters?.page ?? 1,
      pageSize: filters?.pageSize ?? 20,
      ...(filters?.search && { search: filters.search }),
      ...(filters?.role && { role: filters.role }),
      ...(filters?.isActive !== undefined && { isActive: filters.isActive }),
      ...(filters?.sortBy && { sortBy: filters.sortBy }),
      ...(filters?.sortOrder && { sortOrder: filters.sortOrder }),
    }
    const response = await apiClient.get<ApiResponse<User[]>>("/api/v1/users", { headers, params })
    return {
      success: true,
      data: {
        users: response.data.data,
        total: response.data.pagination?.total ?? 0,
        page: response.data.pagination?.page ?? 1,
        pageSize: response.data.pagination?.pageSize ?? 20,
        totalPages: response.data.pagination?.totalPages ?? 1,
      },
    }
  } catch (error) {
    return handleApiError(error)
  }
}

export async function getUserById(id: number): Promise<ActionResult<User>> {
  try {
    const headers = await getAuthHeaders()
    const response = await apiClient.get<ApiResponse<User>>(`/api/v1/users/${id}`, { headers })
    return { success: true, data: response.data.data }
  } catch (error) {
    return handleApiError(error)
  }
}

// ── Mutation Actions ───────────────────────────────────────────────────────

export async function createUser(dto: CreateUserDto): Promise<ActionResult<User>> {
  try {
    const headers = await getAuthHeaders()
    const response = await apiClient.post<ApiResponse<User>>("/api/v1/users", dto, { headers })
    revalidatePath("/users")
    return { success: true, data: response.data.data }
  } catch (error) {
    return handleApiError(error)
  }
}

export async function updateUser(dto: UpdateUserDto): Promise<ActionResult<User>> {
  try {
    const headers = await getAuthHeaders()
    const response = await apiClient.put<ApiResponse<User>>(`/api/v1/users/${dto.id}`, dto, { headers })
    revalidatePath("/users")
    revalidatePath(`/users/${dto.id}`)
    return { success: true, data: response.data.data }
  } catch (error) {
    return handleApiError(error)
  }
}

export async function deleteUser(id: number): Promise<ActionResult<void>> {
  try {
    const headers = await getAuthHeaders()
    await apiClient.delete(`/api/v1/users/${id}`, { headers })
    revalidatePath("/users")
    return { success: true, data: undefined }
  } catch (error) {
    return handleApiError(error)
  }
}
```

---

## API Error Code Reference

The .NET API returns these error codes. The error handler maps them as follows:

| HTTP Status | .NET Error Code | Action Response |
|-------------|----------------|-----------------|
| 400 | `VALIDATION_FAILED` | `fieldErrors` populated from `error.fields` |
| 401 | `AUTH_*` | Generic session expired message |
| 403 | `FORBIDDEN_*` | Permission denied message |
| 404 | `*_NOT_FOUND` | `error.message` from API |
| 409 | `*_CONFLICT` | `error.message` from API |
| 422 | `*_BUSINESS_RULE` | `error.message` from API |
| 503 | `SERVICE_UNAVAILABLE` | Generic retry message |

---

## Common Mistakes — Never Do These

- Never use `"use server"` inside a function body — it must be the very first line of the file, before all imports
- Never add a service or repository layer — actions call the .NET API directly
- Never use axios interceptors to inject auth tokens — they cannot access the Auth.js session from server action context; use `getAuthHeaders()` per action instead
- Never throw errors from an action — always return `ActionResult`
- Never call `redirect()` from inside an action — redirect from the hook or component after `result.success`
- Never call `revalidatePath` inside a catch block — only after confirmed success
- Never expose raw axios error details, stack traces, or internal messages to the client
- Never access `.data.data` without typing the axios response as `ApiResponse<T>` — always provide the generic
- Never write inline error handling per action — always call the shared `handleApiError(error)` function
- Never create separate action files for query vs mutation — one file per feature only
- Never import from another feature's actions — only from your own schema and shared lib files
- Never skip auth check — every action must call `getAuthHeaders()` before any API call
- Never pass `{ headers }` as the second argument on POST/PUT — body is second, config (with headers) is third
- Never use `PATCH` — always `PUT` for updates
- Never use `limit` or `page_size` as query params — always `page` and `pageSize`
- Never create a barrel file for actions — import directly from the actions file path