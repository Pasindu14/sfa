---
name: nextjs-sfa-full-feature
description: Generates a complete, production-ready Next.js feature for the SFA web app based on existing API endpoints. Use this skill whenever the user asks to add a frontend feature, create a web feature, build UI for an API, generate Next.js components for any entity (like distributors, customers, products, orders), or wants to scaffold the complete frontend for an existing backend feature. Trigger on phrases like "create frontend for X", "add web feature for X", "build UI for X API", "generate Next.js feature for X", or when the user wants to connect the web app to an existing API endpoint. This skill creates all required files following the exact Users feature pattern including schema, actions, hooks, stores, components, forms, columns, table, dialogs, and page - with proper error handling, field-level validation, toast notifications, TanStack Query integration, and Zustand stores.
compatibility:
  required_tools:
    - Read
    - Write
    - Edit
    - Glob
    - Grep
    - Bash
---

# Next.js SFA Full Feature Generator

This skill generates a complete, production-ready Next.js feature for the SFA web app that connects to an existing API endpoint. It follows the exact architectural patterns from the `features/user/` feature, ensuring consistency across the codebase.

## What This Skill Creates

For a given entity (e.g., "distributor", "customer", "product"), this skill generates:

1. **Schema file** (`schema/{entity}.schema.ts`) - Zod validation schemas and TypeScript types
2. **Actions file** (`actions/{entity}.actions.ts`) - Server actions using `createAction` wrapper
3. **Hooks file** (`hooks/{entity}.hooks.ts`) - TanStack Query hooks with proper error handling
4. **Store files** (`store/`) - Zustand dialog and filter stores with barrel exports
5. **Form component** (`components/forms/{entity}-form.tsx`) - React Hook Form with field-level errors
6. **Columns** (`components/columns/{entity}-columns.tsx`) - TanStack Table column definitions
7. **Table component** (`components/table/{entity}-table.tsx`) - DataTable integration
8. **Dialogs** (`components/dialogs/{entity}-dialogs.tsx`) - Create, edit, delete dialogs
9. **List page** (`components/pages/{entity}-list-page.tsx`) - Main page component
10. **Barrel exports** - Proper index.ts files for clean imports

## How to Use This Skill

### Step 1: Gather Information

Before generating code, collect the following information from the user:

1. **Entity name** (singular, e.g., "distributor", "customer", "product")
2. **API base path** (e.g., `/api/v1/distributors`)
3. **Field definitions** - For each field in the entity:
   - Field name
   - Field type (string, number, boolean, date, enum)
   - Validation rules (required, min/max length, regex, etc.)
   - Whether it's optional
   - For enums: the possible values
4. **Display fields** - Which fields should show in the table
5. **Form fields** - Which fields are editable (create vs edit might differ)
6. **Special actions** (optional) - Beyond CRUD (e.g., "activate", "deactivate", "approve")
7. **Custom filters** (optional) - Additional filters beyond search (e.g., role, status)

### Step 2: Analyze the API Response

Read the existing API endpoint or DTO to understand:
- Response structure
- Pagination format
- Field types and constraints
- Any nested objects or enums

If the API doesn't exist yet, inform the user they should create it first using the `sfa-feature-generator` skill.

### Step 3: Generate the Feature

Generate all files in the correct order, following the patterns exactly as shown in the reference implementation.

## File Generation Patterns

### 1. Schema File Pattern

Location: `features/{entity}/schema/{entity}.schema.ts`

