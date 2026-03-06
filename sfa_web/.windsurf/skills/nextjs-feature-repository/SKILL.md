---
name: nextjs-feature-repository
description: Creating a repository file for a new Next.js feature module in this codebase. Use this when adding a new feature repository, creating Axios API call methods for a feature like category, product, order, user, or any domain entity. Handles all raw API operations including findById, findAllPaginated, create, update, softDelete, count, exists using the executeQuery wrapper. Always class-based with static methods, always uses executeQuery wrapper. Auth is Bearer JWT only via withToken helper. Updates use PUT. Pagination uses camelCase params (page, pageSize). Never contains business logic.
---

# Repository Skill

## Location

```
features/{feature}/repositories/{feature}.repository.ts
features/{feature}/repositories/index.ts
```

Has barrel file. Always import through `index.ts` from outside this folder.

---

## Rules Before Writing Anything

1. Read `AGENTS.md` at the project root first
2. Read the schema skill output — you need the filter type (`FeatureFilters`) exported from the repository file
3. Check `@/lib/api/client.ts` for `apiClient`, `withToken`, `ApiResponse`, `ApiPaginatedResponse`, `ApiError`
4. Check `@/lib/queries/wrapper.ts` for `executeQuery` signature
5. Check `@/lib/queries/pagination.ts` for pagination types
6. Check `@/lib/auth/helpers.ts` for `getAuthToken()` export name
7. Never add business logic — that belongs in the service
8. This layer is pure HTTP — no database imports, no transactions, only API calls to .NET Core backend

---

## SFA API Contract — Read This First

These are the patterns observed in the SFA API swagger. Every repository must follow them exactly.

### Auth
- Bearer JWT only — `Authorization: Bearer <token>`
- Token is retrieved server-side via `getAuthToken()` at the top of every method

### Pagination (GET list endpoints)
```
GET /api/v1/{features}?page=1&pageSize=10
```
- Params are **camelCase**: `page` and `pageSize` — not `limit`, not `page_size`
- Response shape:
```ts
{
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
```

### Single record
```
GET /api/v1/{features}/{id}
```

### Create
```
POST /api/v1/{features}
Body: { ...fields }
```

### Update
```
PUT /api/v1/{features}/{id}      ← always PUT, never PATCH
Body: { ...fields }
```

### Delete
```
DELETE /api/v1/{features}/{id}
```

---

## Imports — Always This Exact Set

```ts
import { executeQuery } from '@/lib/queries/wrapper'
import { NotFoundError } from '@/lib/errors'
import {
  type OffsetPagination,
  type OffsetPaginatedResult,
  DEFAULT_PAGE_SIZE,
} from '@/lib/queries/pagination'
import {
  apiClient,
  withToken,
  type ApiResponse,
  type ApiPaginatedResponse,
  type ApiError,
} from '@/lib/api/client'
import { getAuthToken } from '@/lib/auth/helpers'
import type { Feature } from '@/features/{feature}/schemas/{feature}.schema'
```

Rules:
- No database imports — this layer only makes HTTP calls to the .NET Core API
- No ORM imports — ever
- `withToken` — the only auth helper needed (no `withAuth`, no `withCompany`)
- Only import types you actually use

---

## File Structure — Exact Order

### 1. Filters type

```ts
export type CategoryFilters = {
  search?: string
  isActive?: boolean
}
```

Add only filters that map to actual API query params.

---

### 2. Class declaration

```ts
export class CategoryRepository {
  private static readonly context = 'CategoryRepository'
  private static readonly basePath = '/api/v1/categories'

  // sections follow...
}
```

Always `private static readonly basePath` — never hardcode the path inside methods.

---

### 3. Section order inside the class

```
// READ - Single Record
// READ - Multiple Records (Offset Pagination)
// READ - Batch
// WRITE - Create
// WRITE - Update
// WRITE - Delete
// AGGREGATES
```

---

## READ Methods

### `findById`
```ts
static async findById(
  id: number
): Promise<Category | null> {
  return executeQuery(
    { context: this.context, method: 'findById', logParams: { id } },
    async () => {
      try {
        const token = await getAuthToken()
        const response = await apiClient.get<ApiResponse<Category>>(
          `${this.basePath}/${id}`,
          withToken(token)
        )
        return response.data.data ?? null
      } catch (error: any) {
        if ((error as ApiError).status === 404) return null
        throw error
      }
    }
  )
}
```

