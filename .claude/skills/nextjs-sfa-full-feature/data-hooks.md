# Data Hooks

**Location:** `features/{entity}/hooks/{entity}.hooks.ts`
**Directive:** `'use client'` at top of file

## Sections
- Query Key Factory
- DataTable Hook
- Single-Entity Hook
- Mutation Hooks

---

## Query Key Factory

```typescript
export const {entity}Keys = {
  all: ['{entity}s'] as const,               // plain array — never call .all()
  lists: () => [...{entity}Keys.all, 'list'] as const,
  list: (filters: object) => [...{entity}Keys.lists(), filters] as const,
  details: () => [...{entity}Keys.all, 'detail'] as const,
  detail: (id: number) => [...{entity}Keys.details(), id] as const,
}
```

---

## DataTable Hook

DataTable calls this via `fetchDataFn`. Three hard rules:
1. Exactly 8 parameters — prefix unused ones with `_`
2. `.isQueryHook = true` set **outside** the function body (after it)
3. Never filter client-side — pass `search` to the action; let the API filter

```typescript
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { get{Entity}sAction } from '../actions/{entity}.actions'

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

// Must be outside the function — DataTable checks this flag to distinguish query hooks
;(use{Entity}DataTable as any).isQueryHook = true
```

---

## Single-Entity Hook

Used by the Edit dialog to pre-populate form fields.

```typescript
import { useQuery } from '@tanstack/react-query'
import { get{Entity}ByIdAction } from '../actions/{entity}.actions'

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
```

---

## Mutation Hooks

All mutations share these SFA-specific behaviours:
- Throw the full `result` object (not `new Error(result.error)`) so `result.fields` reaches `onError`
- Invalidate `{entity}Keys.all` on success — this refetches all list queries
- Call `close()` from the matching dialog store selector
- Expose `fieldErrors` + `clearFieldErrors` for the form component

```typescript
import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { useCreateDialog, useEditDialog, useDeleteDialog } from '../store'
import {
  create{Entity}Action,
  update{Entity}Action,
  delete{Entity}Action,
} from '../actions/{entity}.actions'
import type { Create{Entity}Input, Update{Entity}Input } from '../schema/{entity}.schema'

export function useCreate{Entity}() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: Create{Entity}Input) => {
      const result = await create{Entity}Action(data)
      if (!result.success) throw result          // throw result object, not new Error
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
