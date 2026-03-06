---
name: nextjs-feature-components
description: "Use when creating UI components for a new feature like category, product, order or user. Creates all component files including dialogs, forms, table columns, table wrapper and list page. No shared form-fields file — each form contains its own fields inline typed to its own schema. Always uses shadcn only, always reads from hooks and store, never calls actions directly. Uses Spinner from components/ui/spinner for all loading states — never loading text."
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

1. Read `AGENTS.md` at the project root first
2. Read the hooks skill output — know every hook before writing components
3. Read the store skill output — know every store action and state
4. Every file is `'use client'` — no exceptions in this folder
5. Only shadcn/ui components — never any other library
6. Never call actions directly — always through mutation hooks
7. Never fetch data directly — always through query hooks
8. All loading states use `<Spinner />` from `@/components/ui/spinner` — never loading text
9. Build in this order: types → create-form → update-form → dialogs → columns → table → page
10. Never create a shared form-fields component — type mismatch between create and update schemas
11. **Foreign key fields**: when a form field is a foreign key to another feature, always render it as a separate inline `<Select>` field — import `use{RelatedFeature}Options` directly from the related feature's hooks barrel (`@/features/{related-feature}/hooks`). Never pass options as props. Never use a plain `<Input>` for a foreign key field.

---

## Spinner Rule — Critical

Every loading state uses this exact pattern — no exceptions:

```tsx
import { Spinner } from '@/components/ui/spinner'

<div className="flex items-center justify-center py-8">
  <Spinner />
</div>
```

Never:
```tsx
<div>Loading...</div>           // ❌ never text
<div>Loading <Spinner /></div>  // ❌ never text with spinner
<div className="animate-spin">  // ❌ never custom spinner
```

---

## File 1 — `types.ts`

```ts
import type { Feature, CreateFeatureDto } from '@/features/{feature}/schemas/{feature}.schema'

export type FeatureFormData = CreateFeatureDto
export type FeatureTableData = Feature  // Feature type is defined in schema file

export interface FeatureDialogProps {
  onSuccess?: () => void
}

export interface UpdateFeatureDialogProps extends FeatureDialogProps {
  featureId: number
}
```

Rules:
- `FeatureDialogProps` — base props for create and delete dialogs
- `UpdateFeatureDialogProps` — extends base with required `featureId: number`
- Never redeclare types already inferred from Zod or from `@/db/schema`

---

## File 2 — `forms/create-{feature}-form.tsx`

Each field is written inline. No shared form-fields component.

```tsx
'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { useCreateFeature } from '@/features/{feature}/hooks'
import {
  createFeatureSchema,
  type CreateFeatureDto,
} from '@/features/{feature}/schemas/{feature}.schema'
import type { FeatureDialogProps } from '../types'

export function CreateFeatureForm({ onSuccess }: FeatureDialogProps) {
  const form = useForm<CreateFeatureDto>({
    resolver: zodResolver(createFeatureSchema),
    defaultValues: {
      name: '',
      isActive: true,
    },
  })

  const createFeature = useCreateFeature()

  function onSubmit(data: CreateFeatureDto) {
    createFeature.mutate(data, {
      onSuccess: () => {
        form.reset()
        onSuccess?.()
      },
    })
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
          <Button type="submit" disabled={createFeature.isPending}>
            {createFeature.isPending ? <Spinner /> : 'Create'}
          </Button>
        </div>

      </form>
    </Form>
  )
}
```

Rules:
- `useForm<CreateFeatureDto>` — always typed with create DTO, never untyped
- `zodResolver(createFeatureSchema)` — always
- `defaultValues` must cover every field in the create schema — no missing defaults
- Submit button shows `<Spinner />` when `isPending` — never text
- `form.reset()` before `onSuccess?.()` — always reset first

---

## Foreign Key Fields — Select Pattern

When a form field is a foreign key referencing another feature (e.g. `categoryId` on a product form), always follow this exact pattern:

