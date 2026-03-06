---
name: nextjs-feature-actions
description: Creating a server actions file for a new Next.js feature module in this codebase. Use this when adding a new feature actions file, writing Next.js server actions for a feature like category, product, order, user, or any domain entity. Handles all server actions including getFeature, getFeatures, createFeature, updateFeature, deleteFeature using the createAction wrapper. Always uses createAction wrapper, always calls getAuthUser for userId, always validates with Zod, accepts optional pre-fetched auth to avoid double session lookups, injects audit fields before calling service. Single file per feature, no barrel file.
---

# Actions Skill

## Location

```
features/{feature}/actions/{feature}.actions.ts
```

No barrel file. No index.ts. Import directly from this file path always.

---

## Rules Before Writing Anything

1. Read `AGENTS.md` at the project root first
2. Read the schema skill output — you need every DTO type and schema
3. Read the service skill output — you need to know exactly what service methods exist and what they accept
4. Actions are the entry point from the client — they are the only layer the client ever calls
5. Every action must: authenticate → validate → inject audit fields → call service
6. Never call repositories directly — always go through the service layer
7. Never add manual try/catch — `createAction` handles everything
8. The optional auth parameter exists to avoid double `getAuthUser()` calls when one action calls another feature's action

---

## Imports — Always This Exact Set

```ts
'use server'

import { createAction } from '@/lib/actions/wrapper'
import { getAuthUser } from '@/lib/auth/helpers'
import { FeatureService } from '@/features/{feature}/services'
import {
  createFeatureSchema,
  updateFeatureSchema,
  featureFilterSchema,
  type CreateFeatureDto,
  type UpdateFeatureDto,
  type FeatureFilterDto,
} from '@/features/{feature}/schemas/{feature}.schema'
import type { OffsetPaginatedResult } from '@/lib/queries/pagination'
import type { Feature } from '@/features/{feature}/schemas/{feature}.schema'
import type { ActionResponse } from '@/lib/types/actions'
```

Rules:
- `'use server'` directive always at the very top — first line of the file, before imports
- Import service through its barrel — never import repository or repository methods
- Import schemas and types directly from the schema file — no barrel for schemas
- Import only the schemas and types you actually use
- `ActionResponse<T>` from `@/lib/types/actions` — every action returns this

---

## Optional Auth Type

Define this type once at the top of the file, below imports. Used by every action signature.

```ts
type AuthContext = {
  userId: string
}
```

---

## Action Pattern — Exact Signature Every Time

Every action follows this exact pattern without exception:

```ts
export const actionNameAction = createAction(
  { name: 'actionNameAction', requireAuth: true },
  async (input: InputType, auth?: AuthContext): Promise<ReturnType> => {
    const { companyId, userId } = auth ?? await getAuthUser()
    // validate → inject audit fields → call service
  }
)
```

Rules:
- Name convention: `create{Feature}Action`, `update{Feature}Action`, `delete{Feature}Action`, `get{Feature}Action`, `get{Feature}sAction`
- `requireAuth: true` on every action in this file — no exceptions
- `auth?: AuthContext` always as the second parameter — optional, not required
- `const { companyId, userId } = auth ?? await getAuthUser()` always the first line inside the handler
- Always `await getAuthUser()` — never destructure session directly

---

## File Structure — Exact Order

```
'use server'

[imports]

type AuthContext = { userId: string }

// READ actions
// WRITE actions  
// DELETE actions
```

Use these exact comments as section dividers.

---

## READ Actions

### `getFeatureAction` — Single record
```ts
export const getCategoryAction = createAction(
  { name: 'getCategoryAction', requireAuth: true },
  async (id: number, auth?: AuthContext): Promise<Category> => {
    const { userId } = auth ?? await getAuthUser()
    return CategoryService.getById(id)
  }
)
```

Rules:
- Read actions don't need any auth fields since no audit field injection
- Return type is the entity directly — not wrapped in `ActionResponse` — `createAction` wraps it automatically
- No Zod validation needed for simple ID inputs — validate complex filter inputs

### `getFeaturessAction` — Paginated list
```ts
export const getCategoriesAction = createAction(
  { name: 'getCategoriesAction', requireAuth: true },
  async (
    input: { filters?: CategoryFilterDto; page?: number; pageSize?: number },
    auth?: AuthContext
  ): Promise<OffsetPaginatedResult<Category>> => {
    const { userId } = auth ?? await getAuthUser()

    const validatedFilters = featureFilterSchema.safeParse(input.filters ?? {})
    const filters = validatedFilters.success ? validatedFilters.data : undefined

    return CategoryService.getAll(filters, {
      page: input.page ?? 1,
      pageSize: input.pageSize ?? 10,
    })
  }
)
```

