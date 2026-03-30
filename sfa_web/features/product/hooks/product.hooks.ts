'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getProductsAction,
  getProductByIdAction,
  createProductAction,
  updateProductAction,
  deactivateProductAction,
  deleteProductAction,
  activateProductAction,
  getAllActiveProductsAction,
} from '../actions/product.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeactivateDialog,
  useDeleteDialog,
  useActivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateProductInput, UpdateProductInput } from '../schema/product.schema'

// --- Query key factory ---

export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (filters: object) => [...productKeys.lists(), filters] as const,
  details: () => [...productKeys.all, 'detail'] as const,
  detail: (id: number) => [...productKeys.details(), id] as const,
}

// --- Query options factory ---

export function productQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: productKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getProductsAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useProducts(page: number, pageSize: number) {
  return useQuery(productQueryOptions(page, pageSize))
}

export function useProduct(id: number | null) {
  return useQuery({
    queryKey: productKeys.detail(id!),
    queryFn: async () => {
      const result = await getProductByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook ---
// Uses server-side pagination + search — the API does all filtering.

export function useProductDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: Record<string, unknown>,
) {
  return useQuery({
    queryKey: productKeys.list({ page, pageSize, search }),
    queryFn: async () => {
      const result = await getProductsAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { products, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: products,
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

;(useProductDataTable as unknown as Record<string, unknown>).isQueryHook = true

export function useAllActiveProducts() {
  return useQuery({
    queryKey: [...productKeys.all, 'active-all'] as const,
    queryFn: async () => {
      const result = await getAllActiveProductsAction()
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    staleTime: 5 * 60 * 1000, // 5 min — product list changes infrequently
  })
}

// --- Mutation hooks ---

export function useCreateProduct() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateProductInput) => {
      const result = await createProductAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Product created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'product', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateProduct() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateProductInput }) => {
      const result = await updateProductAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Product updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'product', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeactivateProduct() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateProductAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      close()
      toast.success('Product deactivated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'product', 'deactivate')
    },
  })
}

export function useDeleteProduct() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteProductAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      close()
      toast.success('Product deleted successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'product', 'delete')
    },
  })
}

export function useActivateProduct() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateProductAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      close()
      toast.success('Product activated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'product', 'activate')
    },
  })
}
