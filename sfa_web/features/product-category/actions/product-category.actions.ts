'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateProductCategoryInput,
  UpdateProductCategoryInput,
  ProductCategoryDto,
} from '../schema/product-category.schema'

type ProductCategoriesListResponse = {
  productCategories: ProductCategoryDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getProductCategoriesAction = createAction(
  { name: 'getProductCategoriesAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/product-categories', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as ProductCategoriesListResponse
  }
)

export const getProductCategoryByIdAction = createAction(
  { name: 'getProductCategoryByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/product-categories/${id}`)
    return res.data.data as ProductCategoryDto
  }
)

export const createProductCategoryAction = createAction(
  { name: 'createProductCategoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateProductCategoryInput) => {
    const res = await client.post('/api/v1/product-categories', data)
    revalidatePath('/product-categories')
    return res.data.data as ProductCategoryDto
  }
)

export const updateProductCategoryAction = createAction(
  { name: 'updateProductCategoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateProductCategoryInput) => {
    const res = await client.put(`/api/v1/product-categories/${id}`, data)
    revalidatePath('/product-categories')
    return res.data.data as ProductCategoryDto
  }
)

export const activateProductCategoryAction = createAction(
  { name: 'activateProductCategoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/product-categories/${id}/activate`)
    revalidatePath('/product-categories')
  }
)

export const deactivateProductCategoryAction = createAction(
  { name: 'deactivateProductCategoryAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/product-categories/${id}/deactivate`)
    revalidatePath('/product-categories')
  }
)

// Lightweight fetcher for AsyncSelect dropdowns — calls the /all endpoint
export const fetchProductCategoriesForSelect = async (search?: string): Promise<ProductCategoryDto[]> => {
  const res = await client.get('/api/v1/product-categories/all')
  if (!res.data?.data) return []
  const categories = res.data.data as ProductCategoryDto[]
  if (!search) return categories
  const lower = search.toLowerCase()
  return categories.filter((c) => c.name.toLowerCase().includes(lower))
}
