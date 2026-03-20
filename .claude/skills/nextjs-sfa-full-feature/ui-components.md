# UI Components

## Sections
- Form Component
- Columns
- Dialogs
- Table Component
- List Page
- App Route
- Barrel Exports

---

## Form Component

**File:** `components/forms/{entity}-form.tsx`

One component handles both `create` and `edit` modes. API field errors arrive as props and are wired into the form via `useEffect` + `setError`.

```typescript
'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  create{Entity}Schema,
  update{Entity}Schema,
  type Create{Entity}Input,
} from '../../schema/{entity}.schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { Spinner } from '@/components/ui/spinner'

interface {Entity}FormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<Create{Entity}Input>
  onSubmit: (data: Create{Entity}Input) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function {Entity}Form({ mode, defaultValues, onSubmit, isLoading, fieldErrors }: {Entity}FormProps) {
  const schema = mode === 'create' ? create{Entity}Schema : update{Entity}Schema

  const form = useForm<Create{Entity}Input>({
    resolver: zodResolver(schema as typeof create{Entity}Schema),
    defaultValues: { name: '', ...defaultValues },
  })

  // Apply server-side field errors into form state
  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        form.setError(field as keyof Create{Entity}Input, { message })
      })
    }
  }, [fieldErrors, form.setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
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
        {/* repeat FormField for each editable field */}

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === 'create' ? (
            'Create {Entity}'
          ) : (
            'Update {Entity}'
          )}
        </Button>
      </form>
    </Form>
  )
}
```

**Enum field → Select:**
```typescript
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

// Inside FormField render prop:
<Select onValueChange={field.onChange} defaultValue={field.value}>
  <SelectTrigger><SelectValue placeholder="Select..." /></SelectTrigger>
  <SelectContent>
    <SelectItem value="Value1">Display Label 1</SelectItem>
    <SelectItem value="Value2">Display Label 2</SelectItem>
  </SelectContent>
</Select>
```

---

## Columns

**File:** `components/columns/{entity}-columns.tsx`

Factory function — takes action handlers as argument so columns are stable references.

```typescript
'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { {Entity}Dto } from '../types/{entity}.types'

export interface {Entity}ColumnActions {
  openEdit: (id: number) => void
  openDelete: (id: number) => void
}

export function get{Entity}Columns(actions: {Entity}ColumnActions): ColumnDef<{Entity}Dto>[] {
  const { openEdit, openDelete } = actions

  return [
    {
      accessorKey: 'name',
      header: 'Name',
      cell: ({ row }) => <span className="text-sm font-medium">{row.original.name}</span>,
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <Badge variant={row.original.isActive ? 'default' : 'secondary'}>
          {row.original.isActive ? 'Active' : 'Inactive'}
        </Badge>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <MoreHorizontal className="h-4 w-4" />
              <span className="sr-only">Open menu</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => openEdit(row.original.id)}>Edit</DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onClick={() => openDelete(row.original.id)}
              className="text-destructive focus:text-destructive"
            >
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    },
  ]
}
```

**Combined cell (name + secondary field):**
```typescript
{
  id: 'nameEmail',
  header: '{Entity}',
  cell: ({ row }) => (
    <div className="flex items-center gap-3">
      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold">
        {row.original.name.substring(0, 2).toUpperCase()}
      </div>
      <div>
        <div className="text-sm font-medium">{row.original.name}</div>
        <div className="text-xs text-muted-foreground">{row.original.email}</div>
      </div>
    </div>
  ),
},
```

---

## Dialogs

**File:** `components/dialogs/{entity}-dialogs.tsx`

Three private dialog components + one combined export. Create/Edit use `Dialog`; Delete uses `AlertDialog`.

