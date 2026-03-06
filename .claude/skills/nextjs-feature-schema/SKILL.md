---
name: sfa-nextjs-feature-schema
description: Creating a Zod validation schema file for a new Next.js feature module in the SFA web app. Use this when adding a new feature schema, creating Zod schemas for a feature like user, customer, lead, order, product, visit, or task. Handles createSchema, updateSchema, filterSchema, and all inferred TypeScript types in a single schema file following the Zod 4 type pattern. Never write TypeScript types manually — always infer from Zod. No repositories or services exist in this project — the .NET API handles all business logic and data access. Actions call the .NET API directly.
---

# Schema Skill

## Location

```
features/{feature}/schemas/{feature}.schema.ts
```

No barrel file. No index.ts. Import directly from this file path always.

---

## Rules Before Writing Anything

1. This project uses **Zod 4** — import as `import * as z from "zod"`, not `import { z } from "zod"`
2. Check the .NET API DTOs and response shapes for the feature — field names, field types, nullable fields
3. Never write TypeScript types manually — always use `z.infer<typeof schema>` (except the entity type and option type which mirror the raw API response)
4. Never import types from anywhere else — this file is the single source of truth for all feature types
5. Never include `id` in create schemas — server assigns this
6. Never include audit fields in any schema — `createdAt`, `updatedAt`, `createdBy`, `updatedBy` are always managed server-side by the .NET API
7. Never include `tenantId` or `companyId` in any schema — resolved by the .NET API from the JWT

---

## File Structure — Exact Order

Follow this exact order inside the file. Never reorder sections.

### 1. Imports

Zod 4 (`^4.3.6`) uses a namespace import — completely different from Zod 3's named import. Always use this exact import:

```ts
import * as z from "zod"
```

Never:
```ts
import { z } from "zod"   // ❌ Zod 3 — will cause type errors with Zod 4 APIs
```

---

### 2. Field constants (optional but recommended for reused rules)

Extract repeated validation rules into named constants at the top so they are reused across schemas without duplication.

```ts
const nameField = z
  .string()
  .trim()
  .min(1, "Name is required")
  .max(255, "Name must be 255 characters or less")

const descriptionField = z
  .string()
  .trim()
  .max(1000, "Description must be 1000 characters or less")
  .optional()
```

**Zod 4 error messages** — pass the message string directly as the second argument to validators like `.min()`, `.max()`. Do NOT use `{ message: "..." }` — that is Zod 3 syntax. Zod 4 accepts a plain string or an `{ error: "..." }` params object.

```ts
// ✅ Zod 4 — plain string
z.string().min(1, "Name is required")

// ✅ Zod 4 — params object with error key
z.string().min(1, { error: "Name is required" })

// ❌ Zod 3 — do not use
z.string().min(1, { message: "Name is required" })
```

---

### 3. Create schema

```ts
export const createUserSchema = z.object({
  name: nameField,
  email: z.email("Must be a valid email address"),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters")
    .max(100, "Password must be 100 characters or less"),
  role: z.enum(["Admin", "Manager", "SalesRep"], {
    error: "Role must be Admin, Manager or SalesRep",
  }),
  isActive: z.boolean().optional(),
})

export type CreateUserDto = z.infer<typeof createUserSchema>
```

**Rules:**
- No `id` — server assigns this
- No `createdAt`, `updatedAt`, `createdBy`, `updatedBy` — server manages these
- No `tenantId` / `companyId` — resolved from JWT by .NET API
- Use `.optional()` on boolean fields like `isActive` — **never** `.default()` in schema — the form provides the default via `defaultValues` to avoid react-hook-form type mismatches
- Every required string field must have `.min(1, "message")` — never bare `z.string()`
- Use `z.enum()` for fixed value sets — never `z.string()` for roles, statuses, etc.
- Use `z.email()` top-level for email fields — this is the Zod 4 API, not `z.string().email()`

---

### 4. Update schema

```ts
export const updateUserSchema = createUserSchema
  .omit({ password: true })
  .partial()
  .extend({
    id: z.int().positive("Invalid user ID"),
  })

export type UpdateUserDto = z.infer<typeof updateUserSchema>
```

