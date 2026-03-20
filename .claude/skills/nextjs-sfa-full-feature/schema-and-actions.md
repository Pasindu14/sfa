# Schema & Actions

## Sections
- Schema File
- Actions File
- Checklist

---

## Schema File

**Location:** `features/{entity}/schema/{entity}.schema.ts`

```typescript
import { z } from 'zod'

// Enums — only when field is restricted to known values
export const statusEnum = z.enum(['Active', 'Inactive'])

// Create schema — all fields required to create the entity
export const create{Entity}Schema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
  // add more fields with validation error messages
})

// Update schema — usually identical; omit password-only or immutable fields
export const update{Entity}Schema = create{Entity}Schema

// Filter schema — for DataTable pagination and search
export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

// Derive types from Zod — never write TypeScript interfaces for validated inputs
export type Create{Entity}Input = z.infer<typeof create{Entity}Schema>
export type Update{Entity}Input = z.infer<typeof update{Entity}Schema>
export type {Entity}FilterInput = z.infer<typeof filterSchema>

// DTO — plain type (not Zod), mirrors API response exactly
export type {Entity}Dto = {
  id: number
  name: string
  // all other fields from the API response
  isActive: boolean
  createdAt: string  // ISO 8601
  updatedAt: string
}
```

**Rules:**
- Use Zod for all validated inputs — never write TypeScript interfaces for create/update
- `Dto` is a plain type (not Zod-derived), matching the API response 1:1
- Separate create / update / filter schemas; `update` usually equals `create{Entity}Schema`
- Always include human-readable validation error messages

---

## Actions File

**Location:** `features/{entity}/actions/{entity}.actions.ts`

```typescript
'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { Create{Entity}Input, Update{Entity}Input, {Entity}Dto } from '../schema/{entity}.schema'

type {Entity}sListResponse = {
  {entity}s: {Entity}Dto[]
  totalCount: number
  page: number
  pageSize: number
}

// LIST — server-side pagination + search
export const get{Entity}sAction = createAction(
  { name: 'get{Entity}sAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/{entity}s', {
      params: { page, pageSize, search: search || undefined },  // omit param when empty
    })
    return res.data.data as {Entity}sListResponse
  }
)

// GET BY ID — used by Edit dialog
export const get{Entity}ByIdAction = createAction(
  { name: 'get{Entity}ByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/{entity}s/${id}`)
    return res.data.data as {Entity}Dto
  }
)

// CREATE
export const create{Entity}Action = createAction(
  { name: 'create{Entity}Action', requireAuth: true, requiredRole: 'Admin' },
  async (data: Create{Entity}Input) => {
    const res = await client.post('/api/v1/{entity}s', data)
    revalidatePath('/{entity}s')
    return res.data.data as {Entity}Dto
  }
)

// UPDATE — always PUT (never PATCH)
export const update{Entity}Action = createAction(
  { name: 'update{Entity}Action', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: Update{Entity}Input) => {
    const res = await client.put(`/api/v1/{entity}s/${id}`, data)
    revalidatePath('/{entity}s')
    return res.data.data as {Entity}Dto
  }
)

// DELETE — server soft-deletes (sets IsActive = false); no body needed
export const delete{Entity}Action = createAction(
  { name: 'delete{Entity}Action', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/{entity}s/${id}`)
    revalidatePath('/{entity}s')
  }
)
```

**Rules:**
- `'use server'` at top — required for Next.js server actions
- `createAction` wrapper only — never write manual try/catch
- Import `client` as default (not `apiClient`)
- Always `PUT` for updates — never `PATCH`
- `revalidatePath` after every mutation (create, update, delete)
- `search: search || undefined` so Axios omits the query param when the string is empty
- No `companyId` or tenant header — tenancy resolves server-side from JWT

---

## Checklist

- [ ] `'use server'` at top of file
- [ ] `createAction` used for every exported function
- [ ] Import `client` not `apiClient`
- [ ] `PUT` for update (never `PATCH`)
- [ ] `revalidatePath` after create, update, delete
- [ ] `search: search || undefined` on list action
- [ ] Response typed as `{Entity}sListResponse` or `{Entity}Dto`
- [ ] No tenant or companyId in headers or params
