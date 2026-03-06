---
name: sfa-nextjs-feature-components
description: Creating UI components for a feature module in the SFA Next.js web app. Use this when adding components for any feature like users, customers, leads, orders, products, visits, or tasks. Creates all component files including types, forms, dialogs, columns, table, and list page. No shared form-fields file — each form contains its own fields inline typed to its own schema. Always uses shadcn only. Always reads data from TanStack Query hooks and store selector hooks (imported through store barrel). Never calls actions directly. Uses Spinner from components/ui/spinner for all loading states — never loading text.
---

# Components Skill

## Location

```
features/{feature}/components/
├── dialogs/
│   ├── create-{feature}-dialog.tsx
│   ├── update-{feature}-dialog.tsx
│   ├── delete-{feature}-dialog.tsx
│   ├── {feature}-details-dialog.tsx
│   └── index.ts
├── forms/
│   ├── create-{feature}-form.tsx
│   ├── update-{feature}-form.tsx
│   └── index.ts
├── tables/
│   ├── {feature}-table.tsx
│   ├── {feature}-columns.tsx
│   └── index.ts
├── pages/
│   ├── {feature}-list-page.tsx
│   └── index.ts
├── types.ts
└── index.ts
```

No `{feature}-form-fields.tsx` file. Each form owns its own fields inline. This avoids the `UseFormReturn<CreateDto>` vs `UseFormReturn<UpdateDto>` type mismatch that occurs when sharing fields between forms with different schema types.

---

## Rules Before Writing Anything

1. Read the schema skill output — know every DTO type and schema name before writing components
2. Read the hooks skill output — know every query hook, mutation hook, DataTable hook, and options hook before writing components
3. Read the store skill output — know every named selector hook before writing components
4. Every file is `"use client"` — no exceptions in this folder
5. Only shadcn/ui components — never any other UI library
6. **Icons**: always `lucide-react` — never `@radix-ui/react-icons` or `react-icons` in feature components
7. Never call actions directly — always through mutation hooks
8. Never fetch data directly — always through query hooks
9. All loading states use `<Spinner />` from `@/components/ui/spinner` (local shadcn component) — never from `react-spinners`, never loading text strings
10. Build in this order: types → create-form → update-form → dialogs → columns → table → page
11. Never create a shared form-fields component — type mismatch between create and update schemas
12. **Foreign key fields**: when a form field is a foreign key to another feature, always render it as a `<Select>` field — import `use{RelatedFeature}Options` directly from the related feature's hooks file (`@/features/{related-feature}/hooks/{related-feature}.hooks`). Never pass options as props. Never use a plain `<Input>` for a foreign key field.
13. Always import store selector hooks through the store barrel (`../store` from within the feature, or `@/features/{feature}/store` from outside) — never from individual store files

---

## Spinner Rule — Critical

`<Spinner />` is a **local shadcn component** — not from `react-spinners`. Always import from the local path:

```tsx
import { Spinner } from "@/components/ui/spinner"

<div className="flex items-center justify-center py-8">
  <Spinner />
</div>
```

Never:
```tsx
import { ClipLoader } from "react-spinners"  // ❌ never react-spinners
<div>Loading...</div>                         // ❌ never loading text
<div>Loading <Spinner /></div>                // ❌ never text with spinner
<div className="animate-spin">               // ❌ never custom spinner
```

---

## Why No Shared Form Fields

```
CreateDto  = { name: string; isActive: boolean }                    ← required fields
UpdateDto  = { id: string; name?: string; isActive?: boolean }      ← all optional

UseFormReturn<CreateDto>  ≠  UseFormReturn<UpdateDto>
```

Passing a form typed as `UseFormReturn<UpdateDto>` to a component expecting `UseFormReturn<CreateDto>` causes TS error 2322 — `string | undefined` is not assignable to `string`. The fields are optional in update but required in create. The fix is to never share form fields. Each form owns its fields inline, typed to its own DTO. Duplication of field markup is intentional and correct.