**Rule 1** — Import the related feature's options hook directly from its hooks barrel. This is the only permitted cross-feature hook import.

**Rule 2** — Render it as a separate, standalone `<Select>` field inline in the form. Never a plain `<Input>`. Never passed as props from outside.

**Rule 3** — Handle all three states inside the Select: loading (`<Spinner />`), empty, and populated options list.

```tsx
'use client'

// Own feature imports
import { useCreateProduct } from '@/features/product/hooks'
import { createProductSchema, type CreateProductDto } from '@/features/product/schemas/product.schema'

// Cross-feature options hook — allowed only for dropdown population
import { useCategoryOptions } from '@/features/category/hooks'

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'

export function CreateProductForm({ onSuccess }: ProductDialogProps) {
  const form = useForm<CreateProductDto>({
    resolver: zodResolver(createProductSchema),
    defaultValues: {
      name: '',
      categoryId: undefined,
      isActive: true,
    },
  })

  const createProduct = useCreateProduct()

  // Fetch category options directly inside the form
  const { data: categoryOptions = [], isLoading: categoriesLoading } = useCategoryOptions()

  function onSubmit(data: CreateProductDto) {
    createProduct.mutate(data, {
      onSuccess: () => {
        form.reset()
        onSuccess?.()
      },
    })
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">

        {/* Own fields inline */}
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
                onValueChange={(value) => field.onChange(parseInt(value))}
                value={field.value?.toString() ?? ''}
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
                      <SelectItem key={option.id} value={option.id.toString()}>
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
          <Button type="submit" disabled={createProduct.isPending}>
            {createProduct.isPending ? <Spinner /> : 'Create'}
          </Button>
        </div>

      </form>
    </Form>
  )
}
```

Rules:
- `onValueChange={(value) => field.onChange(parseInt(value))}` — always parse to int for number foreign keys
- `value={field.value?.toString() ?? ''}` — always convert number to string for Select value
- `disabled` when loading or no options — never let user interact with an empty Select
- Three states inside `<SelectContent>`: loading → `<Spinner />`, empty → muted message, populated → mapped options
- The same pattern applies identically to `update-{feature}-form.tsx` for the same foreign key field

---

## File 3 — `forms/update-{feature}-form.tsx`

Fields written inline. Typed with `UpdateFeatureDto` — never with `CreateFeatureDto`.

```tsx
'use client'

import { useForm } from 'react-hook-form'
import { useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { useFeature, useUpdateFeature } from '@/features/{feature}/hooks'
import { type UpdateFeatureDto } from '@/features/{feature}/schemas/{feature}.schema'
import type { UpdateFeatureDialogProps } from '../types'

export function UpdateFeatureForm({ featureId, onSuccess }: UpdateFeatureDialogProps) {
  const { data: feature, isLoading } = useFeature(featureId)
  const updateFeature = useUpdateFeature()

  const form = useForm<UpdateFeatureDto>({
    defaultValues: {
      id: featureId,
      name: '',
      isActive: true,
    },
  })

  useEffect(() => {
    if (feature) {
      form.reset({
        id: feature.id,
        name: feature.name,
        isActive: feature.isActive,
      })
    }
  }, [feature, form])

  function onSubmit(data: UpdateFeatureDto) {
    updateFeature.mutate(data, {
      onSuccess: () => onSuccess?.(),
    })
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
          <Button type="submit" disabled={updateFeature.isPending}>
            {updateFeature.isPending ? <Spinner /> : 'Update'}
          </Button>
        </div>

      </form>
    </Form>
  )
}
```

Rules:
- `useForm<UpdateFeatureDto>` — always typed with UPDATE DTO, never create DTO
- No `zodResolver` on update form — update schema fields are all optional, resolver causes false required errors on untouched fields
- `id: featureId` always in `defaultValues` — update schema requires id
- `isLoading` guard returns centered `<Spinner />` — never text, never null
- `useEffect` depends on `[feature, form]` — always both
- `form.reset()` inside `useEffect` maps all fields from entity including `id`
- Submit button shows `<Spinner />` when `isPending`
- Foreign key fields in update forms follow the exact same Select pattern as create forms — also pre-populate `value` from the loaded entity in the `useEffect` reset

