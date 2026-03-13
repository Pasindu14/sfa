# Example: Category Feature (minimal)

A complete minimal feature — `name` and `description`. No enums, no custom actions beyond CRUD.

---

## schema/category.schema.ts

```typescript
import { z } from 'zod'

export const createCategorySchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
  description: z.string().max(500, 'Description must not exceed 500 characters').optional(),
})

export const updateCategorySchema = createCategorySchema

export const filterSchema = z.object({
  search: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateCategoryInput = z.infer<typeof createCategorySchema>
export type UpdateCategoryInput = z.infer<typeof updateCategorySchema>
export type CategoryFilterInput = z.infer<typeof filterSchema>

export type CategoryDto = {
  id: number
  name: string
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}
```

---

## actions/category.actions.ts

```typescript
'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { CreateCategoryInput, UpdateCategoryInput, CategoryDto } from '../schema/category.schema'

type CategoriesListResponse = {
  categories: CategoryDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getCategoriesAction = createAction(
  { name: 'getCategoriesAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/categories', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as CategoriesListResponse
  }
)

export const getCategoryByIdAction = createAction(
  { name: 'getCategoryByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/categories/${id}`)
    return res.data.data as CategoryDto
  }
)

export const createCategoryAction = createAction(
  { name: 'createCategoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateCategoryInput) => {
    const res = await client.post('/api/v1/categories', data)
    revalidatePath('/categories')
    return res.data.data as CategoryDto
  }
)

export const updateCategoryAction = createAction(
  { name: 'updateCategoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateCategoryInput) => {
    const res = await client.put(`/api/v1/categories/${id}`, data)
    revalidatePath('/categories')
    return res.data.data as CategoryDto
  }
)

export const deleteCategoryAction = createAction(
  { name: 'deleteCategoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/categories/${id}`)
    revalidatePath('/categories')
  }
)
```

---

## store/category.dialog-store.ts

```typescript
import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface CategoryDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDeleteOpen: boolean
  selectedCategoryId: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
}

export const useCategoryDialogStore = create<CategoryDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDeleteOpen: false,
      selectedCategoryId: null,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      openEdit: (id) => set({ isEditOpen: true, selectedCategoryId: id }),
      closeEdit: () => set({ isEditOpen: false, selectedCategoryId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedCategoryId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedCategoryId: null }),
    }),
    { name: 'CategoryDialogStore' }
  )
)
```

---

## app/(protected)/categories/page.tsx

```typescript
'use client'

import dynamic from 'next/dynamic'

const CategoryListPage = dynamic(
  () => import('@/features/category/components').then((m) => ({ default: m.CategoryListPage })),
  { ssr: false }
)

export default function CategoriesPage() {
  return <CategoryListPage />
}
```

> **Key:** Always `dynamic(..., { ssr: false })` — never a direct import — to prevent Radix UI hydration mismatches.