```typescript
import { z } from 'zod'

// Define enums if needed
export const statusEnum = z.enum(['Active', 'Inactive'])

// Define reusable validation rules
const emailRules = z.string().email('Invalid email format')
const phoneRules = z.string().min(10).regex(/^[0-9+\-\s()]+$/)

// Create schema (all fields needed for creation)
export const create{Entity}Schema = z.object({
  name: z.string().min(1, 'Name is required'),
  email: emailRules,
  // ... other fields
})

// Update schema (usually same as create, minus password-like fields)
export const update{Entity}Schema = z.object({
  name: z.string().min(1, 'Name is required'),
  email: emailRules,
  // ... other fields
})

// Filter schema (for search and pagination)
export const filterSchema = z.object({
  search: z.string().optional(),
  status: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

// Infer TypeScript types from schemas
export type Create{Entity}Input = z.infer<typeof create{Entity}Schema>
export type Update{Entity}Input = z.infer<typeof update{Entity}Schema>
export type {Entity}FilterInput = z.infer<typeof filterSchema>

// DTO type (matches API response)
export type {Entity}Dto = {
  id: number
  name: string
  email: string
  // ... all fields from API
  isActive: boolean
  createdAt: string
  updatedAt: string
}
```

**Key points:**
- Always use Zod for validation, never write TypeScript types manually
- Use `z.infer<>` for all type derivations
- Create separate schemas for create, update, and filter operations
- Include helpful error messages in validation rules
- Match DTO exactly to API response structure

### 2. Actions File Pattern

Location: `features/{entity}/actions/{entity}.actions.ts`

```typescript
'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  Create{Entity}Input,
  Update{Entity}Input,
  {Entity}Dto,
} from '../schema/{entity}.schema'

type {Entity}sListResponse = {
  {entity}s: {Entity}Dto[]
  totalCount: number
  page: number
  pageSize: number
}

export const get{Entity}sAction = createAction(
  { name: 'get{Entity}sAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/{entity}s', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as {Entity}sListResponse
  }
)

export const get{Entity}ByIdAction = createAction(
  { name: 'get{Entity}ByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/{entity}s/${id}`)
    return res.data.data as {Entity}Dto
  }
)

export const create{Entity}Action = createAction(
  { name: 'create{Entity}Action', requireAuth: true, requiredRole: 'Admin' },
  async (data: Create{Entity}Input) => {
    const res = await client.post('/api/v1/{entity}s', data)
    revalidatePath('/{entity}s')
    return res.data.data as {Entity}Dto
  }
)

export const update{Entity}Action = createAction(
  { name: 'update{Entity}Action', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: Update{Entity}Input) => {
    const res = await client.put(`/api/v1/{entity}s/${id}`, data)
    revalidatePath('/{entity}s')
    return res.data.data as {Entity}Dto
  }
)

