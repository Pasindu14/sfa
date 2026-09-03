'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type {
  CreateProductInput,
  UpdateProductInput,
  ProductDto,
} from '../schema/product.schema'

type ProductsListResponse = {
  products: ProductDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const getProductsAction = createAction(
  { name: 'getProductsAction', requireAuth: true, requiredRole: 'Admin' },
  async (page: number = 1, pageSize: number = 10, search?: string) => {
    const res = await client.get('/api/v1/products', {
      params: { page, pageSize, search: search || undefined },
    })
    return res.data.data as ProductsListResponse
  }
)

/**
 * Fetcher compatible with AsyncSelect — accepts an optional search string.
 *
 * `isActive: true` is sent to the API rather than filtered client-side, so a deactivated product
 * can never occupy a slot in the 50-row page and push an active one off the end. For products the
 * parameter is `isActive` (routes and distributors use `status=Active` instead).
 */
export const fetchActiveProductsForSelect = async (search?: string): Promise<ProductDto[]> => {
  const res = await getActiveProductsForSelectAction(search)
  if (!res.success) return []
  return res.data.products
}

export const getActiveProductsForSelectAction = createAction(
  { name: 'getActiveProductsForSelectAction', requireAuth: true, requiredRole: 'Admin' },
  async (search?: string) => {
    const res = await client.get('/api/v1/products', {
      params: { page: 1, pageSize: 50, isActive: true, search: search || undefined },
    })
    return res.data.data as ProductsListResponse
  }
)

export const getProductByIdAction = createAction(
  { name: 'getProductByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/products/${id}`)
    return res.data.data as ProductDto
  }
)

export const createProductAction = createAction(
  { name: 'createProductAction', requireAuth: true, requiredRole: 'Admin' },
  async (data: CreateProductInput) => {
    const res = await client.post('/api/v1/products', data)
    revalidatePath('/products')
    return res.data.data as ProductDto
  }
)

export const updateProductAction = createAction(
  { name: 'updateProductAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number, data: UpdateProductInput) => {
    const res = await client.put(`/api/v1/products/${id}`, data)
    revalidatePath('/products')
    return res.data.data as ProductDto
  }
)

export const deleteProductAction = createAction(
  { name: 'deleteProductAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.delete(`/api/v1/products/${id}`)
    revalidatePath('/products')
  }
)

export const deactivateProductAction = createAction(
  { name: 'deactivateProductAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/products/${id}/deactivate`)
    revalidatePath('/products')
  }
)

export const activateProductAction = createAction(
  { name: 'activateProductAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    await client.post(`/api/v1/products/${id}/activate`)
    revalidatePath('/products')
  }
)

export const getAllActiveProductsAction = createAction(
  { name: 'getAllActiveProductsAction', requireAuth: true, requiredRole: 'Admin' },
  async () => {
    const res = await client.get('/api/v1/products', {
      params: { page: 1, pageSize: 1000, isActive: true },
    })
    return (res.data.data as ProductsListResponse).products
  }
)
