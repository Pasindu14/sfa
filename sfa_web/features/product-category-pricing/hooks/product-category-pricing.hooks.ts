'use client'

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getAllProductCategoryPricingsAction,
  bulkUpsertProductCategoryPricingsAction,
} from '../actions/product-category-pricing.actions'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'

export const productCategoryPricingKeys = {
  all: ['product-category-pricings'] as const,
}

export function useProductCategoryPricings() {
  return useQuery({
    queryKey: productCategoryPricingKeys.all,
    queryFn: async () => {
      const result = await getAllProductCategoryPricingsAction()
      if (!result.success) throw result
      return result.data ?? []
    },
  })
}

export function useBulkUpsertProductCategoryPricings() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      items: { productId: number; priceA: number; priceB: number; priceC: number; priceD: number }[]
    ) => {
      const result = await bulkUpsertProductCategoryPricingsAction(items)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productCategoryPricingKeys.all })
      toast.success('Pricing saved successfully.')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'pricing', 'save')
    },
  })
}