---

## File 1 — `types.ts`

```ts
import type { Feature } from "@/features/{feature}/schemas/{feature}.schema"
import type { CreateFeatureDto } from "@/features/{feature}/schemas/{feature}.schema"

export type FeatureFormData = CreateFeatureDto
export type FeatureTableData = Feature

export interface FeatureDialogProps {
  onSuccess?: () => void
}

export interface UpdateFeatureDialogProps extends FeatureDialogProps {
  featureId: string   // string — Guid v7 for all entities except User (use number for User)
}
```

**Rules:**
- `FeatureDialogProps` — base props for create and delete dialogs
- `UpdateFeatureDialogProps` — extends base with required `featureId`
- `featureId` is `string` for all entities except User — User IDs are `number` (int auto increment)
- Never redeclare types already inferred from Zod schemas

---

## File 2 — `forms/create-{feature}-form.tsx`

Each field is written inline. No shared form-fields component. Uses `mutateAsync` to await the `ActionResult` and map server field errors back into form fields.

```tsx
"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Spinner } from "@/components/ui/spinner"
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useCreateFeature } from "@/features/{feature}/hooks/{feature}.hooks"
import {
  createFeatureSchema,
  type CreateFeatureDto,
} from "@/features/{feature}/schemas/{feature}.schema"
import type { FeatureDialogProps } from "../types"

export function CreateFeatureForm({ onSuccess }: FeatureDialogProps) {
  const form = useForm<CreateFeatureDto>({
    resolver: zodResolver(createFeatureSchema),
    defaultValues: {
      name: "",
      isActive: true,
    },
  })

  const mutation = useCreateFeature()

  async function onSubmit(values: CreateFeatureDto) {
    const result = await mutation.mutateAsync(values)

    if (!result.success) {
      // Map server field errors from .NET FluentValidation into form fields
      if (result.fieldErrors) {
        Object.entries(result.fieldErrors).forEach(([field, messages]) => {
          form.setError(field as keyof CreateFeatureDto, {
            type: "server",
            message: messages[0],
          })
        })
      }
      return
    }

    form.reset()
    onSuccess?.()
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">

        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input placeholder="Enter name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="isActive"
          render={({ field }) => (
            <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
              <div className="space-y-0.5">
                <FormLabel>Active</FormLabel>
                <FormDescription>Mark as active</FormDescription>
              </div>
              <FormControl>
                <Checkbox
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-3">
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? <Spinner /> : "Create"}
          </Button>
        </div>

      </form>
    </Form>
  )
}
```

**Rules:**
- `useForm<CreateFeatureDto>` — always typed with the create DTO, never untyped
- `zodResolver(createFeatureSchema)` — always on the create form
- `defaultValues` must cover every field in the create schema — never leave fields missing
- Use `mutateAsync` not `mutate` — `mutateAsync` returns the `ActionResult` so you can `await` it and call `form.setError()` inline
- Map `result.fieldErrors` via `form.setError()` with `type: "server"` — `<FormMessage />` renders these automatically
- `form.reset()` before `onSuccess?.()` — always reset first, then close
- Submit button shows `<Spinner />` when `isPending` — never loading text
- Import hooks from `@/features/{feature}/hooks/{feature}.hooks` directly — no barrel

---

## Foreign Key Fields — Select Pattern

When a form field is a foreign key referencing another feature (e.g. `categoryId` on a product form), always follow this exact pattern.

**Rule 1** — Import the related feature's options hook directly from its hooks file. This is the only permitted cross-feature hook import.

**Rule 2** — Render it as a `<Select>` field inline in the form. Never a plain `<Input>`. Never pass options as props from outside.

**Rule 3** — Handle all three states inside `<SelectContent>`: loading (`<Spinner />`), empty (muted message), and populated options list.