```typescript
'use client'

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Spinner } from '@/components/ui/spinner'
import { useCreateDialog, useEditDialog, useDeleteDialog } from '../../store'
import {
  useCreate{Entity},
  useUpdate{Entity},
  useDelete{Entity},
  use{Entity},
} from '../../hooks/{entity}.hooks'
import { {Entity}Form } from '../forms/{entity}-form'
import type { Create{Entity}Input, Update{Entity}Input } from '../../schema/{entity}.schema'

function Create{Entity}Dialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreate{Entity}()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Create {Entity}</DialogTitle>
          <DialogDescription>Add a new {entity} to the system.</DialogDescription>
        </DialogHeader>
        <{Entity}Form
          mode="create"
          onSubmit={(data) => mutate(data as Create{Entity}Input)}
          isLoading={isPending}
          fieldErrors={fieldErrors}
        />
      </DialogContent>
    </Dialog>
  )
}

function Edit{Entity}Dialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: {entity}, isLoading: isFetching } = use{Entity}(selectedId)
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useUpdate{Entity}()

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) { close(); clearFieldErrors() }
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit {Entity}</DialogTitle>
          <DialogDescription>Update {entity} information.</DialogDescription>
        </DialogHeader>
        {isFetching ? (
          <div className="flex items-center justify-center py-8">
            <Spinner className="size-6" />
          </div>
        ) : (
          <{Entity}Form
            mode="edit"
            defaultValues={{entity}}
            onSubmit={(data) => {
              if (!selectedId) return
              mutate({ id: selectedId, data: data as Update{Entity}Input })
            }}
            isLoading={isPending}
            fieldErrors={fieldErrors}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

function Delete{Entity}Dialog() {
  const { isOpen, selectedId, close } = useDeleteDialog()
  const { mutate, isPending } = useDelete{Entity}()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete {Entity}</AlertDialogTitle>
          <AlertDialogDescription>
            This action cannot be undone. The {entity} will be permanently removed.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Delete
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// Single export — renders all three dialogs
export function {Entity}Dialogs() {
  return (
    <>
      <Create{Entity}Dialog />
      <Edit{Entity}Dialog />
      <Delete{Entity}Dialog />
    </>
  )
}
```

---

## Table Component

**File:** `components/table/{entity}-table.tsx`

```typescript
'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import { useEditDialog, useDeleteDialog, use{Entity}DialogStore } from '../../store'
import { use{Entity}DataTable } from '../../hooks/{entity}.hooks'
import { get{Entity}Columns } from '../columns/{entity}-columns'

export function {Entity}Table() {
  const openCreate = use{Entity}DialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()

  // useCallback prevents unnecessary re-renders — columns are stable
  const getColumns = useCallback(
    (_handleRowDeselection: ((rowId: string) => void) | null | undefined) =>
      get{Entity}Columns({ openEdit, openDelete }),
    [openEdit, openDelete]
  )

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: '{entity}s-table',
        searchPlaceholder: 'Search {entity}s...',
      }}
      getColumns={getColumns}
      fetchDataFn={use{Entity}DataTable as any}
      exportConfig={{
        entityName: '{entity}s',
        columnMapping: { name: 'Name' },
        columnWidths: [{ wch: 25 }],
        headers: ['Name'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add {Entity}
        </Button>
      )}
    />
  )
}
```

---

## List Page

**File:** `components/pages/{entity}-list-page.tsx`

```typescript
'use client'

import { {Entity}Table } from '../table/{entity}-table'
import { {Entity}Dialogs } from '../dialogs/{entity}-dialogs'

export function {Entity}ListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{Entity} Management</h1>
          <p className="text-muted-foreground">Manage your {entity} records</p>
        </div>
      </div>
      <{Entity}Table />
      <{Entity}Dialogs />
    </div>
  )
}
```

---

## App Route

**File:** `app/(protected)/{entity}s/page.tsx`

```typescript
'use client'

import dynamic from 'next/dynamic'

// ssr: false prevents Radix UI hydration mismatch:
// Radix useId() generates different aria-controls between server and client renders.
// The DataTable and its children (DataTableViewOptions) use Radix internally.
// Skipping SSR for this page avoids the mismatch entirely.
const {Entity}ListPage = dynamic(
  () => import('@/features/{entity}/components').then((m) => ({ default: m.{Entity}ListPage })),
  { ssr: false }
)

export default function {Entity}sPage() {
  return <{Entity}ListPage />
}
```

**Never** import and render `{Entity}ListPage` directly — always use `dynamic`.

The `.then((m) => ({ default: m.{Entity}ListPage }))` pattern is required because the components barrel uses **named** exports.

---

## Barrel Exports

**`components/index.ts`** — named exports only:
```typescript
export { {Entity}ListPage } from './pages/{entity}-list-page'
```

**`components/types/{entity}.types.ts`**:
```typescript
export type { {Entity}Dto } from '../../schema/{entity}.schema'

export type {Entity}TableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
}
```
