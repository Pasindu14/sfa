---
name: nextjs-feature-schema
description: Creating a Zod validation schema file for a new Next.js feature module in this codebase. Use this when adding a new feature schema, creating Zod schemas for a feature like category, product, order, user, or any domain entity. Handles createSchema, updateSchema, filterSchema, and all inferred TypeScript types in a single schema file following the project's Zod-first type pattern. Never write TypeScript types manually — always infer from Zod.
---

# Schema Skill

## Location

```
features/{feature}/schemas/{feature}.schema.ts
```

No barrel file. No index.ts. Import directly from this file everywhere.

---

## Rules Before Writing Anything

1. Read `AGENTS.md` at the project root first
2. Check the .NET Core API models to understand field names, field types, nullable fields, and default values
3. Never write TypeScript types manually — always use `z.infer<typeof schema>`
4. Never import types from anywhere else — this file is the single source of truth for all feature types
5. Never include audit fields in any schema — `createdAt`, `updatedAt`, `deletedAt`, `createdBy`, `updatedBy` are always injected server-side and must never appear in any Zod schema

---

## File Structure — Exact Order

Follow this exact order inside the file. Never reorder sections.

### 1. Imports
```ts
import { z } from 'zod'
```
Only Zod. No other imports unless referencing another feature's schema type for a foreign key field.

---

### 2. Field constants (optional but recommended for reused rules)
Extract repeated validation rules into named constants at the top so they are reused across schemas without duplication.

```ts
const nameField = z.string().min(1, 'Name is required').max(255, 'Name too long')
const slugField = z.string().min(1, 'Slug is required').max(255).regex(/^[a-z0-9-]+$/, 'Slug must be lowercase letters, numbers, and hyphens only')
const descriptionField = z.string().max(1000, 'Description too long').optional()
```

---

### 3. Create schema
- Required fields only — no id, no createdAt, no updatedAt, no deletedAt
- These are always injected by the service or repository — never passed from the client
- Every field must have a clear validation message
- **CRITICAL**: Never use `.default()` on boolean fields like `isActive` — use `.optional()` instead to prevent react-hook-form type mismatches. The default value will be provided in the action layer.

```ts
export const createCategorySchema = z.object({
  name: nameField,
  slug: slugField,
  description: descriptionField,
  isActive: z.boolean().optional(),
})

export type CreateCategoryDto = z.infer<typeof createCategorySchema>
```

---

### 4. Update schema
- Always wrap create schema fields with `.partial()` so all fields are optional on update
- Always add `id` as a required field on the update schema
- Never duplicate field definitions — extend or partial the create schema

```ts
export const updateCategorySchema = createCategorySchema
  .partial()
  .extend({
    id: z.number().int().positive('Invalid ID'),
  })

export type UpdateCategoryDto = z.infer<typeof updateCategorySchema>
```

---

### 5. Filter schema
- All fields optional — filters are never required
- Used by DataTable for search, pagination, and filtering
- Must include: search, isActive, pagination fields

```ts
export const categoryFilterSchema = z.object({
  search: z.string().optional(),
  isActive: z.boolean().optional(),
  page: z.number().int().positive().default(1),
  pageSize: z.number().int().positive().max(100).default(10),
  sortBy: z.enum(['name', 'createdAt', 'updatedAt']).optional(),
  sortOrder: z.enum(['asc', 'desc']).default('desc'),
})

export type CategoryFilterDto = z.infer<typeof categoryFilterSchema>
```

---

### 6. Response type (inferred from Zod)
- The full entity type is defined here in the schema file
- This includes all fields returned by the API including audit fields

```ts
export type Category = {
  id: number
  name: string
  slug: string
  description?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  deletedAt?: string
  createdBy: number
  updatedBy?: number
}
```

---

### 7. Select options type (if feature is used as a foreign key in other features)
- Lightweight type for dropdown selects
- Only id and name — nothing else

```ts
export type CategoryOption = {
  id: number
  name: string
}
```

---

## Validation Rules to Always Follow

**Strings:**
- Always set `.min(1)` with message for required string fields — never just `z.string()`
- Always set `.max()` with a reasonable limit — check the DB column length
- Trim whitespace with `.trim()` on name and slug fields

**Numbers:**
- Use `.int()` for integer fields
- Use `.positive()` for IDs
- For price fields use `z.string()` not `z.number()` — .NET Core API returns decimals as strings

**Booleans:**
- **CRITICAL**: Use `.optional()` instead of `.default()` for boolean fields in create schemas to prevent react-hook-form type mismatches
- The default value will be provided in the action layer when calling the service
- Example: `isActive: z.boolean().optional()` not `isActive: z.boolean().default(true)`

**Enums:**
- Use `z.enum([...])` — never `z.string()` for fields with a fixed set of values

**Optional fields:**
- Use `.optional()` for nullable DB columns
- Use `.nullable()` only when the value can explicitly be set to null by the client
- These are different — think carefully about which applies

**Dates:**
- Never accept dates from the client as input — dates are always set server-side
- If a date filter is needed in filterSchema use `z.string().datetime()` and convert server-side

---

## Common Mistakes — Never Do These

- Never add `id`, `createdAt`, `updatedAt`, `deletedAt` to the create schema
- Never write `type CreateCategoryDto = { name: string; ... }` manually — always infer from Zod
- Never use `z.any()`
- Never duplicate field definitions between create and update — always `.partial()` the create schema
- Never put business logic in schemas — schemas only validate shape and format
- Always define the full entity type in the schema file to match the API response

---

## Example — Complete Schema File

This is what a finished schema file looks like. Use this as the reference.

```ts
import { z } from 'zod'

// Field constants
const nameField = z.string().trim().min(1, 'Name is required').max(255, 'Name must be 255 characters or less')
const slugField = z.string().trim().min(1, 'Slug is required').max(255).regex(/^[a-z0-9-]+$/, 'Slug must contain only lowercase letters, numbers, and hyphens')
const descriptionField = z.string().trim().max(1000, 'Description must be 1000 characters or less').optional()

// Create
export const createCategorySchema = z.object({
  name: nameField,
  slug: slugField,
  description: descriptionField,
  isActive: z.boolean().optional(),
})
export type CreateCategoryDto = z.infer<typeof createCategorySchema>

// Update
export const updateCategorySchema = createCategorySchema.partial().extend({
  id: z.number().int().positive('Invalid category ID'),
})
export type UpdateCategoryDto = z.infer<typeof updateCategorySchema>

// Filter
export const categoryFilterSchema = z.object({
  search: z.string().optional(),
  isActive: z.boolean().optional(),
  page: z.number().int().positive().default(1),
  pageSize: z.number().int().positive().max(100).default(10),
  sortBy: z.enum(['name', 'createdAt']).optional(),
  sortOrder: z.enum(['asc', 'desc']).default('desc'),
})
export type CategoryFilterDto = z.infer<typeof categoryFilterSchema>

// Entity type - matches API response
export type Category = {
  id: number
  name: string
  slug: string
  description?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  deletedAt?: string
  createdBy: number
  updatedBy?: number
}

// Select option type for dropdowns
export type CategoryOption = {
  id: number
  name: string
}
```