```tsx
"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Spinner } from "@/components/ui/spinner"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useCreateProduct } from "@/features/product/hooks/product.hooks"
import { createProductSchema, type CreateProductDto } from "@/features/product/schemas/product.schema"
// Cross-feature options hook — allowed only for dropdown population, imported from hooks file directly
import { useCategoryOptions } from "@/features/category/hooks/category.hooks"
import type { ProductDialogProps } from "../types"

export function CreateProductForm({ onSuccess }: ProductDialogProps) {
  const form = useForm<CreateProductDto>({
    resolver: zodResolver(createProductSchema),
    defaultValues: {
      name: "",
      categoryId: undefined,
      isActive: true,
    },
  })

  const mutation = useCreateProduct()
  const { data: categoryOptions = [], isPending: categoriesLoading } = useCategoryOptions()

  async function onSubmit(values: CreateProductDto) {
    const result = await mutation.mutateAsync(values)

    if (!result.success) {
      if (result.fieldErrors) {
        Object.entries(result.fieldErrors).forEach(([field, messages]) => {
          form.setError(field as keyof CreateProductDto, {
            type: "server",
            message: messages[0],
          })
        })
      }
      return
    }

    form.reset()
    onSuccess?.()
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">

        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input placeholder="Enter product name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Foreign key field — always a Select, never an Input */}
        <FormField
          control={form.control}
          name="categoryId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Category</FormLabel>
              <Select
                onValueChange={(value) => field.onChange(value)}
                value={field.value ?? ""}
                disabled={categoriesLoading || categoryOptions.length === 0}
              >
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a category" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {categoriesLoading ? (
                    <div className="flex items-center justify-center py-4">
                      <Spinner />
                    </div>
                  ) : categoryOptions.length === 0 ? (
                    <div className="p-2 text-sm text-muted-foreground">
                      No categories available
                    </div>
                  ) : (
                    categoryOptions.map((option) => (
                      <SelectItem key={option.id} value={option.id}>
                        {option.name}
                      </SelectItem>
                    ))
                  )}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-3">
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? <Spinner /> : "Create"}
          </Button>
        </div>

      </form>
    </Form>
  )
}
```

**Rules:**
- Foreign key IDs in this project are `string` (Guid v7) for all entities except User — pass `value={option.id}` directly without `parseInt`
- For User foreign keys (int ID): `onValueChange={(value) => field.onChange(parseInt(value))}` and `value={field.value?.toString() ?? ""}`
- `disabled` when loading or no options — never let user interact with an empty Select
- Three states inside `<SelectContent>`: loading → `<Spinner />`, empty → muted message, populated → mapped options
- The same Select pattern applies identically in `update-{feature}-form.tsx` — also pre-populate value in the `useEffect` reset

---

## File 3 — `forms/update-{feature}-form.tsx`

Fields written inline. Typed with `UpdateFeatureDto` — never with `CreateFeatureDto`. No `zodResolver` — update fields are all optional, resolver causes false required errors on untouched fields.

```tsx
"use client"

import { useEffect } from "react"
import { useForm } from "react-hook-form"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Spinner } from "@/components/ui/spinner"
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useFeature, useUpdateFeature } from "@/features/{feature}/hooks/{feature}.hooks"
import { type UpdateFeatureDto } from "@/features/{feature}/schemas/{feature}.schema"
import type { UpdateFeatureDialogProps } from "../types"

export function UpdateFeatureForm({ featureId, onSuccess }: UpdateFeatureDialogProps) {
  const { data: feature, isPending: isLoading } = useFeature(featureId)
  const mutation = useUpdateFeature()

  const form = useForm<UpdateFeatureDto>({
    // No zodResolver — update fields are all optional; resolver causes false required validation errors on untouched fields
    defaultValues: {
      id: featureId,
      name: "",
      isActive: true,
    },
  })

  // Pre-fill form when entity loads
  useEffect(() => {
    if (feature) {
      form.reset({
        id: feature.id,
        name: feature.name,
        isActive: feature.isActive,
      })
    }
  }, [feature, form])

  async function onSubmit(values: UpdateFeatureDto) {
    const result = await mutation.mutateAsync(values)

    if (!result.success) {
      if (result.fieldErrors) {
        Object.entries(result.fieldErrors).forEach(([field, messages]) => {
          form.setError(field as keyof UpdateFeatureDto, {
            type: "server",
            message: messages[0],
          })
        })
      }
      return
    }

    onSuccess?.()
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Spinner />
      </div>
    )
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">

        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input placeholder="Enter name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="isActive"
          render={({ field }) => (
            <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
              <div className="space-y-0.5">
                <FormLabel>Active</FormLabel>
                <FormDescription>Mark as active</FormDescription>
              </div>
              <FormControl>
                <Checkbox
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-3">
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? <Spinner /> : "Save changes"}
          </Button>
        </div>

      </form>
    </Form>
  )
}
```