Rules:
- Always returns `T | null` — never throws on 404 at repository level
- Cast error to `ApiError` when checking `.status` — it is always `ApiError` after the interceptor
- `response.data.data` — `response.data` is the Axios body (the `ApiResponse<T>` envelope), `.data` inside that is the actual record
- This is the **only** method that has a manual try/catch

---

### `findAllPaginated` — Offset Pagination
```ts
static async findAllPaginated(
  filters?: CategoryFilters,
  pagination: OffsetPagination = { page: 1, pageSize: DEFAULT_PAGE_SIZE }
): Promise<OffsetPaginatedResult<Category>> {
  return executeQuery(
    { context: this.context, method: 'findAllPaginated', logParams: { filters, ...pagination } },
    async () => {
      const token = await getAuthToken()

      // Build query params — use camelCase to match SFA API
      const params: Record<string, unknown> = {
        page: pagination.page,
        pageSize: pagination.pageSize,
      }
      if (filters?.search) params.search = filters.search
      if (filters?.isActive !== undefined) params.isActive = filters.isActive

      const response = await apiClient.get<ApiPaginatedResponse<Category>>(
        this.basePath,
        withToken(token, { params })
      )

      const { items, page, pageSize, totalCount, totalPages } = response.data

      return {
        items,
        total: totalCount,
        page,
        pageSize,
        totalPages,
        hasMore: page < totalPages,
      }
    }
  )
}
```

Rules:
- Pagination params are **camelCase**: `page` and `pageSize` — never `limit` or `page_size`
- Pass params via `withToken(token, { params })` — never concatenate query strings
- Map `ApiPaginatedResponse` fields to internal `OffsetPaginatedResult` shape:
  - `totalCount` → `total`
  - all other fields map 1:1
- `hasMore: page < totalPages` — computed locally

---

### `findByIds` — Batch Read
```ts
static async findByIds(
  ids: number[]
): Promise<Category[]> {
  return executeQuery(
    { context: this.context, method: 'findByIds', logParams: { count: ids.length } },
    async () => {
      if (ids.length === 0) return []

      const token = await getAuthToken()
      const response = await apiClient.get<ApiPaginatedResponse<Category>>(
        this.basePath,
        withToken(token, { params: { ids: ids.join(',') } })
      )
      return response.data.items
    }
  )
}
```

Rules:
- Always guard `if (ids.length === 0) return []`
- Pass ids as comma-separated query param — verify the exact param name with the API

---

### `getOptions` — Dropdown Select

The SFA API has no dedicated `/options` endpoint. Fall back to a paginated call with a large pageSize and map to `{ id, name }`.

```ts
static async getOptions(): Promise<{ id: number; name: string }[]> {
  return executeQuery(
    { context: this.context, method: 'getOptions' },
    async () => {
      const token = await getAuthToken()
      const response = await apiClient.get<ApiPaginatedResponse<Category>>(
        this.basePath,
        withToken(token, { params: { page: 1, pageSize: 500, isActive: true } })
      )
      return response.data.items.map((item) => ({ id: item.id, name: item.name }))
    }
  )
}
```

Rules:
- `pageSize: 500` is a safe upper bound for dropdown options — adjust if the feature has more records
- Always filter `isActive: true` for options — never show inactive items in dropdowns
- Map to `{ id, name }` — never return full records for dropdowns

---

## WRITE Methods

### `create`
```ts
static async create(
  data: Omit<Category, 'id' | 'createdAt' | 'updatedAt' | 'deletedAt'>
): Promise<Category> {
  return executeQuery(
    { context: this.context, method: 'create' },
    async () => {
      const token = await getAuthToken()
      const response = await apiClient.post<ApiResponse<Category>>(
        this.basePath,
        data,                    // ← body is 2nd arg
        withToken(token)         // ← config is 3rd arg
      )
      if (!response.data.data) throw new Error('Create failed')
      return response.data.data
    }
  )
}
```

Rules:
- `apiClient.post(url, body, config)` — body is **always 2nd**, config is **always 3rd**
- `withToken(token)` is the config — never pass it as 2nd arg
- Throw `new Error('Create failed')` when response has no data

---

