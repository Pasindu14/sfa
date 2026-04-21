'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getProductCategoriesAction,
  getProductCategoryByIdAction,
  createProductCategoryAction,
  updateProductCategoryAction,
  activateProductCategoryAction,
  deactivateProductCategoryAction,
} from '../actions/product-category.actions'
import {
  useCreateDialog,
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateProductCategoryInput, UpdateProductCategoryInput } from '../schema/product-category.schema'

// --- Query key factory ---

export const productCategoryKeys = {
  all: ['product-categories'] as const,
  lists: () => [...productCategoryKeys.all, 'list'] as const,
  list: (filters: object) => [...productCategoryKeys.lists(), filters] as const,
  details: () => [...productCategoryKeys.all, 'detail'] as const,
  detail: (id: number) => [...productCategoryKeys.details(), id] as const,
}

// --- Query hooks ---

export function useProductCategory(id: number | null) {
  return useQuery({
    queryKey: productCategoryKeys.detail(id!),
    queryFn: async () => {
      const result = await getProductCategoryByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook ---

export function useProductCategoryDataTable(
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
    queryKey: productCategoryKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getProductCategoriesAction(page, pageSize, search || undefined)
      if (!result.success) throw new Error(result.error)
      const { productCategories, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: productCategories,
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

;(useProductCategoryDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateProductCategory() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateProductCategoryInput) => {
      const result = await createProductCategoryAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Product category created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'product category', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateProductCategory() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateProductCategoryInput }) => {
      const result = await updateProductCategoryAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryKeys.all })
      setFieldErrors(null)
      close()
      toast.success('Product category updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'product category', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useActivateProductCategory() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateProductCategoryAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryKeys.all })
      close()
      toast.success('Product category activated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'product category', 'activate')
    },
  })
}

export function useDeactivateProductCategory() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateProductCategoryAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryKeys.all })
      close()
      toast.success('Product category deactivated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'product category', 'deactivate')
    },
  })
}