**Rules:**
- `useForm<UpdateFeatureDto>` — always typed with the update DTO, never the create DTO
- No `zodResolver` — update schema fields are all optional; resolver causes false required validation errors on untouched fields
- `id: featureId` always in `defaultValues` — update schema requires id
- `isLoading` guard returns centered `<Spinner />` — never text, never null
- `useEffect` depends on `[feature, form]` — always both dependencies
- `form.reset()` inside `useEffect` maps all fields from the loaded entity including `id`
- Submit button shows `<Spinner />` when `isPending` — never text
- Use `mutateAsync` not `mutate` — same reason as create form; need to `await` result for `form.setError()`
- Do NOT call `form.reset()` before `onSuccess?.()` on update — only create needs that reset
- Foreign key fields follow the exact same Select pattern as create forms — pre-populate `value` from the loaded entity in the `useEffect` reset

---

## File 4 — `dialogs/create-{feature}-dialog.tsx`

Dialog shell. Reads open state from the dialog store. Passes `onSuccess` to the form.

```tsx
"use client"

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { useIsCreateOpen, use{Feature}DialogActions } from "@/features/{feature}/store"
import { Create{Feature}Form } from "../forms"

export function Create{Feature}Dialog() {
  const isCreateOpen = useIsCreateOpen()
  const { closeCreate } = use{Feature}DialogActions()

  return (
    <Dialog open={isCreateOpen} onOpenChange={(open) => !open && closeCreate()}>
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle>Create {Feature}</DialogTitle>
        </DialogHeader>
        <Create{Feature}Form onSuccess={closeCreate} />
      </DialogContent>
    </Dialog>
  )
}
```

**Rules:**
- Import store selectors from `@/features/{feature}/store` (the barrel) — never from individual store files
- Read `isCreateOpen` from the named selector hook — never from the raw store
- `onOpenChange={(open) => !open && closeCreate()}` — handles Escape key and overlay click
- Form handles its own submission and `onSuccess` calls `closeCreate`
- No loading state or spinner in this file — the form manages that internally

---

## File 5 — `dialogs/update-{feature}-dialog.tsx`

Dialog shell. Guards on `updateId`. Form handles its own data fetching and loading state.

```tsx
"use client"

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { useIsUpdateOpen, useUpdateId, use{Feature}DialogActions } from "@/features/{feature}/store"
import { Update{Feature}Form } from "../forms"

export function Update{Feature}Dialog() {
  const isUpdateOpen = useIsUpdateOpen()
  const updateId = useUpdateId()
  const { closeUpdate } = use{Feature}DialogActions()

  if (!updateId) return null

  return (
    <Dialog open={isUpdateOpen} onOpenChange={(open) => !open && closeUpdate()}>
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle>Edit {Feature}</DialogTitle>
        </DialogHeader>
        <Update{Feature}Form featureId={updateId} onSuccess={closeUpdate} />
      </DialogContent>
    </Dialog>
  )
}
```