Rules:
- Use `safeParse` for filter validation — filters are optional so never hard-fail on invalid filters
- Pass pagination params separately from filters — `page` and `pageSize` at the top level
- This is the action that `useFeatureDataTableQuery` calls with DataTable params

### `getFeatureOptionsAction` — Dropdown options
```ts
export const getCategoryOptionsAction = createAction(
  { name: 'getCategoryOptionsAction', requireAuth: true },
  async (_input: void, auth?: AuthContext): Promise<{ id: number; name: string }[]> => {
    const { companyId } = auth ?? await getAuthUser()
    return CategoryService.getOptions(companyId)
  }
)
```

Rules:
- Use `_input: void` when the action takes no input
- Always present if the feature is used as a foreign key in other features

---

## WRITE Actions

### `createFeatureAction`
```ts
export const createCategoryAction = createAction(
  { name: 'createCategoryAction', requireAuth: true },
  async (input: CreateCategoryDto, auth?: AuthContext): Promise<Category> => {
    const { userId } = auth ?? await getAuthUser()

    // Validate
    const validated = createCategorySchema.parse(input)

    // Inject audit fields and default values, then call service
    return CategoryService.create({
      ...validated,
      isActive: validated.isActive ?? true,  // Provide default for optional boolean
      createdBy: parseInt(userId),  // Convert string userId to number for DB
    })
  }
)
```

Rules:
- Always use `.parse()` not `.safeParse()` for write operations — hard fail on invalid input
- `createAction` wrapper catches `ZodError` automatically and returns `ActionResponse` with validation error
- **CRITICAL**: Provide default values for optional boolean fields (e.g., `isActive: validated.isActive ?? true`)
  - Schema uses `.optional()` to prevent react-hook-form type mismatches
  - Action layer provides the default value when undefined
- Always inject `createdBy: parseInt(userId)` — convert string userId to number for database
- Never inject `companyId` into the DTO — it's no longer needed

### `updateFeatureAction`
```ts
export const updateCategoryAction = createAction(
  { name: 'updateCategoryAction', requireAuth: true },
  async (input: UpdateCategoryDto, auth?: AuthContext): Promise<Category> => {
    const { userId } = auth ?? await getAuthUser()

    // Validate
    const validated = updateCategorySchema.parse(input)

    // Inject audit fields and call service
    return CategoryService.update(validated.id, {
      ...validated,
      updatedBy: parseInt(userId),  // Convert string userId to number for DB
    })
  }
)
```

Rules:
- Use `validated.id` directly instead of destructuring to avoid type issues
- Always inject `updatedBy: parseInt(userId)` — convert string userId to number for database

---

## DELETE Actions

### `deleteFeatureAction`
```ts
export const deleteCategoryAction = createAction(
  { name: 'deleteCategoryAction', requireAuth: true },
  async (id: number, auth?: AuthContext): Promise<void> => {
    const { userId } = auth ?? await getAuthUser()
    await CategoryService.delete(id)
  }
)
```

Rules:
- Delete doesn't need any auth fields since no audit field injection on delete
- Return type is `Promise<void>` — `createAction` wraps it as `ActionResponse<void>`
- No Zod validation needed for simple ID inputs

---

## Cross-Feature Actions

When a feature action needs data from another feature, call that feature's action with auth passed down. Never call another feature's service or repository.

```ts
export const createProductAction = createAction(
  { name: 'createProductAction', requireAuth: true },
  async (input: CreateProductDto, auth?: AuthContext): Promise<Product> => {
    const { userId } = auth ?? await getAuthUser()

    // Validate input
    const validated = createProductSchema.parse(input)

    // Cross-feature: verify category exists by calling category action
    // Pass auth down to avoid double getAuthUser() call
    const categoryResult = await getCategoryAction(validated.categoryId, { userId })

    // categoryResult is ActionResponse<Category> — check success
    if (!categoryResult.success) {
      throw new NotFoundError('Category not found')
    }

    return ProductService.create({
      ...validated,
      createdBy: userId,
    })
  }
)
```

Rules:
- Import the other feature's action directly from its file path — no barrel for actions
- Always pass `{ userId }` as the second argument — never let the called action re-fetch session
- The called action returns `ActionResponse<T>` — always check `.success` before using `.data`
- Never import from another feature's service or repository

---

## revalidatePath

After write operations (create, update, delete) that affect a Next.js page, add `revalidatePath` to invalidate the cache:

