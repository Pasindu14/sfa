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