**Rules:**
- Import store selectors from `@/features/{feature}/store` (the barrel)
- `if (!updateId) return null` — always guard before rendering the dialog
- `featureId={updateId}` — pass the ID to the form; the form fetches its own data
- Form handles its own loading state internally — no spinner needed here

---

## File 6 — `dialogs/delete-{feature}-dialog.tsx`

AlertDialog for delete confirmation. No form. Uses `mutate` not `mutateAsync` — no field errors on delete.

```tsx
"use client"

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Spinner } from "@/components/ui/spinner"
import { useIsDeleteOpen, useDeleteId, use{Feature}DialogActions } from "@/features/{feature}/store"
import { useDelete{Feature} } from "@/features/{feature}/hooks/{feature}.hooks"

export function Delete{Feature}Dialog() {
  const isDeleteOpen = useIsDeleteOpen()
  const deleteId = useDeleteId()
  const { closeDelete } = use{Feature}DialogActions()
  const mutation = useDelete{Feature}()

  function handleDelete() {
    if (!deleteId) return
    mutation.mutate(deleteId, {
      onSuccess: () => closeDelete(),
    })
  }

  return (
    <AlertDialog open={isDeleteOpen} onOpenChange={(open) => !open && closeDelete()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete {Feature}</AlertDialogTitle>
          <AlertDialogDescription>
            Are you sure? This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel
            onClick={closeDelete}
            disabled={mutation.isPending}
          >
            Cancel
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleDelete}
            disabled={mutation.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {mutation.isPending ? <Spinner /> : "Delete"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
```

**Rules:**
- `AlertDialog` not `Dialog` — always for destructive confirmation
- `mutation.mutate` not `mutateAsync` — no field errors on delete, no need to await ActionResult inline
- `handleDelete` guards `if (!deleteId) return` — never fire with null ID
- `closeDelete()` only inside `onSuccess` — failed deletes leave the dialog open
- Cancel also `disabled={mutation.isPending}` — prevent closing mid-delete
- Delete button shows `<Spinner />` when `isPending` — never text
- `className="bg-destructive ..."` on `AlertDialogAction` — the default is not red; always apply explicitly

---

## File 7 — `dialogs/{feature}-details-dialog.tsx`

Read-only view dialog. No form. Fetches entity when open.

```tsx
"use client"

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"
import { Spinner } from "@/components/ui/spinner"
import { useIsDetailsOpen, useDetailsId, use{Feature}DialogActions } from "@/features/{feature}/store"
import { use{Feature} } from "@/features/{feature}/hooks/{feature}.hooks"

export function {Feature}DetailsDialog() {
  const isDetailsOpen = useIsDetailsOpen()
  const detailsId = useDetailsId()
  const { closeDetails } = use{Feature}DialogActions()
  const { data: feature, isPending: isLoading } = use{Feature}(detailsId ?? "")

  if (!detailsId) return null

  return (
    <Dialog open={isDetailsOpen} onOpenChange={(open) => !open && closeDetails()}>
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle>{Feature} Details</DialogTitle>
        </DialogHeader>

        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <Spinner />
          </div>
        ) : feature ? (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Name</p>
                <p className="text-sm font-semibold">{feature.name}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Status</p>
                <Badge variant={feature.isActive ? "default" : "secondary"}>
                  {feature.isActive ? "Active" : "Inactive"}
                </Badge>
              </div>
            </div>
            <div className="border-t pt-4 text-xs text-muted-foreground">
              <p>Created: {new Date(feature.createdAt).toLocaleDateString()}</p>
              {feature.updatedAt && (
                <p>Updated: {new Date(feature.updatedAt).toLocaleDateString()}</p>
              )}
            </div>
          </div>
        ) : (
          <div className="py-8 text-center text-sm text-muted-foreground">
            {Feature} not found
          </div>
        )}

      </DialogContent>
    </Dialog>
  )
}
```

**Rules:**
- `if (!detailsId) return null` — always guard before rendering
- Three states: loading → centered `<Spinner />` | data → details grid | no data → not found message
- Dates always `new Date(value).toLocaleDateString()` — never raw ISO string
- Read-only — no form, no mutation hooks