```ts
import { revalidatePath } from 'next/cache'

export const createCategoryAction = createAction(
  { name: 'createCategoryAction', requireAuth: true },
  async (input: CreateCategoryDto, auth?: AuthContext): Promise<Category> => {
    const { companyId, userId } = auth ?? await getAuthUser()
    const validated = createCategorySchema.parse(input)

    const category = await CategoryService.create(companyId, {
      ...validated,
      createdBy: userId,
    })

    revalidatePath('/categories')
    return category
  }
)
```

Rules:
- Only add `revalidatePath` if the feature has a Next.js page route that needs cache invalidation
- Call it after the service call succeeds — never before
- Use the actual route path, not a dynamic segment

---

## Complete Example — category.actions.ts

```ts
'use server'

import { createAction } from '@/lib/actions/wrapper'
import { getAuthUser } from '@/lib/auth/helpers'
import { revalidatePath } from 'next/cache'
import { CategoryService } from '@/features/categories/services'
import {
  createCategorySchema,
  updateCategorySchema,
  categoryFilterSchema,
  type CreateCategoryDto,
  type UpdateCategoryDto,
  type CategoryFilterDto,
} from '@/features/categories/schemas/category.schema'
import type { OffsetPaginatedResult } from '@/lib/queries/pagination'
import type { Category } from '@/features/categories/schemas/category.schema'

type AuthContext = {
  userId: string
}

// ==========================================================================
// READ
// ==========================================================================

export const getCategoryAction = createAction(
  { name: 'getCategoryAction', requireAuth: true },
  async (id: number, auth?: AuthContext): Promise<Category> => {
    const { userId } = auth ?? await getAuthUser()
    return CategoryService.getById(id)
  }
)

export const getCategoriesAction = createAction(
  { name: 'getCategoriesAction', requireAuth: true },
  async (
    input: { filters?: CategoryFilterDto; page?: number; pageSize?: number },
    auth?: AuthContext
  ): Promise<OffsetPaginatedResult<Category>> => {
    const { userId } = auth ?? await getAuthUser()

    const validatedFilters = categoryFilterSchema.safeParse(input.filters ?? {})
    const filters = validatedFilters.success ? validatedFilters.data : undefined

    return CategoryService.getAll(filters, {
      page: input.page ?? 1,
      pageSize: input.pageSize ?? 10,
    })
  }
)

export const getCategoryOptionsAction = createAction(
  { name: 'getCategoryOptionsAction', requireAuth: true },
  async (_input: void, auth?: AuthContext): Promise<{ id: number; name: string }[]> => {
    const { userId } = auth ?? await getAuthUser()
    return CategoryService.getOptions()
  }
)

// ==========================================================================
// WRITE
// ==========================================================================

export const createCategoryAction = createAction(
  { name: 'createCategoryAction', requireAuth: true },
  async (input: CreateCategoryDto, auth?: AuthContext): Promise<Category> => {
    const { userId } = auth ?? await getAuthUser()
    const validated = createCategorySchema.parse(input)

    const category = await CategoryService.create({
      ...validated,
      createdBy: userId,
    })

    revalidatePath('/categories')
    return category
  }
)

export const updateCategoryAction = createAction(
  { name: 'updateCategoryAction', requireAuth: true },
  async (input: UpdateCategoryDto, auth?: AuthContext): Promise<Category> => {
    const { userId } = auth ?? await getAuthUser()
    const validated = updateCategorySchema.parse(input)
    const { id, ...data } = validated

    const category = await CategoryService.update(id, {
      ...data,
      updatedBy: userId,
    })

    revalidatePath('/categories')
    return category
  }
)

// ==========================================================================
// DELETE
// ==========================================================================

export const deleteCategoryAction = createAction(
  { name: 'deleteCategoryAction', requireAuth: true },
  async (id: number, auth?: AuthContext): Promise<void> => {
    const { userId } = auth ?? await getAuthUser()
    await CategoryService.delete(id)
    revalidatePath('/categories')
  }
)
```

---

## Common Mistakes — Never Do These

- Never forget `'use server'` as the very first line — before all imports
- Never call repositories directly from actions — always go through the service layer
- Never skip Zod validation on write operations — always `.parse()` before calling service
- Never inject `companyId` into the DTO — pass it as the first param to service
- Never inject audit fields in the service — always inject `createdBy` and `updatedBy` in the action
- Never call `getAuthUser()` twice in the same action — destructure once at the top
- Never call another feature's service or repository — only call other feature's actions
- Never forget to pass `auth` down when calling another feature's action
- Never add manual try/catch — `createAction` handles all error mapping and response wrapping
- Never use `.safeParse()` for write inputs — use `.parse()` so ZodError is thrown and caught by wrapper
- Never add `revalidatePath` before the service call — always after success
- Never omit `requireAuth: true` — every action in a feature file requires authentication