---

## File 4 — `dialogs/create-{feature}-dialog.tsx`

```tsx
'use client'

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { use{Feature}DialogStore } from '@/features/{feature}/store'
import { Create{Feature}Form } from '../forms'

export function Create{Feature}Dialog() {
  const { isCreateOpen, closeCreate } = use{Feature}DialogStore()

  return (
    <Dialog open={isCreateOpen} onOpenChange={closeCreate}>
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

---

## File 5 — `dialogs/update-{feature}-dialog.tsx`

```tsx
'use client'

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { use{Feature}DialogStore } from '@/features/{feature}/store'
import { Update{Feature}Form } from '../forms'

export function Update{Feature}Dialog() {
  const { isUpdateOpen, updateId, closeUpdate } = use{Feature}DialogStore()

  if (!updateId) return null

  return (
    <Dialog open={isUpdateOpen} onOpenChange={closeUpdate}>
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle>Update {Feature}</DialogTitle>
        </DialogHeader>
        <Update{Feature}Form featureId={updateId} onSuccess={closeUpdate} />
      </DialogContent>
    </Dialog>
  )
}
```

Rules:
- `if (!updateId) return null` — always guard before rendering
- Form handles its own loading state internally — no spinner here

---

## File 6 — `dialogs/delete-{feature}-dialog.tsx`

```tsx
'use client'

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Spinner } from '@/components/ui/spinner'
import { use{Feature}DialogStore } from '@/features/{feature}/store'
import { useDelete{Feature} } from '@/features/{feature}/hooks'

export function Delete{Feature}Dialog() {
  const { isDeleteOpen, deleteId, closeDelete } = use{Feature}DialogStore()
  const deleteFeature = useDelete{Feature}()

  function handleDelete() {
    if (!deleteId) return
    deleteFeature.mutate(deleteId, {
      onSuccess: () => closeDelete(),
    })
  }

  return (
    <AlertDialog open={isDeleteOpen} onOpenChange={closeDelete}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete {Feature}</AlertDialogTitle>
          <AlertDialogDescription>
            Are you sure? This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="flex justify-end gap-3">
          <AlertDialogCancel disabled={deleteFeature.isPending}>
            Cancel
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleDelete}
            disabled={deleteFeature.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {deleteFeature.isPending ? <Spinner /> : 'Delete'}
          </AlertDialogAction>
        </div>
      </AlertDialogContent>
    </AlertDialog>
  )
}
```

Rules:
- `AlertDialog` not `Dialog` — always for destructive actions
- Cancel also `disabled` when `isPending`
- Delete button shows `<Spinner />` when `isPending`
- `handleDelete` guards `if (!deleteId) return`
- Closes only inside `onSuccess`

---

## File 7 — `dialogs/{feature}-details-dialog.tsx`

```tsx
'use client'

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Spinner } from '@/components/ui/spinner'
import { use{Feature}DialogStore } from '@/features/{feature}/store'
import { use{Feature} } from '@/features/{feature}/hooks'
import type { Feature } from '@/features/{feature}/schemas/{feature}.schema'