---

## File 8 — `tables/{feature}-columns.tsx`

TanStack Table column definitions. Actions cell calls store selector hooks inline inside the cell render function.

```tsx
"use client"

import { ColumnDef } from "@tanstack/react-table"
import { MoreHorizontal, Eye, Pencil, Trash2 } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { DataTableColumnHeader } from "@/components/data-table/column-header"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { use{Feature}DialogActions } from "@/features/{feature}/store"
import type { Feature } from "@/features/{feature}/schemas/{feature}.schema"

export const featureColumns: ColumnDef<Feature>[] = [
  {
    id: "select",
    size: 50,
    header: ({ table }) => (
      <Checkbox
        checked={table.getIsAllPageRowsSelected()}
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        aria-label="Select all"
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={(value) => row.toggleSelected(!!value)}
        aria-label="Select row"
      />
    ),
    enableSorting: false,
    enableHiding: false,
  },
  {
    accessorKey: "name",
    size: 300,
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Name" />
    ),
    cell: ({ row }) => (
      <div className="font-medium">{row.getValue("name")}</div>
    ),
  },
  {
    accessorKey: "isActive",
    size: 100,
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Status" />
    ),
    cell: ({ row }) => {
      const isActive = row.getValue<boolean>("isActive")
      return (
        <Badge variant={isActive ? "default" : "secondary"}>
          {isActive ? "Active" : "Inactive"}
        </Badge>
      )
    },
  },
  {
    accessorKey: "createdAt",
    size: 150,
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Created" />
    ),
    cell: ({ row }) => (
      <div className="text-sm text-muted-foreground">
        {new Date(row.getValue("createdAt")).toLocaleDateString()}
      </div>
    ),
  },
  {
    id: "actions",
    size: 80,
    enableHiding: false,
    cell: ({ row }) => {
      const feature = row.original
      // Hooks called inside the cell render function — valid React hook call
      const { openDetails, openUpdate, openDelete } = use{Feature}DialogActions()

      return (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="h-8 w-8 p-0">
              <span className="sr-only">Open menu</span>
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuLabel>Actions</DropdownMenuLabel>
            <DropdownMenuItem onClick={() => openDetails(feature.id)}>
              <Eye className="mr-2 h-4 w-4" />
              View Details
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={() => openUpdate(feature.id)}>
              <Pencil className="mr-2 h-4 w-4" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => openDelete(feature.id)}
              className="text-destructive focus:text-destructive"
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      )
    },
  },
]
```

**Rules:**
- Column order: select → name → feature-specific columns → status → createdAt → actions
- Actions dropdown order: View Details → (separator) → Edit → Delete
- Delete item always `text-destructive focus:text-destructive`
- `DataTableColumnHeader` on all sortable columns — never plain text header
- `aria-label` on both select checkboxes — required for accessibility
- `feature.id` passed directly to store actions — it is already `string` (Guid); for User pass `String(feature.id)` since User ID is `number`
- Import store actions from `@/features/{feature}/store` (the barrel)

---

## File 9 — `tables/{feature}-table.tsx`

DataTable wrapper. Passes the hook and export config. No manual state, no manual refresh.

```tsx
"use client"

import { useMemo } from "react"
import dynamic from "next/dynamic"
import { use{Feature}DataTable } from "@/features/{feature}/hooks/{feature}.hooks"
import { featureColumns } from "./{feature}-columns"

const DataTable = dynamic(
  () => import("@/components/data-table/data-table").then((mod) => mod.DataTable),
  { ssr: false }
)

export function {Feature}Table() {
  const exportConfig = useMemo(
    () => ({
      entityName: "{Features}",
      columnMapping: {
        id: "ID",
        name: "Name",
        isActive: "Status",
        createdAt: "Created",
      },
      columnWidths: [
        { wch: 10 },
        { wch: 30 },
        { wch: 12 },
        { wch: 15 },
      ],
      headers: ["ID", "Name", "Status", "Created"],
    }),
    []
  )

  return (
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
  )
}
```