**Rules:**
- Always start from `createUserSchema` — never duplicate field definitions
- Always `.partial()` — all fields optional on update
- Always `.extend({ id })` — id is required on update
- `id` is `z.int().positive()` for User (int auto increment) — use `z.uuid()` for all other entities (Guid v7)
- Use `.omit()` to exclude fields that cannot be updated (e.g. password has its own dedicated endpoint)

---

### 5. Filter schema

```ts
export const userFilterSchema = z.object({
  search: z.string().optional(),
  role: z.enum(["Admin", "Manager", "SalesRep"]).optional(),
  isActive: z.boolean().optional(),
  page: z.int().positive().default(1),
  pageSize: z.int().positive().max(100).default(20),
  sortBy: z.enum(["name", "email", "createdAt"]).optional(),
  sortOrder: z.enum(["asc", "desc"]).default("desc"),
})

export type UserFilterDto = z.infer<typeof userFilterSchema>
```

**Rules:**
- All filter fields optional — filters are never required
- `page` default `1`, `pageSize` default `20` — matches .NET API defaults
- `pageSize` max `100` — matches .NET API max page size
- `sortBy` enum values must match actual .NET API sortable column names exactly
- Use `z.int()` for page and pageSize — Zod 4 dedicated integer type, not `z.number().int()`

---

### 6. Entity type

The full entity type matching the .NET API response shape exactly. Defined as a plain TypeScript type — not a Zod schema — because we trust the API response and don't validate it at runtime.

```ts
export type User = {
  id: number                 // int — auto increment
  tenantId: string           // Guid — kept for multi-tenancy
  name: string
  email: string
  role: "Admin" | "Manager" | "SalesRep"
  isActive: boolean
  deviceId: string | null    // Mobile device identifier
  createdAt: string          // ISO string from .NET API
  updatedAt: string          // ISO string from .NET API
  isDeleted: boolean
}
```

**Rules:**
- `id` is `number` for User — int auto increment
- All other entity IDs are `string` — Guid v7 from .NET API
- Dates are always `string` — .NET API returns ISO strings, never Date objects
- Nullable fields are `type | null` — never `type | undefined`
- Match field names exactly to what the .NET API returns — camelCase

---

### 7. Select option type (only if feature is used as a FK dropdown in other features)

Lightweight type for dropdown selects — only id and name, nothing else.

```ts
export type UserOption = {
  id: number
  name: string
}
```

---

## Zod 4 API Reference

Key differences from Zod 3 that apply in this project:

| Scenario | Zod 3 (old) | Zod 4 (correct) |
|----------|-------------|-----------------|
| Import | `import { z } from "zod"` | `import * as z from "zod"` |
| Email | `z.string().email()` | `z.email()` |
| UUID | `z.string().uuid()` | `z.uuid()` |
| Integer | `z.number().int()` | `z.int()` |
| URL | `z.string().url()` | `z.url()` |
| ISO datetime | `z.string().datetime()` | `z.iso.datetime()` |
| ISO date | `z.string().isodate()` | `z.iso.date()` |
| Error message | `{ message: "..." }` | `"..."` or `{ error: "..." }` |
| Enum error | `errorMap: () => ({ message: "..." })` | `{ error: "..." }` |
| Native enum | `z.nativeEnum(MyEnum)` | `z.enum(MyEnum)` |

---

## ID Type Reference

| Entity | ID Type in schema | ID Type in entity | Reason |
|--------|------------------|-------------------|--------|
| User | `z.int().positive()` | `number` | int auto increment — fast joins, never generated offline |
| All other entities | `z.uuid()` | `string` | Guid v7 — offline mobile sync compatible, sequential |

---

## Validation Rules Reference

| Field Type | Zod 4 Rule |
|-----------|------------|
| Required string | `z.string().trim().min(1, "Field is required")` |
| Optional string | `z.string().trim().optional()` |
| Email | `z.email("Must be a valid email")` |
| Password | `z.string().min(8, "...").max(100, "...")` |
| Integer FK (User) | `z.int().positive("Invalid ID")` |
| Guid FK (other entities) | `z.uuid("Invalid ID")` |
| Boolean | `z.boolean().optional()` — never `.default()` in schema |
| Enum | `z.enum(["A", "B", "C"])` — never `z.string()` |
| Decimal / Price | `z.string()` — .NET API returns decimals as strings |
| ISO datetime filter | `z.iso.datetime().optional()` |
| ISO date filter | `z.iso.date().optional()` |
| Pagination page | `z.int().positive().default(1)` |
| Pagination pageSize | `z.int().positive().max(100).default(20)` |