export function {Feature}DetailsDialog() {
  const { isDetailsOpen, detailsId, closeDetails } = use{Feature}DialogStore()
  const { data: feature, isLoading } = use{Feature}(detailsId ?? undefined)

  if (!detailsId) return null

  return (
    <Dialog open={isDetailsOpen} onOpenChange={closeDetails}>
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
                <Badge variant={feature.isActive ? 'default' : 'secondary'}>
                  {feature.isActive ? 'Active' : 'Inactive'}
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

Rules:
- `if (!detailsId) return null` — always guard
- Three states: loading → `<Spinner />` | data → details grid | no data → not found message
- Dates always `new Date(value).toLocaleDateString()` — never raw date string

---

## File 8 — `tables/{feature}-columns.tsx`

```tsx
'use client'

import { ColumnDef } from '@tanstack/react-table'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { DataTableColumnHeader } from '@/components/data-table/column-header'
import { MoreHorizontal, Eye, Edit, Trash2 } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { use{Feature}DialogStore } from '@/features/{feature}/store'
import type { Feature } from '@/features/{feature}/schemas/{feature}.schema'

export const featureColumns: ColumnDef<Feature>[] = [
  {
    id: 'select',
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
    accessorKey: 'name',
    size: 300,
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Name" />
    ),
    cell: ({ row }) => (
      <div className="font-medium">{row.getValue('name')}</div>
    ),
  },
  {
    accessorKey: 'isActive',
    size: 100,
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Status" />
    ),
    cell: ({ row }) => {
      const isActive = row.getValue('isActive') as boolean
      return (
        <Badge variant={isActive ? 'default' : 'secondary'}>
          {isActive ? 'Active' : 'Inactive'}
        </Badge>
      )
    },
  },
  {
    accessorKey: 'createdAt',
    size: 150,
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Created" />
    ),
    cell: ({ row }) => (
      <div className="text-sm text-muted-foreground">
        {new Date(row.getValue('createdAt')).toLocaleDateString()}
      </div>
    ),
  },
  {
    id: 'actions',
    size: 80,
    enableHiding: false,
    cell: ({ row }) => {
      const feature = row.original
      const { openDetails, openUpdate, openDelete } = use{Feature}DialogStore()

      return (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="h-8 w-8 p-0">
              <span className="sr-only">Open menu</span>
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => openDetails(feature.id)}>
              <Eye className="mr-2 h-4 w-4" />
              View Details
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => openUpdate(feature.id)}>
              <Edit className="mr-2 h-4 w-4" />
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

Rules:
- Column order: select → name → feature-specific fields → status → createdAt → actions
- Actions dropdown order: View Details → Edit → Delete
- Delete item always `text-destructive focus:text-destructive`
- `DataTableColumnHeader` on all sortable columns
- `aria-label` on both select checkboxes

---

## File 9 — `tables/{feature}-table.tsx`

```tsx
'use client'

import { useMemo } from 'react'
import { use{Feature}DataTable } from '@/features/{feature}/hooks'
import { featureColumns } from './{feature}-columns'
import dynamic from 'next/dynamic';

const DataTable = dynamic(
  () => import('@/components/data-table/data-table').then((mod) => mod.DataTable),
  { ssr: false }
);

export function {Feature}Table() {
  const exportConfig = useMemo(
    () => ({
      entityName: '{Features}',
      columnMapping: {
        id: 'ID',
        name: 'Name',
        isActive: 'Status',
        createdAt: 'Created',
      },
      columnWidths: [
        { wch: 10 },
        { wch: 30 },
        { wch: 12 },
        { wch: 15 },
      ],
      headers: ['ID', 'Name', 'Status', 'Created'],
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

Rules:
- `exportConfig` always in `useMemo`
- `fetchDataFn={use{Feature}DataTable}` — pass the hook directly, NOT a plain async function
- The hook must have `.isQueryHook = true` set in the hooks file — DataTable uses this flag to call it correctly as a hook
- `columnMapping` keys must match actual column `accessorKey` values
- No `useState`, no `useEffect`, no manual refresh logic needed — invalidation is handled automatically by TanStack Query through the hook

---

## File 10 — `pages/{feature}-list-page.tsx`

```tsx
'use client'

import { Button } from '@/components/ui/button'
import { use{Feature}DialogStore } from '@/features/{feature}/store'
import { {Feature}Table } from '../tables'
import {
  Create{Feature}Dialog,
  Update{Feature}Dialog,
  Delete{Feature}Dialog,
  {Feature}DetailsDialog,
} from '../dialogs'

export function {Feature}ListPage() {
  const { openCreate } = use{Feature}DialogStore()

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

      <Create{Feature}Dialog />
      <Update{Feature}Dialog />
      <Delete{Feature}Dialog />
      <{Feature}DetailsDialog />
    </div>
  )
}
```

Rules:
- Only reads `openCreate` from store — nothing else
- All four dialogs always mounted — they control own visibility via store
- Never fetch data in page

---

## Barrel Files

### `forms/index.ts`
```ts
export { Create{Feature}Form } from './create-{feature}-form'
export { Update{Feature}Form } from './update-{feature}-form'
```

No form-fields export.

### `dialogs/index.ts`
```ts
export { Create{Feature}Dialog } from './create-{feature}-dialog'
export { Update{Feature}Dialog } from './update-{feature}-dialog'
export { Delete{Feature}Dialog } from './delete-{feature}-dialog'
export { {Feature}DetailsDialog } from './{feature}-details-dialog'
```

### `tables/index.ts`
```ts
export { {Feature}Table } from './{feature}-table'
export { featureColumns } from './{feature}-columns'
```

### `pages/index.ts`
```ts
export { {Feature}ListPage } from './{feature}-list-page'
```

### `components/index.ts`
```ts
export * from './dialogs'
export * from './forms'
export * from './tables'
export * from './pages'
```

---

## Loading State Reference

```
update-form.tsx        isLoading = true   → centered <Spinner />
details-dialog.tsx     isLoading = true   → centered <Spinner />
create-form.tsx        isPending = true   → <Spinner /> inside submit button
update-form.tsx        isPending = true   → <Spinner /> inside submit button
delete-dialog.tsx      isPending = true   → <Spinner /> inside delete button
foreign key Select     isLoading = true   → <Spinner /> inside SelectContent
{feature}-table.tsx                       → DataTable handles internally via hook
```

---

## Why No Shared Form Fields

```
CreateDto  = { name: string; isActive: boolean }         ← required fields
UpdateDto  = { id: number; name?: string; isActive?: boolean }  ← all optional

UseFormReturn<CreateDto>  ≠  UseFormReturn<UpdateDto>
```

Passing a form typed as `UseFormReturn<UpdateDto>` to a component expecting `UseFormReturn<CreateDto>` causes TS error 2322 — `string | undefined` is not assignable to `string`. The fields are optional in update but required in create.

The fix is to never share form fields between create and update forms. Each form owns its fields inline, typed to its own DTO. Duplication of field markup is intentional and correct.

---

## Common Mistakes — Never Do These

- Never create a shared form-fields component — always inline fields in each form
- Never type update form with `UseFormReturn<CreateFeatureDto>` — always `UseFormReturn<UpdateFeatureDto>`
- Never add `zodResolver` to update form — update fields are optional, resolver causes false validation errors
- Never use loading text — always `<Spinner />`
- Never call actions directly from components — always through mutation hooks
- Never render update dialog without `if (!updateId) return null` guard
- Never render details dialog without `if (!detailsId) return null` guard
- Never forget `form.reset()` after successful create
- Never use `Dialog` for delete confirmation — always `AlertDialog`
- Never forget `disabled={isPending}` on cancel button in delete dialog
- Never pass a plain async function as `fetchDataFn` — always pass the `use{Feature}DataTable` hook
- Never forget that the hook must have `.isQueryHook = true` set in the hooks file — without it DataTable won't call it as a hook and the table won't refresh after mutations
- Never skip `useMemo` on `exportConfig`
- Never use any UI library other than shadcn
- Never add manual `refetchKey`, `useState`, or cache subscription logic in the table component — the hook + `.isQueryHook = true` pattern handles all of that automatically
- Never render a foreign key field as a plain `<Input>` — always a `<Select>` with the related feature's options hook
- Never pass dropdown options as props into a form — always call `use{RelatedFeature}Options` directly inside the form
- Never import another feature's hooks anywhere except `use{Feature}Options` inside a form component
- Never forget to handle all three Select states: loading (`<Spinner />`), empty (muted message), populated (mapped options)
- Never forget to pre-populate foreign key Select value in the update form's `useEffect` reset