**Rules:**
- `exportConfig` always in `useMemo` — avoids recreating the object on every render
- `fetchDataFn={use{Feature}DataTable}` — pass the hook directly, never a plain async function. The DataTable calls it internally as a React hook
- The `use{Feature}DataTable` hook must have `.isQueryHook = true` set in the hooks file — DataTable uses this flag to call it correctly as a React hook
- `columnMapping` keys must match actual column `accessorKey` values exactly
- No `useState`, no `useEffect`, no manual refresh — TanStack Query invalidation through the hook handles all of that automatically
- `dynamic` import with `{ ssr: false }` — DataTable uses browser APIs; disable SSR
- Import hooks from `@/features/{feature}/hooks/{feature}.hooks` directly — no barrel

---

## File 10 — `pages/{feature}-list-page.tsx`

Page shell. Reads only `openCreate` from the dialog store. Mounts all four dialogs always.

```tsx
"use client"

import { Button } from "@/components/ui/button"
import { use{Feature}DialogActions } from "@/features/{feature}/store"
import { {Feature}Table } from "../tables"
import {
  Create{Feature}Dialog,
  Update{Feature}Dialog,
  Delete{Feature}Dialog,
  {Feature}DetailsDialog,
} from "../dialogs"

export function {Feature}ListPage() {
  const { openCreate } = use{Feature}DialogActions()

  return (
    <div className="space-y-6 px-6 py-6">
      <div className="flex items-center justify-between bg-muted/50 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{Feature} Management</h1>
          <p className="text-muted-foreground">Manage your {feature} records</p>
        </div>
        <Button onClick={openCreate}>Create {Feature}</Button>
      </div>

      <{Feature}Table />

      {/* All dialogs always mounted — visibility controlled by dialog store */}
      <Create{Feature}Dialog />
      <Update{Feature}Dialog />
      <Delete{Feature}Dialog />
      <{Feature}DetailsDialog />
    </div>
  )
}
```

**Rules:**
- Only reads `openCreate` from the dialog store — nothing else
- All four dialogs always mounted — they control their own visibility via the dialog store
- Never fetch data in the page — that is the table's and form's concern
- Never pass data as props into dialogs

---

## Barrel Files

### `forms/index.ts`
```ts
export { Create{Feature}Form } from "./create-{feature}-form"
export { Update{Feature}Form } from "./update-{feature}-form"
```

No form-fields export.

### `dialogs/index.ts`
```ts
export { Create{Feature}Dialog } from "./create-{feature}-dialog"
export { Update{Feature}Dialog } from "./update-{feature}-dialog"
export { Delete{Feature}Dialog } from "./delete-{feature}-dialog"
export { {Feature}DetailsDialog } from "./{feature}-details-dialog"
```

### `tables/index.ts`
```ts
export { {Feature}Table } from "./{feature}-table"
export { featureColumns } from "./{feature}-columns"
```

### `pages/index.ts`
```ts
export { {Feature}ListPage } from "./{feature}-list-page"
```

### `components/index.ts`
```ts
export * from "./dialogs"
export * from "./forms"
export * from "./tables"
export * from "./pages"
```

---

## How the Layers Connect