---

## Error Formatting Reference

Zod 4 provides three utilities for extracting errors. Use the right one depending on context.

**`z.flattenError()`** — use this in actions when mapping API validation errors back to form fields. Returns `{ formErrors: string[], fieldErrors: { [field]: string[] } }`.

```ts
const result = createUserSchema.safeParse(input)
if (!result.success) {
  const flat = z.flattenError(result.error)
  // flat.fieldErrors.email => ["Must be a valid email address"]
}
```

**`z.treeifyError()`** — use this for deeply nested object schemas. Returns a nested tree mirroring the schema structure.

**`z.prettifyError()`** — use this for logging or debug output only. Returns a human-readable string — never return this to the client.

---

## Complete Example — user.schema.ts

```ts
import * as z from "zod"

// ── Field Constants ────────────────────────────────────────────────────────

const nameField = z
  .string()
  .trim()
  .min(1, "Name is required")
  .max(255, "Name must be 255 characters or less")

// ── Create ─────────────────────────────────────────────────────────────────

export const createUserSchema = z.object({
  name: nameField,
  email: z.email("Must be a valid email address"),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters")
    .max(100, "Password must be 100 characters or less"),
  role: z.enum(["Admin", "Manager", "SalesRep"], {
    error: "Role must be Admin, Manager or SalesRep",
  }),
  isActive: z.boolean().optional(),
})

export type CreateUserDto = z.infer<typeof createUserSchema>

// ── Update ─────────────────────────────────────────────────────────────────

export const updateUserSchema = createUserSchema
  .omit({ password: true })
  .partial()
  .extend({
    id: z.int().positive("Invalid user ID"),
  })

export type UpdateUserDto = z.infer<typeof updateUserSchema>

// ── Filter ─────────────────────────────────────────────────────────────────

export const userFilterSchema = z.object({
  search: z.string().optional(),
  role: z.enum(["Admin", "Manager", "SalesRep"]).optional(),
  isActive: z.boolean().optional(),
  page: z.int().positive().default(1),
  pageSize: z.int().positive().max(100).default(20),
  sortBy: z.enum(["name", "email", "createdAt"]).optional(),
  sortOrder: z.enum(["asc", "desc"]).default("desc"),
})

export type UserFilterDto = z.infer<typeof userFilterSchema>

// ── Entity Type ────────────────────────────────────────────────────────────

export type User = {
  id: number
  tenantId: string
  name: string
  email: string
  role: "Admin" | "Manager" | "SalesRep"
  isActive: boolean
  deviceId: string | null
  createdAt: string
  updatedAt: string
  isDeleted: boolean
}

// ── Option Type ────────────────────────────────────────────────────────────

export type UserOption = {
  id: number
  name: string
}
```

---

## Common Mistakes — Never Do These

- Never use `import { z } from "zod"` — this project uses Zod 4, always `import * as z from "zod"`
- Never use `z.string().email()` — use `z.email()` (Zod 4 top-level format)
- Never use `z.string().uuid()` — use `z.uuid()` (Zod 4 top-level format)
- Never use `z.number().int()` — use `z.int()` (Zod 4 dedicated integer type)
- Never use `z.string().datetime()` — use `z.iso.datetime()` (Zod 4 API)
- Never use `{ message: "..." }` for error params — use a plain string or `{ error: "..." }` (Zod 4)
- Never use `errorMap: () => ({ message: "..." })` on enums — use `{ error: "..." }` (Zod 4)
- Never write TypeScript types manually for DTOs — always infer from Zod with `z.infer<typeof schema>`
- Never add `id` to create schema — server assigns this
- Never add `createdAt`, `updatedAt`, `createdBy`, `updatedBy` to any schema
- Never add `tenantId` or `companyId` to any schema — .NET API resolves from JWT
- Never use `.default()` on boolean fields in schemas — always `.optional()`; form provides defaultValues
- Never use `z.any()`
- Never use bare `z.string()` on required fields — always `.trim().min(1, "message")`
- Never type dates as `Date` — always `string`
- Never duplicate field definitions between create and update — always `.partial()` the create schema
- Never create a barrel file — import directly from the schema file path