export const delete{Entity}Action = createAction(
  { name: 'delete{Entity}Action', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/{entity}s/${id}`)
    revalidatePath('/{entity}s')
  }
)
```

**Key points:**
- Always use `'use server'` directive at the top
- Use `createAction` wrapper, never manual try/catch
- Import `client` as default from `@/lib/api/client`
- Use `revalidatePath` after mutations
- Type responses correctly (ListResponse vs single DTO)
- Don't add auth headers manually - the client interceptor handles it

### 3. Hooks File Pattern

Location: `features/{entity}/hooks/{entity}.hooks.ts`

```typescript
'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  get{Entity}sAction,
  get{Entity}ByIdAction,
  create{Entity}Action,
  update{Entity}Action,
  delete{Entity}Action,
} from '../actions/{entity}.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { Create{Entity}Input, Update{Entity}Input } from '../schema/{entity}.schema'

// --- Query key factory ---

export const {entity}Keys = {
  all: ['{entity}s'] as const,
  lists: () => [...{entity}Keys.all, 'list'] as const,
  list: (filters: object) => [...{entity}Keys.lists(), filters] as const,
  details: () => [...{entity}Keys.all, 'detail'] as const,
  detail: (id: number) => [...{entity}Keys.details(), id] as const,
}

// --- Query options factory ---

export function {entity}QueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: {entity}Keys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await get{Entity}sAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function use{Entity}s(page: number, pageSize: number) {
  return useQuery({entity}QueryOptions(page, pageSize))
}

export function use{Entity}(id: number | null) {
  return useQuery({
    queryKey: {entity}Keys.detail(id!),
    queryFn: async () => {
      const result = await get{Entity}ByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook ---
// Uses server-side pagination + search — the API does all filtering.
// Never filter client-side here; pass search and customFilters to the action.

export function use{Entity}DataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: Record<string, unknown>,
) {
  return useQuery({
    queryKey: {entity}Keys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      // Pass search and any customFilters directly to the action — let the API filter
      const result = await get{Entity}sAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { {entity}s, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: {entity}s,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    placeholderData: keepPreviousData,
  })
}

;(use{Entity}DataTable as any).isQueryHook = true

// --- Mutation hooks ---

export function useCreate{Entity}() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: Create{Entity}Input) => {
      const result = await create{Entity}Action(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: {entity}Keys.all })
      setFieldErrors(null)
      close()
      toast.success('{Entity} created successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, '{entity}', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdate{Entity}() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: Update{Entity}Input }) => {
      const result = await update{Entity}Action(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: {entity}Keys.all })
      setFieldErrors(null)
      close()
      toast.success('{Entity} updated successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, '{entity}', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDelete{Entity}() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await delete{Entity}Action(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: {entity}Keys.all })
      close()
      toast.success('{Entity} deleted successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, '{entity}', 'delete')
    },
  })
}
```

**Key points:**
- Always include `'use client'` directive
- Use plain array for `all` key (not a function)
- Include all 8 parameters for DataTable hook (even if unused, prefix with `_`)
- Set `isQueryHook = true` flag outside function body
- Mutation hooks spread `...mutation` and add `fieldErrors` + `clearFieldErrors`
- Throw the entire `result` object on failure (not a new Error)
- Use `handleErrorToast` for consistent error messaging

### 4. Store Files Pattern

#### Dialog Store

Location: `features/{entity}/store/{entity}.dialog-store.ts`

```typescript
import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface {Entity}DialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  selected{Entity}Id: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
}

export const use{Entity}DialogStore = create<{Entity}DialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      selected{Entity}Id: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selected{Entity}Id: id }),
      closeEdit: () => set({ isEditOpen: false, selected{Entity}Id: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selected{Entity}Id: id }),
      closeDelete: () => set({ isDeleteOpen: false, selected{Entity}Id: null }),
    }),
    { name: '{Entity}DialogStore' }
  )
)
```

#### Filter Store

Location: `features/{entity}/store/{entity}.filter-store.ts`

```typescript
import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface {Entity}FilterState {
  search: string
  page: number
  pageSize: number
  sortBy: string
  sortOrder: 'asc' | 'desc'
  setSearch: (search: string) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  setSortBy: (sortBy: string) => void
  setSortOrder: (sortOrder: 'asc' | 'desc') => void
  resetFilters: () => void
}

const defaultState = {
  search: '',
  page: 1,
  pageSize: 10,
  sortBy: '',
  sortOrder: 'asc' as const,
}

export const use{Entity}FilterStore = create<{Entity}FilterState>()(
  devtools(
    (set) => ({
      ...defaultState,
      setSearch: (search) => set({ search, page: 1 }),
      setPage: (page) => set({ page }),
      setPageSize: (pageSize) => set({ pageSize, page: 1 }),
      setSortBy: (sortBy) => set({ sortBy }),
      setSortOrder: (sortOrder) => set({ sortOrder }),
      resetFilters: () => set(defaultState),
    }),
    { name: '{Entity}FilterStore' }
  )
)
```

#### Store Barrel

Location: `features/{entity}/store/index.ts`

```typescript
import { useShallow } from 'zustand/react/shallow'
import { use{Entity}DialogStore } from './{entity}.dialog-store'
import { use{Entity}FilterStore } from './{entity}.filter-store'

export { use{Entity}DialogStore }

// --- Dialog selectors ---

export const useCreateDialog = () =>
  use{Entity}DialogStore(
    useShallow((s) => ({
      isOpen: s.isCreateOpen,
      open: s.openCreate,
      close: s.closeCreate,
    }))
  )

export const useEditDialog = () =>
  use{Entity}DialogStore(
    useShallow((s) => ({
      isOpen: s.isEditOpen,
      selectedId: s.selected{Entity}Id,
      open: s.openEdit,
      close: s.closeEdit,
    }))
  )

export const useDeleteDialog = () =>
  use{Entity}DialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selected{Entity}Id,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

// --- Filter selectors ---

export const use{Entity}Filters = () =>
  use{Entity}FilterStore(
    useShallow((s) => ({
      search: s.search,
      page: s.page,
      pageSize: s.pageSize,
      sortBy: s.sortBy,
      sortOrder: s.sortOrder,
      setSearch: s.setSearch,
      setPage: s.setPage,
      setPageSize: s.setPageSize,
      setSortBy: s.setSortBy,
      setSortOrder: s.setSortOrder,
      resetFilters: s.resetFilters,
    }))
  )
```

**Key points:**
- Use Zustand v5 with double-parentheses create syntax: `create<T>()(devtools(...))`
- Import `useShallow` from `'zustand/react/shallow'`
- Store uses primitive state, barrel exports composite selector hooks
- Each dialog hook returns `{ isOpen, open, close }` (plus `selectedId` for edit/delete)
- Dialog stores track which dialog is open and selected ID
- Filter stores handle search, pagination, and sorting state

### 5. Form Component Pattern

Location: `features/{entity}/components/forms/{entity}-form.tsx`

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
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Spinner } from '@/components/ui/spinner'

interface {Entity}FormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<Create{Entity}Input>
  onSubmit: (data: Create{Entity}Input) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function {Entity}Form({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: {Entity}FormProps) {
  const schema = mode === 'create' ? create{Entity}Schema : update{Entity}Schema

  const form = useForm<Create{Entity}Input>({
    resolver: zodResolver(schema as typeof create{Entity}Schema),
    defaultValues: {
      name: '',
      email: '',
      // ... other fields with defaults
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof Create{Entity}Input, { message })
      })
    }
  }, [fieldErrors, setError])

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

        {/* Add more FormFields for each editable field */}

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