```
{Feature}ListPage
  ├── reads openCreate from use{Feature}DialogActions() [via store barrel]
  ├── renders {Feature}Table
  │     ├── passes use{Feature}DataTable hook to DataTable [from hooks file]
  │     └── passes featureColumns (which call use{Feature}DialogActions inside cell) [via store barrel]
  ├── renders Create{Feature}Dialog
  │     ├── reads useIsCreateOpen() from store barrel
  │     └── renders Create{Feature}Form → useCreateFeature() mutation [from hooks file]
  ├── renders Update{Feature}Dialog
  │     ├── reads useIsUpdateOpen() + useUpdateId() from store barrel
  │     └── renders Update{Feature}Form → useFeature(id) query + useUpdateFeature() mutation [from hooks file]
  ├── renders Delete{Feature}Dialog
  │     ├── reads useIsDeleteOpen() + useDeleteId() from store barrel
  │     └── calls useDeleteFeature() mutation [from hooks file]
  └── renders {Feature}DetailsDialog
        ├── reads useIsDetailsOpen() + useDetailsId() from store barrel
        └── calls useFeature(id) query [from hooks file]
```

Data only flows down through props. IDs flow through the store. Dialogs never accept data props — they read their own ID from the store and fetch independently.

---

## Loading State Reference

| Component | Condition | State shown |
|-----------|-----------|-------------|
| `update-{feature}-form.tsx` | `isLoading` true | Centered `<Spinner />` replacing entire form |
| `{feature}-details-dialog.tsx` | `isLoading` true | Centered `<Spinner />` replacing content |
| `create-{feature}-form.tsx` | `isPending` true | `<Spinner />` inside submit button |
| `update-{feature}-form.tsx` | `isPending` true | `<Spinner />` inside submit button |
| `delete-{feature}-dialog.tsx` | `isPending` true | `<Spinner />` inside delete button |
| Foreign key `<Select>` | options loading | `<Spinner />` inside `<SelectContent>` |
| `{feature}-table.tsx` | any | DataTable handles loading internally via hook |

---

## Common Mistakes — Never Do These

- Never create a shared form-fields component — always inline fields in each form
- Never type the update form with `UseFormReturn<CreateFeatureDto>` — always `UseFormReturn<UpdateFeatureDto>`
- Never add `zodResolver` to the update form — update fields are all optional; resolver causes false required validation errors on untouched fields
- Never use loading text — always `<Spinner />`
- Never import `Spinner` from `react-spinners` — always from `@/components/ui/spinner` (local shadcn component)
- Never use `@radix-ui/react-icons` or `react-icons` in feature components — always use `lucide-react`
- Never call actions directly from components — always through mutation hooks
- Never import store selector hooks from individual store files — always import through the `../store` barrel or `@/features/{feature}/store`
- Never import the raw store instance — only import named selector hooks
- Never render the update dialog without `if (!updateId) return null` guard
- Never render the details dialog without `if (!detailsId) return null` guard
- Never use `mutation.mutate()` in create or update forms — use `mutation.mutateAsync()` to await the result and call `form.setError()`
- Never use `mutation.mutateAsync()` in delete dialog — use `mutation.mutate()` with `onSuccess`; no field errors on delete
- Never close dialogs on a failed mutation — only close after confirming `result.success`
- Never forget `form.reset()` after successful create before calling `onSuccess?.()`
- Never use `Dialog` for delete confirmation — always `AlertDialog`
- Never forget `disabled={mutation.isPending}` on the cancel button in delete dialog
- Never pass a plain async function as `fetchDataFn` — always pass the `use{Feature}DataTable` hook directly
- Never forget `.isQueryHook = true` on the `use{Feature}DataTable` hook — without it DataTable will not call it as a React hook and the table will not refresh after mutations
- Never skip `useMemo` on `exportConfig` in the table component
- Never use any UI library other than shadcn
- Never render a foreign key field as a plain `<Input>` — always a `<Select>` with the related feature's options hook
- Never pass dropdown options as props into a form — always call `use{RelatedFeature}Options` directly inside the form
- Never import another feature's hooks except `use{RelatedFeature}Options` inside a form component for FK dropdowns
- Never forget to handle all three Select states: loading (`<Spinner />`), empty (muted message), populated (mapped options)
- Never forget to pre-populate foreign key Select values in the update form's `useEffect` reset
- Never import hooks from a barrel — hooks have no barrel; import from `@/features/{feature}/hooks/{feature}.hooks` directly