### `update`
```ts
static async update(
  id: number,
  data: Partial<Omit<Category, 'id' | 'createdAt' | 'updatedAt' | 'deletedAt'>>
): Promise<Category> {
  return executeQuery(
    { context: this.context, method: 'update', logParams: { id } },
    async () => {
      const token = await getAuthToken()
      const response = await apiClient.put<ApiResponse<Category>>(
        `${this.basePath}/${id}`,
        data,                    // ← body is 2nd arg
        withToken(token)         // ← config is 3rd arg
      )
      if (!response.data.data) throw new NotFoundError(`Category ${id} not found`)
      return response.data.data
    }
  )
}
```

Rules:
- **Always `PUT`** — the SFA API uses PUT for updates, never PATCH
- `apiClient.put(url, body, config)` — same argument order as post
- Throw `NotFoundError` when response has no data

---

## DELETE Methods

### `softDelete`
```ts
static async softDelete(
  id: number
): Promise<void> {
  return executeQuery(
    { context: this.context, method: 'softDelete', logParams: { id } },
    async () => {
      const token = await getAuthToken()
      await apiClient.delete(
        `${this.basePath}/${id}`,
        withToken(token)
      )
    }
  )
}
```

Rules:
- `apiClient.delete(url, config)` — only 2 args, no body
- Return type is always `Promise<void>`
- The response interceptor converts a 404 into `ApiError` which `executeQuery` maps to `NotFoundError` automatically — no manual catch needed

---

## AGGREGATE Methods

### `count`
```ts
static async count(
  filters?: CategoryFilters
): Promise<number> {
  return executeQuery(
    { context: this.context, method: 'count', logParams: { filters } },
    async () => {
      // No dedicated count endpoint — use paginated with pageSize=1 to read totalCount
      const token = await getAuthToken()
      const params: Record<string, unknown> = { page: 1, pageSize: 1 }
      if (filters?.search) params.search = filters.search
      if (filters?.isActive !== undefined) params.isActive = filters.isActive

      const response = await apiClient.get<ApiPaginatedResponse<Category>>(
        this.basePath,
        withToken(token, { params })
      )
      return response.data.totalCount
    }
  )
}
```

### `exists`
```ts
static async exists(
  id: number
): Promise<boolean> {
  return executeQuery(
    { context: this.context, method: 'exists', logParams: { id } },
    async () => {
      const result = await this.findById(id)
      return result !== null
    }
  )
}
```

---

## Barrel File

```ts
// features/{feature}/repositories/index.ts
export { CategoryRepository } from './{feature}.repository'
export type { CategoryFilters } from './{feature}.repository'
```

---

## Response Unwrapping Reference

```ts
// Single record endpoints (GET /{id}, POST, PUT):
// response.data           → ApiResponse<T>   (the Axios body = the envelope)
// response.data.data      → T                (the actual record)
// response.data.success   → boolean
// response.data.message   → string | undefined

// List endpoints (GET /):
// response.data           → ApiPaginatedResponse<T>   (the Axios body — NO outer envelope)
// response.data.items     → T[]
// response.data.page      → number
// response.data.pageSize  → number
// response.data.totalCount→ number
// response.data.totalPages→ number
```

---

## Axios Argument Order — Critical

```ts
// GET / DELETE — 2 args: url, config
apiClient.get(url, withToken(token))
apiClient.get(url, withToken(token, { params }))
apiClient.delete(url, withToken(token))

// POST / PUT — 3 args: url, body, config
apiClient.post(url, data, withToken(token))
apiClient.put(url, data, withToken(token))

// ❌ WRONG — never pass withToken as 2nd arg on write methods
apiClient.post(url, withToken(token))     // sends token as body, no body sent
apiClient.put(url, withToken(token))      // same mistake
```

---

## Common Mistakes — Never Do These

- Never import database or ORM libraries — this layer is pure HTTP to .NET Core API
- Never hardcode the base path inside methods — always use `this.basePath`
- Never use `PATCH` — the SFA API uses `PUT` for all updates
- Never use `limit` or `page_size` as query params — always `page` and `pageSize` (camelCase)
- Never pass `withToken` as the 2nd arg on post/put — body is 2nd, config is 3rd
- Never call `getAuthToken()` once and reuse across methods — call fresh per method
- Never add `x-company-id` header — SFA API uses Bearer only, no tenant header
- Never throw inside `findById` on 404 — catch it and return `null`
- Never add manual try/catch on any method other than `findById`
- Never concatenate query strings manually — always use the `params` config option
- Never forget `executeQuery` on every method
- HTTP layer has no transaction support — transactions are handled by the .NET Core API