**Key points:**
- One form component handles both create and edit modes
- Use different schemas based on mode
- Field errors come from props and are applied via `useEffect`
- Use `Spinner` from `@/components/ui/spinner` for loading states
- Each field uses `FormField` with proper labels and error messages

### 6. Columns Pattern

Location: `features/{entity}/components/columns/{entity}-columns.tsx`

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
  // Add other action handlers as needed
}

export function get{Entity}Columns(actions: {Entity}ColumnActions): ColumnDef<{Entity}Dto>[] {
  const { openEdit, openDelete } = actions

  return [
    {
      id: 'nameEmail',
      header: '{Entity}',
      cell: ({ row }) => {
        const { name, email } = row.original
        return (
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {name.substring(0, 2).toUpperCase()}
            </div>
            <div>
              <div className="text-sm font-medium">{name}</div>
              <div className="text-xs text-muted-foreground">{email}</div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'phone',
      header: 'Phone',
      cell: ({ row }) => <span className="text-sm text-muted-foreground">{row.original.phone}</span>,
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
      cell: ({ row }) => {
        const item = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openEdit(item.id)}>Edit</DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={() => openDelete(item.id)}
                className="text-destructive focus:text-destructive"
              >
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
```

**Key points:**
- Export a factory function that takes action handlers
- Use `ColumnDef<{Entity}Dto>[]` as return type
- Combine related fields into single columns when appropriate
- Use Badge components for status/enum fields
- Actions column always uses DropdownMenu with MoreHorizontal icon

### 7. Dialogs Pattern

Location: `features/{entity}/components/dialogs/{entity}-dialogs.tsx`

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
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
} from '../../store'
import {
  useCreate{Entity},
  useUpdate{Entity},
  useDelete{Entity},
  use{Entity},
} from '../../hooks/{entity}.hooks'
import { {Entity}Form } from '../forms/{entity}-form'
import type { Create{Entity}Input, Update{Entity}Input } from '../../schema/{entity}.schema'

// --- Create Dialog ---

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

// --- Edit Dialog ---

function Edit{Entity}Dialog() {
  const { isOpen, selectedId, close } = useEditDialog()
  const { data: {entity}, isLoading: isLoading{Entity} } = use{Entity}(selectedId)
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
        {isLoading{Entity} ? (
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

// --- Delete Dialog ---

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

// --- Combined Export ---

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

**Key points:**
- Create/Edit use `Dialog`, Delete uses `AlertDialog`
- Clear field errors when dialog closes
- Edit dialog loads data using `use{Entity}(selectedId)` and shows `Spinner` while loading
- Delete uses destructive styling
- Combined export renders all dialogs together

### 8. Table Component Pattern

Location: `features/{entity}/components/table/{entity}-table.tsx`

```typescript
'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import {
  useEditDialog,
  useDeleteDialog,
  use{Entity}DialogStore,
} from '../../store'
import { use{Entity}DataTable } from '../../hooks/{entity}.hooks'
import { get{Entity}Columns } from '../columns/{entity}-columns'

export function {Entity}Table() {
  const openCreate = use{Entity}DialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()

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
        columnMapping: {
          name: 'Name',
          email: 'Email',
          // ... map other fields
        },
        columnWidths: [{ wch: 25 }, { wch: 25 }],
        headers: ['Name', 'Email'],
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

**Key points:**
- Use `useCallback` for `getColumns` to prevent unnecessary re-renders
- Pass `use{Entity}DataTable as any` to `fetchDataFn`
- Configure DataTable with appropriate settings
- Render "Add {Entity}" button in toolbar using `openCreate` from dialog store

### 9. List Page Pattern

Location: `features/{entity}/components/pages/{entity}-list-page.tsx`

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

**Key points:**
- Simple, clean structure
- Header with title and description
- Table and dialogs rendered separately
- Use `'use client'` directive

### 10. App Route Page Pattern

Location: `app/(protected)/{entity}s/page.tsx`

```typescript
'use client'

import dynamic from 'next/dynamic'

const {Entity}ListPage = dynamic(
  () => import('@/features/{entity}/components').then((m) => ({ default: m.{Entity}ListPage })),
  { ssr: false }
)

export default function {Entity}sPage() {
  return <{Entity}ListPage />
}
```

**Key points:**
- Always use `dynamic` with `{ ssr: false }` — NEVER import and render the list page directly
- This prevents React hydration mismatches caused by Radix UI's `useId()` generating different `aria-controls` IDs between server and client renders
- The DataTable component (and its children like `DataTableViewOptions`) uses Radix internally; if client-side state (column visibility, page size) differs between SSR and hydration, IDs shift and React throws a hydration error
- `ssr: false` skips server rendering for the interactive table entirely — the page shell and layout still SSR correctly
- The `.then((m) => ({ default: m.{Entity}ListPage }))` pattern is required because the components barrel uses named exports

### 11. Barrel Exports

#### Components Index

Location: `features/{entity}/components/index.ts`

```typescript
export { {Entity}ListPage } from './pages/{entity}-list-page'
```

#### Types File

Location: `features/{entity}/components/types/{entity}.types.ts`

```typescript
export type { {Entity}Dto } from '../../schema/{entity}.schema'

export type {Entity}TableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
}
```

## Implementation Workflow

When the user requests a new feature:

1. **Confirm the entity details** - Ask about field names, types, validations
2. **Check if API exists** - Verify the backend endpoint is ready
3. **Create directory structure**:
   ```
   features/{entity}/
   ├── schema/
   ├── actions/
   ├── hooks/
   ├── store/
   └── components/
       ├── forms/
       ├── columns/
       ├── table/
       ├── dialogs/
       ├── pages/
       └── types/
   ```
4. **Generate files in order**:
   - Schema first (needed by all other files)
   - Actions (needed by hooks)
   - Stores (needed by hooks and components)
   - Hooks (needed by components)
   - Types
   - Forms
   - Columns
   - Dialogs
   - Table
   - List Page
   - Barrel exports
5. **Verify imports** - Ensure all imports use correct paths
6. **Test the feature** - Suggest the user test create, read, update, delete operations

## Common Customizations

### Adding Custom Actions

For actions beyond CRUD (e.g., "activate", "deactivate", "approve"):

1. Add state to dialog store
2. Add action to actions file
3. Add mutation hook
4. Add dialog component
5. Add menu item to columns dropdown
6. Export selector hook from store barrel

### Adding Custom Filters

For filters beyond search (e.g., status, category):

1. Add state to filter store
2. Update DataTable hook to use the filter
3. Add `renderCustomFilters` prop to DataTable in table component

### Handling Enums

For enum fields (status, role, etc.):

1. Define Zod enum in schema
2. Use `Select` component in form
3. Map enum values to display labels in columns
4. Use Badge component for visual distinction

## Quality Checklist

Before completing, verify:

- [ ] All imports use correct paths (no broken imports)
- [ ] Schema defines all fields with proper validation
- [ ] Actions use `createAction` wrapper (no manual try/catch)
- [ ] Hooks include all 8 DataTable parameters
- [ ] `isQueryHook` flag set on DataTable hook
- [ ] Mutation hooks expose `fieldErrors` and `clearFieldErrors`
- [ ] Form includes `useEffect` to apply field errors
- [ ] Stores use Zustand v5 syntax with devtools
- [ ] Store barrel uses `useShallow` from correct import
- [ ] Dialogs clear field errors on close
- [ ] All files have proper `'use client'` or `'use server'` directives
- [ ] Column definitions typed correctly
- [ ] Success toasts have descriptive messages
- [ ] Error handling uses `handleErrorToast`
- [ ] Spinner used for loading states (not text)
- [ ] App route page uses `dynamic(..., { ssr: false })` — never a direct import

## Error Handling Philosophy

This codebase uses a layered error handling approach:

1. **API Client** - Intercepts all responses, normalizes errors into `ApiError` with `code`, `error`, and `fields`
2. **Actions** - `createAction` wrapper catches errors and wraps them in `ActionResponse`
3. **Hooks** - Mutations throw the full `ActionResponse` object (not a new Error)
4. **Components** - Display field errors inline, use `handleErrorToast` for general errors

Never write manual try/catch in actions or hooks. Trust the wrapper and interceptors to handle errors consistently.

## Common Pitfalls to Avoid

1. **Don't use `apiClient`** - Import is `client` (default export), not `apiClient`
2. **Don't use `ActionResult`** - Type is `ActionResponse` from `@/lib/types/actions`
3. **Don't call `all()` on query keys** - `userKeys.all` is already an array
4. **Don't omit DataTable parameters** - All 8 are required for compatibility
5. **Don't forget `'use client'`** - Hooks files need this directive
6. **Don't import useShallow from wrong path** - Use `'zustand/react/shallow'`
7. **Don't create separate type files for schemas** - Always use `z.infer<>`
8. **Don't use loading text** - Always use `Spinner` component
9. **Don't write custom error toasts** - Use `handleErrorToast` utility
10. **Don't forget revalidatePath** - Required after all mutations
11. **Don't import ListPage directly in app route** - Always use `dynamic(..., { ssr: false })` to prevent Radix UI hydration mismatches
12. **Don't filter client-side in the DataTable hook** - Always pass `search` to the action and let the API filter. Client-side `.filter()` breaks pagination (total_items becomes wrong) and makes the search box appear to do nothing on the current page
13. **Don't send `search: undefined` as a query param** - Use `search: search || undefined` so axios omits the param entirely when the search string is empty

## Summary

This skill creates a complete, battle-tested Next.js feature following the exact patterns used in production SFA code. It handles:

- ✅ Type-safe schemas with Zod
- ✅ Server actions with proper auth
- ✅ TanStack Query with optimistic updates
- ✅ Field-level error handling
- ✅ Zustand stores for UI state
- ✅ Responsive shadcn components
- ✅ Consistent error messages
- ✅ Loading states with spinners
- ✅ Success/error toasts
- ✅ DataTable integration

By following these patterns exactly, the generated code will integrate seamlessly with the existing SFA web application and maintain consistency across all features.
